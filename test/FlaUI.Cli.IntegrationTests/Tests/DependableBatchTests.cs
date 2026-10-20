using System.Text.Json;

namespace FlaUI.Cli.IntegrationTests.Tests;

[Collection("TestApp")]
public class DependableBatchTests(TestAppFixture fixture)
{
    [Theory]
    [InlineData("windowHandle")]
    [InlineData("effectiveWindowHandle")]
    public async Task Batch_ReusesReturnedHandle_AndReportsStepFailure(string handleField)
    {
        var file = Path.Combine(Path.GetTempPath(), $"flaui-batch-{Guid.NewGuid():N}.json");
        File.WriteAllText(file, """
            {"steps":[
              {"cmd":"elem find","args":{"aid":"SubmitButton"}},
              {"cmd":"elem get-value","args":{"id":"$steps[0].elementId","window":"$steps[0].windowHandle"}},
              {"cmd":"window focus","args":{"handle":"0xNOT"}}
            ]}
            """.Replace("$steps[0].windowHandle", "$steps[0]." + handleField));
        try
        {
            var response = await fixture.Cli.RunAsync($"batch --file \"{file}\" --continue-on-error {fixture.SessionArg}");
            Assert.Equal(1, response.ExitCode);
            using var json = JsonDocument.Parse(response.Stdout);
            Assert.Equal("batch_failed", json.RootElement.GetProperty("errorCode").GetString());
            var steps = json.RootElement.GetProperty("steps");
            Assert.True(steps[1].GetProperty("success").GetBoolean());
            Assert.Equal(steps[0].GetProperty("result").GetProperty("windowHandle").GetString(),
                steps[1].GetProperty("result").GetProperty("effectiveWindowHandle").GetString());
            Assert.Equal("invalid_handle", steps[2].GetProperty("error").GetProperty("errorCode").GetString());
        }
        finally { File.Delete(file); }
    }
}
