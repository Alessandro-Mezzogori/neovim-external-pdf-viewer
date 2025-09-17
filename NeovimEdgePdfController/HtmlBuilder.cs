using System.DirectoryServices.ActiveDirectory;

namespace NeovimEdgePdfController;

public class HtmlBuilder
{
    private string? _resource = null;
    private string? _pdf = null;
    private StreamWriter?  _output = null;

    private const string DataMarker = "%embeddata%";
    
    public HtmlBuilder()
    {
        
    }

    public HtmlBuilder SetSource(string resource)
    {
        _resource = resource;
        return this;
    }

    public HtmlBuilder SetPdf(string path)
    {
        _pdf = path;
        return this;
    }

    public HtmlBuilder SetOutput(StreamWriter output)
    {
        _output = output;
        return this;
    }

    public void Build()
    {
        ArgumentNullException.ThrowIfNull(_resource);
        ArgumentNullException.ThrowIfNull(_pdf);
        ArgumentNullException.ThrowIfNull(_output);
        
        using var htmlStream = Resources.GetResource(Resources.WebviewHtml);
        using var htmlReader = new StreamReader(htmlStream);

        var reachedDataMarker = false;
        while (true)
        {
            var line = htmlReader.ReadLine();
            if (line == null)
                break;

            if (reachedDataMarker == false && line.Contains(DataMarker, StringComparison.Ordinal))
            {
                var pdf = Convert.ToBase64String(File.ReadAllBytes(_pdf));
                _output.Write(line.Replace(DataMarker, pdf));
                reachedDataMarker = true;
            }
            else
            {
                _output.WriteLine(line);
            }
        }
    }
}