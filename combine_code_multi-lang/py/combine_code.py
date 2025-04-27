import os
import fnmatch
import sys
import json
import argparse # Import argparse for command-line argument parsing

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
def load_patterns(pattern_file_path): # Modified to accept full path
    patterns = []
    raw_content = ""
    if os.path.exists(pattern_file_path):
        with open(pattern_file_path, 'r') as f:
            raw_content = f.read()
            patterns = [line.strip() for line in raw_content.split('\n') if line.strip() and not line.startswith('#')]
    return patterns, raw_content

# Function to check if a path should be processed based on the selected mode (whitelist or blacklist)
def should_process(path, patterns, root_dir): # Add root_dir parameter
    """
    Checks if a path should be processed based on the selected mode and patterns.
    Args:
        path (str): The path to check.
        patterns (list): List of patterns to match against.
        root_dir (str): The root directory being processed.
    Returns:
        bool: True if the path should be processed, False otherwise.
    """
    normalized_path = os.path.normpath(path)
    # Calculate relative path from the root_dir
    relative_path = os.path.relpath(normalized_path, start=root_dir)

    if DEBUG_MODE:
        print(f"DEBUG: Checking path: {relative_path} (relative to {root_dir})")

    matches_pattern = False
    for pattern in patterns:
        normalized_pattern = os.path.normpath(pattern)
        if DEBUG_MODE:
            print(f"DEBUG: Against pattern: {pattern} (normalized: {normalized_pattern})")

        # Handle directory patterns (ending with /)
        if normalized_pattern.endswith(os.path.sep):
            dir_pattern = normalized_pattern.rstrip(os.path.sep)
            # Check if the path is the directory itself or is inside the directory
            if relative_path == dir_pattern or relative_path.startswith(dir_pattern + os.path.sep):
                 matches_pattern = True
                 break
        # Handle file patterns or exact path patterns
        elif fnmatch.fnmatch(relative_path, normalized_pattern):
             matches_pattern = True
             break # Found a match, no need to check further

    if MODE == "blacklist":
        # In blacklist mode, process if NO pattern matches
        result = not matches_pattern
        if DEBUG_MODE:
            print(f"DEBUG: {'Processing' if result else 'Ignoring'} {relative_path} (Blacklist Mode)")
        return result
    else: # MODE == "whitelist"
        # In whitelist mode, process if ANY pattern matches
        result = matches_pattern
        if DEBUG_MODE:
            print(f"DEBUG: {'Processing' if result else 'Ignoring'} {relative_path} (Whitelist Mode)")
        return result

# Generate the directory and file structure
def generate_structure(root_dir, patterns, apply_filter_to_structure):
    """
    Generates a list representing the directory and file structure.
    Args:
        root_dir (str): The root directory to walk.
        patterns (list): List of patterns to apply.
        apply_filter_to_structure (bool): Whether to apply the filter to the structure output.
    Returns:
        list: A list of strings representing the structure.
    """
    structure = []
    for dirpath, dirnames, filenames in os.walk(root_dir):
        # Decide whether to prune directories from the walk (only in blacklist mode)
        if apply_filter_to_structure and MODE == "blacklist":
            dirs_to_prune_indices = []
            for i in range(len(dirnames)):
                dirname = dirnames[i]
                dir_full_path = os.path.join(dirpath, dirname)
                # In blacklist mode, prune if the directory path itself is ignored
                if not should_process(dir_full_path, patterns, root_dir): # Pass root_dir
                    if DEBUG_MODE:
                        print(f"DEBUG: Pruning directory from walk (blacklist): {dir_full_path}")
                    dirs_to_prune_indices.append(i)

            for i in sorted(dirs_to_prune_indices, reverse=True):
                del dirnames[i]

        # Now, decide which directories and files to list in the structure output
        list_dir_in_structure = False
        if not apply_filter_to_structure:
            list_dir_in_structure = True
        elif MODE == "blacklist":
            # In blacklist mode, list directory if it's not ignored
            list_dir_in_structure = should_process(dirpath, patterns, root_dir)
        else: # MODE == "whitelist" and apply_filter_to_structure is True
            # In whitelist mode, list directory if the directory itself matches a pattern
            # OR if any file within the directory matches a pattern.
            if should_process(dirpath, patterns, root_dir):
                list_dir_in_structure = True
            else:
                # Check if any file in this directory should be processed
                for filename in filenames:
                    file_path = os.path.join(dirpath, filename)
                    if should_process(file_path, patterns, root_dir):
                        list_dir_in_structure = True
                        break # Found a file that should be included, list the directory

        if list_dir_in_structure:
             structure.append(f"{dirpath}/")

             for filename in filenames:
                 file_path = os.path.join(dirpath, filename)
                 # A file is listed if apply_filter_to_structure is False,
                 # OR if apply_filter_to_structure is True AND should_process(file_path, patterns, root_dir) is True.
                 list_file_in_structure = not apply_filter_to_structure or should_process(file_path, patterns, root_dir)
                 if list_file_in_structure:
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
                    if should_process(file_path, patterns, root_dir): # Pass root_dir
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

    parser = argparse.ArgumentParser(description="Combine code files from a directory.")
    parser.add_argument("root_dir", nargs='?', help="The root directory to process.")
    parser.add_argument("--mode", choices=["blacklist", "whitelist"], default="blacklist", help="Filtering mode (blacklist or whitelist). Defaults to blacklist.")
    parser.add_argument("--apply-filter-to-structure", action="store_true", help="Apply the filter to the directory structure output.")
    parser.add_argument("--debug", action="store_true", help="Enable debug mode.")

    args = parser.parse_args()

    DEBUG_MODE = args.debug
    MODE = args.mode

    if args.root_dir:
        # Non-interactive mode
        root_dir = args.root_dir
        apply_filter_to_structure = args.apply_filter_to_structure

        if not os.path.exists(root_dir):
            print(f"Error: The specified root directory '{root_dir}' does not exist.")
            sys.exit(1) # Exit with an error code

        # Load patterns based on mode, relative to the root directory
        pattern_file_name = INCLUDE_FILE if MODE == "whitelist" else IGNORE_FILE
        pattern_file_path = os.path.join(root_dir, pattern_file_name)
        patterns, patterns_content = load_patterns(pattern_file_path)

    else:
        # Interactive mode (existing logic)
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
    
    # Determine the output file path
    output_file_path = os.path.join(root_dir, OUTPUT_FILE)

    # Combine files into the output file, including the run parameters at the beginning
    combine_files(root_dir, output_file_path, patterns, run_parameters, patterns_content)

    # Append directory structure at the end of the output file
    with open(output_file_path, 'a', encoding='utf-8') as out_f:
        out_f.write("\n\n==== Directory Structure ====\n\n")
        for line in structure:
            out_f.write(line + "\n")

    print(f"Combined code and directory structure saved to {output_file_path}")

if __name__ == "__main__":
    main()
