using System.Collections.Concurrent;
using System.Text.Json;
using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace Together.BrowserTests;

public static class StartupSmoke
{
    public static async Task<int> RunAsync(IPlaywright playwright, string baseUrl, string evidence, bool beforeFix)
    {
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
        await using var context = await browser.NewContextAsync(new()
        {
            ViewportSize = new() { Width = 1440, Height = 1000 },
            Locale = "ru-RU"
        });
        var page = await context.NewPageAsync();
        var errors = new ConcurrentQueue<string>();
        var failures = new ConcurrentQueue<string>();
        var noContentNavigations = new ConcurrentQueue<string>();
        var statuses = new ConcurrentDictionary<string, int>();
        page.PageError += (_, message) => errors.Enqueue(message);
        page.Console += (_, message) => { if (message.Type == "error") errors.Enqueue(message.Text); };
        page.Response += (_, response) =>
        {
            statuses[response.Url] = response.Status;
            if (response.Status >= 400) failures.Enqueue($"{response.Status} {response.Url}");
        };
        page.RequestFailed += (_, request) =>
        {
            // Chromium aborts navigation to DevServer's intentional HTTP 204 endpoint.
            // Keep this observation in the report; script failures and all 4xx still fail.
            if (request.Failure == "net::ERR_ABORTED" &&
                new Uri(request.Url).AbsolutePath == "/_framework/blazor-hotreload" &&
                statuses.TryGetValue(request.Url, out var status) && status == 204)
            {
                noContentNavigations.Enqueue(request.Url);
                return;
            }
            failures.Enqueue($"{request.Failure} {request.Url}");
        };
        var modules = new List<object>();
        var passed = false;
        string? assertionError = null;
        try
        {
            await page.GotoAsync(baseUrl);
            // Validate the URLs advertised by this live server, not just files on disk.
            var imports = await page.EvaluateAsync<string[]>("""
                () => Object.values(JSON.parse(document.querySelector('script[type=importmap]').textContent).imports)
                    .filter(path => path.startsWith('./_framework/') && path.endsWith('.js'))
                """);
            using var http = new HttpClient();
            foreach (var path in imports.Distinct())
            {
                var url = new Uri(new Uri(baseUrl.TrimEnd('/') + "/"), path);
                using var response = await http.GetAsync(url);
                modules.Add(new { url = url.ToString(), status = (int)response.StatusCode });
                if (!response.IsSuccessStatusCode) failures.Enqueue($"{(int)response.StatusCode} {url}");
            }
            await Expect(page.GetByRole(AriaRole.Heading, new() { Name = "Все варианты. Все расходы." }))
                .ToBeVisibleAsync(new() { Timeout = 15000 });
            await Expect(page.Locator(".variant-card")).ToHaveCountAsync(3);
            await Expect(page.Locator("#comparison table")).ToBeVisibleAsync();
            await page.GetByRole(AriaRole.Button, new() { Name = "Новая поездка", Exact = true }).ClickAsync();
            await Expect(page.Locator("dialog")).ToBeVisibleAsync();
            await page.GetByRole(AriaRole.Button, new() { Name = "Закрыть форму", Exact = true }).ClickAsync();
            await Expect(page.Locator("dialog")).ToHaveCountAsync(0);
            passed = errors.IsEmpty && failures.IsEmpty;
        }
        catch (Exception ex) when (ex is PlaywrightException or HttpRequestException)
        {
            assertionError = ex.Message;
        }
        var phase = beforeFix ? "before" : "after";
        await page.ScreenshotAsync(new() { Path = Path.Combine(evidence, $"startup-{phase}.png"), FullPage = true });
        await File.WriteAllTextAsync(Path.Combine(evidence, $"startup-{phase}.json"), JsonSerializer.Serialize(new
        {
            baseUrl,
            browser = browser.Version,
            passed,
            errors = errors.ToArray(),
            failedRequests = failures.ToArray(),
            noContentNavigations = noContentNavigations.ToArray(),
            modules,
            assertionError
        }, new JsonSerializerOptions { WriteIndented = true }));
        Console.WriteLine($"{(passed ? "PASS" : "FAIL")} startup: {baseUrl}, HTTP failures={failures.Count}, browser errors={errors.Count}");
        return passed ? 0 : 1;
    }
}
