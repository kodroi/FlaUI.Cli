using System.CommandLine;
using FlaUI.Cli.Commands.Audit;
using FlaUI.Cli.Commands.Elem;
using FlaUI.Cli.Commands.Record;
using FlaUI.Cli.Commands.Session;
using FlaUI.Cli.Commands.Wait;

var sessionOption = new Option<string?>("--session") { Description = "Path to session file", Recursive = true };

var rootCommand = new RootCommand("FlaUI CLI — automate Windows desktop apps from the terminal");
rootCommand.Add(sessionOption);

// Session commands
var sessionCommand = new Command("session", "Manage sessions");
sessionCommand.Add(NewCommand.Create(sessionOption));
sessionCommand.Add(AttachCommand.Create(sessionOption));
sessionCommand.Add(StatusCommand.Create(sessionOption));
sessionCommand.Add(EndCommand.Create(sessionOption));
rootCommand.Add(sessionCommand);

// Element commands
var elemCommand = new Command("elem", "Find and interact with elements");
elemCommand.Add(FindCommand.Create(sessionOption));
elemCommand.Add(TreeCommand.Create(sessionOption));
elemCommand.Add(PropsCommand.Create(sessionOption));
elemCommand.Add(ClickCommand.Create(sessionOption));
elemCommand.Add(TypeCommand.Create(sessionOption));
elemCommand.Add(SetValueCommand.Create(sessionOption));
elemCommand.Add(SelectCommand.Create(sessionOption));
elemCommand.Add(GetValueCommand.Create(sessionOption));
elemCommand.Add(GetStateCommand.Create(sessionOption));
rootCommand.Add(elemCommand);

// Wait command
rootCommand.Add(WaitCommand.Create(sessionOption));

// Record commands
var recordCommand = new Command("record", "Record and manage interaction steps");
recordCommand.Add(StartCommand.Create(sessionOption));
recordCommand.Add(StopCommand.Create(sessionOption));
recordCommand.Add(DropCommand.Create(sessionOption));
recordCommand.Add(KeepCommand.Create(sessionOption));
recordCommand.Add(ListCommand.Create(sessionOption));
recordCommand.Add(ExportCommand.Create(sessionOption));
rootCommand.Add(recordCommand);

// Audit command
rootCommand.Add(AuditCommand.Create(sessionOption));

return await rootCommand.Parse(args).InvokeAsync();
