using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.FileProviders;

namespace Together.BrowserTests;

// Test-only static host. The deployed product consists of wwwroot files.
public static class PublishedPreview
{
    public static async Task RunAsync()
    {
        var root = Path.GetFullPath("artifacts/publish/wwwroot");
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { ContentRootPath = root });
        builder.WebHost.UseUrls("http://localhost:5181");
        var app = builder.Build();
        app.UseFileServer(new FileServerOptions
        {
            FileProvider = new PhysicalFileProvider(root),
            StaticFileOptions = { ServeUnknownFileTypes = true }
        });
        await app.RunAsync();
    }
}
