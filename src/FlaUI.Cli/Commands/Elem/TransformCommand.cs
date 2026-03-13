using System.CommandLine;
using FlaUI.Cli.Infrastructure;
using FlaUI.Cli.Models;
using FlaUI.Cli.Services;

namespace FlaUI.Cli.Commands.Elem;

public static class TransformCommand
{
    public static Command Create(Option<string?> sessionOption)
    {
        var idOption = new Option<string>("--id") { Description = "Element ID" };
        idOption.Required = true;
        var xOption = new Option<double?>("--x") { Description = "X coordinate to move to" };
        var yOption = new Option<double?>("--y") { Description = "Y coordinate to move to" };
        var widthOption = new Option<double?>("--width") { Description = "Width to resize to" };
        var heightOption = new Option<double?>("--height") { Description = "Height to resize to" };
        var rotateOption = new Option<double?>("--rotate") { Description = "Degrees to rotate" };
        var windowOption = CommandHelper.CreateWindowOption();

        var command = new Command("transform", "Move, resize, or rotate an element via the Transform pattern");
        command.Add(idOption);
        command.Add(xOption);
        command.Add(yOption);
        command.Add(widthOption);
        command.Add(heightOption);
        command.Add(rotateOption);
        command.Add(windowOption);

        command.SetAction((ParseResult parseResult) =>
        {
            var elementId = parseResult.GetValue(idOption)!;
            var x = parseResult.GetValue(xOption);
            var y = parseResult.GetValue(yOption);
            var width = parseResult.GetValue(widthOption);
            var height = parseResult.GetValue(heightOption);
            var rotate = parseResult.GetValue(rotateOption);
            var windowHandle = parseResult.GetValue(windowOption);
            var sessionFlag = parseResult.GetValue(sessionOption);

            if (!x.HasValue && !y.HasValue && !width.HasValue && !height.HasValue && !rotate.HasValue)
            {
                JsonOutput.Write(new ErrorResult(false, "At least one transform option (--x, --y, --width, --height, --rotate) must be specified."));
                Environment.ExitCode = ExitCodes.Error;
                return;
            }

            using var engine = new AutomationEngine();
            var sessionManager = new SessionManager();

            try
            {
                var sessionPath = SessionManager.ResolveSessionPath(sessionFlag);
                var session = sessionManager.Load(sessionPath);
                var (_, mainWindow) = engine.ReattachFromSession(session);

                var targetWindow = CommandHelper.ResolveWindow(engine, mainWindow, windowHandle);
                var element = CommandHelper.ResolveElement(engine, sessionManager, session, targetWindow, elementId);
                if (element is null)
                {
                    JsonOutput.Write(new ErrorResult(false, $"Element '{elementId}' not found."));
                    Environment.ExitCode = ExitCodes.Unresolvable;
                    return;
                }

                var result = AutomationEngine.TransformElement(element, elementId, x, y, width, height, rotate);

                CommandHelper.RecordStep(session, "elem transform", elementId,
                    new Dictionary<string, object?> { ["x"] = x, ["y"] = y, ["width"] = width, ["height"] = height, ["rotate"] = rotate }, true);
                sessionManager.Save(sessionPath, session);

                JsonOutput.Write(result);
                Environment.ExitCode = ExitCodes.Success;
            }
            catch (Exception ex)
            {
                JsonOutput.Write(new ErrorResult(false, ex.Message));
                Environment.ExitCode = ExitCodes.Error;
            }
        });

        return command;
    }
}
