#!/usr/bin/python
import os.path
import csv
import json
import sys
import gzip
import ntpath
from datetime import datetime
from dateutil import tz

def write_event(prefix, event_type, headers, event, first):
    data = prepare_data(headers, event)
    path = "%s_%s.csv" % (prefix, event_type)
    if first and os.path.exists(path):
        print("removing existing " + path + " file")
        os.remove(path)
    with open(path, 'a') as fp:
        cw = csv.writer(fp, lineterminator="\n", quoting=csv.QUOTE_NONNUMERIC)

        # write headers only to empty file
        if os.stat(path)[6] == 0:
            cw.writerow(headers)

        cw.writerow(data)

# convert the utc timestamps that GameAnalytics gives us to human-readable dates in the local user's timezone
def handle_timestamp(data):
    n = int(data)
    dt = datetime.utcfromtimestamp(n)
    from_zone = tz.tzutc()
    to_zone = tz.tzlocal()
    dt = dt.replace(tzinfo=from_zone)
    return dt.astimezone(to_zone).strftime("%Y-%m-%d %H:%M:%S")

def prepare_data(headers, event):
    result = []
    for header in headers:
        if "." in header:
            # handle 2nd level fields
            keys = header.split(".")
            if keys[0] in event and keys[1] == "revenue":
                revenue = event[keys[0]][keys[1]]
                if revenue == {}:
                    result.append("")
                else:
                    for revenue_key in revenue:
                        result.append("%s %d" % (revenue_key, revenue[revenue_key]))
            else:
                if keys[0] in event:
                    if keys[1] in event[keys[0]]:
                        if header == "user_meta.install_ts":
                            result.append(handle_timestamp(event["user_meta"]["install_ts"]))
                        else:
                            result.append(event[keys[0]][keys[1]])
                    else:
                        result.append("")
                else:
                    result.append("")
        else:
            # handle 1st level fields
            if header in event:
                if header == "arrival_ts":
                    result.append(handle_timestamp(event[header]))
                else:
                    result.append(event[header])
            else:
                result.append("")

    return tuple(result)

#TODO: check with collectors validation logic for missing fields
def get_csv_header(event_type):
    header = []
    common_data_fields = ["data.session_id", "data.user_id", "data.android_app_version"]
    common_fields = ["country_code","arrival_ts","game_id"]
    common_user_meta_fields = ["user_meta.install_ts","user_meta.revenue"]

    if event_type == "quality":
        header =  common_data_fields
        header += ["data.value","data.event_id","data.area"]
        header += common_fields
        header += ["data.platform","data.device",
                   "data.manufacturer", "data.os_version", 
                   "data.sdk_version"]
        header += common_user_meta_fields
    elif event_type == "design":
        header =  common_data_fields
        header += ["data.area","data.event_id","data.value"]
        header += common_fields
        header += ["data.platform","data.device",
                   "data.manufacturer", "data.os_version", 
                   "data.sdk_version"]
        header += common_user_meta_fields
    elif event_type == "error":
        header =  common_data_fields
        header += ["data.severity","data.x","data.y","data.z","data.area",
                   "data.message"]
        header += common_fields
        header += ["user_meta.gender"]
        header += common_user_meta_fields
    elif event_type == "user":
        header =  common_data_fields
        header += common_fields
        header += ["data.platform","data.device",
                   "data.manufacturer", "data.os_version", 
                   "data.sdk_version"]
        header += common_user_meta_fields
    elif event_type == "business":
        header =  common_data_fields
        header += ["data.event_id","data.area"]
        header += common_fields + ["currency", "amount"]
        header += common_user_meta_fields
    elif event_type == "session_end":
        header = common_data_fields
        header += ["data.length"]
        header += common_fields
        header += common_user_meta_fields

    return header

def main():
    if len(sys.argv) < 2:
        print("Usage: ./events2csv.py <source_file>")
    else:
        source_name = sys.argv[1]
        if os.path.isfile(source_name):
            with open(source_name) as f:
                file_prefix = os.path.splitext(
                    ntpath.basename(source_name))[0].replace(".json", "")

                print("Reading events from file...")
                i = 0
                already_written = {}
                for raw_event in f:
                    event = json.loads(raw_event.encode("ascii", "ignore"))
                    headers = get_csv_header(event['data']["category"])

                    # report progress
                    sys.stdout.write("\r%d rows written..." % i)
                    sys.stdout.flush()

                    first = not (event["data"]["category"] in already_written)
                    write_event(file_prefix, event['data']["category"], headers, event, first)
                    already_written[event["data"]["category"]] = True
                    i = i + 1
                print("\nDone")
        else:
            print("Supplied source file does not exists!")

if __name__ == '__main__':
    main()