using System.IO;
using NDesk.Options;
using System;

namespace GAProcessor
{
	public class Program
	{
		public static void Main(string[] args)
		{
			// parse config from command line
			var help = false;
			var conf = new Config();
			var p = new OptionSet()
			{
				{
					"t|type=",
					"set output type (csv or sql supported, csv default)",
					v => conf.Output = Config.ParseEnumLower<Config.OutputType>(v) },
				{ "d|dir=", "set output directory (default \"output\")", v => conf.OutputDir = v },
				{
					"csv-no-conv-timestamp",
					"set the CSV outputter to not convert timestamps to a human readable format",
					v => conf.CsvConvertTimestamps = v != null
				},
				{
					"sql-text-as-text",
					"set the SQL outputter to output text columns as text instead of appropriately-sized varchars",
					v => conf.SqlTextAsVarchar = v == null
				},
				{
					"sql-generate-alter",
					"set the SQL outputter to output alter statements instead of create statements, " +
					"for inserting into an existing database",
					v => conf.SqlGenerateAlter = v != null
				},
				{
					"sql-database=",
					"set the DBMS to output SQL statements for (postgres, sqlserver, and mysql supported, default postgres)",
					v => conf.SqlDatabase = Config.ParseEnumLower<Config.DatabaseType>(v)
				},
				{ "h|help", "show this help", v => help = v != null }
			};

			var extra = p.Parse(args);

			if(help || extra.Count == 0)
			{
				if(extra.Count == 0)
				{
					Console.WriteLine("Error: no input file.");
				}

				Console.WriteLine($"Usage: {AppDomain.CurrentDomain.FriendlyName} [options] <input file>");
				Console.WriteLine("Available options:");
				p.WriteOptionDescriptions(Console.Out);

				return;
			}

			var inputFile = extra[0];

			if(!Directory.Exists(conf.OutputDir))
			{
				Directory.CreateDirectory(conf.OutputDir);
			}

			if(conf.Output == Config.OutputType.Sql)
			{
				Console.WriteLine("outputting using Sql outputter, using dbms " + conf.SqlDatabase);
			}
			else
			{
				Console.WriteLine($"outputting using {conf.Output} outputter");
			}

			var processor = new Processor(conf);
			processor.Process(inputFile);
			processor.Finish();
		}
	}
}
