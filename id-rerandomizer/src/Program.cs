using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Linq;
using Newtonsoft.Json; // Changed from System.Text.Json

namespace IdRerandomizer
{
    class Program
    {
        // Flag to track verify-only mode globally within the Program class
        public static bool IsVerifyOnlyMode { get; private set; } = false;

        static void Main(string[] args)
        {
            // --- Argument Parsing ---
            string? targetDirectoryArg = null;
            bool noBackup = false;
            bool force = false;
            string? manifestFile = null;
            bool checkNonStandardIds = false;
            bool showHelp = false;

            List<string> positionalArgs = new List<string>();

            for (int i = 0; i < args.Length; i++)
            {
                switch (args[i])
                {
                    case "--help":
                        showHelp = true;
                        break;
                    case "--verify-only":
                        IsVerifyOnlyMode = true; // Set the static flag
                        // Expect path immediately after
                        if (i + 1 < args.Length && !args[i + 1].StartsWith("--"))
                        {
                            targetDirectoryArg = args[++i];
                        }
                        else
                        {
                            Console.WriteLine("Error: --verify-only requires a target directory path.");
                            showHelp = true;
                        }
                        break;
                    case "--no-backup":
                        noBackup = true;
                        break;
                    case "--force":
                        force = true;
                        break;
                    case "--manifest":
                        if (i + 1 < args.Length && !args[i + 1].StartsWith("--"))
                        {
                            manifestFile = args[++i];
                        }
                        else
                        {
                            Console.WriteLine("Error: --manifest requires a file path.");
                            showHelp = true;
                        }
                        break;
                    case "--check-non-standard-ids":
                        checkNonStandardIds = true;
                        break;
                    default:
                        if (!args[i].StartsWith("--"))
                        {
                            positionalArgs.Add(args[i]);
                        }
                        else
                        {
                            Console.WriteLine($"Error: Unknown option '{args[i]}'");
                            showHelp = true;
                        }
                        break;
                }
                if (showHelp) break;
            }

            // Assign positional argument if not already assigned by a flag
            if (targetDirectoryArg == null && positionalArgs.Count > 0)
            {
                targetDirectoryArg = positionalArgs[0];
                if (positionalArgs.Count > 1)
                {
                     Console.WriteLine($"Warning: Multiple paths provided, using '{targetDirectoryArg}'. Ignoring others.");
                }
            }
             else if (targetDirectoryArg != null && positionalArgs.Count > 0)
            {
                 Console.WriteLine($"Warning: Path provided both positionally and with --verify-only. Using path from --verify-only: '{targetDirectoryArg}'.");
            }


            if (showHelp || targetDirectoryArg == null)
            {
                ShowHelp();
                return;
            }

            // --- Execution ---
            try
            {
                 // Handle verify-only separately if specified
                if (IsVerifyOnlyMode)
                {
                    VerifyOnly(Path.GetFullPath(targetDirectoryArg), checkNonStandardIds); // Pass checkNonStandard flag
                    return;
                }

                // Normal execution or check-non-standard execution
                var options = new ToolOptions(
                    targetDirectory: Path.GetFullPath(targetDirectoryArg),
                    backupEnabled: !noBackup,
                    force: force,
                    manifestFile: manifestFile,
                    checkNonStandardIds: checkNonStandardIds
                );

                var rerandomizer = new IdReplacer(options);
                rerandomizer.Run();
            }
            catch (ArgumentException ex) // Catch specific exceptions for better feedback
            {
                 Console.ForegroundColor = ConsoleColor.Red;
                 Console.WriteLine($"Configuration Error: {ex.Message}");
                 Console.ResetColor();
                 Environment.Exit(2);
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Runtime Error: {ex.Message}");
                Console.WriteLine(ex.StackTrace); // Include stack trace for debugging
                Console.ResetColor();
                Environment.Exit(1);
            }
        }

        static void ShowHelp()
        {
            Console.WriteLine(@"ID Re-Randomization Tool
Usage: id-rerandomizer [PATH] [OPTIONS]
       id-rerandomizer --verify-only PATH [OPTIONS]

Description:
  Scans C# files for duplicate 6-character IDs (#XXXXXX# format) and replaces
  each instance with a unique ID. Optionally checks for non-standard length IDs.

Arguments:
  PATH              The target directory to scan. Required unless --help is used.

Options:
  --no-backup       Disable automatic backup of the target directory before changes.
  --force           Skip the confirmation prompt before making changes.
  --manifest FILE   Specify custom manifest file path (default: id-manifest.json).
  --help            Show this help message and exit.
  --verify-only     Scan for duplicate 6-char IDs (and optionally non-standard IDs
                    if --check-non-standard-ids is also used) but make no changes.
                    Reports findings and exits with code 1 if duplicates/non-std
                    IDs are found, 0 otherwise. Requires PATH argument.
  --check-non-standard-ids
                    Activates a mode to check for IDs (#...#) with lengths other
                    than 6. Lists findings. If not in --verify-only mode, prompts
                    interactively to replace instances of these non-standard IDs
                    with unique 6-character IDs."); // Updated help text
        }

        // Updated VerifyOnly to handle both modes based on flag
        static void VerifyOnly(string targetDirectory, bool checkNonStandard)
        {
            Console.WriteLine($"Verifying ID uniqueness in: {targetDirectory}");
            var options = new ToolOptions(targetDirectory, false, true, null, checkNonStandard); // Pass flag
            var verifier = new IdReplacer(options);
            bool issuesFound = false;

            // Always check standard duplicates in verify mode
            Console.WriteLine("\nChecking for duplicate 6-character IDs...");
            var sixCharIds = verifier.ScanForAllIds(onlySixChars: true);
            var standardDuplicates = verifier.IdentifyDuplicates(sixCharIds);
            if (standardDuplicates.Count > 0)
            {
                issuesFound = true;
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"Found {standardDuplicates.Count} duplicate 6-character ID(s):");
                foreach (var dupId in standardDuplicates)
                {
                    Console.WriteLine($"  - {dupId} (Count: {sixCharIds[dupId]})");
                }
                Console.ResetColor();
            } else {
                 Console.WriteLine("No duplicate 6-character IDs found.");
            }


            // Optionally check non-standard IDs
            if (checkNonStandard)
            {
                 Console.WriteLine("\nChecking for non-standard length IDs...");
                 // Need to scan *all* IDs if checking non-standard
                 var allIds = verifier.ScanForAllIds(onlySixChars: false);
                 var nonStandardIds = verifier.IdentifyNonStandardLengthIds(allIds);
                 if (nonStandardIds.Count > 0)
                 {
                    issuesFound = true;
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"Found {nonStandardIds.Count} unique non-standard length ID string(s):");
                     foreach (var kvp in nonStandardIds)
                     {
                         Console.WriteLine($"  - {kvp.Key} (Length: {kvp.Key.Length}, Count: {kvp.Value})");
                     }
                     Console.ResetColor();
                 } else {
                     Console.WriteLine("No non-standard length IDs found.");
                 }
            }

            Console.WriteLine("\nVerification Summary:");
            if (!issuesFound)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("Verification successful: No specified issues found.");
                Console.ResetColor();
                Environment.Exit(0); // Exit code 0 for success
            }
            else
            {
                 Console.ForegroundColor = ConsoleColor.Red;
                 Console.WriteLine("Verification failed: Issues found (see details above).");
                 Console.ResetColor();
                 Environment.Exit(1); // Exit code 1 for failure
            }
        }
    } // End Program class

    // ToolOptions class definition removed.

    public class IdReplacer
    {
        private readonly ToolOptions _options;
        private readonly ManifestHandler _manifestHandler;
        private readonly RandomNumberGenerator _rng = RandomNumberGenerator.Create();
        private const string ExcludedId = "XXXXXX"; // Define constant for exclusion

        // Define Regex patterns as static readonly fields for efficiency
        private static readonly Regex SixCharRegex = new Regex(@"#([A-Z0-9]{6})#", RegexOptions.Compiled);
        private static readonly Regex AnyCharRegex = new Regex(@"#([A-Z0-9]+)#", RegexOptions.Compiled);


        public IdReplacer(ToolOptions options)
        {
            _options = options;
            // Manifest path determined here, using TargetDirectory which is validated in ToolOptions
            string defaultManifestDir = Path.Combine(_options.TargetDirectory, "ignore", "id-rerandomizer");
            Directory.CreateDirectory(defaultManifestDir); // Ensure the directory exists
            _manifestHandler = new ManifestHandler(
                _options.ManifestFile ?? Path.Combine(defaultManifestDir, "id-manifest.json"));
        }

        public void Run()
        {
            // Confirmation prompt (if not forced)
            if (!_options.Force)
            {
                Console.WriteLine($"This tool will scan and potentially modify files in: {_options.TargetDirectory}");
                if (_options.CheckNonStandardIds) {
                     Console.WriteLine("Mode: Checking for non-standard length IDs and prompting for replacement.");
                } else {
                     Console.WriteLine("Mode: Checking for and replacing duplicate 6-character IDs.");
                }
                Console.Write("Continue? (y/n): ");
                if ((Console.ReadLine()?.ToLower() ?? "") != "y")
                {
                     Console.WriteLine("Operation cancelled by user.");
                     return;
                }
            }

            // Load or create manifest
            var manifest = _manifestHandler.LoadManifest() ?? new ReplacementManifest
            {
                OriginalDirectory = _options.TargetDirectory, // TargetDirectory is guaranteed non-null here
                Timestamp = DateTime.UtcNow
            };

            // Execute logic based on mode
            if (_options.CheckNonStandardIds)
            {
                RunNonStandardIdCheckAndReplace(manifest);
            }
            else
            {
                RunStandardDuplicateCheckAndReplace(manifest);
            }
        }

        // --- Mode-Specific Logic ---

        private void RunStandardDuplicateCheckAndReplace(ReplacementManifest manifest)
        {
            Console.WriteLine("\nRunning standard check for duplicate 6-character IDs...");
            var sixCharIdCounts = ScanForAllIds(onlySixChars: true);
            var duplicateIdStrings = IdentifyDuplicates(sixCharIdCounts);

            if (duplicateIdStrings.Count == 0)
            {
                Console.WriteLine("No duplicate 6-character IDs found. No changes needed.");
                return;
            }

            Console.WriteLine($"Found {duplicateIdStrings.Count} duplicate 6-character ID string(s) whose instances will be replaced: {string.Join(", ", duplicateIdStrings)}");

            if (_options.BackupEnabled) CreateBackup(manifest);

            int replacementsMade = ReplaceDuplicateIdInstances(duplicateIdStrings, sixCharIdCounts.Keys);

            // Update manifest
            manifest.OperationMode = "StandardDuplicates";
            manifest.OriginalDuplicatesProcessed = duplicateIdStrings.ToList();
            manifest.NonStandardIdsProcessed = null; // Ensure other mode's field is null
            _manifestHandler.SaveManifest(manifest);

            Console.WriteLine($"\nSuccessfully replaced {replacementsMade} instances of duplicate 6-character IDs.");
        }

        private void RunNonStandardIdCheckAndReplace(ReplacementManifest manifest)
        {
            Console.WriteLine("\nRunning check for non-standard length IDs...");
            var allIdCounts = ScanForAllIds(onlySixChars: false);
            var nonStandardIds = IdentifyNonStandardLengthIds(allIdCounts);

            if (nonStandardIds.Count == 0)
            {
                Console.WriteLine("No non-standard length IDs found.");
                // Decide if we should also check/fix standard duplicates in this mode?
                // For now, keeping modes separate as requested.
                return;
            }

            Console.WriteLine($"Found {nonStandardIds.Count} unique non-standard length ID string(s):");
            var idsToReplace = new HashSet<string>(); // Store non-standard IDs approved for replacement

            // --- Interactive Prompting ---
            bool yesToAll = false;
            bool noToAll = false;

            Console.WriteLine("\nYou will be prompted for each unique non-standard ID found.");

            foreach (var kvp in nonStandardIds.OrderBy(x => x.Key)) // Process alphabetically
            {
                if (noToAll) break; // Skip remaining if user chose No to All

                string id = kvp.Key;
                int count = kvp.Value;
                char choice = ' ';

                if (yesToAll)
                {
                    choice = 'y';
                    Console.WriteLine($"  - Auto-replacing '{id}' (Length: {id.Length}, Count: {count}) due to 'All' selection.");
                }
                else
                {
                    while (true)
                    {
                        Console.Write($"  - Replace all {count} instances of '{id}' (Length: {id.Length})? (Y=Yes, N=No, A=Yes to All, X=Skip All): ");
                        var input = Console.ReadLine()?.ToLowerInvariant().Trim();
                        if (input == "y" || input == "n" || input == "a" || input == "x")
                        {
                            choice = input[0];
                            break;
                        }
                        Console.WriteLine("    Invalid input. Please enter Y, N, A, or X.");
                    }
                }

                switch (choice)
                {
                    case 'y':
                        idsToReplace.Add(id);
                        break;
                    case 'n':
                        // Do nothing, skip this ID
                        break;
                    case 'a':
                        idsToReplace.Add(id);
                        yesToAll = true; // Apply 'Yes' to all subsequent IDs
                        break;
                    case 'x':
                        noToAll = true; // Skip all subsequent IDs
                        break;
                }
                 if (noToAll) break; // Exit loop immediately if No to All chosen
            }

            if (idsToReplace.Count == 0)
            {
                Console.WriteLine("\nNo non-standard IDs selected for replacement.");
                return;
            }

            Console.WriteLine($"\nProceeding to replace instances of {idsToReplace.Count} selected non-standard ID(s).");

            // --- Replacement ---
            if (_options.BackupEnabled) CreateBackup(manifest);

            // Get *all* IDs again to ensure uniqueness checks are comprehensive
            // Note: This includes 6-char IDs, needed for uniqueness check during replacement
            var currentAllIds = ScanForAllIds(onlySixChars: false);
            int replacementsMade = ReplaceNonStandardIdInstances(idsToReplace, currentAllIds.Keys);

            // --- Update Manifest ---
            manifest.OperationMode = "NonStandardCheck";
            manifest.NonStandardIdsProcessed = idsToReplace.ToList();
            manifest.OriginalDuplicatesProcessed = null; // Ensure other mode's field is null
            _manifestHandler.SaveManifest(manifest);

            Console.WriteLine($"\nSuccessfully replaced {replacementsMade} instances of selected non-standard IDs with unique 6-character IDs.");
        } // <-- This brace closes RunNonStandardIdCheckAndReplace

        // --- Supporting Methods ---

        public Dictionary<string, int> ScanForAllIds(bool onlySixChars = false)
        {
            Regex regexToUse = onlySixChars ? SixCharRegex : AnyCharRegex; // Use static regex fields

            Console.WriteLine($"Scanning files for {(onlySixChars ? "6-character" : "all")} IDs...");
            var idCounts = new Dictionary<string, int>();
            string targetDir = _options.TargetDirectory;
            var allFiles = Directory.EnumerateFiles(targetDir, "*.cs", SearchOption.AllDirectories);
            var filesToScan = allFiles.Where(f => !f.Contains(Path.DirectorySeparatorChar + "backups" + Path.DirectorySeparatorChar));

            foreach (var filePath in filesToScan)
            {
                var content = File.ReadAllText(filePath);
                var matches = regexToUse.Matches(content); // Use the selected regex
                foreach (Match match in matches)
                {
                    var id = match.Groups[1].Value;
                    if (id == ExcludedId) continue; // Skip excluded ID

                    if (idCounts.ContainsKey(id))
                    {
                        idCounts[id]++;
                    }
                    else
                    {
                        idCounts[id] = 1;
                    }
                }
            }
            Console.WriteLine($"Scan complete. Found {idCounts.Count} unique ID strings (excluding '{ExcludedId}').");
            return idCounts;
        }

        public HashSet<string> IdentifyDuplicates(Dictionary<string, int> idCounts)
        {
            // This method now correctly identifies duplicates among the *provided* counts
            // (which will be only 6-char IDs if called from RunStandardDuplicateCheck)
            return idCounts.Where(kvp => kvp.Value > 1)
                           .Select(kvp => kvp.Key)
                           .ToHashSet();
        }

        public Dictionary<string, int> IdentifyNonStandardLengthIds(Dictionary<string, int> allIdCounts)
        {
            return allIdCounts.Where(kvp => kvp.Key.Length != 6)
                              .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        }

        // Replaces instances of specific duplicate 6-char IDs
        private int ReplaceDuplicateIdInstances(HashSet<string> duplicateSixCharIdStrings, ICollection<string> allOriginalIds)
        {
            Console.WriteLine("Replacing instances of duplicate 6-character IDs...");
            string targetDir = _options.TargetDirectory;
            var allFiles = Directory.EnumerateFiles(targetDir, "*.cs", SearchOption.AllDirectories);
            var filesToProcess = allFiles.Where(f => !f.Contains(Path.DirectorySeparatorChar + "backups" + Path.DirectorySeparatorChar));

            int replacementsMadeCount = 0;
            var newlyGeneratedIds = new HashSet<string>();

            foreach (var filePath in filesToProcess)
            {
                var originalContent = File.ReadAllText(filePath);
                bool fileModified = false;

                // Use MatchEvaluator with the 6-char regex
                var newContent = SixCharRegex.Replace(originalContent, match =>
                {
                    var currentId = match.Groups[1].Value;
                    if (currentId == ExcludedId) return match.Value; // Should not happen if scan excludes it, but safe check

                    if (duplicateSixCharIdStrings.Contains(currentId))
                    {
                        string newUniqueId;
                        do {
                            newUniqueId = GenerateSecureIdInternal();
                        } while (allOriginalIds.Contains(newUniqueId) || newlyGeneratedIds.Contains(newUniqueId));

                        newlyGeneratedIds.Add(newUniqueId);
                        fileModified = true;
                        replacementsMadeCount++;
                        // Console.WriteLine($"  - Replacing instance of '{currentId}' with '{newUniqueId}' in {Path.GetFileName(filePath)}"); // Optional verbose logging
                        return $"#{newUniqueId}#";
                    }
                    return match.Value; // Leave non-duplicates alone
                });

                if (fileModified)
                {
                    File.WriteAllText(filePath, newContent);
                     Console.WriteLine($"  - Updated {Path.GetFileName(filePath)}"); // Log file update
                }
            }
            Console.WriteLine("Replacement process complete.");
            return replacementsMadeCount;
        }

         // New method to replace instances of selected non-standard length IDs
        private int ReplaceNonStandardIdInstances(HashSet<string> nonStandardIdsToReplace, ICollection<string> allOriginalIds)
        {
            Console.WriteLine("Replacing instances of selected non-standard length IDs...");
            string targetDir = _options.TargetDirectory;
            var allFiles = Directory.EnumerateFiles(targetDir, "*.cs", SearchOption.AllDirectories);
            var filesToProcess = allFiles.Where(f => !f.Contains(Path.DirectorySeparatorChar + "backups" + Path.DirectorySeparatorChar));

            int replacementsMadeCount = 0;
            var newlyGeneratedIds = new HashSet<string>(); // Track IDs generated in this specific run

            foreach (var filePath in filesToProcess)
            {
                var originalContent = File.ReadAllText(filePath);
                bool fileModified = false;

                // Use MatchEvaluator with the Any-length regex
                var newContent = AnyCharRegex.Replace(originalContent, match =>
                {
                    var currentId = match.Groups[1].Value;
                    if (currentId == ExcludedId) return match.Value; // Skip excluded ID

                    // Only replace if the current ID is one of the non-standard ones selected by the user
                    if (nonStandardIdsToReplace.Contains(currentId))
                    {
                        string newUniqueId;
                        // Generate a new 6-char ID, ensuring uniqueness
                        do {
                            newUniqueId = GenerateSecureIdInternal();
                        } while (allOriginalIds.Contains(newUniqueId) || newlyGeneratedIds.Contains(newUniqueId));

                        newlyGeneratedIds.Add(newUniqueId);
                        fileModified = true;
                        replacementsMadeCount++;
                        // Console.WriteLine($"  - Replacing instance of '{currentId}' with '{newUniqueId}' in {Path.GetFileName(filePath)}"); // Optional verbose logging
                        return $"#{newUniqueId}#"; // Replace with new 6-char ID
                    }
                    // Leave standard IDs and non-selected non-standard IDs alone
                    return match.Value;
                });

                if (fileModified)
                {
                    File.WriteAllText(filePath, newContent);
                    Console.WriteLine($"  - Updated {Path.GetFileName(filePath)}"); // Log file update
                }
            }
            Console.WriteLine("Replacement process complete.");
            return replacementsMadeCount;
        }


        private string GenerateSecureIdInternal()
        {
            const string validChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            var bytes = new byte[1];
            var sb = new StringBuilder(6);
            string newId;

            do {
                sb.Clear();
                while (sb.Length < 6)
                {
                    _rng.GetBytes(bytes);
                    sb.Append(validChars[bytes[0] % validChars.Length]);
                }
                newId = sb.ToString();
            } while (newId == ExcludedId); // Regenerate if excluded ID is generated

            return newId;
        }

        private void CreateBackup(ReplacementManifest manifest)
        {
            var backupBaseDir = Path.Combine(_options.TargetDirectory, "ignore", "id-rerandomizer", "backups");
            var backupDir = Path.Combine(backupBaseDir, DateTime.Now.ToString("yyyyMMdd-HHmmss"));
            // Directory.CreateDirectory will create all necessary parent directories
            Directory.CreateDirectory(backupDir);

            Console.WriteLine($"Creating backup at {backupDir}..."); // Add progress message

            // Copy files individually to handle potential errors better
            int filesCopied = 0;
            int errors = 0;
            foreach (var file in Directory.EnumerateFiles(_options.TargetDirectory, "*", SearchOption.AllDirectories))
            {
                 // Skip files within the backups directory itself
                 if (file.Contains(Path.DirectorySeparatorChar + "backups" + Path.DirectorySeparatorChar)) continue;

                try
                {
                    var relativePath = Path.GetRelativePath(_options.TargetDirectory, file);
                    var backupPath = Path.Combine(backupDir, relativePath);
                    var backupPathDirectory = Path.GetDirectoryName(backupPath);

                    if (backupPathDirectory != null && !Directory.Exists(backupPathDirectory))
                    {
                        Directory.CreateDirectory(backupPathDirectory);
                    }
                    if (backupPath != null) // Ensure backupPath is not null
                    {
                         File.Copy(file, backupPath, true); // Overwrite if exists (shouldn't normally happen)
                         filesCopied++;
                    } else {
                         Console.WriteLine($"  - Warning: Could not determine backup path for {file}");
                         errors++;
                    }

                }
                catch (Exception ex)
                {
                     Console.WriteLine($"  - Error copying {Path.GetFileName(file)} to backup: {ex.Message}");
                     errors++;
                }
            }

             if (errors > 0) {
                 Console.WriteLine($"Backup completed with {errors} errors.");
             } else {
                 Console.WriteLine($"Backup complete ({filesCopied} files copied).");
             }


            manifest.BackupLocation = backupDir;
            // Save manifest immediately after successful backup creation? Or wait until end?
            // Waiting until end seems safer.
        }
    } // End IdReplacer class

    // ReplacementManifest class moved to Manifest/ReplacementManifest.cs

    public class ManifestHandler
    {
        public string ManifestPath { get; }

        public ManifestHandler(string manifestPath)
        {
            ManifestPath = manifestPath;
        }

        public ReplacementManifest? LoadManifest()
        {
             if (!File.Exists(ManifestPath)) return null;
             try {
                return JsonConvert.DeserializeObject<ReplacementManifest>(File.ReadAllText(ManifestPath));
             } catch (JsonException ex) {
                 Console.WriteLine($"Warning: Could not parse existing manifest file at '{ManifestPath}'. A new one will be created. Error: {ex.Message}");
                 return null;
             }
        }

        public void SaveManifest(ReplacementManifest manifest)
        {
            try {
                var options = new JsonSerializerSettings { Formatting = Formatting.Indented, NullValueHandling = NullValueHandling.Ignore };
                File.WriteAllText(ManifestPath, JsonConvert.SerializeObject(manifest, options));
            } catch (Exception ex) {
                 Console.WriteLine($"Error saving manifest file to '{ManifestPath}': {ex.Message}");
                 // Decide if this should halt the program? For now, just warn.
            }

        }
    } // End ManifestHandler class
} // End namespace
