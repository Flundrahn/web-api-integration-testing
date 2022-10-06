using Microsoft.EntityFrameworkCore;

public class ItemsContext : DbContext
{
    public ItemsContext(DbContextOptions<ItemsContext> options)
        : base(options)
    {
    }

    public DbSet<WebApi.Models.Item> Item { get; set; } = default!;
}
