# GameAnalytics Export Processor

GameAnalytics exports data as a long list of JSON objects. [They offer a Python script for processing these into CSVs](https://gameanalytics.com/docs/item/data-export), but it doesn't work with the latest version of GameAnalytics or Python. This repo contains an updated version of that script as well as a script for downloading the exported data in the first place.

To use these scripts, you'll need some version of Python 3 installed.

## Usage

1. Go to Settings -> Export data in your GameAnalytics dashboard, select the time frame you want to export, and copy the raw URLs (not the ones for the linux terminal).
2. Paste these URLs into a text file in an empty directory somewhere.
3. Open up a commandline window (Shift + Right Click in Windows Explorer -> Open PowerShell Window Here or right click on the start menu -> PowerShell or Command Prompt).
4. Run the download.py script on the text file containing the URLs (for example, if the file was named "urls.txt", you'd do `python download.py urls.txt`). Wait for the script to run. When it's done you should have a large file named "merged.json" in that directory.
5. Run the process.py script on the merged JSON file. For example, `python process.py merged.json`.
6. Once the script is finished, you should now have each type of event separated out into its own CSV file. You can open these in Excel, import them into a database, or whatever you want to do.

## Converting to SQL

I've also written a script to convert the generated CSV files into SQL for importing into a database. I've written this tool for use with PostgreSQL, but if you aren't using that, it should be easy to modify to work with MySQL or SQL Server or whatever you want.

To use the tool, simply run convert.py with the CSV file you want to convert. For example: `python convert.py merged_design.csv`.