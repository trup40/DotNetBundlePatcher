using AsmResolver.DotNet.Bundles;
using AsmResolver.PE.File;
using AsmResolver.PE.File.Headers;
using System;
using System.IO;
using System.Linq;

class DotNetBundlePatcher
{
    private const string InputFolder = "in";
    private const string WorkFolder = "work";
    private const string OutputFolder = "out";
    private const string Version = "1.0.0";

    static void Main()
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        ShowMainMenu();
    }

    static void ShowMainMenu()
    {
        Console.Clear();
        PrintHeader("📦 .NET BUNDLE PATCHER", ConsoleColor.Cyan);

        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine($"  Version {Version}");
        Console.ResetColor();
        Console.WriteLine();

        PrintOption("1", "Extract Bundle", ConsoleColor.Green);
        Console.WriteLine();

        PrintOption("2", "Repack Bundle", ConsoleColor.Yellow);
        Console.WriteLine();

        PrintOption("3", "Show Bundle Details", ConsoleColor.Magenta);
        Console.WriteLine();

        PrintOption("4", "Clean Work Folder", ConsoleColor.Red);
        Console.WriteLine();

        PrintOption("5", "Batch Operations", ConsoleColor.Cyan);
        Console.WriteLine();

        PrintOption("X", "Exit", ConsoleColor.Gray);

        Console.WriteLine();
        PrintSeparator();
        Console.Write("Your choice: ");

        string? choice = Console.ReadLine()?.Trim().ToLower();

        switch (choice)
        {
            case "1":
                ExtractMenu();
                break;
            case "2":
                RepackMenu();
                break;
            case "3":
                ShowDetailsMenu();
                break;
            case "4":
                CleanWorkFolder();
                break;
            case "5":
                BatchOperationsMenu();
                break;
            case "x":
                Environment.Exit(0);
                break;
            default:
                Console.Clear();
                LogError("Invalid selection!");
                WaitForKey();
                ShowMainMenu();
                break;
        }
    }

    static void ExtractMenu()
    {
        Console.Clear();
        EnsureDirectoryExists(InputFolder);

        var files = GetFilesFromDirectory(InputFolder);
        if (files.Length == 0)
        {
            LogWarning($"No files found in '{InputFolder}' folder!");
            LogInfo("Please add bundle files to 'in' folder and try again.");
            WaitForKey();
            ShowMainMenu();
            return;
        }

        PrintHeader("📂 EXTRACT BUNDLE", ConsoleColor.Green);
        LogInfo($"Total {files.Length} file(s) found\n");

        DisplayFileList(files);

        Console.WriteLine();
        PrintOption("A", "Extract All", ConsoleColor.Cyan);
        PrintOption("B", "Back to Main Menu", ConsoleColor.Gray);
        Console.WriteLine();
        Console.Write("Your choice (number or A/B): ");

        string? choice = Console.ReadLine()?.Trim().ToLower();

        if (choice == "b")
        {
            ShowMainMenu();
            return;
        }

        if (choice == "a")
        {
            ExtractAllBundles(files);
            return;
        }

        if (int.TryParse(choice, out int index) && index >= 0 && index < files.Length)
        {
            ExtractBundle(files[index]);
        }
        else
        {
            Console.Clear();
            LogError("Invalid selection!");
            WaitForKey();
            ExtractMenu();
        }
    }

    static void ExtractAllBundles(FileInfo[] files)
    {
        Console.Clear();
        PrintHeader($"📦 EXTRACTING {files.Length} FILE(S)", ConsoleColor.Green);

        int success = 0;
        int failed = 0;

        foreach (var file in files)
        {
            Console.WriteLine();
            LogInfo($"Processing: {file.Name}");

            if (ExtractBundleInternal(file, false))
                success++;
            else
                failed++;
        }

        Console.WriteLine();
        PrintSeparator();
        LogSuccess($"✓ Successful: {success}");
        if (failed > 0)
            LogError($"✗ Failed: {failed}");

        WaitForKey();
        ShowMainMenu();
    }

    static void ExtractBundle(FileInfo file)
    {
        Console.Clear();
        PrintHeader($"📂 {file.Name}", ConsoleColor.Green);

        string workPath = Path.Combine(WorkFolder, file.Name);

        if (Directory.Exists(workPath))
        {
            LogWarning($"Work folder exists for '{file.Name}'!");
            Console.Write("Overwrite? (Y/N): ");

            if (Console.ReadLine()?.Trim().ToLower() != "y")
            {
                ExtractMenu();
                return;
            }

            try
            {
                Directory.Delete(workPath, true);
                LogSuccess("Old folder deleted.");
            }
            catch (Exception ex)
            {
                LogError($"Cannot delete folder: {ex.Message}");
                WaitForKey();
                ExtractMenu();
                return;
            }
        }

        ExtractBundleInternal(file, true);
        WaitForKey();
        ShowMainMenu();
    }

    static bool ExtractBundleInternal(FileInfo file, bool showDetails)
    {
        try
        {
            string workPath = Path.Combine(WorkFolder, file.Name);
            Directory.CreateDirectory(workPath);

            var manifest = BundleManifest.FromFile(file.FullName);

            if (showDetails)
            {
                LogInfo($"Bundle contains: {manifest.Files.Count} file(s)");
                Console.WriteLine();
            }

            int current = 0;
            foreach (var bundleFile in manifest.Files)
            {
                current++;
                string outputPath = Path.Combine(workPath, bundleFile.RelativePath);

                string? directory = Path.GetDirectoryName(outputPath);
                if (!string.IsNullOrEmpty(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                byte[] contents = bundleFile.GetData();

                if (showDetails)
                {
                    Console.Write($"{GetTimestamp()} > [{current}/{manifest.Files.Count}] ");
                    Console.ForegroundColor = ConsoleColor.White;
                    Console.Write(bundleFile.RelativePath);
                    Console.ResetColor();
                    Console.WriteLine($" ({FormatFileSize(contents.Length)})");
                }

                File.WriteAllBytes(outputPath, contents);
            }

            if (showDetails)
            {
                Console.WriteLine();
                LogSuccess($"✓ All files extracted to '{workPath}'");
                LogWarning("! For repacking: Keep only modified files in work folder!");
            }

            return true;
        }
        catch (Exception ex)
        {
            LogError($"Error: {ex.Message}");
            return false;
        }
    }

    static void RepackMenu()
    {
        Console.Clear();

        if (!Directory.Exists(WorkFolder))
        {
            LogWarning($"'{WorkFolder}' folder not found!");
            LogInfo("You need to extract a bundle first.");
            WaitForKey();
            ShowMainMenu();
            return;
        }

        var dirs = GetDirectoriesFromPath(WorkFolder);
        if (dirs.Length == 0)
        {
            LogWarning($"No work folders found in '{WorkFolder}'!");
            WaitForKey();
            ShowMainMenu();
            return;
        }

        PrintHeader("🔧 REPACK BUNDLE", ConsoleColor.Yellow);
        LogInfo($"Total {dirs.Length} work folder(s) found\n");

        DisplayDirectoryList(dirs);

        Console.WriteLine();
        PrintOption("B", "Back to Main Menu", ConsoleColor.Gray);
        Console.WriteLine();
        Console.Write("Your choice: ");

        string? choice = Console.ReadLine()?.Trim().ToLower();

        if (choice == "b")
        {
            ShowMainMenu();
            return;
        }

        if (int.TryParse(choice, out int index) && index >= 0 && index < dirs.Length)
        {
            RepackBundle(dirs[index]);
        }
        else
        {
            Console.Clear();
            LogError("Invalid selection!");
            WaitForKey();
            RepackMenu();
        }
    }

    static void RepackBundle(DirectoryInfo workDir)
    {
        Console.Clear();
        PrintHeader($"🔧 {workDir.Name}", ConsoleColor.Yellow);

        string inputBundlePath = Path.Combine(InputFolder, workDir.Name);
        if (!File.Exists(inputBundlePath))
        {
            LogError($"Original bundle not found: {inputBundlePath}");
            LogInfo("Original file must be in 'in' folder for repacking.");
            WaitForKey();
            RepackMenu();
            return;
        }

        var modifiedFiles = workDir.GetFiles("*.*", SearchOption.AllDirectories);

        LogInfo($"Files to be updated: {modifiedFiles.Length}\n");

        foreach (var file in modifiedFiles)
        {
            string relativePath = Path.GetRelativePath(workDir.FullName, file.FullName);
            Console.Write($"{GetTimestamp()} > ");
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write(relativePath);
            Console.ResetColor();
            Console.WriteLine($" ({FormatFileSize(file.Length)})");
        }

        Console.WriteLine();
        Console.Write("Start repacking? (Y/N): ");

        if (Console.ReadLine()?.Trim().ToLower() != "y")
        {
            RepackMenu();
            return;
        }

        try
        {
            Console.Clear();
            PrintHeader($"🔧 REPACKING: {workDir.Name}", ConsoleColor.Yellow);

            LogInfo("Reading bundle manifest...");
            var manifest = BundleManifest.FromFile(inputBundlePath);

            int updated = 0;
            int notFound = 0;

            foreach (var modifiedFile in modifiedFiles)
            {
                string relativePath = Path.GetRelativePath(workDir.FullName, modifiedFile.FullName);

                var targetFile = manifest.Files.FirstOrDefault(x =>
                    x.RelativePath.Equals(relativePath, StringComparison.OrdinalIgnoreCase));

                if (targetFile != null)
                {
                    Console.Write($"{GetTimestamp()} > ");
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.Write("✓ ");
                    Console.ResetColor();
                    Console.WriteLine($"{targetFile.RelativePath} updated");

                    targetFile.Contents = new AsmResolver.DataSegment(
                        File.ReadAllBytes(modifiedFile.FullName));
                    updated++;
                }
                else
                {
                    Console.Write($"{GetTimestamp()} > ");
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.Write("✗ ");
                    Console.ResetColor();
                    Console.WriteLine($"{relativePath} not found in bundle!");
                    notFound++;
                }
            }

            EnsureDirectoryExists(OutputFolder);

            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string outputPath = Path.Combine(OutputFolder,
                workDir.Name.Replace(".exe", $"_patched_{timestamp}.exe"));

            Console.WriteLine();
            LogInfo("Writing bundle to disk...");

            manifest.WriteUsingTemplate(
                outputPath,
                BundlerParameters.FromExistingBundle(
                    originalFile: inputBundlePath,
                    appBinaryPath: manifest.Files[0].RelativePath));

            Console.WriteLine();
            PrintSeparator();
            LogSuccess($"✓ Successfully completed!");
            LogInfo($"Output: {outputPath}");
            LogSuccess($"Updated: {updated}");
            if (notFound > 0)
                LogWarning($"Not found: {notFound}");
        }
        catch (Exception ex)
        {
            LogError($"Error occurred: {ex.Message}");
        }

        WaitForKey();
        ShowMainMenu();
    }

    static void ShowDetailsMenu()
    {
        Console.Clear();
        EnsureDirectoryExists(InputFolder);

        var files = GetFilesFromDirectory(InputFolder);
        if (files.Length == 0)
        {
            LogWarning($"No files found in '{InputFolder}' folder!");
            WaitForKey();
            ShowMainMenu();
            return;
        }

        PrintHeader("🔍 BUNDLE DETAILS", ConsoleColor.Magenta);
        DisplayFileList(files);

        Console.WriteLine();
        PrintOption("B", "Back to Main Menu", ConsoleColor.Gray);
        Console.WriteLine();
        Console.Write("Your choice: ");

        string? choice = Console.ReadLine()?.Trim().ToLower();

        if (choice == "b")
        {
            ShowMainMenu();
            return;
        }

        if (int.TryParse(choice, out int index) && index >= 0 && index < files.Length)
        {
            ShowBundleDetails(files[index]);
        }
        else
        {
            Console.Clear();
            LogError("Invalid selection!");
            WaitForKey();
            ShowDetailsMenu();
        }
    }

    static void ShowBundleDetails(FileInfo file)
    {
        Console.Clear();
        PrintHeader($"📋 {file.Name}", ConsoleColor.Magenta);

        try
        {
            var manifest = BundleManifest.FromFile(file.FullName);
            var peFile = PEFile.FromFile(file.FullName);

            LogInfo($"File size: {FormatFileSize(file.Length)}");
            LogInfo($"Bundle version: {manifest.MajorVersion}.{manifest.MinorVersion}");

            string architecture = GetArchitecture(peFile.FileHeader.Machine);
            LogInfo($"Architecture: {architecture}");

            string dotnetVersion = GetDotNetVersion(manifest.MajorVersion, manifest.MinorVersion);
            LogInfo($".NET Version: {dotnetVersion}");

            LogInfo($"Content count: {manifest.Files.Count}");
            Console.WriteLine();

            PrintSeparator();
            Console.WriteLine("CONTENT LIST:\n");

            int i = 1;
            long totalSize = 0;
            foreach (var bundleFile in manifest.Files)
            {
                byte[] data = bundleFile.GetData();
                totalSize += data.Length;

                Console.Write($"{GetTimestamp()} > {i,3}. ");
                Console.ForegroundColor = ConsoleColor.White;
                Console.Write(bundleFile.RelativePath.PadRight(45));
                Console.ResetColor();
                Console.Write($" {FormatFileSize(data.Length).PadLeft(10)}");
                Console.Write($"  [{bundleFile.Type}]");
                Console.WriteLine();
                i++;
            }

            Console.WriteLine();
            PrintSeparator();
            LogInfo($"Total content size: {FormatFileSize(totalSize)}");
        }
        catch (Exception ex)
        {
            LogError($"Cannot read bundle: {ex.Message}");
        }

        WaitForKey();
        ShowDetailsMenu();
    }

    static void BatchOperationsMenu()
    {
        Console.Clear();
        PrintHeader("⚡ BATCH OPERATIONS", ConsoleColor.Cyan);

        Console.WriteLine();
        PrintOption("1", "Extract All Bundles", ConsoleColor.Green);
        Console.WriteLine();

        PrintOption("2", "Compare Bundle Versions", ConsoleColor.Yellow);
        Console.WriteLine();

        PrintOption("3", "Verify Bundle Integrity", ConsoleColor.Magenta);
        Console.WriteLine();

        PrintOption("B", "Back to Main Menu", ConsoleColor.Gray);

        Console.WriteLine();
        PrintSeparator();
        Console.Write("Your choice: ");

        string? choice = Console.ReadLine()?.Trim().ToLower();

        switch (choice)
        {
            case "1":
                var files = GetFilesFromDirectory(InputFolder);
                if (files.Length > 0)
                    ExtractAllBundles(files);
                else
                {
                    Console.Clear();
                    LogWarning("No files to extract!");
                    WaitForKey();
                    BatchOperationsMenu();
                }
                break;
            case "2":
                CompareBundleVersions();
                break;
            case "3":
                VerifyBundleIntegrity();
                break;
            case "b":
                ShowMainMenu();
                break;
            default:
                Console.Clear();
                LogError("Invalid selection!");
                WaitForKey();
                BatchOperationsMenu();
                break;
        }
    }

    static void CompareBundleVersions()
    {
        Console.Clear();
        PrintHeader("📊 COMPARE BUNDLE VERSIONS", ConsoleColor.Yellow);

        var files = GetFilesFromDirectory(InputFolder);
        if (files.Length < 2)
        {
            LogWarning("At least 2 bundles required for comparison!");
            WaitForKey();
            BatchOperationsMenu();
            return;
        }

        LogInfo($"Found {files.Length} bundle(s)\n");

        foreach (var file in files)
        {
            try
            {
                var manifest = BundleManifest.FromFile(file.FullName);
                var peFile = PEFile.FromFile(file.FullName);

                Console.Write($"{GetTimestamp()} > ");
                Console.ForegroundColor = ConsoleColor.White;
                Console.Write(file.Name.PadRight(40));
                Console.ResetColor();
                Console.Write($" | Bundle v{manifest.MajorVersion}.{manifest.MinorVersion}");
                Console.Write($" | {GetArchitecture(peFile.FileHeader.Machine)}");
                Console.Write($" | {manifest.Files.Count} files");
                Console.WriteLine();
            }
            catch (Exception ex)
            {
                LogError($"Cannot read {file.Name}: {ex.Message}");
            }
        }

        WaitForKey();
        BatchOperationsMenu();
    }

    static void VerifyBundleIntegrity()
    {
        Console.Clear();
        PrintHeader("🔍 VERIFY BUNDLE INTEGRITY", ConsoleColor.Magenta);

        var files = GetFilesFromDirectory(InputFolder);
        if (files.Length == 0)
        {
            LogWarning("No bundles to verify!");
            WaitForKey();
            BatchOperationsMenu();
            return;
        }

        int valid = 0;
        int invalid = 0;

        foreach (var file in files)
        {
            Console.Write($"{GetTimestamp()} > Checking {file.Name}... ");

            try
            {
                var manifest = BundleManifest.FromFile(file.FullName);
                var peFile = PEFile.FromFile(file.FullName);

                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("✓ Valid");
                Console.ResetColor();
                valid++;
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"✗ Invalid: {ex.Message}");
                Console.ResetColor();
                invalid++;
            }
        }

        Console.WriteLine();
        PrintSeparator();
        LogSuccess($"Valid: {valid}");
        if (invalid > 0)
            LogError($"Invalid: {invalid}");

        WaitForKey();
        BatchOperationsMenu();
    }

    static void CleanWorkFolder()
    {
        Console.Clear();
        PrintHeader("🗑️ CLEAN WORK FOLDER", ConsoleColor.Red);

        if (!Directory.Exists(WorkFolder))
        {
            LogInfo("Nothing to clean.");
            WaitForKey();
            ShowMainMenu();
            return;
        }

        var dirs = GetDirectoriesFromPath(WorkFolder);

        if (dirs.Length == 0)
        {
            LogInfo("Work folder is empty.");
            WaitForKey();
            ShowMainMenu();
            return;
        }

        LogInfo($"Found {dirs.Length} work folder(s)\n");

        DisplayDirectoryList(dirs);

        Console.WriteLine();
        PrintOption("A", "Delete All", ConsoleColor.Red);
        PrintOption("B", "Back to Main Menu", ConsoleColor.Gray);
        Console.WriteLine();
        Console.Write("Your choice (number or A/B): ");

        string? choice = Console.ReadLine()?.Trim().ToLower();

        if (choice == "b")
        {
            ShowMainMenu();
            return;
        }

        if (choice == "a")
        {
            Console.WriteLine();
            LogWarning($"All {dirs.Length} work folder(s) will be deleted!");
            Console.Write("Are you sure? (Y/N): ");

            if (Console.ReadLine()?.Trim().ToLower() == "y")
            {
                try
                {
                    int deleted = 0;
                    foreach (var dir in dirs)
                    {
                        try
                        {
                            Directory.Delete(dir.FullName, true);
                            LogSuccess($"✓ Deleted: {dir.Name}");
                            deleted++;
                        }
                        catch (Exception ex)
                        {
                            LogError($"✗ Failed to delete {dir.Name}: {ex.Message}");
                        }
                    }
                    Console.WriteLine();
                    LogSuccess($"Total deleted: {deleted}/{dirs.Length}");
                }
                catch (Exception ex)
                {
                    LogError($"Error: {ex.Message}");
                }
            }
        }
        else if (int.TryParse(choice, out int index) && index >= 0 && index < dirs.Length)
        {
            var selectedDir = dirs[index];
            Console.WriteLine();
            LogWarning($"'{selectedDir.Name}' will be deleted!");
            Console.Write("Are you sure? (Y/N): ");

            if (Console.ReadLine()?.Trim().ToLower() == "y")
            {
                try
                {
                    Directory.Delete(selectedDir.FullName, true);
                    LogSuccess($"✓ Deleted: {selectedDir.Name}");
                }
                catch (Exception ex)
                {
                    LogError($"Error: {ex.Message}");
                }
            }
        }
        else
        {
            Console.Clear();
            LogError("Invalid selection!");
        }

        WaitForKey();
        CleanWorkFolder();
    }

    // Helper Methods
    static string GetTimestamp()
    {
        return DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
    }

    static string GetArchitecture(MachineType machine)
    {
        return machine switch
        {
            MachineType.I386 => "x86 (32-bit)",
            MachineType.Amd64 => "x64 (64-bit)",
            MachineType.Arm => "ARM (32-bit)",
            MachineType.Arm64 => "ARM64 (64-bit)",
            _ => $"Unknown ({machine})"
        };
    }

    static string GetDotNetVersion(uint major, uint minor)
    {
        return (major, minor) switch
        {
            (1, 0) => ".NET Core 3.0 / 3.1",
            (2, 0) => ".NET 5.0",
            (6, 0) => ".NET 6.0 (LTS)",
            (7, 0) => ".NET 7.0",
            (8, 0) => ".NET 8.0 (LTS)",
            (9, 0) => ".NET 9.0",
            (10, 0) => ".NET 10.0",
            _ => $"Unknown (v{major}.{minor})"
        };
    }

    static void EnsureDirectoryExists(string path)
    {
        if (!Directory.Exists(path))
            Directory.CreateDirectory(path);
    }

    static FileInfo[] GetFilesFromDirectory(string path)
    {
        var dir = new DirectoryInfo(path);
        return dir.Exists ? dir.GetFiles("*.*") : Array.Empty<FileInfo>();
    }

    static DirectoryInfo[] GetDirectoriesFromPath(string path)
    {
        var dir = new DirectoryInfo(path);
        return dir.Exists ? dir.GetDirectories() : Array.Empty<DirectoryInfo>();
    }

    static void DisplayFileList(FileInfo[] files)
    {
        PrintSeparator();
        for (int i = 0; i < files.Length; i++)
        {
            Console.Write($"  {i}. ");
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write(files[i].Name.PadRight(45));
            Console.ResetColor();
            Console.WriteLine($" ({FormatFileSize(files[i].Length)})");
        }
        PrintSeparator();
    }

    static void DisplayDirectoryList(DirectoryInfo[] dirs)
    {
        PrintSeparator();
        for (int i = 0; i < dirs.Length; i++)
        {
            var files = dirs[i].GetFiles("*.*", SearchOption.AllDirectories);
            Console.Write($"  {i}. ");
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write(dirs[i].Name.PadRight(45));
            Console.ResetColor();
            Console.WriteLine($" ({files.Length} file(s))");
        }
        PrintSeparator();
    }

    static string FormatFileSize(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB" };
        double len = bytes;
        int order = 0;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len /= 1024;
        }
        return $"{len:0.##} {sizes[order]}";
    }

    static void PrintHeader(string text, ConsoleColor color)
    {
        PrintSeparator('═');
        Console.ForegroundColor = color;
        Console.WriteLine($"  {text}");
        Console.ResetColor();
        PrintSeparator('═');
    }

    static void PrintSeparator(char c = '─')
    {
        Console.WriteLine(new string(c, 80));
    }

    static void PrintOption(string key, string text, ConsoleColor color)
    {
        Console.Write("  [");
        Console.ForegroundColor = color;
        Console.Write(key);
        Console.ResetColor();
        Console.WriteLine($"] {text}");
    }

    static void LogSuccess(string message)
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"{GetTimestamp()} > {message}");
        Console.ResetColor();
    }

    static void LogError(string message)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"{GetTimestamp()} > {message}");
        Console.ResetColor();
    }

    static void LogWarning(string message)
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine($"{GetTimestamp()} > {message}");
        Console.ResetColor();
    }

    static void LogInfo(string message)
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine($"{GetTimestamp()} > {message}");
        Console.ResetColor();
    }

    static void WaitForKey()
    {
        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine("Press any key to continue...");
        Console.ResetColor();
        Console.ReadKey(true);
        Console.Clear();
    }
}