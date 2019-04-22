using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace GADownloader
{
	public class Downloader
	{
		// number of concurrent downloads
		private const int CONCURRENT_DLS = 5;
		private List<Task> _tasks;
		private List<string> _filenames;

		public Downloader(string[] urls)
		{
			_tasks = new List<Task>();
			_filenames = new List<string>();

			foreach(var u in urls)
			{
				var f = Path.GetFileName(new Uri(u).LocalPath);
				_tasks.Add(DownloadTask(u, f));
				_filenames.Add(f);
			}
		}

		/// <summary>
		/// Download all files asynchronously.
		/// </summary>
		public async Task DownloadAll()
		{
			// run CONCURRENT_DLS tasks at once
			for(var i = 0; i < _tasks.Count / CONCURRENT_DLS; i++)
			{
				await Task.WhenAll(_tasks.Skip(i * CONCURRENT_DLS).Take(CONCURRENT_DLS));
			}
		}

		/// <summary>
		/// Merges all downloaded files into the output file.
		/// </summary>
		public void MergeAll(string output)
		{
			// output gzip compressed, because we have to de-gzip to merge together
			using(var outputFile = File.Open(output, FileMode.OpenOrCreate))
			using(var outputGzip = new GZipStream(outputFile, CompressionLevel.Optimal))
			{
				foreach(var file in _filenames)
				{
					using(var input = File.Open(file, FileMode.Open))
					using(var inputGzip = new GZipStream(input, CompressionMode.Decompress))
					{
						inputGzip.CopyTo(outputGzip);
					}
				}
			}
		}

		/// <summary>
		/// Cleans up the intermediate files.
		/// </summary>
		public void Cleanup()
		{
			foreach(var file in _filenames)
			{
				File.Delete(file);
			}
		}

		private async Task DownloadTask(string url, string filename)
		{
			if(File.Exists(filename))
			{
				Console.WriteLine($"File already exists: {filename}, skipping.");
				return;
			}

			using(var client = new HttpClient())
			{
				var bytes = await client.GetByteArrayAsync(url);
				await File.WriteAllBytesAsync(filename, bytes);
				Console.WriteLine($"wrote {filename}");
			}
		}
	}
}
