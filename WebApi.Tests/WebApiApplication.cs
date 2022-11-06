using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;

namespace WebApi.Tests;

class WebApiApplication : WebApplicationFactory<Program>
{
    protected override IHost CreateHost(IHostBuilder builder)
    {
        // 1.
        builder.UseEnvironment("Testing");

        // NOTE Here we could use builder.ConfigureLogging to configure logging here, which I understand is the way we can get hold of what is logged inside our API, even though it is running in-memory of the test.

        builder.ConfigureServices(services =>
        {
            // 2.
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<ItemsContext>)
            );
            services.Remove(descriptor);

            // 3.
            string dbName = "DbForTesting";
            services.AddDbContext<ItemsContext>(options =>
            {
                options.UseInMemoryDatabase(dbName);
            });

            // 4.
            var serviceProvider = services.BuildServiceProvider();
            using (var scope = serviceProvider.CreateScope())
            using (var dbContext = scope.ServiceProvider.GetRequiredService<ItemsContext>())
            {
                try
                {
                    // NOTE Using EnsureCreated is not recommended for relational db if one plans to use EF Migrations, see MS Docs link in end, but should be fine for our test db.
                    dbContext.Database.EnsureCreated();
                }
                catch (Exception ex)
                {
                    // TODO Log error here
                    throw;
                }
            }
        });

        // 5.
        return base.CreateHost(builder);
    }
}