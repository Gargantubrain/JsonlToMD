# JsonlToMD

A cross-platform tool for converting Codex session JSONL files to readable Markdown format. 

## Why JsonlToMD?

The Codex extension and Codex CLI don't have a way to export chat history. 

Codex Tasks/Chats are logged as .jsonl files with names starting with rollout, located in the .codex folder in the user's home folder.

- For Mac, that's ~/.codex/sessions/year/month/day/
- For Windows, that's C:/users/username/.codex/sessions/year/month/day/

The files have names in the format:

- rollout-year-month-dateThour-minute-second-GUID.jsonl

Example:

- rollout-2025-09-05T16-15-09-0c5ea123-4bc5-6def-78aa-90b123456c78.jsonl

## Overview

JsonlToMD extracts chat conversations, tool calls, and reasoning from Codex session files and converts them to clean, readable Markdown. It supports both command-line and interactive GUI modes.

## Features

- **Cross-platform**: Windows, macOS, Linux support
- **Dual interface**: Command-line and interactive GUI
- **Flexible output**: Multiple export options and filtering
- **Bulk processing**: Convert multiple files at once
- **Clean formatting**: Removes technical metadata, focuses on conversation
- **Multiple modes**: Chat-only, with tools, with reasoning, or everything

## Installation

### Prerequisites
- .NET 8.0 Runtime (included with Windows 11, available for macOS/Linux)

### Download
1. Download the latest release from [GitHub Releases](https://github.com/Gargantubrain/JsonlToMD/releases)
2. Extract the executable for your platform
3. Run `JsonlToMD.exe` (Windows) or `JsonlToMD` (macOS/Linux)

### Build from Source
```bash
git clone https://github.com/Gargantubrain/JsonlToMD.git
cd JsonlToMD
dotnet publish -c Release
```

## Usage

### Interactive GUI Mode
```bash
# Launch GUI (default when no arguments)
JsonlToMD.exe

# Or explicitly
JsonlToMD.exe -ui
```

The GUI provides:
- File browser for selecting JSONL files
- Checkboxes for conversion options
- Save dialog for output location
- Real-time status updates

### Command Line Mode

#### Basic Usage
```bash
# Convert single file
JsonlToMD.exe session.jsonl

# Convert multiple files
JsonlToMD.exe session1.jsonl session2.jsonl session3.jsonl

# Bulk convert with wildcards
JsonlToMD.exe *.jsonl -o ./converted
```

#### Options
| Option | Description |
|--------|-------------|
| `-tools` | Include tool calls and outputs |
| `-noreasoning`, `-noplanning`, `-nothinking` | Exclude assistant reasoning |
| `-all` | Include everything (chat + tools + reasoning) |
| `-nouser` | Exclude user messages |
| `-noassistant`, `-noagent` | Exclude assistant messages |
| `-stdout` | Output to standard output (for piping) |
| `-noemoji` | Remove emojis from output |
| `-o <folder>` | Output folder for bulk mode |
| `-ui` | Show interactive GUI |
| `-h`, `--help`, `-help` | Show help message |

#### Examples
```bash
# Basic conversion
JsonlToMD.exe session.jsonl

# Include tools and reasoning
JsonlToMD.exe session.jsonl -tools -reasoning

# Assistant-only view with tools
JsonlToMD.exe session.jsonl -nouser -tools

# Bulk convert to specific folder
JsonlToMD.exe *.jsonl -o ./output -noreasoning

# Pipe to other tools
JsonlToMD.exe session.jsonl -stdout | findstr "error"

# Clean output without emojis
JsonlToMD.exe session.jsonl -noemoji -tools
```

## Output Format

### Chat Messages
```markdown
## 👤 User

Your message content here

---

## 🤖 Assistant

Assistant response here

---
```

### Tool Calls
```markdown
### 🔧 Tool Call: shell

**Arguments:**
```json
{"command": ["ls", "-la"]}
```

### 📤 Tool Output

**Result:**
```
total 8
drwxr-xr-x  2 user user 4096 Jan 1 12:00 .
drwxr-xr-x  3 user user 4096 Jan 1 12:00 ..
```

---
```

### Reasoning
```markdown
### 🧠 Assistant Reasoning

**Planning the next steps**

I need to analyze the file structure and identify the key components...

---
```

## File Structure

```
JsonlToMD/
├── Program.cs              # Main application code
├── JsonlToMd.csproj        # .NET project file
├── .gitignore              # Git ignore rules
├── LICENSE                 # MIT License
├── docs/
│   └── README.md          # This file
└── bin/Release/net8.0/     # Compiled executables
    ├── win-x64/           # Windows executable
    ├── osx-x64/           # macOS executable
    └── linux-x64/         # Linux executable
```

## Development

### Requirements
- .NET 8.0 SDK
- Terminal.Gui (for GUI mode)

### Building
```bash
# Restore dependencies
dotnet restore

# Build
dotnet build

# Publish for all platforms
dotnet publish -c Release -r win-x64 --self-contained false
dotnet publish -c Release -r osx-x64 --self-contained false
dotnet publish -c Release -r linux-x64 --self-contained false
```

### Project Structure
- `Program.cs` - Main application with CLI and GUI logic
- `JsonlToMd.csproj` - .NET project configuration
- Uses Terminal.Gui for cross-platform GUI
- Zero external dependencies for core functionality

## Contributing

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Acknowledgments

- Built with [.NET 8.0](https://dotnet.microsoft.com/)
- GUI powered by [Terminal.Gui](https://github.com/gui-cs/Terminal.Gui)
- Inspired by the need to export Codex session history

## Changelog

### v1.1 (2025-09-05)
- Fixed wildcard support to work correctly for Windows users
- Improved file path handling across all platforms
- Enhanced error messages for better user experience

### v1.0 (2025-09-05)
- Initial release
- Cross-platform support (Windows, macOS, Linux)
- Interactive GUI with file browser and checkboxes
- Command-line interface with comprehensive options
- Bulk processing support
- Multiple output formats and filtering options
- Clean Markdown output with emoji support

## Support

- **Issues**: [GitHub Issues](https://github.com/Gargantubrain/JsonlToMD/issues)
- **Discussions**: [GitHub Discussions](https://github.com/Gargantubrain/JsonlToMD/discussions)
- **Releases**: [GitHub Releases](https://github.com/Gargantubrain/JsonlToMD/releases)

---

**JsonlToMD** - Convert your Codex sessions to readable Markdown format
