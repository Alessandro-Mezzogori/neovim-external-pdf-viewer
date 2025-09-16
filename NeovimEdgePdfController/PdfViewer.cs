using System.Diagnostics.CodeAnalysis;
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
    private int _currentPage;
    private string _pdfPath;
    private string Url => $"file:///{_pdfPath}#page={_currentPage}&view=FitV&t{DateTime.UtcNow.Ticks}";
    
    private CoreWebView2Environment env;
    private CoreWebView2 webView;
    private WebView2 browser;

    public PdfViewer(string pdfPath)
    {
        // TODO variable client size
        this.ClientSize = new Size(800, 800);
        
        InitializeWebView(pdfPath);
        StartHttpServer();

        _currentPage = 0;
        _pdfPath = Path.GetFullPath(pdfPath);
    }

    private async void InitializeWebView(string pdfPath)
    {
        browser = new Microsoft.Web.WebView2.WinForms.WebView2(); 
        env = await CoreWebView2Environment.CreateAsync();
        await browser.EnsureCoreWebView2Async(env);
        webView = browser.CoreWebView2;
        
        this.Controls.Add(browser);
        browser.Dock = DockStyle.Fill;
        
        UpdateWebView();
    }

    private void UpdateWebView()
    {
        const string empty = "about:blank";
        string file = $"""
                       <script>
                         pdfdata = atob('{Convert.ToBase64String(File.ReadAllBytes(_pdfPath))}')
                       </script>
                       """;
        string html = $$"""
                            <html>
                                <head>
                                    <script src="https://cdnjs.cloudflare.com/ajax/libs/pdf.js/5.4.149/pdf.min.mjs" type="module"></script>
                                    <link rel="stylesheet" href="https://cdnjs.cloudflare.com/ajax/libs/pdf.js/5.4.149/pdf_viewer.min.css" integrity="sha512-qbvpAGzPFbd9HG4VorZWXYAkAnbwKIxiLinTA1RW8KGJEZqYK04yjvd+Felx2HOeKPDKVLetAqg8RIJqHewaIg==" crossorigin="anonymous" referrerpolicy="no-referrer" />
                                    
                                    <script>
                                      pdfdata = atob('{{Convert.ToBase64String(File.ReadAllBytes(_pdfPath))}}')
                                    </script>
                                    
                                    <script type="module">
                                      // If absolute URL from the remote server is provided, configure the CORS
                                      // header on that server.
                                      var url = '{{Url.Replace("\\", "/")}}';
                                    
                                      // Loaded via <script> tag, create shortcut to access PDF.js exports.
                                      var { pdfjsLib } = globalThis;
                                    
                                      // The workerSrc property shall be specified.
                                      pdfjsLib.GlobalWorkerOptions.workerSrc = 'https://cdnjs.cloudflare.com/ajax/libs/pdf.js/5.4.149/pdf.worker.mjs'; // '//mozilla.github.io/pdf.js/build/pdf.worker.mjs';
                                    
                                      // Asynchronous download of PDF
                                      var loadingTask = pdfjsLib.getDocument({data: pdfdata});
                                      loadingTask.promise.then(function(pdf) {
                                        console.log('PDF loaded');
                                    
                                        // Fetch the first page
                                        var pageNumber = 1;
                                        pdf.getPage(pageNumber).then(function(page) {
                                          console.log('Page loaded');
                                    
                                          var scale = 1.5;
                                          var viewport = page.getViewport({scale: scale});
                                    
                                          // Prepare canvas using PDF page dimensions
                                          var canvas = document.getElementById('the-canvas');
                                          var context = canvas.getContext('2d');
                                          canvas.height = viewport.height;
                                          canvas.width = viewport.width;
                                    
                                          // Render PDF page into canvas context
                                          var renderContext = {
                                            canvasContext: context,
                                            viewport: viewport
                                          };
                                          var renderTask = page.render(renderContext);
                                          renderTask.promise.then(function () {
                                            console.log('Page rendered');
                                          });
                                        });
                                      }, function (reason) {
                                        // PDF loading error
                                        console.error(reason);
                                      });
                                    </script>
                                </head>
                                <body>
                                    <canvas style="width:100%" id="the-canvas"></canvas>
                                </body>
                            </html>
                            """;

        
       html = $$"""
                            <html>
                                <head>
                                    <script src="https://cdnjs.cloudflare.com/ajax/libs/pdf.js/5.4.149/pdf.min.mjs" type="module"></script>
                                    <link rel="stylesheet" href="https://cdnjs.cloudflare.com/ajax/libs/pdf.js/5.4.149/pdf_viewer.min.css" integrity="sha512-qbvpAGzPFbd9HG4VorZWXYAkAnbwKIxiLinTA1RW8KGJEZqYK04yjvd+Felx2HOeKPDKVLetAqg8RIJqHewaIg==" crossorigin="anonymous" referrerpolicy="no-referrer" />
                                    
                                    <script>
                                      pdfdata = atob('{{Convert.ToBase64String(File.ReadAllBytes(_pdfPath))}}')
                                    </script>
                                    <script type="module">
                                      // If absolute URL from the remote server is provided, configure the CORS
                                      // header on that server.

                                      // Loaded via <script> tag, create shortcut to access PDF.js exports.
                                      var { pdfjsLib } = globalThis;

                                      // The workerSrc property shall be specified.
                                      pdfjsLib.GlobalWorkerOptions.workerSrc = 'https://cdnjs.cloudflare.com/ajax/libs/pdf.js/5.4.149/pdf.worker.mjs'; // '//mozilla.github.io/pdf.js/build/pdf.worker.mjs';

                                      var pdfDoc = null,
                                          pageNum = 1,
                                          pageRendering = false,
                                          pageNumPending = null,
                                          scale = 0.8,
                                          canvas = document.getElementById('the-canvas'),
                                          ctx = canvas.getContext('2d');

                                      /**
                                       * Get page info from document, resize canvas accordingly, and render page.
                                       * @param num Page number.
                                       */
                                      function renderPage(num) {
                                        pageRendering = true;
                                        // Using promise to fetch the page
                                        pdfDoc.getPage(num).then(function(page) {
                                        //const desiredWidth =  window.innerWidth - 0.01*window.innerWidth;
                            const desiredWidth = parseFloat(getComputedStyle(document.getElementById('canvas-container')).width.slice(0, -2));
                                          const original_viewport = page.getViewport({scale: 1.0});
                                          const scale =  desiredWidth / original_viewport.width;
                                          var viewport = page.getViewport({scale: scale});
                                          
                                          console.log("viewport size", scale, canvas.width, original_viewport.width, viewport.width);
                                        console.log(ctx);
                                          //var viewport = page.getViewport(canvas.width / page.getViewport(1.0).width);
                                          canvas.height = viewport.height;
                                          canvas.width = viewport.width;

                                          // Render PDF page into canvas context
                                          var renderContext = {
                                            canvasContext: ctx,
                                            viewport: viewport
                                          };
                                          var renderTask = page.render(renderContext);

                                          // Wait for rendering to finish
                                          renderTask.promise.then(function() {
                                            pageRendering = false;
                                            if (pageNumPending !== null) {
                                              // New page rendering is pending
                                              renderPage(pageNumPending);
                                              pageNumPending = null;
                                            }
                                          });
                                        });

                                        // Update page counters
                                        document.getElementById('page_num').textContent = num;
                                      }

                                      /**
                                       * If another page rendering in progress, waits until the rendering is
                                       * finised. Otherwise, executes rendering immediately.
                                       */
                                      function queueRenderPage(num) {
                                        if (pageRendering) {
                                          pageNumPending = num;
                                        } else {
                                          renderPage(num);
                                        }
                                      }

                                      /**
                                       * Displays previous page.
                                       */
                                      function onPrevPage() {
                                        if (pageNum <= 1) {
                                          return;
                                        }
                                        pageNum--;
                                        queueRenderPage(pageNum);
                                      }
                                      document.getElementById('prev').addEventListener('click', onPrevPage);

                                      /**
                                       * Displays next page.
                                       */
                                      function onNextPage() {
                                        if (pageNum >= pdfDoc.numPages) {
                                          return;
                                        }
                                        pageNum++;
                                        queueRenderPage(pageNum);
                                      }
                                      document.getElementById('next').addEventListener('click', onNextPage);

                                      /**
                                       * Asynchronously downloads PDF.
                                       */
                                      pdfjsLib.getDocument({data: pdfdata}).promise.then(function(pdfDoc_) {
                                        pdfDoc = pdfDoc_;
                                        document.getElementById('page_count').textContent = pdfDoc.numPages;

                                        // Initial/first page rendering
                                        renderPage(pageNum);
                                      });
                                      
                                      window.onNextPage = onNextPage;
                                      window.onPrevPage = onPrevPage;
                                      window.onSetPage = (num) => {
                                          pageNum = num;
                                          queueRenderPage(pageNum);
                                      }
                                    </script>

                            <body>
                                    <h1>PDF.js Previous/Next example</h1>

                                    <p>Please use <a href="https://mozilla.github.io/pdf.js/getting_started/#download"><i>official releases</i></a> in production environments.</p>

                                    <div>
                                      <button id="prev">Previous</button>
                                      <button id="next">Next</button>
                                      &nbsp; &nbsp;
                                      <span>Page: <span id="page_num"></span> / <span id="page_count"></span></span>
                                    </div>

                                    <div id="canvas-container">
                                        <canvas id="the-canvas"></canvas>
                                    </div>
                                    </body>
                            </html>
                            """;

        string filename = System.IO.Path.GetTempFileName() + ".html";
        File.WriteAllText(filename, html);

        //browser.Source = new Uri(Url);
        //browser.Reload();
        //browser.Update();
        //browser.Refresh();
        webView.Navigate(filename);
    }

    private void SetPage(int page)
    {
        _currentPage = page;
    }

    private void NextPage()
    {
        SetPage(_currentPage + 1);
    }

    private void PreviousPage()
    {
        SetPage(_currentPage - 1);   
    }

    protected override void OnDoubleClick(EventArgs e)
    {
        base.OnDoubleClick(e);
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

        webView.ExecuteScriptAsync("""
                                   let canvas = document.getElementById('the-canvas');
                                   canvas.width = window.screen.width;
                                   canvas.height = window.screen.height;
                                   """);

        // NextPage();
        // Console.WriteLine($"NEXT {_currentPage}");
    }

    private void StartHttpServer()
    {
        Task.Run(async () =>
        {
            HttpListener listener = new HttpListener();
            listener.Prefixes.Add("http://localhost:12345/");
            listener.Start();

            while (true)
            {
                var ctx = listener.GetContext();
                var req = new StreamReader(ctx.Request.InputStream).ReadToEnd();

                if (req.StartsWith("page:"))
                {
                    var page = req.Substring(5);
                    try
                    {
                        this.Invoke(() =>
                        {
                            //webView.ExecuteScriptAsync($"console.log({page});");
                            webView.ExecuteScriptAsync($"console.log(window);");
                            //webView.ExecuteScriptAsync($"console.log(window.onNextPage, window.onPrevPage, window.onSetPage);");
                            webView.ExecuteScriptAsync($"onSetPage({page});");
                        });
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                    }
                }

                var buffer = Encoding.UTF8.GetBytes("OK");
                ctx.Response.OutputStream.Write(buffer, 0, buffer.Length);
                ctx.Response.Close();
            }
        });
    }
}
