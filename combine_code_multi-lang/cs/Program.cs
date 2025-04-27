using System;
using System.IO;
using combine_code_multi_lang.cs; // Assuming the namespace is combine_code_multi_lang.cs

public class Program
{
    public static void Main(string[] args)
    {
        if (args.Length == 0)
        {
            Console.WriteLine("Usage: dotnet run <root_directory> [filter_file_path] [mode]");
            Console.WriteLine("Modes: blacklist (default), whitelist");
            return;
        }

        string rootDir = args[0];
        string filterFilePath = Path.Combine(rootDir, ".copyignore"); // Default filter file
        bool isBlacklist = true; // Default mode

        if (args.Length > 1)
        {
            filterFilePath = args[1];
        }

        if (args.Length > 2)
        {
            if (args[2].ToLower() == "whitelist")
            {
                isBlacklist = false;
            }
            else if (args[2].ToLower() != "blacklist")
            {
                Console.WriteLine($"Invalid mode: {args[2]}. Using default mode: blacklist.");
            }
        }

        if (!Directory.Exists(rootDir))
        {
            Console.WriteLine($"Error: Root directory not found: {rootDir}");
            return;
        }

        try
        {
            FileFilter fileFilter = new FileFilter(filterFilePath, isBlacklist);
            CodeCombiner combiner = new CodeCombiner(rootDir, fileFilter);
            string combinedContent = combiner.Combine();

            string outputFileName = "combined_code.txt";
            File.WriteAllText(outputFileName, combinedContent);

            Console.WriteLine($"Code combined successfully to {outputFileName}");
        }
        catch (DirectoryNotFoundException ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An unexpected error occurred: {ex.Message}");
        }
    }
}
