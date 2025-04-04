using System;
using System.Collections.Generic;

namespace IdRerandomizer
{
    public class ReplacementManifest
    {
        public DateTime Timestamp { get; set; }
        public string? OriginalDirectory { get; set; }
        public string? BackupLocation { get; set; }
        public string? OperationMode { get; set; } // Track which operation was performed

        // List of original 6-char duplicates processed in Standard mode
        public List<string>? OriginalDuplicatesProcessed { get; set; }
        // List of non-standard IDs selected for replacement in NonStandardCheck mode
         public List<string>? NonStandardIdsProcessed { get; set; }


        [Obsolete("Replacements dictionary is no longer used.")]
        public Dictionary<string, string>? Replacements { get; set; }
    } // End ReplacementManifest class
} // End namespace
