#!/bin/bash

# NOTE: Runs scripts from several projects in public-utils 
# without having to go into each sub-project's respective folder.

# Constants
EXPECTED_PATH="$HOME/code/public-utils"
CONFIG_FILE="$EXPECTED_PATH/.script_runner_recent"
MAX_RECENT_SCRIPTS=10

# Function to check if we're in the correct directory
check_directory() {
    if [[ "$PWD" != "$EXPECTED_PATH" ]]; then
        echo "Error: This script must be run from $EXPECTED_PATH"
        exit 1
    fi
}

# Function to initialize config file if it doesn't exist
init_config() {
    if [[ ! -f "$CONFIG_FILE" ]]; then
        touch "$CONFIG_FILE"
    fi
}

# Function to find executable scripts recursively
find_executables() {
    local search_dir="$1"
    find "$search_dir" -type f -executable \
        ! -path "*/venv/*" \
        ! -path "*/.git/*" \
        ! -path "*/node_modules/*" \
        ! -path "*/bin/*" \
        ! -path "*/obj/*" \
        ! -path "*/__pycache__/*" \
        -printf "%P\n" | sort
}

# Function to update recent scripts list
update_recent_scripts() {
    local script_path="$1"
    local temp_file="${CONFIG_FILE}.tmp"
    
    # Create new list with current script at top
    echo "$script_path" > "$temp_file"
    
    # Add previous entries, excluding the current script
    if [[ -f "$CONFIG_FILE" ]]; then
        grep -v "^$script_path\$" "$CONFIG_FILE" | head -n $((MAX_RECENT_SCRIPTS - 1)) >> "$temp_file"
    fi
    
    # Replace old file with new one
    mv "$temp_file" "$CONFIG_FILE"
}

# Function to execute a script
execute_script() {
    local script_path="$1"
    local script_dir="$(dirname "$EXPECTED_PATH/$script_path")"
    local script_name="$(basename "$script_path")"
    
    # Check if the script exists and is executable
    if [[ ! -x "$EXPECTED_PATH/$script_path" ]]; then
        echo "Error: Script is not executable or doesn't exist: $script_path"
        return 1
    fi
    
    echo -e "\nExecuting: $script_path\n"
    
    # Navigate to script directory
    cd "$script_dir"
    
    # Check if it's a Python script and has a venv directory
    if [[ "$script_name" == *.py ]] && [[ -d "$script_dir/venv" ]]; then
        echo "Python script detected with virtual environment. Activating venv..."
        source "$script_dir/venv/bin/activate"
        python "./$script_name"
        deactivate
    else
        # Execute the script directly
        "./$script_name"
    fi
    
    # Return to original directory
    cd "$EXPECTED_PATH"
    
    echo -e "\nScript execution completed.\n"
    read -p "Press Enter to continue..."
}

# Function to get available scripts
get_available_scripts() {
    local -n available_ref=$1  # Reference to the array where we'll store available scripts
    local all_scripts=("${@:2}")  # All remaining arguments are the scripts
    
    # Clear the array
    available_ref=()
    
    # Add recent scripts first
    if [[ -f "$CONFIG_FILE" ]]; then
        while IFS= read -r script; do
            if [[ -x "$EXPECTED_PATH/$script" ]]; then
                available_ref+=("$script")
            fi
        done < "$CONFIG_FILE"
    fi
    
    # Add remaining scripts
    for script in "${all_scripts[@]}"; do
        if ! grep -q "^$script\$" "$CONFIG_FILE" 2>/dev/null; then
            available_ref+=("$script")
        fi
    done
}

# Function to display menu
display_menu() {
    local recent_scripts=()
    local all_scripts=("$@")
    local script_count=1
    
    # Read recent scripts if they exist
    if [[ -f "$CONFIG_FILE" ]]; then
        mapfile -t recent_scripts < "$CONFIG_FILE"
    fi
    
    echo "=== Script Selection Menu ===\n"
    
    # Display recent scripts if there are any
    if [[ ${#recent_scripts[@]} -gt 0 && -n "${recent_scripts[0]}" ]]; then
        echo "Recent Scripts:"
        echo "---------------"
        for script in "${recent_scripts[@]}"; do
            if [[ -x "$EXPECTED_PATH/$script" ]]; then
                printf "%2d. %s\n" "$script_count" "$script"
                ((script_count++))
            fi
        done
        echo
    fi
    
    # Display all other executable scripts
    echo "Available Scripts:"
    echo "----------------"
    for script in "${all_scripts[@]}"; do
        # Only display if not in recent scripts
        if ! grep -q "^$script\$" "$CONFIG_FILE" 2>/dev/null; then
            printf "%2d. %s\n" "$script_count" "$script"
            ((script_count++))
        fi
    done
    
    echo
    echo " 0. Enter a custom script path"
    echo " q. Quit"
    echo

    # Return total count for validation
    local max_count=$((script_count - 1))
    # Print prompt without newline
    printf "Select an option (0-%d, or q to quit): " "$max_count"
    # Return the max count
    echo "$max_count" >&2
}

# Main script
main() {
    # Check if we're in the correct directory
    check_directory
    
    # Initialize config if needed
    init_config
    
    while true; do
        # Get list of all executable scripts
        mapfile -t all_scripts < <(find_executables "$EXPECTED_PATH")
        
        # Display menu and get max count (redirected to stderr)
        max_count=$(display_menu "${all_scripts[@]}" 2>&1 >/dev/tty)
        
        # Get user choice
        read choice
        
        # Check for quit condition
        if [[ "$choice" == "q" ]] || [[ "$choice" == "Q" ]]; then
            echo "Goodbye!"
            break
        fi
        
        # Process user choice
        if [[ "$choice" == "0" ]]; then
            read -p "Enter the path to the script (relative to $EXPECTED_PATH): " custom_path
            if [[ -x "$EXPECTED_PATH/$custom_path" ]]; then
                update_recent_scripts "$custom_path"
                execute_script "$custom_path"
            else
                echo "Error: Invalid script path or script is not executable"
                read -p "Press Enter to continue..."
            fi
        elif [[ "$choice" =~ ^[0-9]+$ ]] && (( choice > 0 )); then
            # Get list of all available scripts in display order
            declare -a available_scripts
            get_available_scripts available_scripts "${all_scripts[@]}"
            
            if (( choice <= ${#available_scripts[@]} )); then
                selected_script="${available_scripts[$((choice-1))]}"
                update_recent_scripts "$selected_script"
                execute_script "$selected_script"
            else
                echo "Error: Invalid selection"
                read -p "Press Enter to continue..."
            fi
        else
            echo "Error: Invalid input"
            read -p "Press Enter to continue..."
        fi
    done
}

# Run main function
main