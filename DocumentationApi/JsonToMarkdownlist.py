import json
import sys

input_file = sys.argv[1]
output_file = sys.argv[2]

with open(input_file, "r", encoding="utf-8") as f:
	data = json.load(f)

with open(output_file, "w", encoding="utf-8") as f:
	for item in data:
		f.write(f"* Name: \"{item['filterName']}\", filterId: {item['filterId']}\n")

print(f"Markdown file '{output_file}' has been generated.")
