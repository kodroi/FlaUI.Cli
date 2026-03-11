using System.CommandLine;
using FlaUI.Cli.Infrastructure;
using FlaUI.Cli.Models;
using FlaUI.Cli.Services;

namespace FlaUI.Cli.Commands.Elem;

public static class PropsCommand
{
    public static Command Create(Option<string?> sessionOption)
    {
        var idOption = new Option<string>("--id") { Description = "Short element ID returned by 'elem find'. Returns all UIA properties including bounds, enabled, and offscreen state" };
        idOption.Required = true;

        var command = new Command("props", "Get element properties");
        command.Add(idOption);

        command.SetAction((ParseResult parseResult) =>
        {
            var elementId = parseResult.GetValue(idOption)!;
            var sessionFlag = parseResult.GetValue(sessionOption);

            using var engine = new AutomationEngine();
            var sessionManager = new SessionManager();

            try
            {
                var sessionPath = SessionManager.ResolveSessionPath(sessionFlag);
                var session = sessionManager.Load(sessionPath);
                var (_, mainWindow) = engine.ReattachFromSession(session);

                var element = ResolveElementById(engine, sessionManager, session, mainWindow, elementId);
                if (element is null)
                {
                    JsonOutput.Write(new ErrorResult(false, $"Element '{elementId}' not found."));
                    Environment.ExitCode = ExitCodes.Unresolvable;
                    return;
                }

                var bounds = element.BoundingRectangle;

                JsonOutput.Write(new ElementPropsResult(
                    Success: true,
                    Message: "Properties retrieved.",
                    ElementId: elementId,
                    AutomationId: element.Properties.AutomationId.ValueOrDefault,
                    Name: element.Properties.Name.ValueOrDefault,
                    ControlType: element.Properties.ControlType.ValueOrDefault.ToString(),
                    ClassName: element.Properties.ClassName.ValueOrDefault,
                    Bounds: new BoundsInfo(bounds.X, bounds.Y, bounds.Width, bounds.Height),
                    IsEnabled: element.IsEnabled,
                    IsOffscreen: element.IsOffscreen,
                    RuntimeId: element.Properties.RuntimeId.ValueOrDefault,
                    HelpText: element.Properties.HelpText.ValueOrDefault,
                    AcceleratorKey: element.Properties.AcceleratorKey.ValueOrDefault));

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

    internal static FlaUI.Core.AutomationElements.AutomationElement? ResolveElementById(
        AutomationEngine engine,
        SessionManager sessionManager,
        SessionFile session,
        FlaUI.Core.AutomationElements.AutomationElement mainWindow,
        string elementId)
    {
        var entry = sessionManager.GetElement(session, elementId);
        if (entry is null) return null;

        var resolver = engine.CreateSelectorResolver();
        var result = resolver.Resolve(mainWindow, entry.AutomationId, entry.Name,
            entry.ControlType, entry.ClassName, 5000);

        return result?.Element;
    }
}
