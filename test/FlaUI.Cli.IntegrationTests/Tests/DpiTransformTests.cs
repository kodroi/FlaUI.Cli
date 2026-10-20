using FlaUI.Cli.Services;

namespace FlaUI.Cli.IntegrationTests.Tests;

[Collection("TestApp")]
public class DpiTransformTests
{
    [Fact]
    public async Task Resize_UsesPhysicalPixelDimensions()
    {
        await using var app = new DpiFixture(new(SolutionLocator.GetCliPath(SolutionLocator.FindSolutionRoot())));
        await app.Start();
        using (var engine = new AutomationEngine())
        {
            engine.Attach(app.Pid);
            var window = engine.GetMainWindow();
            var before = window.BoundingRectangle;
            AutomationEngine.TransformElement(window, "fixture", null, null, 640, 480, null);
            var resized = window.BoundingRectangle;
            Assert.Equal(640, resized.Width);
            Assert.Equal(480, resized.Height);
            AutomationEngine.TransformElement(window, "fixture", before.X + 20, before.Y + 20, null, null, null);
            var moved = window.BoundingRectangle;
            Assert.Equal(before.X + 20, moved.X);
            Assert.Equal(before.Y + 20, moved.Y);
        }
    }
}
