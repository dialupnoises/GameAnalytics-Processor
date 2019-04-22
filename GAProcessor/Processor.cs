using GAProcessor.Outputters;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace GAProcessor
{
	public class Processor
	{
		private const int NUM_CONCURRENT_TASKS = 4;

		// columns for each type of event
		private Dictionary<string, EventParser> _parsers = new Dictionary<string, EventParser>()
		{
			{ "design", new DesignParser() },
			{ "error", new ErrorParser() },
			{ "progression", new ProgressionParser() },
			{ "user", new UserParser() },
			{ "session_end", new SessionEndParser() },
			{ "sdk_error", new SdkErrorParser() }
		};

		private Dictionary<string, IOutputter> _outputters = new Dictionary<string, IOutputter>();
		private int _counter;

		private Config _config;

		public Processor(Config config)
		{
			_config = config;
		}

		/// <summary>
		/// Processes the given file.
		/// </summary>
		public void Process(string inputFile)
		{
			var lines = ReadLines(inputFile).ToArray();
			var linesPerTask = lines.Length / NUM_CONCURRENT_TASKS;
			var tasks = new Task[NUM_CONCURRENT_TASKS];
			_counter = 0;

			// create each task
			for(var i = 0; i < NUM_CONCURRENT_TASKS; i++)
			{
				var start = i * linesPerTask;
				// if this is the last task, we need to process remaining lines, to account for rounding errors
				var count = i == NUM_CONCURRENT_TASKS - 1 ? lines.Length - i * linesPerTask : linesPerTask;
				var n = i;
				tasks[i] = Task.Run(() =>
				{
					ProcessLines(lines, start, count);
				});
			}

			var task = Task.WhenAll(tasks);

			// update counter while waiting for processing
			Console.Write("0 lines processed");
			while(!task.IsCompleted)
			{
				Console.Write($"\r{_counter} lines processed");
				Thread.Sleep(100);
			}

			Console.WriteLine($"\r{_counter} lines processed");
		}

		private void ProcessLines(string[] lines, int start, int count)
		{
			// personal cache to avoid locking dictionary for every line
			var outputterCache = new Dictionary<string, IOutputter>();

			// for every line we're supposed to touch, read the object and parse it if possible
			for(var i = start; i < start + count; i++)
			{
				var obj = JObject.Parse(lines[i]);
				var type = obj["data"]["category"].Value<string>();
				if(_parsers.ContainsKey(type))
				{
					if(!outputterCache.ContainsKey(type))
					{
						outputterCache[type] = GetOutputter(type);
					}

					outputterCache[type].AddItem(start, _parsers[type].ParseEvent(obj));
					Interlocked.Increment(ref _counter);
				}
				else
				{
					Console.WriteLine($"Unknown event type '{type}', skipping.");
				}
			}
		}

		/// <summary>
		/// Writes the output file.
		/// </summary>
		public void Finish()
		{
			Console.WriteLine($"outputting to {_config.OutputDir}/");
			foreach(var kv in _outputters)
			{
				kv.Value.Finish(Path.Combine(_config.OutputDir, kv.Key));
			}
			Console.WriteLine("output finished");
		}

		private IOutputter GetOutputter(string category)
		{
			lock(_outputters)
			{
				if(_outputters.ContainsKey(category))
				{
					return _outputters[category];
				}

				var outputter = CreateOutputter();
				_outputters[category] = outputter;
				outputter.SetHeader(category, _parsers[category].Header);
				return outputter;
			}
		}

		// creates an outputter for the given category
		private IOutputter CreateOutputter()
		{
			switch(_config.Output)
			{
				case Config.OutputType.Csv:
					return new CsvOutputter(_config);
				case Config.OutputType.Sql:
					return new SqlOutputter(_config);
				default:
					throw new NotImplementedException();
			}
		}

		// reads lines from gzipped input
		private IEnumerable<string> ReadLines(string inputFile)
		{
			using(var file = File.Open(inputFile, FileMode.Open))
			using(var gzip = new GZipStream(file, CompressionMode.Decompress))
			using(var reader = new StreamReader(gzip))
			{
				string line;
				while((line = reader.ReadLine()) != null)
				{
					yield return line;
				}
			}
		}
	}
}
