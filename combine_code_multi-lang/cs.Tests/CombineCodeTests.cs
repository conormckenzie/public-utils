using System;
using System.IO;
using Xunit;
using combine_code_multi_lang.cs; // Reference the main project namespace

namespace combine_code_multi_lang.cs.Tests
{
    public class CombineCodeTests : IDisposable
    {
        private readonly string _testRoot;

        public CombineCodeTests()
        {
            // Create a unique temporary directory for each test run
            _testRoot = Path.Combine(Path.GetTempPath(), "CombineCodeTests", Guid.NewGuid().ToString());
            Directory.CreateDirectory(_testRoot);
        }

        [Fact]
        public void TestCase1_BasicCombination_NoFiltering_Required()
        {
            // Scenario: Combine all files in a simple directory structure without any filter file present.
            // Status: Required

            // Input Structure:
            // test_root/
            // ├── file1.txt
            // ├── subdir/
            // │   └── file2.txt
            // └── file3.md

            // Create input files and directories
            Directory.CreateDirectory(Path.Combine(_testRoot, "subdir"));
            File.WriteAllText(Path.Combine(_testRoot, "file1.txt"), "Content of file1.txt");
            File.WriteAllText(Path.Combine(_testRoot, "subdir", "file2.txt"), "Content of file2.txt");
            File.WriteAllText(Path.Combine(_testRoot, "file3.md"), "Content of file3.md");

            // Instantiate FileFilter (simulating no filter file in blacklist mode)
            string nonExistentFilterFilePath = Path.Combine(_testRoot, "nonexistent_filter_file.txt");
            FileFilter fileFilter = new FileFilter(nonExistentFilterFilePath, true); // true for blacklist

            // Instantiate CodeCombiner
            CodeCombiner combiner = new CodeCombiner(_testRoot, fileFilter);

            // Combine code
            string actualOutput = combiner.Combine();

            // Expected Output content (excluding Run Parameters and Filter Contents sections as they are handled by Program.Main)
            // We will check for the presence of file headers, content, and structure lines.
            // The order of files in the output might vary depending on OS file system enumeration.

            Assert.Contains("--- File: file1.txt ---", actualOutput);
            Assert.Contains("Content of file1.txt", actualOutput);

            Assert.Contains("--- File: subdir/file2.txt ---", actualOutput);
            Assert.Contains("Content of file2.txt", actualOutput);

            Assert.Contains("--- File: file3.md ---", actualOutput);
            Assert.Contains("Content of file3.md", actualOutput);

            Assert.Contains("--- Directory Structure ---", actualOutput);
            Assert.Contains($"{Path.GetFileName(_testRoot)}/", actualOutput); // Root directory in structure
            Assert.Contains("    file1.txt", actualOutput);
            Assert.Contains("subdir/", actualOutput);
            Assert.Contains("    file2.txt", actualOutput);
            Assert.Contains("    file3.md", actualOutput); // file3.md is at root level in structure

            // Refined structure check - check for specific lines
            string[] actualLines = actualOutput.Split('\n');
            bool foundRoot = false;
            bool foundFile1 = false;
            bool foundSubdir = false;
            bool foundFile2 = false;
            bool foundFile3 = false;

            bool inStructureSection = false;
            foreach (string line in actualLines)
            {
                if (line.Contains("--- Directory Structure ---"))
                {
                    inStructureSection = true;
                    continue;
                }
                if (inStructureSection)
                {
                    if (line.Contains($"{Path.GetFileName(_testRoot)}/")) foundRoot = true;
                    if (line.Contains("    file1.txt")) foundFile1 = true;
                    if (line.Contains("subdir/")) foundSubdir = true;
                    if (line.Contains("    file2.txt")) foundFile2 = true;
                    if (line.Contains("    file3.md")) foundFile3 = true;
                }
            }

            Assert.True(foundRoot, "Root directory not found in structure.");
            Assert.True(foundFile1, "file1.txt not found in structure.");
            Assert.True(foundSubdir, "subdir/ not found in structure.");
            Assert.True(foundFile2, "file2.txt not found in structure.");
            Assert.True(foundFile3, "file3.md not found in structure.");
        }

        [Fact]
        public void TestCase2_BlacklistFiltering_FileExclusion_Required()
        {
            // Scenario: Exclude a specific file using a .copyignore file.
            // Status: Required

            // Input Structure:
            // test_root/
            // ├── .copyignore
            // ├── file_a.txt
            // ├── file_b.log
            // └── file_c.txt

            // Create input files and directories
            File.WriteAllText(Path.Combine(_testRoot, ".copyignore"), "*.log");
            File.WriteAllText(Path.Combine(_testRoot, "file_a.txt"), "Content of file_a.txt");
            File.WriteAllText(Path.Combine(_testRoot, "file_b.log"), "Content of file_b.log");
            File.WriteAllText(Path.Combine(_testRoot, "file_c.txt"), "Content of file_c.txt");

            // Instantiate FileFilter
            FileFilter fileFilter = new FileFilter(Path.Combine(_testRoot, ".copyignore"), true); // true for blacklist

            // Instantiate CodeCombiner
            CodeCombiner combiner = new CodeCombiner(_testRoot, fileFilter);

            // Combine code
            string actualOutput = combiner.Combine();

            // Assertions
            Assert.Contains("--- File: file_a.txt ---", actualOutput);
            Assert.Contains("Content of file_a.txt", actualOutput);

            Assert.DoesNotContain("--- File: file_b.log ---", actualOutput);
            Assert.DoesNotContain("Content of file_b.log", actualOutput);

            Assert.Contains("--- File: file_c.txt ---", actualOutput);
            Assert.Contains("Content of file_c.txt", actualOutput);

            Assert.Contains("--- Directory Structure ---", actualOutput);
            Assert.Contains($"{Path.GetFileName(_testRoot)}/", actualOutput);
            Assert.Contains("    file_a.txt", actualOutput);
            Assert.DoesNotContain("    file_b.log", actualOutput);
            Assert.Contains("    file_c.txt", actualOutput);
        }

        [Fact]
        public void TestCase3_WhitelistFiltering_FileInclusion_Required()
        {
            // Scenario: Include only specific files using a .copyinclude file.
            // Status: Required

            // Input Structure:
            // test_root/
            // ├── .copyinclude
            // ├── file_x.txt
            // ├── file_y.js
            // └── file_z.txt

            // Create input files and directories
            File.WriteAllText(Path.Combine(_testRoot, ".copyinclude"), "*.txt");
            File.WriteAllText(Path.Combine(_testRoot, "file_x.txt"), "Content of file_x.txt");
            File.WriteAllText(Path.Combine(_testRoot, "file_y.js"), "Content of file_y.js");
            File.WriteAllText(Path.Combine(_testRoot, "file_z.txt"), "Content of file_z.txt");

            // Instantiate FileFilter
            FileFilter fileFilter = new FileFilter(Path.Combine(_testRoot, ".copyinclude"), false); // false for whitelist

            // Instantiate CodeCombiner
            CodeCombiner combiner = new CodeCombiner(_testRoot, fileFilter);

            // Combine code
            string actualOutput = combiner.Combine();

            // Assertions
            Assert.Contains("--- File: file_x.txt ---", actualOutput);
            Assert.Contains("Content of file_x.txt", actualOutput);

            Assert.DoesNotContain("--- File: file_y.js ---", actualOutput);
            Assert.DoesNotContain("Content of file_y.js", actualOutput);

            Assert.Contains("--- File: file_z.txt ---", actualOutput);
            Assert.Contains("Content of file_z.txt", actualOutput);

            Assert.Contains("--- Directory Structure ---", actualOutput);
            Assert.Contains($"{Path.GetFileName(_testRoot)}/", actualOutput);
            Assert.Contains("    file_x.txt", actualOutput);
            Assert.DoesNotContain("    file_y.js", actualOutput);
            Assert.Contains("    file_z.txt", actualOutput);
        }

        [Fact]
        public void TestCase5_InvalidRootDirectory_Required()
        {
            // Scenario: Run the utility with a root directory that does not exist.
            // Status: Required

            // Command (Non-interactive): run_combine_code /path/to/nonexistent/directory
            string nonExistentDir = Path.Combine(_testRoot, "nonexistent_dir");

            // Instantiate FileFilter (doesn't matter for this test as it should fail before filtering)
            FileFilter fileFilter = new FileFilter(Path.Combine(_testRoot, ".copyignore"), true);

            // Instantiate CodeCombiner and expect an exception
            var exception = Assert.Throws<DirectoryNotFoundException>(() => new CodeCombiner(nonExistentDir, fileFilter).Combine());

            // Assertions
            Assert.Contains($"Root directory not found: {nonExistentDir}", exception.Message);
            // No output file should be created, but this is handled by the exception.
        }

        [Fact]
        public void TestCase6_EmptyRootDirectory_Required()
        {
            // Scenario: Run the utility on an empty directory.
            // Status: Required

            // Input Structure:
            // empty_root/ (which is _testRoot itself)

            // Create an empty root directory (already done in constructor)

            // Instantiate FileFilter (simulating no filter file in blacklist mode)
            string nonExistentFilterFilePath = Path.Combine(_testRoot, "nonexistent_filter_file.txt");
            FileFilter fileFilter = new FileFilter(nonExistentFilterFilePath, true); // true for blacklist

            // Instantiate CodeCombiner
            CodeCombiner combiner = new CodeCombiner(_testRoot, fileFilter);

            // Combine code
            string actualOutput = combiner.Combine();

            // Assertions
            Assert.DoesNotContain("--- File:", actualOutput); // No files should be processed

            Assert.Contains("--- Directory Structure ---", actualOutput);
            Assert.Contains($"{Path.GetFileName(_testRoot)}/", actualOutput); // Should only contain the root directory in structure
        }

        [Fact]
        public void TestCase7_FileWithSpecialCharacters_Required()
        {
            // Scenario: Ensure files with spaces or special characters in their names are handled correctly.
            // Status: Required

            // Input Structure:
            // test_root/
            // └── file with spaces & (symbols).txt

            // Create input file
            string specialFileName = "file with spaces & (symbols).txt";
            File.WriteAllText(Path.Combine(_testRoot, specialFileName), $"Content of {specialFileName}");

            // Instantiate FileFilter (simulating no filter file in blacklist mode)
            string nonExistentFilterFilePath = Path.Combine(_testRoot, "nonexistent_filter_file.txt");
            FileFilter fileFilter = new FileFilter(nonExistentFilterFilePath, true); // true for blacklist

            // Instantiate CodeCombiner
            CodeCombiner combiner = new CodeCombiner(_testRoot, fileFilter);

            // Combine code
            string actualOutput = combiner.Combine();

            // Assertions
            Assert.Contains($"--- File: {specialFileName} ---", actualOutput);
            Assert.Contains($"Content of {specialFileName}", actualOutput);

            Assert.Contains("--- Directory Structure ---", actualOutput);
            Assert.Contains($"{Path.GetFileName(_testRoot)}/", actualOutput);
            Assert.Contains($"    {specialFileName}", actualOutput);
        }

        [Fact]
        public void TestCase8_FileReadingError_Required()
        {
            // Scenario: Test handling of a file that cannot be read (e.g., due to permissions).
            // Status: Required

            // Input Structure:
            // test_root/
            // ├── readable_file.txt
            // └── unreadable_file.txt  (Permissions set to prevent reading by the user/process)

            // Create input files
            File.WriteAllText(Path.Combine(_testRoot, "readable_file.txt"), "Content of readable file.");
            string unreadableFilePath = Path.Combine(_testRoot, "unreadable_file.txt");
            File.WriteAllText(unreadableFilePath, "Content of unreadable file.");

            // Attempt to make the file unreadable. This might require specific OS permissions handling
            // and might not be universally achievable in a standard test environment without elevated privileges.
            // For demonstration, we'll simulate the error handling path by attempting to read and catching the exception.
            // A more robust test might involve platform-specific permission changes or mocking.

            // Instantiate FileFilter (simulating no filter file in blacklist mode)
            string nonExistentFilterFilePath = Path.Combine(_testRoot, "nonexistent_filter_file.txt");
            FileFilter fileFilter = new FileFilter(nonExistentFilterFilePath, true); // true for blacklist

            // Instantiate CodeCombiner
            CodeCombiner combiner = new CodeCombiner(_testRoot, fileFilter);

            // Combine code
            // Note: The current CodeCombiner.ProcessDirectory catches UnicodeDecodeError (from Python)
            // when reading files. It should catch IOException or UnauthorizedAccessException for file access issues in C#.
            // We will test the current behavior and note the required code change.
            // A proper test would involve setting file permissions, which is platform-dependent.
            // For now, we'll rely on the general exception handling if permissions cannot be set.

            string actualOutput = combiner.Combine();

            // Assertions
            Assert.Contains("--- File: readable_file.txt ---", actualOutput);
            Assert.Contains("Content of readable file.", actualOutput);

            Assert.DoesNotContain("--- File: unreadable_file.txt ---", actualOutput); // Unreadable file should not be included
            Assert.DoesNotContain("Content of unreadable file.", actualOutput);

            // Note: Asserting specific error messages for file reading errors is difficult without proper
            // exception handling in CodeCombiner. The current implementation might just skip the file
            // or throw a general exception caught by the test runner.
            // A more robust test would verify a specific exception or logged error message.

            Assert.Contains("--- Directory Structure ---", actualOutput);
            Assert.Contains($"{Path.GetFileName(_testRoot)}/", actualOutput);
            Assert.Contains("    readable_file.txt", actualOutput);
            Assert.DoesNotContain("    unreadable_file.txt", actualOutput); // Unreadable file should not be in structure
        }

        [Fact]
        public void TestCase4_MissingFilterFile_Blacklist_Desirable()
        {
            // Scenario: Run the utility in blacklist mode when the default .copyignore file is missing.
            // Status: Desirable (Flexible)

            // Input Structure:
            // test_root/
            // ├── file1.txt
            // └── subdir/
            //     └── file2.txt

            // Create input files and directories
            Directory.CreateDirectory(Path.Combine(_testRoot, "subdir"));
            File.WriteAllText(Path.Combine(_testRoot, "file1.txt"), "Content of file1.txt");
            File.WriteAllText(Path.Combine(_testRoot, "subdir", "file2.txt"), "Content of file2.txt");
            // Ensure .copyignore does NOT exist

            // Instantiate FileFilter (simulating a missing filter file)
            string missingFilterFilePath = Path.Combine(_testRoot, ".copyignore");
            FileFilter fileFilter = new FileFilter(missingFilterFilePath, true); // true for blacklist

            // Instantiate CodeCombiner
            CodeCombiner combiner = new CodeCombiner(_testRoot, fileFilter);

            // Combine code
            string actualOutput = combiner.Combine();

            // Expected Output: Should be the same as Test Case 1 (Basic Combination), as a missing filter file in blacklist mode should result in no filtering.

            Assert.Contains("--- File: file1.txt ---", actualOutput);
            Assert.Contains("Content of file1.txt", actualOutput);

            Assert.Contains("--- File: subdir/file2.txt ---", actualOutput);
            Assert.Contains("Content of file2.txt", actualOutput);

            Assert.Contains("--- Directory Structure ---", actualOutput);
            Assert.Contains($"{Path.GetFileName(_testRoot)}/", actualOutput);
            Assert.Contains("    file1.txt", actualOutput);
            Assert.Contains("subdir/", actualOutput);
            Assert.Contains("    file2.txt", actualOutput);
        }

        [Fact]
        public void TestCase9_BlacklistFiltering_DirectoryExclusion_Desirable()
        {
            // Scenario: Exclude an entire directory using a .copyignore file.
            // Status: Desirable (Flexible)

            // Input Structure:
            // test_root/
            // ├── .copyignore
            // ├── file_a.txt
            // ├── excluded_dir/
            // │   ├── file_in_excluded.txt
            // │   └── another_file.md
            // └── included_dir/
            //     └── file_in_included.txt

            // Create input files and directories
            Directory.CreateDirectory(Path.Combine(_testRoot, "excluded_dir"));
            Directory.CreateDirectory(Path.Combine(_testRoot, "included_dir"));
            File.WriteAllText(Path.Combine(_testRoot, ".copyignore"), "excluded_dir/");
            File.WriteAllText(Path.Combine(_testRoot, "file_a.txt"), "Content of file_a.txt");
            File.WriteAllText(Path.Combine(_testRoot, "excluded_dir", "file_in_excluded.txt"), "Content in excluded dir 1.");
            File.WriteAllText(Path.Combine(_testRoot, "excluded_dir", "another_file.md"), "Content in excluded dir 2.");
            File.WriteAllText(Path.Combine(_testRoot, "included_dir", "file_in_included.txt"), "Content in included dir.");

            // Instantiate FileFilter
            FileFilter fileFilter = new FileFilter(Path.Combine(_testRoot, ".copyignore"), true); // true for blacklist

            // Instantiate CodeCombiner
            CodeCombiner combiner = new CodeCombiner(_testRoot, fileFilter);

            // Combine code
            string actualOutput = combiner.Combine();

            // Assertions
            Assert.Contains("--- File: file_a.txt ---", actualOutput);
            Assert.Contains("Content of file_a.txt", actualOutput);

            Assert.Contains("--- File: included_dir/file_in_included.txt ---", actualOutput);
            Assert.Contains("Content in included dir.", actualOutput);

            Assert.DoesNotContain("--- File: excluded_dir/file_in_excluded.txt ---", actualOutput);
            Assert.DoesNotContain("Content in excluded dir 1.", actualOutput);
            Assert.DoesNotContain("--- File: excluded_dir/another_file.md ---", actualOutput);
            Assert.DoesNotContain("Content in excluded dir 2.", actualOutput);

            Assert.Contains("--- Directory Structure ---", actualOutput);
            Assert.Contains($"{Path.GetFileName(_testRoot)}/", actualOutput);
            Assert.Contains("    file_a.txt", actualOutput);
            Assert.Contains("included_dir/", actualOutput);
            Assert.Contains("    file_in_included.txt", actualOutput);
            Assert.DoesNotContain("excluded_dir/", actualOutput); // Excluded directory should not be in structure
        }

        [Fact]
        public void TestCase10_WhitelistFiltering_DirectoryInclusion_Desirable()
        {
            // Scenario: Include only a specific directory and its contents using a .copyinclude file.
            // Status: Desirable (Flexible)

            // Input Structure:
            // test_root/
            // ├── .copyinclude
            // ├── file_a.txt
            // ├── included_dir/
            // │   ├── file_in_included.txt
            // │   └── another_file.md
            // └── excluded_dir/
            //     └── file_in_excluded.txt

            // Create input files and directories
            Directory.CreateDirectory(Path.Combine(_testRoot, "included_dir"));
            Directory.CreateDirectory(Path.Combine(_testRoot, "excluded_dir"));
            File.WriteAllText(Path.Combine(_testRoot, ".copyinclude"), "included_dir/");
            File.WriteAllText(Path.Combine(_testRoot, "file_a.txt"), "Content of file_a.txt");
            File.WriteAllText(Path.Combine(_testRoot, "included_dir", "file_in_included.txt"), "Content in included dir 1.");
            File.WriteAllText(Path.Combine(_testRoot, "included_dir", "another_file.md"), "Content in included dir 2.");
            File.WriteAllText(Path.Combine(_testRoot, "excluded_dir", "file_in_excluded.txt"), "Content in excluded dir.");

            // Instantiate FileFilter
            FileFilter fileFilter = new FileFilter(Path.Combine(_testRoot, ".copyinclude"), false); // false for whitelist

            // Instantiate CodeCombiner
            CodeCombiner combiner = new CodeCombiner(_testRoot, fileFilter);

            // Combine code
            string actualOutput = combiner.Combine();

            // Assertions
            Assert.DoesNotContain("--- File: file_a.txt ---", actualOutput);
            Assert.DoesNotContain("Content of file_a.txt", actualOutput);

            Assert.Contains("--- File: included_dir/file_in_included.txt ---", actualOutput);
            Assert.Contains("Content in included dir 1.", actualOutput);
            Assert.Contains("--- File: included_dir/another_file.md ---", actualOutput);
            Assert.Contains("Content in included dir 2.", actualOutput);

            Assert.DoesNotContain("--- File: excluded_dir/file_in_excluded.txt ---", actualOutput);
            Assert.DoesNotContain("Content in excluded dir.", actualOutput);

            Assert.Contains("--- Directory Structure ---", actualOutput);
            Assert.Contains($"{Path.GetFileName(_testRoot)}/", actualOutput);
            Assert.Contains("included_dir/", actualOutput);
            Assert.Contains("    file_in_included.txt", actualOutput);
            Assert.Contains("    another_file.md", actualOutput);
            Assert.DoesNotContain("excluded_dir/", actualOutput); // Excluded directory should not be in structure
        }


        public void Dispose()
        {
            // Clean up the temporary directory after each test
            if (Directory.Exists(_testRoot))
            {
                Directory.Delete(_testRoot, true);
            }
        }
    }
}
