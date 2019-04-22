using System;

namespace GAProcessor.Outputters.Sql
{
	public class SqlColumn
	{
		/// <summary>
		/// The name of this column.
		/// </summary>
		public string Name;
		/// <summary>
		/// The type of this column.
		/// </summary>
		public Type ColumnType;
		/// <summary>
		/// The maximum length of this column.
		/// </summary>
		public int MaxLength = 0;

		/// <summary>
		/// Determines the type of the column.
		/// </summary>
		public static Type DetermineType(string name, string val)
		{
			if(name.EndsWith("_ts") || name.StartsWith("user_meta_cohort"))
			{
				return Type.Timestamp;
			}
			else if(Guid.TryParse(val, out var _))
			{
				return Type.UniqueIdentifier;
			}
			else if(int.TryParse(val, out var _))
			{
				return Type.Integer;
			}
			else if(float.TryParse(val, out var _))
			{
				return Type.Float;
			}

			return Type.Text;
		}

		/// <summary>
		/// Possible column types.
		/// </summary>
		public enum Type
		{
			Unknown,
			Integer,
			Float,
			Timestamp,
			Text,
			UniqueIdentifier
		}
	}
}
