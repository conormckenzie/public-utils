using System;

namespace IdRerandomizer
{
    public class ToolOptions
    {
        public string TargetDirectory { get; }
        public bool BackupEnabled { get; }
        public bool Force { get; }
        public string? ManifestFile { get; }
        public bool CheckNonStandardIds { get; }

        public ToolOptions(string targetDirectory, bool backupEnabled, bool force, string? manifestFile, bool checkNonStandardIds)
        {
            if (string.IsNullOrEmpty(targetDirectory)) // Check for null or empty
            {
                throw new ArgumentException("Target directory cannot be null or empty.", nameof(targetDirectory));
            }
            TargetDirectory = targetDirectory;
            BackupEnabled = backupEnabled;
            Force = force;
            ManifestFile = manifestFile;
            CheckNonStandardIds = checkNonStandardIds;
        }
    } // End ToolOptions class
} // End namespace
