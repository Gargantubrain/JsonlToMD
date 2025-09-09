using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Terminal.Gui;
using NStack;

class Program
{
    static bool IncludeTools = false;
    static bool IncludeReasoning = true;  // Default to true
    static bool IncludeAll = false;
    static bool ExcludeUser = false;
    static bool ExcludeAssistant = false;
    static bool OutputToStdout = false;
    static bool NoEmoji = false;
    static bool ShowUI = false;
    static string OutputFolder = "";
    static List<string> InputFiles = new List<string>();

    static List<string> ExpandWildcards(string pattern)
    {
        var expandedFiles = new List<string>();
        
        try
        {
            // Check if the pattern contains wildcards
            if (pattern.Contains('*') || pattern.Contains('?'))
            {
                string directory = Path.GetDirectoryName(pattern);
                string fileName = Path.GetFileName(pattern);
                
                // If no directory specified, use current directory
                if (string.IsNullOrEmpty(directory))
                {
                    directory = Environment.CurrentDirectory;
                }
                
                // Get all matching files
                var matchingFiles = Directory.GetFiles(directory, fileName, SearchOption.TopDirectoryOnly);
                expandedFiles.AddRange(matchingFiles);
            }
            else
            {
                // No wildcards, add the file as-is
                expandedFiles.Add(pattern);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error expanding wildcard pattern '{pattern}': {ex.Message}");
        }
        
        return expandedFiles;
    }

    static void Main(string[] args)
    {
        if (args.Length < 1)
        {
            ShowUI = true;
        }
        else
        {
            // Parse command line arguments
            for (int i = 0; i < args.Length; i++)
            {
                string arg = args[i];
                
                if (arg.StartsWith("-"))
                {
                    // Handle options
                    switch (arg.ToLower())
                    {
                        case "-tools":
                            IncludeTools = true;
                            break;
                        case "-noreasoning":
                        case "-noplanning":
                        case "-nothinking":
                            IncludeReasoning = false;
                            break;
                        case "-all":
                            IncludeAll = true;
                            break;
                        case "-nouser":
                            ExcludeUser = true;
                            break;
                        case "-noassistant":
                        case "-noagent":
                            ExcludeAssistant = true;
                            break;
                        case "-stdout":
                            OutputToStdout = true;
                            break;
                        case "-noemoji":
                            NoEmoji = true;
                            break;
                        case "-ui":
                            ShowUI = true;
                            break;
                        case "-o":
                            if (i + 1 < args.Length)
                            {
                                OutputFolder = args[++i];
                            }
                            else
                            {
                                Console.WriteLine("❌ -o requires an output folder path");
                                return;
                            }
                            break;
                        case "-h":
                        case "--help":
                        case "-help":
                            ShowUsage();
                            return;
                        default:
                            Console.WriteLine($"❌ Unknown option: {arg}");
                            ShowUsage();
                            return;
                    }
                }
                else
                {
                    // Handle input files - expand wildcards if present
                    var expandedFiles = ExpandWildcards(arg);
                    InputFiles.AddRange(expandedFiles);
                }
            }
        }

        if (ShowUI)
        {
            try
            {
                ShowInteractiveUI();
                return;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"GUI Error: {ex.Message}");
                Console.WriteLine("Falling back to command-line mode...");
                ShowUsage();
                return;
            }
        }

        if (InputFiles.Count == 0)
        {
            Console.WriteLine("❌ No input files specified");
            ShowUsage();
            return;
        }

        // Validate output folder if specified
        if (!string.IsNullOrEmpty(OutputFolder))
        {
            if (!Directory.Exists(OutputFolder))
            {
                try
                {
                    Directory.CreateDirectory(OutputFolder);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Cannot create output folder '{OutputFolder}': {ex.Message}");
                    return;
                }
            }
        }

        // Process files
        if (InputFiles.Count == 1 && OutputToStdout)
        {
            // Single file to stdout
            ConvertFileToStdout(InputFiles[0]);
        }
        else
        {
            // Bulk mode or single file to file
            ProcessBulkFiles();
        }
    }

    static void ShowUsage()
    {
        Console.WriteLine("JsonlToMD v1.1 build 2025-09-09 - https://github.com/Gargantubrain/JsonlToMD");
        Console.WriteLine("\nUsage: JsonlToMD <file1.jsonl> [file2.jsonl ...] [options]");
        Console.WriteLine("       JsonlToMD -ui (interactive mode)");
        Console.WriteLine("\nNote: Wildcards (*.jsonl, session*.jsonl) are supported on all platforms");
        Console.WriteLine("\nOptions:");
        Console.WriteLine("  -tools      Include tool calls and outputs");
        Console.WriteLine("  -noreasoning, -noplanning, -nothinking  Exclude assistant reasoning");
        Console.WriteLine("  -all        Include everything (chat + tools + reasoning)");
        Console.WriteLine("  -nouser     Exclude user messages");
        Console.WriteLine("  -noassistant, -noagent  Exclude assistant messages");
        Console.WriteLine("  -stdout     Output to standard output (for piping)");
        Console.WriteLine("  -noemoji    Remove emojis from output");
        Console.WriteLine("  -o <folder> Output folder for bulk mode");
        Console.WriteLine("  -ui         Show interactive UI");
        Console.WriteLine("  -h, --help, -help  Show this help message");
        Console.WriteLine("\nExamples:");
        Console.WriteLine("  JsonlToMD session.jsonl");
        Console.WriteLine("  JsonlToMD session1.jsonl session2.jsonl -tools");
        Console.WriteLine("  JsonlToMD *.jsonl -o ./output -noreasoning");
        Console.WriteLine("  JsonlToMD session*.jsonl -tools");
        Console.WriteLine("  JsonlToMD session.jsonl -stdout | findstr \"error\"");
        Console.WriteLine("  JsonlToMD -ui");
    }

    static void ShowInteractiveUI()
    {
        Application.Init();
        try
        {
            var top = Application.Top;

            var win = new Window("JsonToMD v1.0 - Interactive UI")
            {
                X = 0,
                Y = 1, // Leave space for the top menu if any
                Width = Dim.Fill(),
                Height = Dim.Fill()
            };
            top.Add(win);

            // JSONL file selector
            var fileLabel = new Label("JSONL File:") { X = 1, Y = 1 };
            var filePathField = new TextField("")
            {
                X = Pos.Right(fileLabel) + 1,
                Y = fileLabel.Y,
                Width = Dim.Fill(8)
            };
            var browseBtn = new Button("...")
            {
                X = Pos.Right(filePathField) + 1,
                Y = filePathField.Y,
            };
            browseBtn.Clicked += () =>
            {
                var od = new OpenDialog("Open JSONL", "Select a JSONL file")
                {
                    AllowsMultipleSelection = false,
                    CanChooseDirectories = false
                };
                Application.Run(od);
                try
                {
                    if (!od.Canceled)
                    {
                        string selected = null;
                        // Terminal.Gui 1.x exposes FilePaths; some versions expose FilePath
                        if (od.FilePaths != null && od.FilePaths.Count > 0)
                            selected = od.FilePaths[0];
#pragma warning disable CS0618
                        if (selected == null && od.FilePath != null)
                            selected = od.FilePath.ToString();
#pragma warning restore CS0618
                        if (!string.IsNullOrWhiteSpace(selected))
                        {
                            filePathField.Text = ustring.Make(selected);
                        }
                    }
                }
                finally
                {
                    od.Dispose();
                }
            };

            // Section label
            var optionsHeader = new Label("Conversion Options:")
            {
                X = 1,
                Y = Pos.Bottom(fileLabel) + 2
            };

            // Checkboxes
            var cbIncludeUser = new CheckBox("Include User Messages", is_checked: true)
            {
                X = 3,
                Y = Pos.Bottom(optionsHeader) + 1
            };
            var cbIncludeReasoning = new CheckBox("Include Reasoning/Thinking", is_checked: true)
            {
                X = 3,
                Y = Pos.Bottom(cbIncludeUser) + 1
            };
            var cbIncludeTools = new CheckBox("Include Tool Calls & Outputs", is_checked: false)
            {
                X = 3,
                Y = Pos.Bottom(cbIncludeReasoning) + 1
            };
            var cbIncludeAssistant = new CheckBox("Include Assistant Messages", is_checked: true)
            {
                X = 3,
                Y = Pos.Bottom(cbIncludeTools) + 1
            };
            var cbIncludeEmoji = new CheckBox("Include Emojis in Output", is_checked: true)
            {
                X = 3,
                Y = Pos.Bottom(cbIncludeAssistant) + 1
            };

            // User and Assistant are independent to match CLI (-nouser / -noassistant)

            // Buttons
            var btnConvert = new Button("Convert")
            {
                X = 3,
                Y = Pos.Bottom(cbIncludeEmoji) + 2
            };
            var btnCancel = new Button("Close/Cancel")
            {
                X = Pos.Right(btnConvert) + 2,
                Y = btnConvert.Y
            };
            var btnHelp = new Button("Help")
            {
                X = Pos.Right(btnCancel) + 2,
                Y = btnConvert.Y
            };

            // Status label
            var statusLabel = new Label("Status: Ready")
            {
                X = 1,
                Y = Pos.Bottom(btnConvert) + 2,
                Width = Dim.Fill()
            };

            // Button behavior
            btnHelp.Clicked += () =>
            {
                MessageBox.Query("Help", "Command-line has many options including batch mode. Use JsonlToMD --help to see usage.\nUse [...] to Select a JSONL file and choose the Conversion Options. Use [Convert] to save as a .md file.", "OK");
            };

            btnCancel.Clicked += () =>
            {
                Application.RequestStop();
            };

            btnConvert.Clicked += () =>
            {
                string inputPath = filePathField.Text?.ToString() ?? string.Empty;
                if (string.IsNullOrWhiteSpace(inputPath))
                {
                    MessageBox.ErrorQuery("Validation", "Please select a JSONL file.", "OK");
                    return;
                }
                if (!File.Exists(inputPath))
                {
                    MessageBox.ErrorQuery("File Not Found", $"{inputPath}", "OK");
                    return;
                }

                // Map UI selections to flags
                IncludeTools = cbIncludeTools.Checked;
                IncludeReasoning = cbIncludeReasoning.Checked;
                NoEmoji = !cbIncludeEmoji.Checked;
                ExcludeUser = !cbIncludeUser.Checked;
                ExcludeAssistant = !cbIncludeAssistant.Checked;

                try
                {
                    statusLabel.Text = ustring.Make("Status: Converting...");
                    Application.Refresh();

                    // Generate content in-memory first
                    var content = ConvertFileToString(inputPath);

                    // Prompt for save location with default filename
                    var defaultOutputPath = Path.ChangeExtension(inputPath, ".md");
                    var sd = new SaveDialog("Save Markdown", "Choose where to save the .md");
                    try
                    {
                        var defaultDir = Path.GetDirectoryName(defaultOutputPath) ?? Environment.CurrentDirectory;
                        // Pre-fill directory and file name when supported
                        try { sd.DirectoryPath = ustring.Make(defaultDir); } catch { }
                        try { sd.FilePath = ustring.Make(defaultOutputPath); } catch { }
                        Application.Run(sd);
                        if (sd.Canceled)
                        {
                            statusLabel.Text = ustring.Make("Status: Canceled");
                            return;
                        }
                        string savePath = null;
                        if (sd.FilePath != null)
                            savePath = sd.FilePath.ToString();
                        if (string.IsNullOrWhiteSpace(savePath))
                        {
                            statusLabel.Text = ustring.Make("Status: No path selected");
                            return;
                        }

                        // Ensure .md extension
                        if (Path.GetExtension(savePath).Length == 0)
                        {
                            savePath = Path.ChangeExtension(savePath, ".md");
                        }

                        File.WriteAllText(savePath, content, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));

                        statusLabel.Text = ustring.Make($"Status: Saved {Path.GetFileName(savePath)}");
                        MessageBox.Query("Success", $"Saved to:\n{savePath}", "OK");
                    }
                    finally
                    {
                        sd.Dispose();
                    }
                }
                catch (Exception ex)
                {
                    statusLabel.Text = ustring.Make("Status: Error");
                    MessageBox.ErrorQuery("Error", ex.Message, "OK");
                }
            };

            // Add controls
            win.Add(
                fileLabel,
                filePathField,
                browseBtn,
                optionsHeader,
                cbIncludeUser,
                cbIncludeReasoning,
                cbIncludeTools,
                cbIncludeAssistant,
                cbIncludeEmoji,
                btnConvert,
                btnCancel,
                btnHelp,
                statusLabel
            );

            Application.Run();
        }
        finally
        {
            Application.Shutdown();
        }
    }

    static string ConvertFileToString(string inputPath)
    {
        using var reader = new StreamReader(inputPath, Encoding.UTF8);
        var sb = new StringBuilder();
        using var writer = new StringWriter(sb);
        ProcessJsonlFile(reader, writer, Path.GetFileName(inputPath));
        return sb.ToString();
    }

    static void ShowSimpleConsoleUI()
    {
        Console.Clear();
        Console.WriteLine("╔══════════════════════════════════════════════════════════════╗");
        Console.WriteLine("║                    JsonToMD v1.0 - Interactive UI            ║");
        Console.WriteLine("║              https://github.com/Gargantubrain/JsonlToMD     ║");
        Console.WriteLine("╚══════════════════════════════════════════════════════════════╝");
        Console.WriteLine();

        // File selection
        string inputPath = "";
        while (string.IsNullOrWhiteSpace(inputPath))
        {
            Console.Write("📁 JSONL File Path: ");
            inputPath = Console.ReadLine()?.Trim() ?? "";
            
            if (string.IsNullOrWhiteSpace(inputPath))
            {
                Console.WriteLine("❌ Please enter a file path.");
                continue;
            }
            
            if (!File.Exists(inputPath))
            {
                Console.WriteLine($"❌ File not found: {inputPath}");
                inputPath = "";
            }
        }

        Console.WriteLine();
        Console.WriteLine("⚙️  Conversion Options:");
        Console.WriteLine();

        // Options with defaults
        bool includeChat = true;
        bool includeReasoning = true;
        bool includeAssistant = true;
        bool includeEmoji = true;
        bool includeTools = false;

        // Interactive checkboxes
        Console.WriteLine("┌─ Content Options ──────────────────────────────────────────────┐");
        Console.WriteLine($"│ [{(includeChat ? "X" : " ")}] Include Chat Messages (User & Assistant)                    │");
        Console.WriteLine($"│ [{(includeReasoning ? "X" : " ")}] Include Reasoning/Thinking                              │");
        Console.WriteLine($"│ [{(includeTools ? "X" : " ")}] Include Tool Calls & Outputs                               │");
        Console.WriteLine($"│ [{(includeAssistant ? "X" : " ")}] Include Assistant Messages                              │");
        Console.WriteLine($"│ [{(includeEmoji ? "X" : " ")}] Include Emojis in Output                                   │");
        Console.WriteLine("└───────────────────────────────────────────────────────────────┘");
        Console.WriteLine();

        // Toggle options
        Console.WriteLine("Press keys to toggle options (c=chat, r=reasoning, t=tools, a=assistant, e=emoji, h=help, enter=convert):");
        
        while (true)
        {
            var key = Console.ReadKey(true);
            
            switch (key.KeyChar.ToString().ToLower())
            {
                case "c":
                    includeChat = !includeChat;
                    break;
                case "r":
                    includeReasoning = !includeReasoning;
                    break;
                case "t":
                    includeTools = !includeTools;
                    break;
                case "a":
                    includeAssistant = !includeAssistant;
                    break;
                case "e":
                    includeEmoji = !includeEmoji;
                    break;
                case "h":
                    ShowUsage();
                    Console.WriteLine("\nPress any key to continue...");
                    Console.ReadKey();
                    break;
                case "\r": // Enter
                    goto Convert;
            }
            
            // Redraw checkboxes
            Console.SetCursorPosition(0, 7);
            Console.WriteLine("┌─ Content Options ──────────────────────────────────────────────┐");
            Console.WriteLine($"│ [{(includeChat ? "X" : " ")}] Include Chat Messages (User & Assistant)                    │");
            Console.WriteLine($"│ [{(includeReasoning ? "X" : " ")}] Include Reasoning/Thinking                              │");
            Console.WriteLine($"│ [{(includeTools ? "X" : " ")}] Include Tool Calls & Outputs                               │");
            Console.WriteLine($"│ [{(includeAssistant ? "X" : " ")}] Include Assistant Messages                              │");
            Console.WriteLine($"│ [{(includeEmoji ? "X" : " ")}] Include Emojis in Output                                   │");
            Console.WriteLine("└───────────────────────────────────────────────────────────────┘");
        }

        Convert:
        Console.WriteLine();
        Console.WriteLine("🔄 Converting...");

        // Apply settings
        IncludeTools = includeTools;
        IncludeReasoning = includeReasoning;
        ExcludeUser = !includeChat;
        ExcludeAssistant = !includeAssistant;
        NoEmoji = !includeEmoji;

        // Convert
        string outputPath = Path.ChangeExtension(inputPath, ".md");
        ConvertFile(inputPath, outputPath);
    }

    static void ConvertFile(string inputPath, string outputPath)
    {
        try
        {
            using var reader = new StreamReader(inputPath, Encoding.UTF8);
            using var writer = new StreamWriter(outputPath, false, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));

            ProcessJsonlFile(reader, writer, Path.GetFileName(inputPath));

            Console.WriteLine($"✅ Converted to: {outputPath}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error: {ex.Message}");
        }
    }

    static void ConvertFileToStdout(string inputPath)
    {
        try
        {
            using var reader = new StreamReader(inputPath, Encoding.UTF8);
            using var writer = new StreamWriter(Console.OpenStandardOutput(), new UTF8Encoding(encoderShouldEmitUTF8Identifier: false)) { AutoFlush = true };

            ProcessJsonlFile(reader, writer, Path.GetFileName(inputPath));

            Console.Error.WriteLine("✅ Converted and output to stdout");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error: {ex.Message}");
        }
    }

    static void ProcessBulkFiles()
    {
        int successCount = 0;
        int errorCount = 0;

        Console.WriteLine($"Processing {InputFiles.Count} file(s)...");
        Console.WriteLine();

        foreach (string inputFile in InputFiles)
        {
            try
            {
                if (!File.Exists(inputFile))
                {
                    Console.WriteLine($"❌ File not found: {inputFile}");
                    errorCount++;
                    continue;
                }

                string outputFile;
                if (!string.IsNullOrEmpty(OutputFolder))
                {
                    // Output to specified folder
                    string fileName = Path.GetFileNameWithoutExtension(inputFile) + ".md";
                    outputFile = Path.Combine(OutputFolder, fileName);
                }
                else
                {
                    // Output next to input file
                    outputFile = Path.ChangeExtension(inputFile, ".md");
                }

                using var reader = new StreamReader(inputFile, Encoding.UTF8);
                using var writer = new StreamWriter(outputFile, false, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));

                ProcessJsonlFile(reader, writer, Path.GetFileName(inputFile));

                Console.WriteLine($"✅ {Path.GetFileName(inputFile)} → {Path.GetFileName(outputFile)}");
                successCount++;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error processing {Path.GetFileName(inputFile)}: {ex.Message}");
                errorCount++;
            }
        }

        Console.WriteLine();
        Console.WriteLine($"📊 Summary: {successCount} successful, {errorCount} errors");
    }

    static void ProcessJsonlFile(StreamReader reader, TextWriter writer, string fileName)
    {
        writer.WriteLine($"# Chat Conversation: {fileName}");
        writer.WriteLine("\n*Exported from Codex session*\n");

        string line;
        while ((line = reader.ReadLine()) != null)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;

            try
            {
                var doc = JsonDocument.Parse(line);
                var root = doc.RootElement;

                if (root.TryGetProperty("type", out var type))
                {
                    string typeStr = type.GetString();
                    
                    // Process chat messages
                    if (typeStr == "message" && 
                        root.TryGetProperty("role", out var role) &&
                        role.GetString() is string roleStr &&
                        (roleStr == "user" || roleStr == "assistant") &&
                        root.TryGetProperty("content", out var content))
                    {
                        // Check if we should exclude this role
                        if ((roleStr == "user" && ExcludeUser) || 
                            (roleStr == "assistant" && ExcludeAssistant))
                            continue;

                        string text = ExtractText(content);
                        if (!string.IsNullOrWhiteSpace(text) && !text.StartsWith("<environment_context>"))
                        {
                            string roleDisplay = NoEmoji ? 
                                (roleStr == "user" ? "User" : "Assistant") :
                                (roleStr == "user" ? "👤 User" : "🤖 Assistant");
                            writer.WriteLine($"## {roleDisplay}\n\n{text}\n\n---\n");
                        }
                    }
                    // Process tool calls and outputs
                    else if ((IncludeTools || IncludeAll) && 
                             (typeStr == "function_call" || typeStr == "function_call_output"))
                    {
                        ProcessToolCall(writer, root, typeStr);
                    }
                    // Process reasoning
                    else if ((IncludeReasoning || IncludeAll) && typeStr == "reasoning")
                    {
                        ProcessReasoning(writer, root);
                    }
                }
            }
            catch (JsonException)
            {
                // Skip invalid JSON lines
                continue;
            }
        }
    }

    static void ProcessToolCall(TextWriter writer, JsonElement root, string type)
    {
        if (type == "function_call")
        {
            if (root.TryGetProperty("name", out var name) && 
                root.TryGetProperty("arguments", out var args))
            {
                string toolName = name.GetString() ?? "unknown";
                string arguments = args.GetString() ?? "";
                
                string toolCallHeader = NoEmoji ? 
                    $"### Tool Call: {toolName}\n" :
                    $"### 🔧 Tool Call: {toolName}\n";
                writer.WriteLine(toolCallHeader);
                writer.WriteLine($"**Arguments:**\n```json\n{arguments}\n```\n");
            }
        }
        else if (type == "function_call_output")
        {
            if (root.TryGetProperty("call_id", out var callId) && 
                root.TryGetProperty("output", out var output))
            {
                string outputText = output.GetString() ?? "";
                string toolOutputHeader = NoEmoji ? 
                    "### Tool Output\n" :
                    "### 📤 Tool Output\n";
                writer.WriteLine(toolOutputHeader);
                writer.WriteLine($"**Result:**\n```\n{outputText}\n```\n");
            }
        }
        writer.WriteLine("---\n");
    }

    static void ProcessReasoning(TextWriter writer, JsonElement root)
    {
        if (root.TryGetProperty("summary", out var summary) && 
            summary.ValueKind == JsonValueKind.Array)
        {
            var reasoningText = new StringBuilder();
            
            foreach (var item in summary.EnumerateArray())
            {
                if (item.TryGetProperty("text", out var text) && 
                    text.ValueKind == JsonValueKind.String)
                {
                    string textValue = text.GetString() ?? "";
                    if (!string.IsNullOrWhiteSpace(textValue))
                    {
                        reasoningText.AppendLine(textValue);
                    }
                }
            }
            
            // Only write the reasoning section if there's actual content
            if (reasoningText.Length > 0)
            {
                string reasoningHeader = NoEmoji ? 
                    "### Assistant Reasoning\n" :
                    "### 🧠 Assistant Reasoning\n";
                writer.WriteLine(reasoningHeader);
                writer.WriteLine(reasoningText.ToString().Trim());
                writer.WriteLine("\n---\n");
            }
        }
    }

    static string ExtractText(JsonElement content)
    {
        if (content.ValueKind == JsonValueKind.String)
        {
            return content.GetString() ?? "";
        }

        if (content.ValueKind == JsonValueKind.Array)
        {
            var parts = new StringBuilder();
            foreach (var item in content.EnumerateArray())
            {
                if (item.ValueKind == JsonValueKind.Object)
                {
                    if (item.TryGetProperty("text", out var text) && text.ValueKind == JsonValueKind.String)
                    {
                        parts.AppendLine(text.GetString());
                    }
                    else if (item.TryGetProperty("type", out var type) && 
                             type.GetString() is string typeStr &&
                             (typeStr == "text" || typeStr == "output_text") &&
                             item.TryGetProperty("text", out var typeText) && 
                             typeText.ValueKind == JsonValueKind.String)
                    {
                        parts.AppendLine(typeText.GetString());
                    }
                }
            }
            return parts.ToString().Trim();
        }

        if (content.ValueKind == JsonValueKind.Object)
        {
            if (content.TryGetProperty("text", out var text) && text.ValueKind == JsonValueKind.String)
            {
                return text.GetString() ?? "";
            }
            return content.GetRawText();
        }

        return content.ValueKind == JsonValueKind.Null ? "" : content.GetRawText();
    }
}
