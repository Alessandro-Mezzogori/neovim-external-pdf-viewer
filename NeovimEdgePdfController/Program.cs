using System.Reflection;

namespace NeovimEdgePdfController;

static class Program
{
    /// <summary>
    ///  The main entry point for the application.
    /// </summary>
    [STAThread]
    static void Main(string[] args)
    {
        // To customize application configuration such as set high DPI settings or default font,
        // see https://aka.ms/applicationconfiguration.
        ApplicationConfiguration.Initialize();

        string pdf = args[0];
        int witdh = args.Length > 2 ? int.Parse(args[1]) : 800;
        int height = args.Length > 3 ? int.Parse(args[2]) : 800;
        int page = args.Length > 4 ? int.Parse(args[3]) : 1;
        int port = args.Length > 5 ? int.Parse(args[4]) : 12345;
        
        Application.Run(new PdfViewer(pdf, witdh, height, page, port));
    }
}