using System;
using System.Collections.Generic;

namespace GAProcessor.Outputters.Sql
{
	public class MsSqlGenerator : ISqlGenerator
	{
		private Config _config;

		public MsSqlGenerator(Config config)
		{
			_config = config;
		}

		/// <summary>
		/// Generates a create statement in PG SQL.
		/// </summary>
		public string GenerateCreateStatement(string tableName, SqlColumn[] columns)
		{
			var fields = new List<string>();
			foreach(var col in columns)
			{
				fields.Add($"{col.Name} {GetTypeName(col)}");
			}

			return $"CREATE TABLE {tableName} ({string.Join(", ", fields)});";
		}

		/// <summary>
		/// Generates an alter statement in PG SQL.
		/// </summary>
		public string GenerateAlterStatement(string tableName, SqlColumn[] columns)
		{
			var lines = new List<string>();
			foreach(var col in columns)
			{
				lines.Add($"ALTER TABLE {tableName} ALTER COLUMN {col.Name} {GetTypeName(col)};");
			}

			return string.Join("\n", lines);
		}

		/// <summary>
		/// Generates an insert statement in PG SQL.
		/// </summary>
		public string GenerateInsertStatement(string tableName, string[] rows, SqlColumn[] columns)
		{
			var fields = new List<string>();
			for(var i = 0; i < columns.Length; i++)
			{
				fields.Add(GetTypeValue(columns[i], rows[i]));
			}

			return $"INSERT INTO {tableName} VALUES({string.Join(", ", fields)});";
		}

		private string GetTypeValue(SqlColumn col, string value)
		{
			switch(col.ColumnType)
			{
				case SqlColumn.Type.UniqueIdentifier:
				case SqlColumn.Type.Text:
					return $"'{value.Replace("'", "''")}'";
				case SqlColumn.Type.Float:
				case SqlColumn.Type.Integer:
					return value.Length > 0 ? value : "0";
				case SqlColumn.Type.Timestamp:
					var dt = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
					return "'" + dt.AddSeconds(double.Parse(value)).ToString("yyyy-MM-dd HH:mm:ss") + "+00:00'";
				default:
					throw new NotImplementedException();
			}
		}

		private string GetTypeName(SqlColumn col)
		{
			switch(col.ColumnType)
			{
				case SqlColumn.Type.Float:
					return "FLOAT";
				case SqlColumn.Type.Integer:
					return "INTEGER";
				case SqlColumn.Type.Text:
					return _config.SqlTextAsVarchar ? $"VARCHAR({col.MaxLength})" : "TEXT";
				case SqlColumn.Type.Timestamp:
					return "DATETIMEOFFSET";
				case SqlColumn.Type.UniqueIdentifier:
					return "uniqueidentifier";
				default:
					throw new NotImplementedException();
			}
		}
	}
}
