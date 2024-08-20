#!/bin/bash

# Get the directory where this script is located
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

# Check if the current directory is the same as the script directory
if [[ "$PWD" != "$SCRIPT_DIR" ]]; then
    echo "Error: This script must be run from its own directory."
    exit 1
fi

# Check if combine_code.py exists in the script directory
if [[ ! -f "$SCRIPT_DIR/combine_code.py" ]]; then
    echo "Error: combine_code.py not found in the script directory."
    exit 1
fi

# Name of the virtual environment directory
VENV_DIR="venv"

# Check if the virtual environment directory exists
if [[ ! -d "$VENV_DIR" ]]; then
    echo "Creating virtual environment..."
    python -m venv "$VENV_DIR"
fi

# Activate the virtual environment (Windows and Unix-like systems)
if [[ -f "$VENV_DIR/Scripts/activate" ]]; then
    source "$VENV_DIR/Scripts/activate"  # Windows
elif [[ -f "$VENV_DIR/bin/activate" ]]; then
    source "$VENV_DIR/bin/activate"      # Unix-like systems
else
    echo "Error: Failed to activate the virtual environment."
    exit 1
fi

# Install dependencies if needed
REQUIRED_PACKAGES=("os" "pathlib")

for package in "${REQUIRED_PACKAGES[@]}"; do
    if ! python -c "import $package" &> /dev/null; then
        echo "Installing $package..."
        pip install $package
    else
        echo "$package is already installed."
    fi
done

# Run combine_code.py
echo "Running combine_code.py..."
python "$SCRIPT_DIR/combine_code.py"

# Deactivate the virtual environment after running the script
deactivate
echo "Virtual environment deactivated."
