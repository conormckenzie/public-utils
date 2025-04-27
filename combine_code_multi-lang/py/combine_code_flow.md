graph TD
    A[Start] --> B{Check for command-line arguments?};
    B -- Yes --> C[Non-interactive Mode];
    C --> D{Validate root_dir?};
    D -- Yes --> E[Load patterns based on mode and root_dir];
    D -- No --> F[Exit with error];
    B -- No --> G[Interactive Mode];
    G --> H{Prompt user for mode?};
    H --> I{Prompt user for root_dir?};
    I --> J[Load patterns based on mode];
    E --> K[Prepare run parameters];
    J --> K;
    K --> L[Generate directory structure];
    L --> M[Combine files];
    M --> N[Append directory structure to output];
    N --> O[Print completion message];
    O --> P[End];
    F --> P;

    subgraph Non-interactive Flow
        C; D; E; F;
    end

    subgraph Interactive Flow
        G; H; I; J;
    end

    subgraph Core Processing
        K; L; M; N; O;
    end
```

### Explanation of Flow:

1.  **Start:** The script begins execution.
2.  **Check for command-line arguments?:** The script checks if a `root_dir` argument was provided.
3.  **Non-interactive Mode:** If arguments are present, it enters the non-interactive flow.
4.  **Validate root\_dir?:** It checks if the provided `root_dir` exists.
5.  **Load patterns based on mode and root\_dir:** If the `root_dir` is valid, it loads patterns from `.copyignore` or `.copyinclude` relative to the `root_dir`.
6.  **Exit with error:** If the `root_dir` is invalid, the script exits.
7.  **Interactive Mode:** If no command-line arguments are present, it enters the interactive flow.
8.  **Prompt user for mode?:** The user is prompted to choose between blacklist and whitelist modes.
9.  **Prompt user for root\_dir?:** The user is prompted to enter the root directory, with recent paths offered.
10. **Load patterns based on mode:** Patterns are loaded from `.copyignore` or `.copyinclude` in the current working directory.
11. **Prepare run parameters:** Run parameters (root directory, mode, filter setting, debug mode) are collected.
12. **Generate directory structure:** The directory and file structure is generated, applying the filter if specified.
13. **Combine files:** The contents of the relevant files are combined into the output file.
14. **Append directory structure to output:** The generated directory structure is added to the end of the output file.
15. **Print completion message:** A message indicating the output file location is printed.
