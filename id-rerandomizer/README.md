# ID Re-Randomizer Tool

## Overview

The ID Re-Randomizer is a command-line utility designed to scan C# source files (`*.cs`) within a specified project directory. It helps manage and enforce uniqueness for special ID strings embedded in the code, specifically those formatted as `#XXXXXX#` (6 uppercase alphanumeric characters) or `#...#` (other lengths).

This tool is useful for maintaining consistency and avoiding collisions when using generated or manually assigned IDs within comments or code.

## Core Features

1.  **ID Format Recognition:** Identifies IDs enclosed in `#` symbols, containing only uppercase letters (A-Z) and digits (0-9). Example: `#AB12CD#`.
2.  **Exclusion:** The specific ID string `#XXXXXX#` is always ignored during scanning and replacement. Newly generated IDs will never be `#XXXXXX#`.
3.  **Targeted Scanning:** Operates recursively within the specified target directory but automatically excludes files within any subdirectory named `backups` (relative to the target directory root) and the tool's own output directory (`ignore/id-rerandomizer/`).
4.  **Safe Output:** By default, all generated output (backups and the operation manifest) is placed within an `ignore/id-rerandomizer/` subdirectory inside the target project. This location is typically suitable for adding to a `.gitignore` file.
5.  **Backup:** Before making any file modifications, the tool creates a timestamped backup of the relevant files within the `ignore/id-rerandomizer/backups/` directory. This can be disabled using the `--no-backup` flag.
6.  **Manifest:** Generates a JSON manifest file (`ignore/id-rerandomizer/id-manifest.json` by default) detailing the operation performed, the IDs processed, and the backup location (if created). The manifest path can be customized using the `--manifest` flag.

## Operational Modes

The tool offers several modes to check for and fix different types of ID issues:

### 1. Standard Mode (Default)

*   **Goal:** Ensure all 6-character IDs (excluding `#XXXXXX#`) are unique across the scanned files.
*   **Action:**
    *   Scans for all 6-character IDs (`#XXXXXX#`).
    *   Identifies any 6-character ID strings used more than once (duplicates).
    *   If duplicates are found:
        *   Prompts the user for confirmation (can be skipped with `--force`).
        *   Creates a backup (unless `--no-backup` is used).
        *   Replaces **each instance** of an original duplicate 6-character ID with a **newly generated, unique 6-character ID**.
        *   Updates the manifest (`OperationMode: "StandardDuplicates"`) listing the original duplicate ID strings that were processed (`OriginalDuplicatesProcessed`).
    *   If no duplicates are found, reports this and exits cleanly.

### 2. Non-Standard ID Check Mode (`--check-non-standard-ids`)

*   **Goal:** Identify IDs that do not conform to the standard 6-character length and optionally replace them.
*   **Action:**
    *   Scans for *all* alphanumeric IDs (`#...#`, excluding `#XXXXXX#`).
    *   Identifies IDs whose length is *not* exactly 6 characters.
    *   Lists all unique non-standard length IDs found, along with their length and count.
    *   If no non-standard IDs are found, reports this and exits.
    *   If non-standard IDs are found **and** the tool is **not** in `--verify-only` mode:
        *   Prompts the user interactively for **each unique non-standard ID string**:
            *   `Y`: Replace all instances of *this specific* non-standard ID.
            *   `N`: Skip all instances of *this specific* non-standard ID.
            *   `A`: Replace all instances of *this* non-standard ID and *all subsequent* non-standard IDs automatically.
            *   `X`: Skip all instances of *this* non-standard ID and *all subsequent* non-standard IDs automatically.
        *   If any non-standard IDs are approved for replacement:
            *   Creates a backup (unless `--no-backup` is used).
            *   Replaces **each instance** of an approved non-standard ID with a **newly generated, unique 6-character ID**.
            *   Updates the manifest (`OperationMode: "NonStandardCheck"`) listing the non-standard ID strings that were approved for processing (`NonStandardIdsProcessed`).
    *   **(Note:** This mode does *not* simultaneously process standard 6-character duplicates. Run the tool in standard mode separately if needed.)

### 3. Verification Mode (`--verify-only`)

*   **Goal:** Report on ID status without modifying any files. Ideal for CI checks or quick scans.
*   **Action:**
    *   Performs scans based on other flags:
        *   With only `--verify-only PATH`: Scans for and reports duplicate 6-character IDs (excluding `#XXXXXX#`).
        *   With `--verify-only PATH --check-non-standard-ids`: Scans for and reports **both** duplicate 6-character IDs **and** non-standard length IDs.
    *   Makes **no changes** to any files.
    *   Creates **no backup**.
    *   Creates **no manifest**.
    *   Exits with code 0 if no issues (as defined by the flags used) are found.
    *   Exits with code 1 if any issues are found.

## Usage

### Prerequisites

*   .NET 8.0 SDK or Runtime

### Building (Optional)

Navigate to the `id-rerandomizer` directory and run:
```bash
dotnet build -c Release
```
The executable will be located in `bin/Release/net8.0/`.

### Running

Execute the tool from the command line, providing the path to the target directory.

**General Syntax:**

```bash
# Using dotnet run (builds and runs)
dotnet run --project <path-to-id-rerandomizer.csproj> -- [TARGET_PATH] [OPTIONS]

# Using the compiled executable
<path-to-executable>/id-rerandomizer [TARGET_PATH] [OPTIONS]
```

**Command-Line Options:**

*   `TARGET_PATH`: (Required) The target directory containing the C# project files to scan.
*   `--check-non-standard-ids`: (Optional) Activates the non-standard ID checking mode.
*   `--verify-only`: (Optional) Activates verification mode. Requires `TARGET_PATH`. When used, no changes are made.
*   `--no-backup`: (Optional) Disables the automatic backup creation before making changes.
*   `--force`: (Optional) Skips the confirmation prompt before making changes in modes that modify files.
*   `--manifest FILE`: (Optional) Specify a custom absolute or relative path for the manifest file. (Default: `[TARGET_PATH]/ignore/id-rerandomizer/id-manifest.json`)
*   `--help`: (Optional) Displays the help message and exits.

**Examples:**

1.  **Check for and fix duplicate 6-character IDs:**
    ```bash
    dotnet run --project ./id-rerandomizer.csproj -- ../my-csharp-project/
    ```
    *(Prompts before fixing)*

2.  **Check for and fix non-standard IDs, skipping confirmation:**
    ```bash
    id-rerandomizer ../my-csharp-project/ --check-non-standard-ids --force
    ```
    *(Assumes `id-rerandomizer` is in PATH or run from its directory)*

3.  **Verify both duplicate and non-standard IDs without making changes:**
    ```bash
    dotnet run --project ./id-rerandomizer.csproj -- ../my-csharp-project/ --verify-only --check-non-standard-ids
    ```

4.  **Fix duplicates, disable backup, use custom manifest location:**
    ```bash
    id-rerandomizer ../my-csharp-project/ --no-backup --manifest ../my-project-output/rerandomizer-log.json
    ```

## Exit Codes

*   `0`: Operation completed successfully, or verification found no issues.
*   `1`: Verification mode (`--verify-only`) found issues (duplicates or non-standard IDs, depending on flags). Or, a runtime error occurred during normal operation.
*   `2`: Configuration error (e.g., invalid arguments).
