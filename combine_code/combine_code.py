import os
import fnmatch
import sys
import json

# Constants
OUTPUT_FILE = "code.copy"
IGNORE_FILE = ".copyignore"
INCLUDE_FILE = ".copyinclude"
CONFIG_FILE = "combine_code_config.json"  # Updated config file name to reflect JSON format
DEBUG_MODE = False  # Global variable to track debug mode
MODE = "blacklist"  # Default mode is blacklist
MAX_RECENT_PATHS = 10  # Maximum number of recent paths to store

# Function to load recent directories from config file
def load_recent_directories_from_config(config_file):
    if os.path.exists(config_file):
        with open(config_file, 'r') as f:
            try:
                data = json.load(f)
                return data.get("recent_paths", [])
            except json.JSONDecodeError:
                return []
    return []

# Function to save recent directories to config file
def save_recent_directories_to_config(config_file, recent_paths):
    data = {"recent_paths": recent_paths}
    with open(config_file, 'w') as f:
        json.dump(data, f, indent=4)  # Saving JSON in a prettified format with indentation

# Function to prompt the user for the root directory
def get_root_directory_from_user():
    recent_paths = load_recent_directories_from_config(CONFIG_FILE)

    if recent_paths:
        print("Recent paths:")
        for i, path in enumerate(recent_paths):
            print(f"{i + 1}. {path}")
        print("0. Enter a new path")

        choice = input("Choose a recent path or enter 0 to input a new path: ").strip()

        if choice.isdigit():
            choice = int(choice)
            if 0 < choice <= len(recent_paths):
                return recent_paths[choice - 1]

    root_dir = input("Enter the path of the root directory: ").strip()
    while not os.path.exists(root_dir):
        print("The specified path does not exist. Please try again.")
        root_dir = input("Enter the path of the root directory: ").strip()

    if root_dir not in recent_paths:
        recent_paths.insert(0, root_dir)
        if len(recent_paths) > MAX_RECENT_PATHS:
            recent_paths.pop()

    save_recent_directories_to_config(CONFIG_FILE, recent_paths)

    return root_dir

# Function to load patterns from a file
def load_patterns(pattern_file):
    patterns = []
    raw_content = ""
    if os.path.exists(pattern_file):
        with open(pattern_file, 'r') as f:
            raw_content = f.read()
            patterns = [line.strip() for line in raw_content.split('\n') if line.strip() and not line.startswith('#')]
    return patterns, raw_content

# Function to check if a path should be processed based on the selected mode (whitelist or blacklist)
def should_process(path, patterns):
    normalized_path = os.path.normpath(path)
    relative_path = os.path.relpath(normalized_path)

    if DEBUG_MODE:
        print(f"DEBUG: Checking path: {relative_path}")

    for pattern in patterns:
        normalized_pattern = os.path.normpath(pattern)

        if DEBUG_MODE:
            print(f"DEBUG: Against pattern: {pattern} (normalized: {normalized_pattern})")

        # Check if the pattern represents a directory
        if pattern.endswith('/') or pattern.endswith(os.path.sep):
            path_parts = relative_path.split(os.path.sep)
            pattern_parts = normalized_pattern.rstrip(os.path.sep).split(os.path.sep)
            
            if any(fnmatch.fnmatch(part, pattern_parts[-1]) for part in path_parts):
                if MODE == "blacklist":
                    if DEBUG_MODE:
                        print(f"DEBUG: Ignoring {relative_path} because it matches directory pattern {normalized_pattern}")
                    return False
                else:
                    if DEBUG_MODE:
                        print(f"DEBUG: Including {relative_path} because it matches directory pattern {normalized_pattern}")
                    return True
        
        # Check if the pattern matches the full path or the filename
        elif fnmatch.fnmatch(relative_path, pattern) or fnmatch.fnmatch(os.path.basename(relative_path), pattern):
            if MODE == "blacklist":
                if DEBUG_MODE:
                    print(f"DEBUG: Ignoring {relative_path} because it matches pattern {pattern}")
                return False
            else:
                if DEBUG_MODE:
                    print(f"DEBUG: Including {relative_path} because it matches pattern {pattern}")
                return True

    if DEBUG_MODE:
        print(f"DEBUG: {'Not ignoring' if MODE == 'blacklist' else 'Not including'} {relative_path}")
    return MODE == "blacklist"

# Generate the directory and file structure
def generate_structure(root_dir, patterns, apply_filter_to_structure):
    structure = []
    for dirpath, dirnames, filenames in os.walk(root_dir):
        if apply_filter_to_structure and not should_process(dirpath, patterns):
            continue  # Skip this directory if it should be ignored based on the filter

        dir_structure = f"{dirpath}/"
        structure.append(dir_structure)
        
        for filename in filenames:
            file_path = os.path.join(dirpath, filename)
            if not apply_filter_to_structure or should_process(file_path, patterns):
                structure.append(f"    {filename}")
    return structure

# Combine files into a single output file
def combine_files(root_dir, output_file, patterns, run_parameters, patterns_content):
    with open(output_file, 'w', encoding='utf-8') as out_f:
        # Write the run parameters at the beginning of the output file
        out_f.write("==== Run Parameters ====\n")
        out_f.write("This file was generated by combining the contents of multiple files. Below are the settings used during this process:\n")
        for param, value in run_parameters.items():
            out_f.write(f"{param}: {value}\n")
        
        # Include the patterns content
        out_f.write(f"\n==== {IGNORE_FILE if MODE == 'blacklist' else INCLUDE_FILE} Contents ====\n")
        out_f.write(patterns_content)
        out_f.write("\n\n")

        for dirpath, dirnames, filenames in os.walk(root_dir):
            if DEBUG_MODE:
                print(f"DEBUG: Scanning directory: {dirpath}")
                print(f"DEBUG: Files found: {filenames}")
            for filename in filenames:
                file_path = os.path.join(dirpath, filename)
                if should_process(file_path, patterns):
                    if DEBUG_MODE:
                        print(f"DEBUG: Processing file: {file_path}")  # Debugging output
                    out_f.write(f"\n\n==== File: {file_path} ====\n\n")
                    try:
                        with open(file_path, 'r', encoding='utf-8') as in_f:
                            out_f.write(in_f.read())
                    except UnicodeDecodeError as e:
                        if DEBUG_MODE:
                            print(f"DEBUG: Error reading {file_path}: {e}")
                elif DEBUG_MODE:
                    print(f"DEBUG: Skipping file: {file_path}")

# Main function
def main():
    global DEBUG_MODE, MODE

    # Prompt user for debug mode
    if "--debug" in sys.argv:
        DEBUG_MODE = True

    # Ask user to choose mode
    while True:
        mode_choice = input("Choose mode: (1) Blacklist (default) or (2) Whitelist: ").strip()
        if mode_choice in ["1", "2"]:
            MODE = "whitelist" if mode_choice == "2" else "blacklist"
            break
        else:
            print("Invalid choice. Please enter 1 for Blacklist or 2 for Whitelist.")

    # Ask user if they want to apply the filter to the directory structure
    apply_filter_to_structure = input("Apply the filter to the directory structure? (y/n): ").strip().lower() == 'y'

    # Prompt user for root directory
    root_dir = get_root_directory_from_user()

    # Ensure the root directory path is valid
    if not os.path.exists(root_dir):
        print(f"Error: The specified root directory '{root_dir}' does not exist.")
        return

    # Load patterns based on mode
    patterns, patterns_content = load_patterns(INCLUDE_FILE if MODE == "whitelist" else IGNORE_FILE)

    # Prepare the run parameters to be recorded in the output file
    run_parameters = {
        "Root Directory": root_dir,
        "Mode": MODE,
        "Apply Filter to Directory Structure": apply_filter_to_structure,
        "Debug Mode": DEBUG_MODE,
    }

    # Generate directory structure
    structure = generate_structure(root_dir, patterns, apply_filter_to_structure)
    
    # Combine files into the output file, including the run parameters at the beginning
    combine_files(root_dir, OUTPUT_FILE, patterns, run_parameters, patterns_content)

    # Append directory structure at the end of the output file
    with open(OUTPUT_FILE, 'a', encoding='utf-8') as out_f:
        out_f.write("\n\n==== Directory Structure ====\n\n")
        for line in structure:
            out_f.write(line + "\n")

    print(f"Combined code and directory structure saved to {OUTPUT_FILE}")

if __name__ == "__main__":
    main()