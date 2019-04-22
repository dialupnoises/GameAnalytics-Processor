namespace GAProcessor.Outputters.Sql
{
	public interface ISqlGenerator
	{
		/// <summary>
		/// Generates a create statement in SQL.
		/// </summary>
		string GenerateCreateStatement(string tableName, SqlColumn[] columns);
		/// <summary>
		/// Generates an alter statement to update columns to new types.
		/// </summary>
		string GenerateAlterStatement(string tableName, SqlColumn[] columns);
		/// <summary>
		/// Generates an insert statement in SQL.
		/// </summary>
		string GenerateInsertStatement(string tableName, string[] rows, SqlColumn[] columns);
	}
}
