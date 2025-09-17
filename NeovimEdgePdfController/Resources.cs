namespace NeovimEdgePdfController;

public static class Resources
{
    public const string WebviewHtml = "NeovimEdgePdfController.resources.webview.html";

    public static Stream GetResource(string name)
    {
        var stream = typeof(INeovimEdgePdfControllerMarker).Assembly.GetManifestResourceStream(name);
        ArgumentNullException.ThrowIfNull(stream, $"No embedded resource found {name}");
        
        return stream;
    }
}