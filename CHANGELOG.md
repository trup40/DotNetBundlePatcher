# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [2.0.0] - 2024-12-28

### Added
- Hash verification system
  - Export file hashes (SHA256)
  - Verify bundle integrity
  - Compare against saved hashes
  - Detect modified, added, and removed files
- Bundle comparison feature
  - Side-by-side comparison
  - File differences
  - Size differences
  - Architecture comparison
- Manifest export functionality
  - JSON format export
  - XML format export
  - TXT format export
  - Detailed metadata included
- Search in bundles
  - Search across multiple bundles
  - Case-insensitive search
  - Results with file sizes
- Automatic backup system
  - Timestamped backups
  - Created before each repack
  - Stored in dedicated folder
- Session-based logging
  - Timestamped log files
  - Detailed operation tracking
  - Error logging
- Batch operations menu
  - Extract all bundles
  - Compare bundle versions
  - Verify bundle integrity
- Work folder management
  - Clean individual folders
  - Clean all folders
  - Confirmation dialogs
- Progress visualization
  - Real-time progress bars
  - File-by-file tracking
  - Operation feedback

### Improved
- Console UI with color coding
  - Color-coded operations
  - Better readability
  - Status indicators
- Error handling
  - More descriptive messages
  - Graceful error recovery
  - Detailed error logging
- User experience
  - First-time user guidance
  - Confirmation dialogs
  - Clear operation feedback
- File size formatting
  - Human-readable formats
  - Byte precision
- Architecture detection
  - Support for more architectures
  - Detailed architecture info

### Changed
- Menu system restructured
  - Cleaner organization
  - More intuitive flow
  - Better categorization
- File organization
  - Dedicated folders for each function
  - Better separation of concerns

### Fixed
- Various minor bugs

## [1.0.0] - 2024-12-27

### Added
- Initial release
- Bundle extraction
- Bundle repacking
- Bundle details inspection
- Basic folder management
- .NET version detection
- Architecture detection

[unreleased]: https://github.com/trup40/DotNetBundlePatcher/compare/v2.0.0...HEAD
[2.0.0]: https://github.com/trup40/DotNetBundlePatcher/compare/v1.0.0...v2.0.0
[1.0.0]: https://github.com/trup40/DotNetBundlePatcher/releases/tag/v1.0.0