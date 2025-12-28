# Contributing to .NET Bundle Patcher

First off, thank you for considering contributing! 🎉

## Code of Conduct

Be respectful and constructive. We're all here to learn and improve.

## How Can I Contribute?

### 🐛 Reporting Bugs

Before creating bug reports, please check existing issues. When you create a bug report, include:

- **Clear title** describing the issue
- **Steps to reproduce**
- **Expected behavior**
- **Actual behavior**
- **Environment details**:
  - OS (Windows/Linux/macOS)
  - .NET version
  - Bundle type being processed
  - App version
- **Log files** (from `logs/` folder)
- **Screenshots** if applicable

### 💡 Suggesting Features

Feature suggestions are welcome! Please provide:

- **Clear description** of the feature
- **Use case** - why is it needed?
- **Proposed implementation** (if you have ideas)
- **Alternatives considered**

### 🔧 Pull Requests

1. **Fork the repo** and create your branch from `main`
2. **Make your changes**
3. **Test thoroughly**
4. **Update documentation** if needed
5. **Add/update tests** if applicable
6. **Ensure code follows style guidelines**
7. **Commit with clear messages**
8. **Submit PR** with detailed description

#### PR Checklist
- [ ] Code builds without errors
- [ ] Tests pass (if applicable)
- [ ] Documentation updated
- [ ] CHANGELOG.md updated
- [ ] Commit messages are clear
- [ ] Code follows project style

## Development Setup

```bash
# Clone your fork
git clone https://github.com/YOUR-USERNAME/DotNetBundlePatcher.git
cd DotNetBundlePatcher

# Add upstream remote
git remote add upstream https://github.com/trup40/DotNetBundlePatcher.git

# Install dependencies
dotnet restore

# Build
dotnet build

# Run
dotnet run
```

## Coding Guidelines

### Style
- Use **C# naming conventions**
- **4 spaces** for indentation (no tabs)
- **Descriptive variable names**
- **XML documentation** for public methods
- **Comments** for complex logic

### Best Practices
- Keep methods **focused and small**
- Use **meaningful names**
- Handle **exceptions appropriately**
- Add **logging** for important operations
- **Validate inputs**

### Example
```csharp
/// <summary>
/// Calculates SHA256 hash of the provided data.
/// </summary>
/// <param name="data">Byte array to hash.</param>
/// <returns>Lowercase hexadecimal hash string.</returns>
/// <exception cref="ArgumentNullException">Thrown when data is null.</exception>
static string CalculateHash(byte[] data)
{
    if (data == null)
        throw new ArgumentNullException(nameof(data));
        
    using var sha256 = SHA256.Create();
    byte[] hash = sha256.ComputeHash(data);
    return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
}
```

## Testing

- Test with different bundle types
- Test with various .NET versions
- Test on different platforms
- Test error scenarios
- Test edge cases

## Commit Messages

Use clear, descriptive commit messages:

```
✅ Good:
- Add hash verification feature
- Fix crash when bundle file is missing
- Update README with new features

❌ Bad:
- Update
- Fix bug
- Changes
```

### Format
```
<type>: <description>

[optional body]
[optional footer]
```

**Types:**
- `feat`: New feature
- `fix`: Bug fix
- `docs`: Documentation
- `style`: Formatting
- `refactor`: Code restructuring
- `test`: Adding tests
- `chore`: Maintenance

## Questions?

Feel free to:
- Open an issue
- Start a discussion
- Reach out via email

## License

By contributing, you agree that your contributions will be licensed under the MIT License.