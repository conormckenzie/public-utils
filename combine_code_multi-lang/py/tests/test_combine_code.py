import os
import sys
import pytest
import tempfile
import shutil
from unittest.mock import patch

# Add the parent directory to the sys.path to import combine_code
sys.path.insert(0, os.path.abspath(os.path.join(os.path.dirname(__file__), '..')))

# Import the main function from the combine_code script
from combine_code import main

# Define a fixture to create a temporary directory structure for testing (blacklist)
@pytest.fixture
def temp_project_blacklist(request):
    temp_dir = tempfile.mkdtemp()

    # Create a simple project structure
    os.makedirs(os.path.join(temp_dir, "src"))
    os.makedirs(os.path.join(temp_dir, "docs"))
    os.makedirs(os.path.join(temp_dir, "ignore_me"))

    # Create some files
    with open(os.path.join(temp_dir, "src", "file1.py"), "w") as f:
        f.write("print('Hello from file1')\n")
    with open(os.path.join(temp_dir, "src", "file2.txt"), "w") as f:
        f.write("This is file2.\n")
    with open(os.path.join(temp_dir, "docs", "doc1.md"), "w") as f:
        f.write("# Documentation\n")
    with open(os.path.join(temp_dir, "ignore_me", "secret.txt"), "w") as f:
        f.write("This should be ignored.\n")
    with open(os.path.join(temp_dir, ".copyignore"), "w") as f:
        f.write("ignore_me/\n")
        f.write("*.txt\n") # Ignore all .txt files

    yield temp_dir

    # Clean up the temporary directory
    shutil.rmtree(temp_dir)

# Define a fixture to create a temporary directory structure for testing (whitelist)
@pytest.fixture
def temp_project_whitelist(request):
    temp_dir = tempfile.mkdtemp()

    # Create a simple project structure
    os.makedirs(os.path.join(temp_dir, "include_me"))
    os.makedirs(os.path.join(temp_dir, "other_dir"))
    os.makedirs(os.path.join(temp_dir, "another_dir"))

    # Create some files
    with open(os.path.join(temp_dir, "include_me", "file_a.py"), "w") as f:
        f.write("print('Hello from file_a')\n")
    with open(os.path.join(temp_dir, "include_me", "file_b.txt"), "w") as f:
        f.write("This is file_b.\n")
    with open(os.path.join(temp_dir, "other_dir", "file_c.md"), "w") as f:
        f.write("## Another file\n")
    with open(os.path.join(temp_dir, "another_dir", "file_d.py"), "w") as f:
        f.write("print('Hello from file_d')\n")
    with open(os.path.join(temp_dir, ".copyinclude"), "w") as f:
        f.write("include_me/\n")
        f.write("*.py\n") # Include all .py files

    yield temp_dir

    # Clean up the temporary directory
    shutil.rmtree(temp_dir)


# Test case for non-interactive mode (blacklist)
def test_non_interactive_blacklist(temp_project_blacklist): # Use the blacklist fixture
    output_file = os.path.join(temp_project_blacklist, "code.copy")

    # Simulate command-line arguments for non-interactive mode
    test_args = [
        'combine_code.py', # Script name (sys.argv[0])
        temp_project_blacklist,      # root_dir
        '--mode', 'blacklist',
        '--apply-filter-to-structure',
        '--debug' # Add debug flag
    ]

    with patch('sys.argv', test_args):
        # Run the main function
        main()

    # Assert that the output file was created
    assert os.path.exists(output_file)

    # Read the output file content
    with open(output_file, 'r') as f:
        content = f.read()

    # Assert that expected files are included and ignored files are excluded
    assert "==== File: {} ====".format(os.path.join(temp_project_blacklist, "src", "file1.py")) in content
    assert "==== File: {} ====".format(os.path.join(temp_project_blacklist, "docs", "doc1.md")) in content
    assert "==== File: {} ====".format(os.path.join(temp_project_blacklist, "src", "file2.txt")) not in content # Ignored by *.txt
    assert "==== File: {} ====".format(os.path.join(temp_project_blacklist, "ignore_me", "secret.txt")) not in content # Ignored by ignore_me/

    # Assert that the directory structure reflects the filter
    assert "{}/".format(os.path.join(temp_project_blacklist, "src")) in content
    assert "{}/".format(os.path.join(temp_project_blacklist, "docs")) in content
    assert "{}/".format(os.path.join(temp_project_blacklist, "ignore_me")) not in content # Directory ignored

    # Assert file names in structure
    assert "    file1.py" in content
    assert "    doc1.md" in content
    assert "    file2.txt" not in content # File ignored

# Test case for non-interactive mode (whitelist)
def test_non_interactive_whitelist(temp_project_whitelist): # Use the whitelist fixture
    output_file = os.path.join(temp_project_whitelist, "code.copy")

    # Simulate command-line arguments for non-interactive mode
    test_args = [
        'combine_code.py', # Script name (sys.argv[0])
        temp_project_whitelist,      # root_dir
        '--mode', 'whitelist',
        '--apply-filter-to-structure',
        '--debug' # Add debug flag
    ]

    with patch('sys.argv', test_args):
        # Run the main function
        main()

    # Assert that the output file was created
    assert os.path.exists(output_file)

    # Read the output file content
    with open(output_file, 'r') as f:
        content = f.read()

    # Assert that expected files are included and ignored files are excluded
    assert "==== File: {} ====".format(os.path.join(temp_project_whitelist, "include_me", "file_a.py")) in content
    assert "==== File: {} ====".format(os.path.join(temp_project_whitelist, "another_dir", "file_d.py")) in content
    assert "==== File: {} ====".format(os.path.join(temp_project_whitelist, "include_me", "file_b.txt")) not in content # Not included by *.py
    assert "==== File: {} ====".format(os.path.join(temp_project_whitelist, "other_dir", "file_c.md")) not in content # Not included by include_me/ or *.py

    # Assert that the directory structure reflects the filter
    assert "{}/".format(os.path.join(temp_project_whitelist, "include_me")) in content
    assert "{}/".format(os.path.join(temp_project_whitelist, "other_dir")) not in content # Directory not included
    assert "{}/".format(os.path.join(temp_project_whitelist, "another_dir")) in content # Directory included because it contains a .py file

    # Assert file names in structure
    assert "    file_a.py" in content
    assert "    file_d.py" in content
    assert "    file_b.txt" not in content # File not included
    assert "    file_c.md" not in content # File not included

# TODO: Add more test cases (no filter on structure, different patterns, empty directories, etc.)
# TODO: Add tests for interactive mode (requires mocking input)
