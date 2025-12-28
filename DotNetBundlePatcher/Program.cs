using AsmResolver.DotNet.Bundles;
using AsmResolver.PE.File;
using AsmResolver.PE.File.Headers;
using System.Text;
using System.Security.Cryptography;
using System.Text.Json;
using System.Xml.Linq;

class DotNetBundlePatcher
{
    private const string InputFolder = "in";
    private const string WorkFolder = "work";
    private const string OutputFolder = "out";
    private const string LogFolder = "logs";
    private const string BackupFolder = "backup";
    private const string ExportFolder = "exports";
    private const string Version = "2.0.0";
    private static string? currentLogFile;

    static void Main()
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        InitializeLogger();
        ShowMainMenu();
    }

    static void InitializeLogger()
    {
        EnsureDirectoryExists(LogFolder);
        string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        currentLogFile = Path.Combine(LogFolder, $"session_{timestamp}.log");
        WriteLog("=== Session Started ===");
    }

    static void WriteLog(string message)
    {
        try
        {
            if (!string.IsNullOrEmpty(currentLogFile))
            {
                string logEntry = $"{GetTimestamp()} > {message}";
                File.AppendAllText(currentLogFile, logEntry + Environment.NewLine);
            }
        }
        catch { /* Ignore logging errors */ }
    }

    static void ShowMainMenu()
    {
        Console.Clear();
        PrintHeader("📦 .NET BUNDLE PATCHER", ConsoleColor.Cyan);

        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine($"  Version {Version}");
        Console.ResetColor();
        Console.WriteLine();

        ShowFirstTimeInfo();

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

        PrintOption("6", "Export Bundle Manifest", ConsoleColor.Blue);
        Console.WriteLine();

        PrintOption("7", "Search in Bundles", ConsoleColor.White);
        Console.WriteLine();

        PrintOption("8", "Compare Bundles", ConsoleColor.DarkYellow);
        Console.WriteLine();

        PrintOption("X", "Exit", ConsoleColor.Gray);

        Console.WriteLine();
        PrintSeparator();
        Console.Write("Your choice: ");

        string? choice = Console.ReadLine()?.Trim().ToLower();
        WriteLog($"User selected: {choice}");

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
            case "6":
                ExportManifestMenu();
                break;
            case "7":
                SearchInBundles();
                break;
            case "8":
                CompareBundlesMenu();
                break;
            case "x":
                WriteLog("=== Session Ended ===");
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

    static void ShowFirstTimeInfo()
    {
        if (!Directory.Exists(InputFolder) || GetFilesFromDirectory(InputFolder).Length == 0)
        {
            Console.ForegroundColor = ConsoleColor.DarkCyan;
            Console.WriteLine("  ℹ First time? Drop your bundle files into the 'in' folder!");
            Console.ResetColor();
            Console.WriteLine();
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

        WriteLog($"Batch extract completed: {success} success, {failed} failed");
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
            int totalFiles = manifest.Files.Count;

            if (showDetails)
            {
                LogInfo($"Bundle contains: {totalFiles} file(s)");
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
                    DrawProgressBar(current, totalFiles, bundleFile.RelativePath);
                }

                File.WriteAllBytes(outputPath, contents);
            }

            if (showDetails)
            {
                Console.WriteLine();
                Console.WriteLine();
                LogSuccess($"✓ All files extracted to '{workPath}'");
                LogWarning("! For repacking: Keep only modified files in work folder!");
            }

            WriteLog($"Extracted: {file.Name} ({totalFiles} files)");
            return true;
        }
        catch (Exception ex)
        {
            LogError($"Error: {ex.Message}");
            WriteLog($"Extract failed: {file.Name} - {ex.Message}");
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

        // Create backup
        CreateBackup(inputBundlePath);

        try
        {
            Console.Clear();
            PrintHeader($"🔧 REPACKING: {workDir.Name}", ConsoleColor.Yellow);

            LogInfo("Reading bundle manifest...");
            var manifest = BundleManifest.FromFile(inputBundlePath);

            int updated = 0;
            int notFound = 0;
            int totalFiles = modifiedFiles.Length;
            int current = 0;

            foreach (var modifiedFile in modifiedFiles)
            {
                current++;
                string relativePath = Path.GetRelativePath(workDir.FullName, modifiedFile.FullName);

                var targetFile = manifest.Files.FirstOrDefault(x =>
                    x.RelativePath.Equals(relativePath, StringComparison.OrdinalIgnoreCase));

                if (targetFile != null)
                {
                    DrawProgressBar(current, totalFiles, $"Updating: {targetFile.RelativePath}");

                    targetFile.Contents = new AsmResolver.DataSegment(
                        File.ReadAllBytes(modifiedFile.FullName));
                    updated++;
                }
                else
                {
                    Console.WriteLine();
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

            WriteLog($"Repacked: {workDir.Name} - {updated} files updated, {notFound} not found");
        }
        catch (Exception ex)
        {
            LogError($"Error occurred: {ex.Message}");
            WriteLog($"Repack failed: {workDir.Name} - {ex.Message}");
        }

        WaitForKey();
        ShowMainMenu();
    }

    static void CreateBackup(string filePath)
    {
        try
        {
            EnsureDirectoryExists(BackupFolder);
            string fileName = Path.GetFileName(filePath);
            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string backupPath = Path.Combine(BackupFolder, $"{Path.GetFileNameWithoutExtension(fileName)}_backup_{timestamp}{Path.GetExtension(fileName)}");

            LogInfo($"Creating backup...");
            File.Copy(filePath, backupPath, true);
            LogSuccess($"✓ Backup created: {Path.GetFileName(backupPath)}");
            WriteLog($"Backup created: {backupPath}");
            Console.WriteLine();
        }
        catch (Exception ex)
        {
            LogWarning($"! Backup failed: {ex.Message}");
            WriteLog($"Backup failed: {ex.Message}");
        }
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

            // Calculate hash
            string hash = CalculateFileHash(file.FullName);
            LogInfo($"SHA256: {hash}");

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

            Console.WriteLine();
            Console.Write("Show file hashes? (Y/N): ");
            if (Console.ReadLine()?.Trim().ToLower() == "y")
            {
                ShowFileHashes(manifest, file.FullName);
            }
        }
        catch (Exception ex)
        {
            LogError($"Cannot read bundle: {ex.Message}");
        }

        WaitForKey();
        ShowDetailsMenu();
    }

    static void ShowFileHashes(BundleManifest manifest, string bundleFileName)
    {
        Console.Clear();
        PrintHeader("🔐 FILE HASHES (SHA256)", ConsoleColor.Cyan);

        Console.WriteLine();
        PrintOption("1", "Show in Console", ConsoleColor.Green);
        PrintOption("2", "Export to File", ConsoleColor.Yellow);
        PrintOption("3", "Both", ConsoleColor.Cyan);
        PrintOption("4", "Verify with Saved Hashes", ConsoleColor.Magenta);
        Console.WriteLine();
        Console.Write("Your choice: ");

        string? choice = Console.ReadLine()?.Trim().ToLower();

        if (choice == "4")
        {
            VerifyBundleWithHashes(manifest, bundleFileName);
            return;
        }

        Console.Clear();
        PrintHeader("🔐 CALCULATING HASHES...", ConsoleColor.Cyan);
        Console.WriteLine();

        var hashData = new List<(string path, long size, string hash)>();
        int current = 0;
        int total = manifest.Files.Count;

        foreach (var bundleFile in manifest.Files)
        {
            current++;
            byte[] data = bundleFile.GetData();
            string hash = CalculateHash(data);
            hashData.Add((bundleFile.RelativePath, data.Length, hash));

            DrawProgressBar(current, total, bundleFile.RelativePath);
        }

        Console.WriteLine();
        Console.WriteLine();

        // Konsola yazdır
        if (choice == "1" || choice == "3")
        {
            Console.Clear();
            PrintHeader("🔐 FILE HASHES (SHA256)", ConsoleColor.Cyan);
            Console.WriteLine();

            foreach (var item in hashData)
            {
                Console.Write($"{GetTimestamp()} > ");
                Console.ForegroundColor = ConsoleColor.White;
                Console.Write(item.path.PadRight(50));
                Console.ResetColor();
                Console.WriteLine();

                Console.Write("      ");
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.Write($"Size: {FormatFileSize(item.size).PadRight(12)}");
                Console.ResetColor();
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"Hash: {item.hash}");
                Console.ResetColor();
                Console.WriteLine();
            }

            LogSuccess("✓ Hash calculation completed!");
        }

        // Dosyaya export et
        if (choice == "2" || choice == "3")
        {
            try
            {
                EnsureDirectoryExists(ExportFolder);
                string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                string baseName = Path.GetFileNameWithoutExtension(bundleFileName);
                string outputPath = Path.Combine(ExportFolder, $"{baseName}_hashes_{timestamp}.txt");

                var sb = new StringBuilder();
                sb.AppendLine("=".PadRight(100, '='));
                sb.AppendLine($"FILE HASHES (SHA256)");
                sb.AppendLine($"Bundle: {bundleFileName}");
                sb.AppendLine($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                sb.AppendLine($"Total Files: {hashData.Count}");
                sb.AppendLine("=".PadRight(100, '='));
                sb.AppendLine();

                foreach (var item in hashData)
                {
                    sb.AppendLine($"File: {item.path}");
                    sb.AppendLine($"Size: {FormatFileSize(item.size)} ({item.size:N0} bytes)");
                    sb.AppendLine($"SHA256: {item.hash}");
                    sb.AppendLine();
                }

                File.WriteAllText(outputPath, sb.ToString());

                if (choice == "2")
                {
                    Console.Clear();
                    PrintHeader("🔐 FILE HASHES", ConsoleColor.Cyan);
                    Console.WriteLine();
                }

                LogSuccess($"✓ Hashes exported to: {Path.GetFileName(outputPath)}");
                WriteLog($"Hash export completed: {outputPath}");
            }
            catch (Exception ex)
            {
                LogError($"Export failed: {ex.Message}");
            }
        }

        if (choice != "1" && choice != "2" && choice != "3")
        {
            LogError("Invalid choice!");
        }
    }

    static void VerifyBundleWithHashes(BundleManifest manifest, string bundleFileName)
    {
        Console.Clear();
        PrintHeader("🔍 VERIFY WITH SAVED HASHES", ConsoleColor.Magenta);

        EnsureDirectoryExists(ExportFolder);
        var hashFiles = Directory.GetFiles(ExportFolder, "*_hashes_*.txt");

        if (hashFiles.Length == 0)
        {
            LogWarning("No saved hash files found in exports folder!");
            LogInfo("Please export hashes first (option 2 or 3).");
            WaitForKey();
            return;
        }

        Console.WriteLine();
        LogInfo($"Found {hashFiles.Length} hash file(s)\n");

        for (int i = 0; i < hashFiles.Length; i++)
        {
            Console.Write($"  {i}. ");
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine(Path.GetFileName(hashFiles[i]));
            Console.ResetColor();
        }

        Console.WriteLine();
        Console.Write("Select hash file to verify against (number): ");
        string? choice = Console.ReadLine()?.Trim();

        if (!int.TryParse(choice, out int index) || index < 0 || index >= hashFiles.Length)
        {
            LogError("Invalid selection!");
            WaitForKey();
            return;
        }

        try
        {
            Console.Clear();
            PrintHeader("🔍 VERIFYING BUNDLE...", ConsoleColor.Magenta);
            Console.WriteLine();

            // Kaydedilmiş hash'leri oku
            var savedHashes = ParseHashFile(hashFiles[index]);

            LogInfo($"Loaded {savedHashes.Count} hash(es) from file");
            LogInfo($"Verifying {manifest.Files.Count} file(s) in current bundle...");
            Console.WriteLine();

            int current = 0;
            int total = manifest.Files.Count;
            int matched = 0;
            int modified = 0;
            int notInSaved = 0;
            var modifiedFiles = new List<(string path, string oldHash, string newHash, long size)>();

            foreach (var bundleFile in manifest.Files)
            {
                current++;
                byte[] data = bundleFile.GetData();
                string currentHash = CalculateHash(data);

                DrawProgressBar(current, total, bundleFile.RelativePath);

                if (savedHashes.TryGetValue(bundleFile.RelativePath, out var savedHash))
                {
                    if (currentHash == savedHash)
                    {
                        matched++;
                    }
                    else
                    {
                        modified++;
                        modifiedFiles.Add((bundleFile.RelativePath, savedHash, currentHash, data.Length));
                    }
                }
                else
                {
                    notInSaved++;
                }
            }

            Console.WriteLine();
            Console.WriteLine();
            PrintSeparator();
            Console.WriteLine("VERIFICATION RESULTS:");
            Console.WriteLine();

            LogSuccess($"✓ Matched:           {matched}");

            if (modified > 0)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"{GetTimestamp()} > ⚠ Modified:          {modified}");
                Console.ResetColor();
            }

            if (notInSaved > 0)
            {
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine($"{GetTimestamp()} > ℹ Not in saved:      {notInSaved}");
                Console.ResetColor();
            }

            // Kaydedilmiş hash'lerde olup bundle'da olmayan dosyaları bul
            var bundleFiles = manifest.Files.Select(f => f.RelativePath).ToHashSet();
            var missingFiles = savedHashes.Keys.Where(k => !bundleFiles.Contains(k)).ToList();

            if (missingFiles.Any())
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"{GetTimestamp()} > ✗ Missing in bundle: {missingFiles.Count}");
                Console.ResetColor();
            }

            // Değişen dosyaları göster
            if (modifiedFiles.Any())
            {
                Console.WriteLine();
                PrintSeparator();
                Console.WriteLine("MODIFIED FILES:");
                Console.WriteLine();

                foreach (var file in modifiedFiles)
                {
                    Console.Write($"{GetTimestamp()} > ");
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.Write("⚠ ");
                    Console.ResetColor();
                    Console.ForegroundColor = ConsoleColor.White;
                    Console.WriteLine(file.path);
                    Console.ResetColor();

                    Console.Write("      ");
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                    Console.WriteLine($"Size: {FormatFileSize(file.size)}");
                    Console.ResetColor();

                    Console.Write("      ");
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"Old:  {file.oldHash}");
                    Console.ResetColor();

                    Console.Write("      ");
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"New:  {file.newHash}");
                    Console.ResetColor();
                    Console.WriteLine();
                }
            }

            // Bundle'da olmayan dosyaları göster
            if (missingFiles.Any())
            {
                Console.WriteLine();
                PrintSeparator();
                Console.WriteLine("MISSING IN BUNDLE (exists in saved hashes):");
                Console.WriteLine();

                foreach (var file in missingFiles)
                {
                    Console.Write($"{GetTimestamp()} > ");
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.Write("✗ ");
                    Console.ResetColor();
                    Console.WriteLine(file);
                }
            }

            // Yeni eklenen dosyaları göster
            if (notInSaved > 0)
            {
                Console.WriteLine();
                PrintSeparator();
                Console.WriteLine("NEW FILES (not in saved hashes):");
                Console.WriteLine();

                foreach (var bundleFile in manifest.Files)
                {
                    if (!savedHashes.ContainsKey(bundleFile.RelativePath))
                    {
                        byte[] data = bundleFile.GetData();
                        Console.Write($"{GetTimestamp()} > ");
                        Console.ForegroundColor = ConsoleColor.Cyan;
                        Console.Write("+ ");
                        Console.ResetColor();
                        Console.Write(bundleFile.RelativePath);
                        Console.ForegroundColor = ConsoleColor.DarkGray;
                        Console.WriteLine($" ({FormatFileSize(data.Length)})");
                        Console.ResetColor();
                    }
                }
            }

            Console.WriteLine();
            PrintSeparator();

            if (modified == 0 && missingFiles.Count == 0 && notInSaved == 0)
            {
                LogSuccess("✓ Bundle integrity verified! All files match saved hashes.");
            }
            else
            {
                LogWarning("⚠ Bundle has been modified since hash export!");
            }

            WriteLog($"Hash verification completed: {matched} matched, {modified} modified, {notInSaved} new, {missingFiles.Count} missing");
        }
        catch (Exception ex)
        {
            LogError($"Verification failed: {ex.Message}");
            WriteLog($"Hash verification failed: {ex.Message}");
        }

        WaitForKey();
    }

    static Dictionary<string, string> ParseHashFile(string filePath)
    {
        var hashes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        try
        {
            var lines = File.ReadAllLines(filePath);
            string? currentFile = null;

            foreach (var line in lines)
            {
                if (line.StartsWith("File: "))
                {
                    currentFile = line.Substring(6).Trim();
                }
                else if (line.StartsWith("SHA256: ") && currentFile != null)
                {
                    string hash = line.Substring(8).Trim();
                    hashes[currentFile] = hash;
                    currentFile = null;
                }
            }
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to parse hash file: {ex.Message}");
        }

        return hashes;
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

    static void ExportManifestMenu()
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

        PrintHeader("📄 EXPORT BUNDLE MANIFEST", ConsoleColor.Blue);
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
            ExportManifest(files[index]);
        }
        else
        {
            Console.Clear();
            LogError("Invalid selection!");
            WaitForKey();
            ExportManifestMenu();
        }
    }

    static void ExportManifest(FileInfo file)
    {
        Console.Clear();
        PrintHeader($"📄 EXPORT: {file.Name}", ConsoleColor.Blue);

        Console.WriteLine();
        PrintOption("1", "Export as JSON", ConsoleColor.Green);
        PrintOption("2", "Export as XML", ConsoleColor.Yellow);
        PrintOption("3", "Export as TXT", ConsoleColor.Cyan);
        PrintOption("B", "Back", ConsoleColor.Gray);
        Console.WriteLine();
        Console.Write("Format: ");

        string? format = Console.ReadLine()?.Trim().ToLower();

        if (format == "b")
        {
            ExportManifestMenu();
            return;
        }

        try
        {
            EnsureDirectoryExists(ExportFolder);
            var manifest = BundleManifest.FromFile(file.FullName);
            var peFile = PEFile.FromFile(file.FullName);
            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string baseName = Path.GetFileNameWithoutExtension(file.Name);

            switch (format)
            {
                case "1":
                    ExportAsJson(manifest, peFile, file, Path.Combine(ExportFolder, $"{baseName}_{timestamp}.json"));
                    break;
                case "2":
                    ExportAsXml(manifest, peFile, file, Path.Combine(ExportFolder, $"{baseName}_{timestamp}.xml"));
                    break;
                case "3":
                    ExportAsTxt(manifest, peFile, file, Path.Combine(ExportFolder, $"{baseName}_{timestamp}.txt"));
                    break;
                default:
                    LogError("Invalid format!");
                    WaitForKey();
                    ExportManifest(file);
                    return;
            }

            LogSuccess("✓ Manifest exported successfully!");
            WriteLog($"Exported manifest: {file.Name} as {format}");
        }
        catch (Exception ex)
        {
            LogError($"Export failed: {ex.Message}");
            WriteLog($"Export failed: {file.Name} - {ex.Message}");
        }

        WaitForKey();
        ExportManifestMenu();
    }

    static void ExportAsJson(BundleManifest manifest, PEFile peFile, FileInfo file, string outputPath)
    {
        var data = new
        {
            FileName = file.Name,
            FileSize = file.Length,
            FileSizeFormatted = FormatFileSize(file.Length),
            SHA256 = CalculateFileHash(file.FullName),
            BundleVersion = $"{manifest.MajorVersion}.{manifest.MinorVersion}",
            Architecture = GetArchitecture(peFile.FileHeader.Machine),
            DotNetVersion = GetDotNetVersion(manifest.MajorVersion, manifest.MinorVersion),
            ExportDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
            FileCount = manifest.Files.Count,
            Files = manifest.Files.Select((f, idx) => new
            {
                Index = idx + 1,
                Path = f.RelativePath,
                Type = f.Type.ToString(),
                Size = f.GetData().Length,
                SizeFormatted = FormatFileSize(f.GetData().Length)
            }).ToList()
        };

        string json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(outputPath, json);
        LogInfo($"Saved to: {outputPath}");
    }

    static void ExportAsXml(BundleManifest manifest, PEFile peFile, FileInfo file, string outputPath)
    {
        var root = new XElement("Bundle",
            new XElement("FileName", file.Name),
            new XElement("FileSize", file.Length),
            new XElement("FileSizeFormatted", FormatFileSize(file.Length)),
            new XElement("SHA256", CalculateFileHash(file.FullName)),
            new XElement("BundleVersion", $"{manifest.MajorVersion}.{manifest.MinorVersion}"),
            new XElement("Architecture", GetArchitecture(peFile.FileHeader.Machine)),
            new XElement("DotNetVersion", GetDotNetVersion(manifest.MajorVersion, manifest.MinorVersion)),
            new XElement("ExportDate", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")),
            new XElement("FileCount", manifest.Files.Count),
            new XElement("Files",
                manifest.Files.Select((f, idx) => new XElement("File",
                    new XElement("Index", idx + 1),
                    new XElement("Path", f.RelativePath),
                    new XElement("Type", f.Type.ToString()),
                    new XElement("Size", f.GetData().Length),
                    new XElement("SizeFormatted", FormatFileSize(f.GetData().Length))
                ))
            )
        );

        var doc = new XDocument(new XDeclaration("1.0", "utf-8", "yes"), root);
        doc.Save(outputPath);
        LogInfo($"Saved to: {outputPath}");
    }

    static void ExportAsTxt(BundleManifest manifest, PEFile peFile, FileInfo file, string outputPath)
    {
        var sb = new StringBuilder();
        sb.AppendLine("=".PadRight(80, '='));
        sb.AppendLine($"BUNDLE MANIFEST: {file.Name}");
        sb.AppendLine("=".PadRight(80, '='));
        sb.AppendLine();
        sb.AppendLine($"File Name:       {file.Name}");
        sb.AppendLine($"File Size:       {FormatFileSize(file.Length)} ({file.Length:N0} bytes)");
        sb.AppendLine($"SHA256:          {CalculateFileHash(file.FullName)}");
        sb.AppendLine($"Bundle Version:  {manifest.MajorVersion}.{manifest.MinorVersion}");
        sb.AppendLine($"Architecture:    {GetArchitecture(peFile.FileHeader.Machine)}");
        sb.AppendLine($".NET Version:    {GetDotNetVersion(manifest.MajorVersion, manifest.MinorVersion)}");
        sb.AppendLine($"Export Date:     {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine($"File Count:      {manifest.Files.Count}");
        sb.AppendLine();
        sb.AppendLine("=".PadRight(80, '='));
        sb.AppendLine("CONTENT LIST");
        sb.AppendLine("=".PadRight(80, '='));
        sb.AppendLine();
        int idx = 1;
        foreach (var f in manifest.Files)
        {
            byte[] data = f.GetData();
            sb.AppendLine($"{idx,4}. {f.RelativePath}");
            sb.AppendLine($"      Type: {f.Type}");
            sb.AppendLine($"      Size: {FormatFileSize(data.Length)} ({data.Length:N0} bytes)");
            sb.AppendLine();
            idx++;
        }

        File.WriteAllText(outputPath, sb.ToString());
        LogInfo($"Saved to: {outputPath}");
    }

    static void SearchInBundles()
    {
        Console.Clear();
        PrintHeader("🔍 SEARCH IN BUNDLES", ConsoleColor.White);

        var files = GetFilesFromDirectory(InputFolder);
        if (files.Length == 0)
        {
            LogWarning($"No bundles found in '{InputFolder}' folder!");
            WaitForKey();
            ShowMainMenu();
            return;
        }

        LogInfo($"Found {files.Length} bundle(s)\n");

        Console.Write("Enter search term (file name): ");
        string? searchTerm = Console.ReadLine()?.Trim();

        if (string.IsNullOrEmpty(searchTerm))
        {
            LogError("Search term cannot be empty!");
            WaitForKey();
            ShowMainMenu();
            return;
        }

        Console.Clear();
        PrintHeader($"🔍 SEARCH RESULTS: \"{searchTerm}\"", ConsoleColor.White);

        int totalMatches = 0;

        foreach (var file in files)
        {
            try
            {
                var manifest = BundleManifest.FromFile(file.FullName);
                var matches = manifest.Files.Where(f =>
                    f.RelativePath.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)).ToList();

                if (matches.Any())
                {
                    Console.WriteLine();
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.WriteLine($"📦 {file.Name}");
                    Console.ResetColor();

                    foreach (var match in matches)
                    {
                        byte[] data = match.GetData();
                        Console.Write($"{GetTimestamp()} > ");
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.Write("✓ ");
                        Console.ResetColor();
                        Console.Write($"{match.RelativePath}");
                        Console.ForegroundColor = ConsoleColor.DarkGray;
                        Console.WriteLine($" ({FormatFileSize(data.Length)})");
                        Console.ResetColor();
                        totalMatches++;
                    }
                }
            }
            catch (Exception ex)
            {
                LogError($"Cannot search in {file.Name}: {ex.Message}");
            }
        }

        Console.WriteLine();
        PrintSeparator();
        if (totalMatches > 0)
            LogSuccess($"Found {totalMatches} match(es)");
        else
            LogWarning("No matches found");

        WriteLog($"Search completed: \"{searchTerm}\" - {totalMatches} matches");
        WaitForKey();
        ShowMainMenu();
    }

    static void CompareBundlesMenu()
    {
        Console.Clear();
        PrintHeader("📊 COMPARE BUNDLES", ConsoleColor.DarkYellow);

        var files = GetFilesFromDirectory(InputFolder);
        if (files.Length < 2)
        {
            LogWarning("At least 2 bundles required for comparison!");
            WaitForKey();
            ShowMainMenu();
            return;
        }

        DisplayFileList(files);

        Console.WriteLine();
        Console.Write("Select first bundle (number): ");
        string? choice1 = Console.ReadLine()?.Trim();

        Console.Write("Select second bundle (number): ");
        string? choice2 = Console.ReadLine()?.Trim();

        if (!int.TryParse(choice1, out int idx1) || !int.TryParse(choice2, out int idx2) ||
            idx1 < 0 || idx1 >= files.Length || idx2 < 0 || idx2 >= files.Length || idx1 == idx2)
        {
            LogError("Invalid selection!");
            WaitForKey();
            ShowMainMenu();
            return;
        }

        CompareBundles(files[idx1], files[idx2]);
    }

    static void CompareBundles(FileInfo file1, FileInfo file2)
    {
        Console.Clear();
        PrintHeader($"📊 COMPARING BUNDLES", ConsoleColor.DarkYellow);

        try
        {
            var manifest1 = BundleManifest.FromFile(file1.FullName);
            var manifest2 = BundleManifest.FromFile(file2.FullName);

            Console.WriteLine();
            LogInfo($"Bundle 1: {file1.Name}");
            LogInfo($"Bundle 2: {file2.Name}");
            Console.WriteLine();

            var files1 = manifest1.Files.Select(f => f.RelativePath).ToHashSet();
            var files2 = manifest2.Files.Select(f => f.RelativePath).ToHashSet();

            var onlyIn1 = files1.Except(files2).ToList();
            var onlyIn2 = files2.Except(files1).ToList();
            var inBoth = files1.Intersect(files2).ToList();

            PrintSeparator();
            Console.WriteLine("STATISTICS:");
            Console.WriteLine();
            LogInfo($"Files in Bundle 1:     {files1.Count}");
            LogInfo($"Files in Bundle 2:     {files2.Count}");
            LogSuccess($"Common files:          {inBoth.Count}");
            LogWarning($"Only in Bundle 1:      {onlyIn1.Count}");
            LogWarning($"Only in Bundle 2:      {onlyIn2.Count}");

            if (onlyIn1.Any())
            {
                Console.WriteLine();
                PrintSeparator();
                Console.WriteLine("FILES ONLY IN BUNDLE 1:");
                Console.WriteLine();
                foreach (var f in onlyIn1)
                {
                    Console.Write($"{GetTimestamp()} > ");
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"- {f}");
                    Console.ResetColor();
                }
            }

            if (onlyIn2.Any())
            {
                Console.WriteLine();
                PrintSeparator();
                Console.WriteLine("FILES ONLY IN BUNDLE 2:");
                Console.WriteLine();
                foreach (var f in onlyIn2)
                {
                    Console.Write($"{GetTimestamp()} > ");
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"+ {f}");
                    Console.ResetColor();
                }
            }

            Console.WriteLine();
            Console.Write("Show size differences for common files? (Y/N): ");
            if (Console.ReadLine()?.Trim().ToLower() == "y")
            {
                Console.WriteLine();
                PrintSeparator();
                Console.WriteLine("SIZE DIFFERENCES:");
                Console.WriteLine();

                foreach (var fileName in inBoth)
                {
                    var f1 = manifest1.Files.First(f => f.RelativePath == fileName);
                    var f2 = manifest2.Files.First(f => f.RelativePath == fileName);

                    long size1 = f1.GetData().Length;
                    long size2 = f2.GetData().Length;

                    if (size1 != size2)
                    {
                        Console.Write($"{GetTimestamp()} > {fileName}");
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        long diff = size2 - size1;
                        string sign = diff > 0 ? "+" : "";
                        Console.WriteLine($" ({FormatFileSize(size1)} → {FormatFileSize(size2)}, {sign}{FormatFileSize(Math.Abs(diff))})");
                        Console.ResetColor();
                    }
                }
            }

            WriteLog($"Compared bundles: {file1.Name} vs {file2.Name}");
        }
        catch (Exception ex)
        {
            LogError($"Comparison failed: {ex.Message}");
            WriteLog($"Comparison failed: {ex.Message}");
        }

        WaitForKey();
        ShowMainMenu();
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
                    WriteLog($"Cleaned work folders: {deleted}/{dirs.Length}");
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
                    WriteLog($"Deleted work folder: {selectedDir.Name}");
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

    static string CalculateFileHash(string filePath)
    {
        using var sha256 = SHA256.Create();
        using var stream = File.OpenRead(filePath);
        byte[] hash = sha256.ComputeHash(stream);
        return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
    }

    static string CalculateHash(byte[] data)
    {
        using var sha256 = SHA256.Create();
        byte[] hash = sha256.ComputeHash(data);
        return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
    }

    static void DrawProgressBar(int current, int total, string message)
    {
        const int barWidth = 40;
        double progress = (double)current / total;
        int filled = (int)(barWidth * progress);

        Console.Write($"\r{GetTimestamp()} > [");
        Console.ForegroundColor = ConsoleColor.Green;
        Console.Write(new string('█', filled));
        Console.ResetColor();
        Console.Write(new string('░', barWidth - filled));
        Console.Write($"] {current}/{total} ");

        Console.ForegroundColor = ConsoleColor.DarkGray;
        string truncated = message.Length > 40 ? message.Substring(0, 37) + "..." : message;
        Console.Write(truncated.PadRight(40));
        Console.ResetColor();

        if (current == total)
            Console.WriteLine();
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