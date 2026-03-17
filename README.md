# 📦 .NET Bundle Patcher v2.0

[![.NET](https://img.shields.io/badge/.NET-6.0+-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![License](https://img.shields.io/github/license/trup40/DotNetBundlePatcher)](LICENSE)
[![Release](https://img.shields.io/github/v/release/trup40/DotNetBundlePatcher)](https://github.com/trup40/DotNetBundlePatcher/releases)
[![Stars](https://img.shields.io/github/stars/trup40/DotNetBundlePatcher)](https://github.com/trup40/DotNetBundlePatcher/stargazers)

> Professional CLI tool for extracting, modifying, and repacking .NET single-file bundles with advanced features

## ✨ What's New in v2.0

### 🎉 Major Features
- 🔐 **Hash Verification System** - Detect file modifications with SHA256
- 📊 **Bundle Comparison** - Side-by-side comparison of two bundles
- 📄 **Manifest Export** - Export bundle metadata (JSON/XML/TXT)
- 🔍 **Search Functionality** - Find files across multiple bundles
- 💾 **Auto Backup** - Automatic backup before repacking
- 📝 **Session Logging** - Detailed operation logs

### 🔧 Improvements
- Enhanced progress visualization with progress bars
- Colored console output for better UX
- Batch operations support
- Comprehensive error handling
- Work folder management

## 🎯 Features

### Core Functionality
- ✅ **Extract Bundles** - Unpack all embedded files from single-file executables
- ✅ **Repack Bundles** - Modify and inject files back into bundles
- ✅ **Inspect Details** - View bundle metadata and architecture info
- ✅ **Batch Operations** - Process multiple bundles simultaneously

### Advanced Features
- 🔐 **Hash Verification** - Compare bundle integrity against saved hashes
- 📊 **Bundle Diff** - Identify differences between bundle versions
- 🔍 **File Search** - Search for specific files across bundles
- 📄 **Export Manifests** - Generate detailed bundle reports
- 💾 **Smart Backup** - Timestamped backups with restore capability

### Supported .NET Versions
- .NET Core 3.0 / 3.1
- .NET 5.0
- .NET 6.0 (LTS)
- .NET 7.0
- .NET 8.0 (LTS)
- .NET 9.0
- .NET 10.0

### Supported Architectures
- x86 (32-bit)
- x64 (64-bit)
- ARM (32-bit)
- ARM64 (64-bit)

## 📦 Installation

### Prerequisites
- .NET 6.0 SDK or higher
- Windows, Linux, or macOS

### Option 1: Download Pre-built Binary
Download the latest release from [Releases](https://github.com/trup40/DotNetBundlePatcher/releases)

### Option 2: Build from Source
```bash
git clone https://github.com/trup40/DotNetBundlePatcher.git
cd DotNetBundlePatcher
dotnet build -c Release
```

## 🚀 Quick Start

### Basic Workflow

1. **Place your bundle** in the `in/` folder
2. **Run the application**
   ```bash
   dotnet run
   # or
   ./DotNetBundlePatcher
   ```
3. **Extract**: Select option `1` to extract files
4. **Modify**: Edit files in `work/<bundle-name>/`
5. **Repack**: Select option `2` to create patched bundle
6. **Output**: Find result in `out/` folder

### Example Session
```
════════════════════════════════════════════════════════════════════════════════
  📦 .NET BUNDLE PATCHER
════════════════════════════════════════════════════════════════════════════════
  Version 2.0.0

  [1] Extract Bundle
  [2] Repack Bundle
  [3] Show Bundle Details
  [4] Clean Work Folder
  [5] Batch Operations
  [6] Export Bundle Manifest
  [7] Search in Bundles
  [8] Compare Bundles
  [X] Exit

Your choice: 1
```

## 📖 Detailed Usage

### Extracting Bundles
Extracts all embedded files from a single-file bundle.
- Files are extracted to `work/<bundle-name>/`
- Original structure is preserved
- Progress bar shows extraction status

### Repacking Bundles
Creates a new patched bundle with modified files.
- Only modified files need to be present
- Original bundle must remain in `in/` folder
- Automatic backup is created
- Output: `<name>_patched_<timestamp>.exe`

### Hash Verification
Verify bundle integrity using saved hashes.

1. **Export hashes** (Option 6 → Hash verification)
2. **Modify files** in work folder
3. **Verify** to see what changed
   - Shows modified files
   - Displays old vs new hashes
   - Identifies added/removed files

### Bundle Comparison
Compare two bundles side-by-side.
- File count differences
- Added/removed files
- Size differences
- Architecture comparison

### Search
Find specific files across all bundles in `in/` folder.
```
Search term: appsettings.json
Results:
  📦 App1.exe
    ✓ appsettings.json (2.34 KB)
  📦 App2.exe
    ✓ appsettings.production.json (1.89 KB)
```

## 📂 Directory Structure

```
DotNetBundlePatcher/
├── in/          ← Place original bundles here
├── work/        ← Extracted files (edit these)
├── out/         ← Patched bundles output
├── logs/        ← Session logs
├── backup/      ← Automatic backups
└── exports/     ← Exported manifests
```

## 🔧 Advanced Usage

### Batch Extract
Extract all bundles in `in/` folder at once:
1. Option `5` (Batch Operations)
2. Option `1` (Extract All)

### Export Manifest
Generate detailed bundle report:
1. Option `6` (Export Bundle Manifest)
2. Choose format: JSON, XML, or TXT
3. Find in `exports/` folder

### Verify Integrity
Check if bundle was modified:
1. Export hashes before modification
2. Make changes
3. Run verification to see differences

## 🛡️ Security & Best Practices

### ⚠️ Important Notes
- **Code Signing**: Repacked bundles lose original signatures
- **Testing**: Always test in isolated environment first
- **Backups**: Tool creates automatic backups, but keep your own too
- **Legality**: Only modify software you own or have rights to

### Recommended Workflow
1. ✅ Create manual backup of original
2. ✅ Extract and verify with hash
3. ✅ Make minimal changes
4. ✅ Test in isolated environment
5. ✅ Verify hash after repacking
6. ✅ Document all changes

## 🐛 Troubleshooting

### Common Issues

**Q: "Cannot read bundle" error**
- Ensure file is a valid .NET single-file bundle
- Check if .NET version is supported
- Verify file is not corrupted

**Q: Repack fails**
- Original bundle must be in `in/` folder
- Check if work folder matches bundle name
- Review logs in `logs/` folder

**Q: Modified files not updating**
- Ensure files are in correct relative path
- File names are case-sensitive on Linux/Mac
- Check logs for detailed error messages

**Q: Hash verification shows all files modified**
- Ensure you're comparing against correct hash file
- Check if bundle version matches
- Verify file was not re-extracted

### Getting Help
- 📝 Check [logs/] for detailed error messages
- 🐛 [Report issues](https://github.com/trup40/DotNetBundlePatcher/issues)
- 💬 [Discussions](https://github.com/trup40/DotNetBundlePatcher/discussions)

## 🤝 Contributing

Contributions are welcome! Please check [CONTRIBUTING.md](CONTRIBUTING.md) for guidelines.

### Development Setup
```bash
git clone https://github.com/trup40/DotNetBundlePatcher.git
cd DotNetBundlePatcher
dotnet restore
dotnet build
```

### Running Tests
```bash
dotnet test
```

## 📝 Changelog

### [v2.0.0] - 2024-12-28
#### Added
- Hash verification system with export and verify features
- Bundle comparison functionality
- Manifest export (JSON/XML/TXT formats)
- Search in bundles feature
- Automatic backup system
- Session-based logging
- Progress bars for all operations
- Batch operations menu
- Work folder management

#### Improved
- Enhanced console UI with colors
- Better error handling and messages
- Detailed operation feedback
- User experience improvements

#### Changed
- Restructured menu system
- Improved file organization

### [v1.0.0] - 2024-12-27
- Initial release
- Basic extract and repack functionality
- Bundle details inspection

## 🙏 Acknowledgments

- Built with [AsmResolver](https://github.com/Washi1337/AsmResolver) by Washi
- Inspired by .NET single-file deployment format
- Community feedback and contributions

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## ⚠️ Legal Disclaimer

This tool is for:
- ✅ Educational purposes
- ✅ Debugging your own applications
- ✅ Recovering lost source code from your apps
- ✅ Legitimate software analysis

**NOT for:**
- ❌ Piracy or license circumvention
- ❌ Malware creation
- ❌ Unauthorized software modification
- ❌ Violating software terms of service

Users are responsible for ensuring compliance with applicable laws and software licenses.

## 📊 Statistics

![GitHub stars](https://img.shields.io/github/stars/trup40/DotNetBundlePatcher)
![GitHub forks](https://img.shields.io/github/forks/trup40/DotNetBundlePatcher)
![GitHub issues](https://img.shields.io/github/issues/trup40/DotNetBundlePatcher)
![GitHub downloads](https://img.shields.io/github/downloads/trup40/DotNetBundlePatcher/total)

## 💬 Support

- 🐛 [Report Bug](https://github.com/trup40/DotNetBundlePatcher/issues/new?template=bug_report.md)
- 💡 [Request Feature](https://github.com/trup40/DotNetBundlePatcher/issues/new?template=feature_request.md)
- 💬 [Discussions](https://github.com/trup40/DotNetBundlePatcher/discussions)

---

**⭐ Star this repository if you find it useful!**

AI was used for console visualization and README.md preparation.


## ☕ Donate

These projects are free and open-source. If you find them useful and would like to support the development process, you can buy me a coffee using the crypto addresses below! 🚀

### 🪙 Crypto Donations

| Coin | Network | Wallet Address |
| :--- | :--- | :--- |
| **USDT (Tether)** | **TRC20** (Tron) | `TWxJVQ3PBCd8ZJJVkX2joe8WRGcSCdh8Ws` |
| **BTC (Bitcoin)** | Bitcoin (Bech32)| `bc1q7207qk3wk94a94xvxx43lxawsg69zpm0atvtd8` |
| **ETH (Ethereum)** | ERC20 | `0x1f5A2e35752c6f01c753F334292Fc7635Caeef56` |
| **BNB** | **BSC** (BEP20) | `0x93845c5Fb889C36E072B5683f1616C625C2deBe7` |

> [!IMPORTANT]
> Please ensure that the **Network** selection matches the table exactly. Using the wrong network may result in a permanent loss of funds.
