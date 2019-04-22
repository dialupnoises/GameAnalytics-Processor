using System;
using System.IO;

namespace GADownloader
{
	public class Program
	{
		static void Main(string[] args)
		{
			if(args.Length < 2)
			{
				Console.WriteLine($"Usage: {AppDomain.CurrentDomain.FriendlyName} <path to text file containing urls> <path to output file>");
				return;
			}

			var input = args[0];
			var output = args[1];

			if(!File.Exists(input))
			{
				Console.WriteLine("Error: input file doesn't exist");
				return;
			}

			var urls = File.ReadAllLines(input);
			var downloader = new Downloader(urls);
			downloader.DownloadAll().Wait();
			downloader.MergeAll(output);
			downloader.Cleanup();

			Console.WriteLine("merged downloaded files to " + output);
		}
	}
}
