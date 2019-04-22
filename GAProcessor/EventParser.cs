using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace GAProcessor
{
	public class EventParser
	{
		private List<string> _columns;
		private Regex[] _columnRegexes;
		private string[] _columnRegexesPrevious;
		private string _category;

		/// <summary>
		/// The header for this event parser.
		/// </summary>
		public string[] Header => _columns.ToArray();

		/// <summary>
		/// Creates a new event parser in the given category.
		/// </summary>
		public EventParser(string category)
		{
			_category = category;
			_columns = new List<string>();

			AddColumns(
				"data.session_id",
				"data.session_num",
				"data.user_id",
				"data.platform",
				"data.os_version",
				"data.sdk_version",
				"data.device",
				"data.manufacturer",
				"data.*_app_version",
				"data.connection_type",
				"country_code",
				"arrival_ts",
				"game_id",
				"user_meta.install_ts",
				"user_meta.cohort_week"
			);
		}

		/// <summary>
		/// Adds the given columns with the given paths to the event.
		/// </summary>
		/// <param name="path">The paths by keys, ex. data.device.</param>
		public void AddColumns(params string[] paths)
		{
			_columns.AddRange(paths);

			_columnRegexes = new Regex[_columns.Count];
			for(var i = 0; i < _columns.Count; i++)
			{
				// contains wildcard, make a regex for it
				if(_columns[i].Contains("*"))
				{
					_columnRegexes[i] = new Regex(Regex.Escape(_columns[i].Split('.').Last()).Replace(@"\*", ".*"));
				}
			}

			_columnRegexesPrevious = new string[_columns.Count];
		}

		/// <summary>
		/// Parses a JSON blob of an event into columns.
		/// </summary>
		public string[] ParseEvent(JObject obj)
		{
			var columns = new string[_columns.Count];
			for(int i = 0; i < _columns.Count; i++)
			{
				var col = _columns[i];
				var currObj = obj;
				var parts = col.Split('.');
				// travel down hierarchy except for last bit
				foreach(var part in parts.Take(parts.Length - 1))
				{
					currObj = (JObject)currObj[part];
				}

				// match the last bit
				var key = parts.Last();

				// wildcard, have to match keys
				if(key.Contains("*"))
				{
					// skip enumerating all keys if done before
					if(_columnRegexesPrevious[i] != null && currObj.ContainsKey(_columnRegexesPrevious[i]))
					{
						key = _columnRegexesPrevious[i];
					}
					else
					{
						// find first key that matches this column
						key = currObj.Properties().Where(p => _columnRegexes[i].IsMatch(p.Name)).First().Name;
						_columnRegexesPrevious[i] = key;
					}
				}

				if(currObj.ContainsKey(key))
				{
					columns[i] = currObj[key].Value<string>();
				}
				else
				{
					columns[i] = "";
				}
			}

			return columns;
		}

		/// <summary>
		/// Clears all columns.
		/// </summary>
		protected void ClearColumns()
		{
			_columns.Clear();
		}
	}

	#region Event Definitions
	public class DesignParser : EventParser
	{
		public DesignParser() : base("design")
		{
			AddColumns(
				"data.event_id",
				"data.value"
			);
		}
	}

	public class ErrorParser : EventParser
	{
		public ErrorParser() : base("error")
		{
			AddColumns(
				"data.severity",
				"data.message"
			);
		}
	}

	public class UserParser : EventParser
	{
		public UserParser() : base("user")
		{
			AddColumns("data.install");
		}
	}

	public class ProgressionParser : EventParser
	{
		public ProgressionParser() : base("progression")
		{
			AddColumns(
				"data.event_id"
			);
		}
	}

	public class SessionEndParser : EventParser
	{
		public SessionEndParser() : base("session_end")
		{
			AddColumns(
				"data.length"
			);
		}
	}

	public class SdkErrorParser : EventParser
	{
		public SdkErrorParser() : base("sdk_error")
		{
			ClearColumns();

			AddColumns(
				"data.os_version",
				"data.manufacturer",
				"data.device",
				"data.platform",
				"data.type",
				"country_code",
				"arrival_ts",
				"game_id"
			);
		}
	}
	#endregion
}
