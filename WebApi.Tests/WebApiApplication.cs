using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Xunit.Abstractions;

namespace WebApi.Tests;

class WebApiApplication : WebApplicationFactory<Program>
{
    protected override IHost CreateHost(IHostBuilder builder)
    {
        builder.UseEnvironment("Test");

        // builder.ConfigureLogging TODO Configure logging here, xUnitLogger or similar 

        builder.ConfigureServices(services => 
        {
            // TODO Find out if I can avoid having to do this
            // var descriptor = services.SingleOrDefault(
			// 		d => d.ServiceType ==
			// 				 typeof(DbContextOptions<AppDbContext>)); // TODO Test if this actually does anything when it looks like it is removed below

            string dbName = "DbForTesting";

            // Add db to context
            services.AddDbContext<ItemsContext>(options =>
            {
                options.UseInMemoryDatabase(dbName);
            });

            var serviceProvider = services.BuildServiceProvider();

            using (var scope = serviceProvider.CreateScope())
            using (var appContext = scope.ServiceProvider.GetRequiredService<ItemsContext>())
            {
                try
                {
                    appContext.Database.EnsureCreated();
                }
                catch (Exception ex)
                {
                    // TODO Log error
                    throw;
                }
            }
        });        

        return base.CreateHost(builder);
    }
}