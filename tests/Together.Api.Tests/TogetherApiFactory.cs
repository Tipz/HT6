using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Together.Api.Data;

namespace Together.Api.Tests;

public sealed class TogetherApiFactory : WebApplicationFactory<Program>
{
    private readonly string databaseName = $"together-tests-{Guid.NewGuid()}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            var databaseRegistrations = services.Where(descriptor =>
                descriptor.ServiceType == typeof(DbContextOptions<ApplicationDbContext>) ||
                descriptor.ServiceType.FullName?.Contains("IDbContextOptionsConfiguration", StringComparison.Ordinal) == true).ToArray();
            foreach (var descriptor in databaseRegistrations)
                services.Remove(descriptor);
            services.RemoveAll<ApplicationDbContext>();
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseInMemoryDatabase(databaseName));
        });
    }
}
