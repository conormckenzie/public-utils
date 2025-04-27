using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace combine_code_multi_lang.cs
{
    public class FileFilter
    {
        private readonly List<string> _patterns = new List<string>();
        private readonly bool _isBlacklist;

        /// <summary>
        /// Initializes a new instance of the FileFilter class.
        /// </summary>
        /// <param name="filterFilePath">The path to the .copyignore or .copyinclude file.</param>
        /// <param name="isBlacklist">True if using a blacklist (.copyignore), false if using a whitelist (.copyinclude).</param>
        public FileFilter(string filterFilePath, bool isBlacklist)
        {
            _isBlacklist = isBlacklist;
            LoadPatterns(filterFilePath);
        }

        /// <summary>
        /// Loads patterns from the specified filter file.
        /// </summary>
        /// <param name="filterFilePath">The path to the filter file.</param>
        private void LoadPatterns(string filterFilePath)
        {
            if (File.Exists(filterFilePath))
            {
                _patterns.AddRange(File.ReadAllLines(filterFilePath)
                                     .Where(line => !string.IsNullOrWhiteSpace(line) && !line.TrimStart().StartsWith("#")));
            }
            // TODO: Add more sophisticated pattern matching logic (e.g., glob patterns)
        }

        /// <summary>
        /// Determines if a file path should be included based on the loaded patterns and mode.
        /// </summary>
        /// <param name="filePath">The file path to check.</param>
        /// <param name="rootDir">The root directory the filter is relative to.</param>
        /// <returns>True if the file should be included, false otherwise.</returns>
        public bool ShouldInclude(string filePath, string rootDir)
        {
            // Normalize paths for consistent comparison
            var relativePath = Path.GetRelativePath(rootDir, filePath).Replace("\\", "/");

            bool isMatch = _patterns.Any(pattern =>
                // Simple substring match for now
                relativePath.Contains(pattern.Replace("\\", "/"), StringComparison.OrdinalIgnoreCase)
                // TODO: Implement proper glob pattern matching
            );

            if (_isBlacklist)
            {
                // In blacklist mode, include if NO pattern matches
                return !isMatch;
            }
            else
            {
                // In whitelist mode, include if ANY pattern matches
                return isMatch;
            }
        }
    }
}
