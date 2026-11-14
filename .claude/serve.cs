// Minimal static file server for previewing the portfolio when Python is not installed.
// usage: dotnet run .claude/serve.cs -- <root folder> <port>
using System.Net;

string root = Path.GetFullPath(args.Length > 0 ? args[0] : ".");
// PORT (set by the preview tool when the configured port is busy) wins over the argument
int port = int.TryParse(Environment.GetEnvironmentVariable("PORT"), out int envPort) ? envPort
         : args.Length > 1 ? int.Parse(args[1]) : 4600;

var types = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
{
    [".html"] = "text/html; charset=utf-8", [".htm"] = "text/html; charset=utf-8",
    [".css"] = "text/css; charset=utf-8", [".js"] = "text/javascript; charset=utf-8",
    [".json"] = "application/json; charset=utf-8", [".md"] = "text/markdown; charset=utf-8",
    [".tsv"] = "text/tab-separated-values; charset=utf-8", [".txt"] = "text/plain; charset=utf-8",
    [".svg"] = "image/svg+xml", [".png"] = "image/png", [".jpg"] = "image/jpeg", [".jpeg"] = "image/jpeg",
    [".gif"] = "image/gif", [".webp"] = "image/webp", [".ico"] = "image/x-icon",
    [".woff"] = "font/woff", [".woff2"] = "font/woff2", [".ttf"] = "font/ttf",
    [".mp4"] = "video/mp4", [".webm"] = "video/webm", [".cs"] = "text/plain; charset=utf-8",
};

var listener = new HttpListener();
listener.Prefixes.Add($"http://localhost:{port}/");
listener.Start();
Console.WriteLine($"Serving {root} at http://localhost:{port}/");

while (true)
{
    var ctx = await listener.GetContextAsync();
    _ = Task.Run(async () =>
    {
        var res = ctx.Response;
        try
        {
            string rel = Uri.UnescapeDataString(ctx.Request.Url!.AbsolutePath).TrimStart('/');
            string path = Path.GetFullPath(Path.Combine(root, rel));
            if (!path.StartsWith(root, StringComparison.OrdinalIgnoreCase)) { res.StatusCode = 403; return; }
            if (Directory.Exists(path)) path = Path.Combine(path, "index.html");
            if (!File.Exists(path)) { res.StatusCode = 404; return; }
            res.ContentType = types.GetValueOrDefault(Path.GetExtension(path), "application/octet-stream");
            res.AddHeader("Cache-Control", "no-store");
            var bytes = await File.ReadAllBytesAsync(path);
            res.ContentLength64 = bytes.Length;
            if (ctx.Request.HttpMethod != "HEAD") await res.OutputStream.WriteAsync(bytes);
            Console.WriteLine($"200 /{rel}");
        }
        catch (Exception e) { res.StatusCode = 500; Console.WriteLine($"500 {e.Message}"); }
        finally { res.Close(); }
    });
}
