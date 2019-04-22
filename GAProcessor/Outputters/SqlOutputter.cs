using GAProcessor.Outputters.Sql;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace GAProcessor.Outputters
{
	public class SqlOutputter : IOutputter
	{
		private string[] _header;
		private SqlColumn[] _columns;
		private string _tableName;
		private Config _config;
		private Dictionary<int, string> _currentFilenames = new Dictionary<int, string>();
		private Dictionary<int, StreamWriter> _currentWriters = new Dictionary<int, StreamWriter>();
		private ISqlGenerator _generator;

		private readonly Regex _headerNameRegex = new Regex("_+");

		public SqlOutputter(Config config)
		{
			_config = config;
			switch(_config.SqlDatabase)
			{
				case Config.DatabaseType.Postgres:
					_generator = new PostgresSqlGenerator(config);
					break;
				case Config.DatabaseType.SqlServer:
					_generator = new MsSqlGenerator(config);
					break;
				case Config.DatabaseType.MySql:
					_generator = new MySqlGenerator(config);
					break;
				default:
					throw new NotImplementedException();
			}
		}

		/// <summary>
		/// Sets the header of this SQL output.
		/// </summary>
		public void SetHeader(string category, string[] header)
		{
			_tableName = _config.SqlTablePrefix + category;
			_header = header.Select(h => _headerNameRegex.Replace(h.Replace(".", "_").Replace("*", ""), "_")).ToArray();
			_columns = new SqlColumn[_header.Length];
		}

		/// <summary>
		/// Adds an item to this SQL file.
		/// </summary>
		public void AddItem(int index, string[] row)
		{
			if(!_currentFilenames.ContainsKey(index))
			{
				lock(_currentFilenames)
				{
					_currentFilenames[index] = Path.Combine(Path.GetTempPath(), Path.GetTempFileName());
					_currentWriters[index] = new StreamWriter(File.Open(_currentFilenames[index], FileMode.Create));
				}
			}

			// write rows as a json array to a temp file to avoid having to store them in memory
			_currentWriters[index].WriteLine(JsonConvert.SerializeObject(row));

			// store datatypes
			for(int i = 0; i < row.Length; i++)
			{
				lock(_columns)
				{
					// no column already, add one
					if(_columns[i] == null)
					{
						_columns[i] = new SqlColumn()
						{
							Name = _header[i],
							ColumnType = SqlColumn.DetermineType(_header[i], row[i]),
							MaxLength = row[i].Length
						};
					}
					else
					{
						// if type isn't already text, we should allow the type to be converted if necessary
						if(_columns[i].ColumnType != SqlColumn.Type.Text)
						{
							var newType = SqlColumn.DetermineType(_header[i], row[i]);

							var numberTypes = new SqlColumn.Type[] { SqlColumn.Type.Float, SqlColumn.Type.Integer };

							// allow integer columns to become floats
							if(
								newType != _columns[i].ColumnType &&
								numberTypes.Contains(_columns[i].ColumnType) &&
								numberTypes.Contains(newType))
							{
								_columns[i].ColumnType = SqlColumn.Type.Float;
							}
							// otherwise convert to text if types are different
							// unless they're different because this row is null
							else if(newType != _columns[i].ColumnType && row[i].Length > 0)
							{
								_columns[i].ColumnType = SqlColumn.Type.Text;
							}
						}

						// update max length
						_columns[i].MaxLength = Math.Max(_columns[i].MaxLength, row[i].Length);
					}
				}
			}
		}

		/// <summary>
		/// Read back cached rows and output SQL.
		/// </summary>
		public void Finish(string outputFile)
		{
			// add extension
			outputFile += ".sql";

			// close current writers
			foreach(var writer in _currentWriters)
			{
				writer.Value.Close();
			}

			using(var output = File.Open(outputFile, FileMode.Create))
			using(var writer = new StreamWriter(output))
			{
				// output create or alter statement
				var create = _config.SqlGenerateAlter ?
					_generator.GenerateAlterStatement(_tableName, _columns) :
					_generator.GenerateCreateStatement(_tableName, _columns);
				writer.WriteLine(create);

				// read back in every line and write the insert statements to the output
				var n = 0;
				foreach(var k in _currentFilenames.Keys.OrderBy(k => k))
				{
					using(var input = File.Open(_currentFilenames[k], FileMode.Open))
					using(var reader = new StreamReader(input))
					{
						string line;
						while((line = reader.ReadLine()) != null)
						{
							var row = JsonConvert.DeserializeObject<string[]>(line);
							n++;
							var insert = _generator.GenerateInsertStatement(_tableName, row, _columns);
							writer.WriteLine(insert);
						}
					}

					File.Delete(_currentFilenames[k]);
				}

				writer.Flush();
			}
		}
	}
}
