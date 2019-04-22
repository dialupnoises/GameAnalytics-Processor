namespace GAProcessor.Outputters
{
	public interface IOutputter
	{
		/// <summary>
		/// Sets the header for this outputter.
		/// This will be called before any lines are added.
		/// </summary>
		void SetHeader(string category, string[] header);
		/// <summary>
		/// Adds a row to the outputter.
		/// </summary>
		/// <param name="i">The line this file started at - this is used to identify separate files.</param>
		void AddItem(int i, string[] row);
		/// <summary>
		/// Merge all files and cleanup.
		/// </summary>
		void Finish(string outputFile);
	}
}