using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace combine_code_multi_lang.cs
{
    public class CodeCombiner
    {
        private readonly FileFilter _fileFilter;
        private readonly string _rootDir;
        private readonly StringBuilder _combinedContent = new StringBuilder();
        private readonly StringBuilder _directoryStructure = new StringBuilder();

        /// <summary>
        /// Initializes a new instance of the CodeCombiner class.
        /// </summary>
        /// <param name="rootDir">The root directory to combine code from.</param>
        /// <param name="fileFilter">The FileFilter instance to use for including/excluding files.</param>
        public CodeCombiner(string rootDir, FileFilter fileFilter)
        {
            _rootDir = rootDir;
            _fileFilter = fileFilter;
        }

        /// <summary>
        /// Combines the code files based on the filter and generates the directory structure.
        /// </summary>
        /// <returns>The combined code content including the directory structure.</returns>
        public string Combine()
        {
            if (!Directory.Exists(_rootDir))
            {
                throw new DirectoryNotFoundException($"Root directory not found: {_rootDir}");
            }

            ProcessDirectory(_rootDir, "");

            _combinedContent.AppendLine("\n--- Directory Structure ---");
            _combinedContent.Append(_directoryStructure.ToString());

            return _combinedContent.ToString();
        }

        /// <summary>
        /// Recursively processes directories and files.
        /// </summary>
        /// <param name="currentDir">The current directory being processed.</param>
        /// <param name="indent">The indentation string for the directory structure.</param>
        private void ProcessDirectory(string currentDir, string indent)
        {
            // Add directory to structure
            if (currentDir != _rootDir)
            {
                 _directoryStructure.AppendLine($"{indent}|-- {Path.GetFileName(currentDir)}/");
                 indent += "|   ";
            }


            // Process files in the current directory
            foreach (var filePath in Directory.GetFiles(currentDir))
            {
                if (_fileFilter.ShouldInclude(filePath, _rootDir))
                {
                    _combinedContent.AppendLine($"--- File: {Path.GetRelativePath(_rootDir, filePath)} ---");
                    _combinedContent.AppendLine(File.ReadAllText(filePath));
                    _combinedContent.AppendLine(); // Add a blank line after each file content

                    _directoryStructure.AppendLine($"{indent}|-- {Path.GetFileName(filePath)}");
                }
            }

            // Process subdirectories
            foreach (var subDir in Directory.GetDirectories(currentDir))
            {
                // TODO: Add logic to exclude directories based on filter patterns if needed
                ProcessDirectory(subDir, indent);
            }
        }
    }
}
