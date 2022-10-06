using WebApi.Models;

internal class SeedData
{
    internal static Item[] Items()
    {
        var id = 1;

        var items = new Item[]
        {
            new Item
            {
                // NOTE The key value is required so it must be supplied. When a migration is created, the SQL Server provider will enable the insertion of Identity values
                Id = id++,
                Name = "Item1",
                IsComplete = false
            },
            new Item
            {
                Id = id++,
                Name = "Item2",
                IsComplete = false
            }
        };

        return items;
    }
}