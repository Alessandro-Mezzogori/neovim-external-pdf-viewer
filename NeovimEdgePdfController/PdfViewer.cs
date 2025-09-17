using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Microsoft.Web.WebView2.WinForms;

namespace NeovimEdgePdfController;

using Microsoft.Web.WebView2.Core;
using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

partial class PdfViewer : Form
{
    private int _port;
    private int _currentPage;
    private string _pdfPath;
    
    private readonly WebView2 _browser;
    private readonly PdfController _controller;

    public PdfViewer(string pdfPath, int width = 800, int height = 800, int page = 1, int port = 12345)
    {
        this._currentPage = page;
        this._pdfPath = Path.GetFullPath(pdfPath);
        this.ClientSize = new Size(width, height);
        this._port = port;
        
        // Component 
        this._browser = new Microsoft.Web.WebView2.WinForms.WebView2();
        this._controller = new PdfController(this._browser);
        _browser.Dock = DockStyle.Fill;
        this.Controls.Add(_browser);
        
        InitializeComponent();
    }

    private async Task InitializeWebView(string pdfPath)
    {
        Console.WriteLine("Initializing webview...");
        var env = await CoreWebView2Environment.CreateAsync();
        await _browser.EnsureCoreWebView2Async(env);
        
        Console.WriteLine("Initializing Html...");
        InitializeHtml(pdfPath);
    }

    private void InitializeHtml(string pdfPath)
    {
        var filename = $"{System.IO.Path.GetTempFileName()}.html";
        using (var fileStream = new FileStream(filename, FileMode.Create))
        {
            using (var writer = new StreamWriter(fileStream))
            {
                new HtmlBuilder()
                    .SetPdf(pdfPath)
                    .SetSource(Resources.WebviewHtml)
                    .SetOutput(writer)
                    .Build();
                
                writer.Flush();
            }
        }
        
        _browser.CoreWebView2.Navigate(filename);
    }
    
    protected override void OnResizeBegin(EventArgs e)
    {
        this.SuspendLayout();
        base.OnResizeBegin(e);
    }

    protected override void OnResizeEnd(EventArgs e)
    {
        this.ResumeLayout();
        base.OnResizeEnd(e);

        _controller.ResizeCanvas();
    }

    private Task StartHttpServer()
    {
        Console.WriteLine("Starting http server...");
        return Task.Run(async () =>
        {
            var prefix = $"http://localhost:{_port}/";
            Console.WriteLine("Listenin on {0}...", prefix);
            
            HttpListener listener = new HttpListener();
            listener.Prefixes.Add(prefix);
            
            // TODO: handle exception back to caller to change port
            listener.Start();

            while (true)
            {
                var ctx = listener.GetContext();
                var req = new StreamReader(ctx.Request.InputStream).ReadToEnd();

                Result? result = null;
                if (!string.IsNullOrWhiteSpace(req))
                {
                    Console.WriteLine(req);
                    try
                    {
                        result = await Invoke(() => _controller.HandleMessage(req));
                    }
                    catch (Exception ex)
                    {
                        result = new Result
                        {
                            Ok = false,
                            Message = ex.Message,
                        };
                    }
                }
                else
                {
                    result = new Result
                    {
                        Ok = false,
                        Message = "Not found",
                    };
                }

                ctx.Response.StatusCode = result?.Ok == true ? 200 : 500;
                if (result != null)
                {
                    var responseBody = JsonSerializer.Serialize(result, JsonSerializerOptions.Web);
                    ctx.Response.OutputStream.Write(Encoding.UTF8.GetBytes(responseBody));
                }
                ctx.Response.Close();
            }
        });
    }

    private async void PdfViewer_Load(object sender, EventArgs e)
    {
        Console.WriteLine("Loading...");
        
        await InitializeWebView(this._pdfPath);
        
        StartHttpServer();
        
        Console.WriteLine("Loaded...");
    }
}