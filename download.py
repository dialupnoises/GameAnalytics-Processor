import urllib.request
import sys
import os
import shutil
import gzip

def main():
	if len(sys.argv) < 2:
		print("Usage: download.py <file containing list of urls>")
	else:
		source_file = sys.argv[1]
		n = 0
		files = []
		if os.path.isfile(source_file):
			# download every URL listed in the source file
			with open(source_file) as f:
				for line in f:
					print("Downloading " + line)
					filename = "data" + str(n) + ".json.gz"
					urllib.request.urlretrieve(line, filename)
					files.append(filename)
					n += 1
			# un-gzip each source file as we go and write them to the output
			with open("merged.json", "wb") as outfile:
				for f in files:
					with gzip.open(f, "rb") as fd:
						shutil.copyfileobj(fd, outfile, 1024)
		else:
			print("List of URLs does not exist!")

if __name__ == "__main__":
	main()