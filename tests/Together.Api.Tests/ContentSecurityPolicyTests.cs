using Together.Api.Security;

namespace Together.Api.Tests;

public sealed class ContentSecurityPolicyTests
{
    [Fact]
    public void Create_AllowsPublishedImportMapByHashWithoutUnsafeInline()
    {
        const string html = "<script type=\"importmap\">{\n  \"imports\": {}\n}</script>";

        var hash = ContentSecurityPolicyFactory.GetImportMapHash(html);
        var policy = ContentSecurityPolicyFactory.Create(false, html);

        Assert.NotNull(hash);
        Assert.Contains($"'{hash}'", policy);
        Assert.DoesNotContain("'unsafe-inline'", policy.Split("; ").Single(x => x.StartsWith("script-src")));
        Assert.DoesNotContain("mc.yandex.ru", policy);
    }

    [Fact]
    public void Create_AddsOnlyApprovedAnalyticsOrigin()
    {
        var policy = ContentSecurityPolicyFactory.Create(true, null);

        Assert.Contains("script-src 'self' 'wasm-unsafe-eval' https://mc.yandex.ru", policy);
        Assert.Contains("connect-src 'self' https://mc.yandex.ru", policy);
    }
}
