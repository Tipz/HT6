using System.Security.Cryptography;
using System.Text;

namespace Together.Api.Security;

public static class ContentSecurityPolicyFactory
{
    private const string ImportMapStart = "<script type=\"importmap\">";
    private const string ScriptEnd = "</script>";

    public static string Create(bool analyticsEnabled, string? indexHtml)
    {
        var importMapHash = GetImportMapHash(indexHtml);
        var scriptSources = new StringBuilder("'self' 'wasm-unsafe-eval'");
        if (importMapHash is not null)
            scriptSources.Append(" '").Append(importMapHash).Append('\'');
        if (analyticsEnabled)
            scriptSources.Append(" https://mc.yandex.ru");

        var yandexImageSource = analyticsEnabled ? " https://mc.yandex.ru" : string.Empty;
        var yandexConnectSource = analyticsEnabled ? " https://mc.yandex.ru" : string.Empty;
        return $"default-src 'self'; script-src {scriptSources}; style-src 'self' 'unsafe-inline'; "
            + $"img-src 'self' data:{yandexImageSource}; connect-src 'self'{yandexConnectSource}; "
            + "font-src 'self' data:; object-src 'none'; base-uri 'self'; frame-ancestors 'none'";
    }

    public static string? GetImportMapHash(string? indexHtml)
    {
        if (indexHtml is null)
            return null;

        var start = indexHtml.IndexOf(ImportMapStart, StringComparison.Ordinal);
        if (start < 0)
            return null;
        start += ImportMapStart.Length;
        var end = indexHtml.IndexOf(ScriptEnd, start, StringComparison.Ordinal);
        if (end < 0)
            return null;

        var content = indexHtml[start..end]
            .Replace("\r\n", "\n", StringComparison.Ordinal)
            .Replace('\r', '\n');
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(content));
        return $"sha256-{Convert.ToBase64String(hash)}";
    }
}
