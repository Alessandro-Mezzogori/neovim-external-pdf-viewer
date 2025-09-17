using System.Text.Json;

namespace NeovimEdgePdfController;

public class PdfController
{
    private readonly Microsoft.Web.WebView2.WinForms.WebView2 _browser;
    
    public PdfController(Microsoft.Web.WebView2.WinForms.WebView2 browser)
    {
        _browser = browser;
    }

    private async Task<Result> Execute(string message)
    {
        string result = await _browser.ExecuteScriptAsync(message);
        return JsonSerializer.Deserialize<Result>(result, JsonSerializerOptions.Web);
    }

    public Task<Result> HandleMessage(string message)
    {
        return Execute($"handle_message('{message}');");
    }
    
    public Task ResizeCanvas()
    {
        return Task.CompletedTask;
        return Execute("""
                     let canvas = document.getElementById('the-canvas');
                     canvas.width = window.screen.width;
                     canvas.height = window.screen.height;
                     """);
    }
}

public record struct Result
{
    public bool Ok { get; init; }
    public string? Message { get; init; }

    public override string ToString()
    {
        return $"{Ok} - Message: {Message}";
    }
}
