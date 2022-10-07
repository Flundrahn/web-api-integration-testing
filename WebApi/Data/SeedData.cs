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