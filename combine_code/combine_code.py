import os
import fnmatch

# Constants
OUTPUT_FILE = "code.copy"
IGNORE_FILE = ".copyignore"
CONFIG_FILE = "combine_code.conf"

# Function to load root directory from config file
def load_root_directory_from_config(config_file):
    if os.path.exists(config_file):
        with open(config_file, 'r') as f:
            line = f.readline().strip()
            if line.startswith("ROOT_DIRECTORY="):
                return line.split("=", 1)[1]
    return None

# Function to prompt the user for the root directory
def get_root_directory_from_user():
    root_dir = input("Enter the path of the root directory: ").strip()
    while not os.path.exists(root_dir):
        print("The specified path does not exist. Please try again.")
        root_dir = input("Enter the path of the root directory: ").strip()
    
    save_to_config = input(f"Would you like to save this path to {CONFIG_FILE} for future use? (y/n): ").strip().lower()
    if save_to_config == 'y':
        with open(CONFIG_FILE, 'w') as f:
            f.write(f"ROOT_DIRECTORY={root_dir}")
        print(f"Root directory saved to {CONFIG_FILE}.")
    
    return root_dir

# Read ignore patterns from .copyignore file
def load_ignore_patterns(ignore_file):
    if os.path.exists(ignore_file):
        with open(ignore_file, 'r') as f:
            return [line.strip() for line in f if line.strip() and not line.startswith('#')]
    return []



def should_ignore(path, ignore_patterns):
    normalized_path = os.path.normpath(path)
    relative_path = os.path.relpath(normalized_path)

    print(f"Checking path: {relative_path}")  # Debugging output

    for pattern in ignore_patterns:
        normalized_pattern = os.path.normpath(pattern)

        print(f"  Against pattern: {pattern} (normalized: {normalized_pattern})")  # Debugging output

        # Check if the pattern represents a directory and matches part of the path
        if normalized_pattern in relative_path:
            print(f"  Ignoring {relative_path} because it is inside directory {normalized_pattern}")
            return True
        
        # Check if the pattern matches the full path or the filename
        if fnmatch.fnmatch(relative_path, normalized_pattern) or fnmatch.fnmatch(os.path.basename(relative_path), normalized_pattern):
            print(f"  Ignoring {relative_path} because it matches pattern {normalized_pattern}")
            return True

    print(f"  Not ignoring {relative_path}")  # Debugging output for paths not ignored
    return False



# Generate the directory and file structure
def generate_structure(root_dir, ignore_patterns):
    structure = []
    for dirpath, dirnames, filenames in os.walk(root_dir):
        dir_structure = f"{dirpath}/"
        structure.append(dir_structure)
        
        for filename in filenames:
            if not should_ignore(os.path.join(dirpath, filename), ignore_patterns):
                structure.append(f"    {filename}")
    return structure

# Combine files into a single output file
def combine_files(root_dir, output_file, ignore_patterns):
    with open(output_file, 'w', encoding='utf-8') as out_f:
        for dirpath, dirnames, filenames in os.walk(root_dir):
            for filename in filenames:
                file_path = os.path.join(dirpath, filename)
                if not should_ignore(file_path, ignore_patterns):
                    print(f"Processing file: {file_path}")  # Debugging output
                    out_f.write(f"\n\n==== File: {file_path} ====\n\n")
                    try:
                        with open(file_path, 'r', encoding='utf-8') as in_f:
                            out_f.write(in_f.read())
                    except UnicodeDecodeError as e:
                        print(f"Error reading {file_path}: {e}")


# Main function
def main():
    # Attempt to load root directory from config file
    root_dir = load_root_directory_from_config(CONFIG_FILE)
    
    if root_dir:
        use_saved_dir = input(f"Would you like to use the saved root directory '{root_dir}'? (y/n): ").strip().lower()
        if use_saved_dir != 'y':
            # If user opts not to use the saved directory, prompt for a new one
            root_dir = get_root_directory_from_user()
    else:
        # If config file doesn't exist or root directory is not found, prompt the user
        root_dir = get_root_directory_from_user()

    # Ensure the root directory path is valid
    if not os.path.exists(root_dir):
        print(f"Error: The specified root directory '{root_dir}' does not exist.")
        return

    # Load ignore patterns
    ignore_patterns = load_ignore_patterns(IGNORE_FILE)

    # Generate directory structure
    structure = generate_structure(root_dir, ignore_patterns)
    
    # Combine files into the output file
    combine_files(root_dir, OUTPUT_FILE, ignore_patterns)

    # Append directory structure at the end of the output file
    with open(OUTPUT_FILE, 'a') as out_f:
        out_f.write("\n\n==== Directory Structure ====\n\n")
        for line in structure:
            out_f.write(line + "\n")

    print(f"Combined code and directory structure saved to {OUTPUT_FILE}")

if __name__ == "__main__":
    main()
