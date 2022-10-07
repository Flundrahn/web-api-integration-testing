using Microsoft.EntityFrameworkCore;
using WebApi.Models;

public class ItemsContext : DbContext
{
    public ItemsContext(DbContextOptions<ItemsContext> options)
        : base(options)
    {
    }

    public DbSet<WebApi.Models.Item> Item { get; set; } = default!;

	protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Item>().HasData(SeedData.Items());
    }

}
