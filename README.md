# GameAnalytics Export Processor

This is a tool designed to process exported data from GameAnalytics. It was originally a python script created by GameAnalytics that I modified to work on Python 3, but is now a C# application that can handle the task much more quickly.

If you don't want to compile this yourself (you probably don't), you can get a build on the [releases page](https://github.com/cpancake/GameAnalytics-Processor/releases).

## Usage

1. Go to Settings -> Export data in your GameAnalytics dashboard, select the time frame you want to export, and copy the raw URLs (not the ones for the linux terminal).
2. Paste these URLs into a text file in an empty directory somewhere. I usually call this file `urls.txt`.
3. Open up a command line window (Shift + Right Click in Windows Explorer -> Open PowerShell Window Here or right click on the start menu -> PowerShell or Command Prompt).
4. Run the GADownloader program on the text file containing the URLs, with the second parameter being the name of the file you want it to output (for example, `GADownloader urls.txt merged.json.gz`). When the script is done, your output file should be in the directory you ran the program.
5. Run the GAProcessor program on the merged file. For example, `GAProcessor merged.json.gz`. This will separate the data into CSV files by default for each event, outputted in the `output` directory.

## Outputting SQL

If you want the program to output SQL instead of CSV files, you can specify this on the command line. For example, to output SQL intended for MySQL, you'd specify:
```
GAProcessor -t sql --sql-database=mysql merged.json.gz
```

The full list of options are:
```
Usage: GAProcessor [options] <input file>
Available options:
  -t, --type=VALUE           set output type (csv or sql supported, csv
                               default)
  -d, --dir=VALUE            set output directory (default "output")
      --csv-no-conv-timestamp
                             set the CSV outputter to not convert timestamps
                               to a human readable format
      --sql-text-as-text     set the SQL outputter to output text columns as
                               text instead of appropriately-sized varchars
      --sql-generate-alter   set the SQL outputter to output alter statements
                               instead of create statements, for inserting into
                               an existing database
      --sql-database=VALUE   set the DBMS to output SQL statements for
                               (postgres, sqlserver, and mysql supported,
                               default postgres)
      --sql-table-prefix=VALUE
                             set the prefix for table names. default is 'ga'
  -h, --help                 show this help
 ```

## License

This software is licensed under [the MIT license](https://opensource.org/licenses/MIT). 