# Combine Code Utility - Language Agnostic Description

This document describes the core functionality and behavior of the `combine_code` utility in a language-agnostic manner. The purpose is to provide a clear specification that can be used to implement the utility in various programming languages while maintaining consistent behavior.

## Purpose

**Required:** The `combine_code` utility must consolidate source code files from a specified root directory into a single output file. It must provide filtering capabilities to include or exclude files based on patterns defined in configuration files, and it must append a representation of the directory structure of the included files to the output.

## Functionality

**Required:** The utility must operate in a **Non-interactive Mode** where it is executed with command-line arguments specifying the root directory and optionally the filter file path and mode.

**Desirable (Flexible):** The utility *should* also support an **Interactive Mode** where, if no command-line arguments are provided, it prompts the user for the necessary information (root directory, mode, filter file). Implementations may prioritize the non-interactive mode if interactive mode is complex or less relevant for the target language/environment.

## Inputs

**Required:**
-   **Root Directory (`root_dir`):** The starting directory from which to recursively search for code files. This is a required input.

**Desirable (Flexible):**
-   **Filter File Path (Optional):** The path to a file containing patterns for filtering files. If not provided, the utility *should* look for a default filter file (e.g., `.copyignore` or `.copyinclude`) within the `root_dir`. Implementations may choose a different default location or require the path if language constraints make defaulting difficult.
-   **Mode (Optional):** Specifies how the patterns in the filter file should be interpreted.
    -   `blacklist` (Default): Patterns specify files/directories to *exclude*.
    -   `whitelist`: Patterns specify files/directories to *include*. Implementations *should* support both modes, with `blacklist` as the default if no mode is specified.

## Filte

**Required:** The utility must use a filter file (`.copyignore` for blacklist, `.copyinclude` for whitelist) located relative to the `root_dir` or specified explicitly. Each non-empty, non-comment line (lines not starting with `#`) in the filter file must represent a pattern.

**Desirable (Flexible):** The pattern matching *should ideally* support glob-like patterns (e.g., `*.cs`, `src/`, `node_modules/`). Implementations may start with simpler pattern matching (like substring containment) and evolve to more sophisticated methods if needed. The filtering logic must determine whether a given file path should be processed based on the loaded patterns and the selected mode.

**Required:**
-   **Blacklist Mode:** A file must be included if its path *does not* match any pattern in the filter file.
-   **Whitelist Mode:** A file must be included if its path *matches* at least one pattern in the filter file.

**Desirable (Flexible):** Directory filtering *should* also be considered, preventing traversal into excluded directories in blacklist mode or only traversing into included directories in whitelist mode.

## Processing

**Required:**
1.  **Input Validation:** Validate that the specified `root_dir` exists.
2.  **Filter Loading:** Load patterns from the specified or default filter file.
3.  **File Traversal:** Recursively traverse the `root_dir`.
4.  **File Filtering:** For each file encountered, apply the filtering logic based on the loaded patterns and mode to determine if it should be included.
5.  **Content Combination:** For each included file:
    -   Append a header indicating the relative path of the file (e.g., `--- File: path/to/file.ext ---`). The exact format of the header can be flexible, but it must clearly identify the file path relative to the root.
    -   Append the entire content of the file.
    -   Append a separator (e.g., a blank line) after the file content. The exact separator can be flexible.
6.  **Structure Generation:** Generate a text-based representation of the directory structure, showing only the directories and files that were *included* in the combination process. The exact format of the structure representation can be flexible (e.g., tree-like, list).
7.  **Output Assembly:** Combine the accumulated file contents and the generated directory structure into a single output string. Append a clear separator before the directory structure (e.g., `--- Directory Structure ---`). The exact separator can be flexible.

## Output

**Required:** The final combined content (file contents + directory structure) must be written to a single output file (e.g., `combined_code.txt`) in the directory where the utility is executed. The default output file name can be flexible if necessary.

## Error Handling

**Required:** The utility must handle potential errors gracefully, such as:
-   Root directory not found.
-   File reading errors during content combination.

**Desirable (Flexible):** Handling of a missing filter file *should* result in no filtering being applied rather than a fatal error.

## Language-Specific Considerations

Implementations in different languages must adhere to the **Required** specifications while leveraging language-specific features and best practices for file system operations, argument parsing, and string manipulation. The **Desirable (Flexible)** specifications represent ideal behavior but can be adapted if language constraints or other considerations warrant it. The goal is functional equivalence for the core required features across implementations.
