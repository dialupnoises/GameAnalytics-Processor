using System;
using System.Linq;

namespace GAProcessor
{
	public class Config
	{
		/// <summary>
		/// The type of the output files.
		/// </summary>
		public OutputType Output = OutputType.Csv;

		/// <summary>
		/// The directory to write output files.
		/// </summary>
		public string OutputDir = "output";

		/// <summary>
		/// Should timestamps be converted in CSV files?
		/// </summary>
		public bool CsvConvertTimestamps = true;

		/// <summary>
		/// Prefix for SQL table names.
		/// </summary>
		public string SqlTablePrefix = "";

		/// <summary>
		/// Should we generate varchar columns or text columns?
		/// </summary>
		public bool SqlTextAsVarchar = true;

		/// <summary>
		/// Should we generate alter statements instead of create statements?
		/// </summary>
		public bool SqlGenerateAlter = false;

		/// <summary>
		/// The type of DBMS to output for when outputting SQL.
		/// </summary>
		public DatabaseType SqlDatabase = DatabaseType.Postgres;

		/// <summary>
		/// Parses enum from string, comparing lowercase.
		/// </summary>
		public static T ParseEnumLower<T>(string value) where T : struct, IConvertible
		{
			var names = Enum.GetNames(typeof(T)).Where(n => n.ToLower() == value.ToLower());
			if(names.Any())
			{
				return Enum.Parse<T>(names.First());
			}

			return default(T);
		}

		public enum OutputType
		{
			Csv,
			Sql
		}

		public enum DatabaseType
		{
			Postgres,
			SqlServer,
			MySql
		}
	}
}
