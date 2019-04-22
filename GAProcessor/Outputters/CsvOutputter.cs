using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace GAProcessor.Outputters
{
	public class CsvOutputter : IOutputter
	{
		private string[] _header;
		// columns that contain timestamp values
		private int[] _timestampColumns;
		private Dictionary<int, string> _currentFilenames = new Dictionary<int, string>();
		private Dictionary<int, StreamWriter> _currentWriters = new Dictionary<int, StreamWriter>();
		private Config _config;

		public CsvOutputter(Config config)
		{
			_config = config;
		}

		/// <summary>
		/// Sets the header of this CSV file.
		/// </summary>
		public void SetHeader(string category, string[] header)
		{
			_header = header;

			var ts = new List<int>();
			for(var i = 0; i < header.Length; i++)
			{
				// i don't think there's a better way to do this than hardcoding
				if(header[i].EndsWith("_ts") || header[i].StartsWith("user_meta.cohort"))
				{
					ts.Add(i);
				}
			}

			_timestampColumns = ts.ToArray();
		}

		/// <summary>
		/// Adds an item to this CSV file.
		/// </summary>
		public void AddItem(int i, string[] row)
		{
			if(!_currentFilenames.ContainsKey(i))
			{
				lock(_currentFilenames)
				{
					_currentFilenames[i] = Path.Combine(Path.GetTempPath(), Path.GetTempFileName());
					_currentWriters[i] = new StreamWriter(File.Open(_currentFilenames[i], FileMode.Create));
				}
			}

			// convert timestamp columns
			if(_config.CsvConvertTimestamps)
			{
				foreach(var ts in _timestampColumns)
				{
					row[ts] = ConvertTimestamp(row[ts]);
				}
			}

			var rowText = string.Join(",", row.Select(c => EscapeCsv(c)));
			_currentWriters[i].WriteLine(rowText);
		}

		/// <summary>
		/// Merges all the CSV files together into an output.
		/// </summary>
		public void Finish(string outputFile)
		{
			// add extension
			outputFile += ".csv";

			// close current writers and streams
			foreach(var writer in _currentWriters.Values)
			{
				writer.Close();
			}

			using(var output = File.Open(outputFile, FileMode.Create))
			{
				// write header first
				var headerStr = string.Join(",", _header) + Environment.NewLine;
				var headerBytes = Encoding.UTF8.GetBytes(headerStr);
				output.Write(headerBytes);

				// read each file in order and write them to the output
				foreach(var k in _currentFilenames.Keys.OrderBy(k => k))
				{
					using(var input = File.Open(_currentFilenames[k], FileMode.Open))
					{
						input.CopyTo(output);
					}

					// cleanup temp file
					File.Delete(_currentFilenames[k]);
				}
			}
		}

		// convert utc timestamp to a human readable value in the local timezone
		private string ConvertTimestamp(string ts)
		{
			var dt = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
			return dt.AddSeconds(double.Parse(ts)).ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss");
		}

		// https://stackoverflow.com/a/6377656/3697679
		private string EscapeCsv(string str)
		{
			var mustQuote = (
				str.Contains(",") ||
				str.Contains("\"") ||
				str.Contains("\r") ||
				str.Contains("\n"));

			if(mustQuote)
			{
				var sb = new StringBuilder();
				sb.Append("\"");
				foreach(var nextChar in str)
				{
					sb.Append(nextChar);
					if(nextChar == '"')
					{
						sb.Append("\"");
					}
				}
				sb.Append("\"");
				return sb.ToString();
			}

			return str;
		}
	}
}
