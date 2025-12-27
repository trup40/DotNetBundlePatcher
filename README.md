# 📦 .NET Bundle Patcher

**DotNetBundlePatcher** is a powerful CLI tool designed to analyze, extract, and repack .NET Single-File applications (Bundles). Built using the robust [AsmResolver](https://github.com/Washi1337/AsmResolver) library, it allows developers and reverse engineers to modify the contents of self-contained .NET executables effortlessly.

## 🚀 Features

* **Extract Bundles:** Unpack all embedded files (DLLs, configs, resources) from a single-file executable into a working directory.
* **Repack/Patch:** Modify extracted files and inject them back into the bundle without breaking the executable structure.
* **Inspect Details:** View detailed metadata including .NET version (Core 3.x to .NET 10), architecture (x64, x86, ARM), and file manifests.
* **Batch Operations:** Perform bulk extraction, version comparison, and integrity checks on multiple files.
* **Safe Workflow:** Distinct folders for input (`in`), workspace (`work`), and output (`out`) to prevent accidental overwrites.

## 🛠️ Prerequisites

* .NET SDK (6.0 or later recommended)
* **Dependencies:**
    * `AsmResolver.DotNet`
    * `AsmResolver.PE`

## 📦 Installation & Build

1.  **Clone the repository:**
    ```bash
    git clone [https://github.com/yourusername/DotNetBundlePatcher.git](https://github.com/yourusername/DotNetBundlePatcher.git)
    cd DotNetBundlePatcher
    ```

2.  **Install required packages:**
    ```bash
    dotnet add package AsmResolver.DotNet
    dotnet add package AsmResolver.PE
    ```

3.  **Build and Run:**
    ```bash
    dotnet run
    ```

## 📖 How to Use

The tool uses a directory-based workflow. Upon first launch, it will automatically create the necessary folders.

### 1. Extracting a Bundle
1.  Place your target `.exe` file (single-file bundle) into the **`in/`** folder.
2.  Run the tool and select **Option 1 (Extract Bundle)**.
3.  The contents will be extracted to `work/<filename>/`.

### 2. Modifying Files
Navigate to `work/<filename>/`. You can now edit `.dll`, `.json`, or any other configuration files extracted from the bundle.

> **Note:** Do not delete files from the work folder unless you intend to, but the repacker primarily looks for *modified* content to update the original manifest.

### 3. Repacking
1.  Select **Option 2 (Repack Bundle)** from the main menu.
2.  Choose the folder you modified.
3.  The tool will compare the files in `work/` with the original bundle in `in/`.
4.  A new, patched executable will be generated in the **`out/`** folder (e.g., `MyApp_patched_20251227.exe`).

### 4. Inspecting
Select **Option 3** to see what's inside a bundle without extracting it. It provides useful info like:
* Real File Size vs. Bundle Size
* Target Architecture
* .NET Runtime Version

## 📂 Directory Structure

```text
DotNetBundlePatcher/
├── in/        <-- Put original .exe files here
├── work/      <-- Extracted files appear here (Edit these)
├── out/       <-- Patched .exe files are saved here
└── Program.cs
```

⚠️ Disclaimer
This tool is intended for educational purposes, debugging, and recovering lost source code from your own applications. Please respect software licenses and intellectual property rights when using this tool.

📄 License
MIT License

AI was used for console visualization and README.md preparation.