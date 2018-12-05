from uuid import UUID
import sys
import csv
import time
import os.path
from dateutil import tz
import datetime

# check if a string contains an int
def check_int(s):
	if len(s) == 0:
		return False
	if s[0] in ('-', '+'):
		return s[1:].isdigit()
	return s.isdigit()

# check if a string contains a float
def check_float(s):
	if len(s) == 0:
		return False
	try:
		float(s)
		return True
	except ValueError:
		return False

# check if a string contains a valid uuid
def check_uuid(s):
	try:
		val = UUID(s, version=4)
	except ValueError:
		return False
	return True

# check if a string contains a timestamp
def check_timestamp(s):
	try:
		time.strptime(s, "%Y-%m-%d %H:%M:%S")
	except ValueError:
		return False
	return True

# gives the proper postgres datatype for the given string
def find_datatype(value, dbms):
	if check_int(value):
		return "INTEGER"
	if check_float(value):
		return "FLOAT"
	if check_uuid(value):
		if dbms == "postgres":
			return "UUID"
		elif dbms == "sqlserver":
			return "uniqueidentifier"
	if check_timestamp(value):
		if dbms == "postgres":
			return "TIMESTAMP WITH TIME ZONE"
		elif dbms == "sqlserver":
			return "DATETIMEOFFSET"
	return "TEXT"

# attaches the timezone of the timestamp to the end
def format_timestamps(ts, dbms):
	current_zone = tz.tzlocal()
	offset = datetime.datetime.now().astimezone(current_zone).strftime("%z")
	if dbms == "sqlserver":
		offset = offset[:3] + ":" + offset[3:]
	return ts + " " + offset

# generates a CREATE TABLE statement for the given csv file
def make_create_statement(table_name, f, dbms):
	with open(f) as csvfile:
		reader = csv.reader(csvfile)
		header = next(reader)
		lines = ["CREATE TABLE " + table_name + " ("]
		longest = {}
		types = {}
		# go through every row and make sure the datatypes agree
		for row in reader:
			for i in range(len(row)):
				col = row[i]
				# store the longest value so we can make the text fields varchars
				if i in longest:
					longest[i] = max(longest[i], len(col))
				else:
					longest[i] = len(col)
				# if the types don't agree, make them text
				coltype = find_datatype(col, dbms)
				if i in types:
					if coltype != types[i]:
						types[i] = "TEXT"
				else:
					types[i] = coltype
		# add each field to create statement
		for i in range(len(header)):
			name = header[i].replace(".", "_")
			coltype = types[i]
			# convert TEXTs to VARCHARs if we have a length for them
			if coltype == "TEXT" and i in longest:
				coltype = f"VARCHAR({max(longest[i], 1)})"
			lines.append("\t" + name + " " + coltype + ",")
		# strip comma from last line
		lines[-1] = lines[-1].strip(",")
		lines.append(");")
		return "\n".join(lines)

def write_insert_statements(table, f, out, dbms):
	with open(f) as csvfile:
		reader = csv.reader(csvfile)
		# skip header
		next(reader)
		for row in reader:
			fields = []
			for col in row:
				coltype = find_datatype(col, dbms)
				# if a number, we don't need quotes
				if coltype == "INTEGER" or coltype == "FLOAT":
					fields.append(col)
				# if a timestamp, we need to add a timezone to the end of it
				elif coltype == "TIMESTAMP WITH TIME ZONE" or coltype == "DATETIMEOFFSET":
					fields.append("'" + format_timestamps(col, dbms) + "'")
				# if a string but there's nothing in it, we output NULL
				elif coltype == "TEXT" and len(col) == 0:
					fields.append("NULL")
				# quote the string when outputting
				else:
					fields.append(f"'{col}'")
			line = "INSERT INTO " + table + " VALUES(" + ", ".join(fields) + ");\n"
			out.write(line)

def main():
	filename = sys.argv[1]
	dbms = "postgres"
	if len(sys.argv) > 2:
		dbms = sys.argv[2]
	if not dbms in ('postgres', 'sqlserver'):
		print("Unsupported DBMS!")
		return
	print("DBMS set to " + dbms)
	if os.path.isfile(filename):
		# table name is filename without extension
		table_name = os.path.splitext(os.path.basename(filename))[0]
		output_name = table_name + ".sql"
		with open(output_name, "w") as out:
			print("Finding types...")
			out.write(make_create_statement(table_name, filename, dbms) + "\n")
			print("Writing data...")
			write_insert_statements(table_name, filename, out, dbms)
	else:
		print("File not found!")

if __name__ == "__main__":
	main()