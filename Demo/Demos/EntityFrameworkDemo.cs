using Demo.Data;
using Microsoft.EntityFrameworkCore;
using RoslynRules.Models;

namespace Demo.Demos;

public static class EntityFrameworkDemo
{
    public static async Task Run()
    {
        // Create and seed the in-memory database
        await using var db = new GroceryDbContext();
        await SeedDatabase(db);

        // Fetch data into memory before compiling rules
        var items = await db.GroceryItems.ToListAsync();
        var lists = await db.GroceryLists.Include(l => l.Items).ToListAsync();

        // Rule 1: Check if list has perishable items (Dairy or Produce)
        var perishableRule = new Rule
        {
            Description = "Has perishable items",
            Expression = "items.Any(i => i.Category == \"Dairy\" || i.Category == \"Produce\")"
        };

        // Rule 2: Check if total is under $30
        var budgetRule = new Rule
        {
            Description = "Under $30 budget",
            Expression = "items.Sum(i => i.Price) <= 30m"
        };

        // Compile rules
        var compileParams = new[] { new RuleParameter("items", typeof(List<GroceryItem>)) };
        perishableRule.Compile(DemoRunner.Compiler, compileParams, null, DemoRunner.ReferenceProvider);
        budgetRule.Compile(DemoRunner.Compiler, compileParams, null, DemoRunner.ReferenceProvider);

        // Execute against each grocery list by looking up item details from the DB
        foreach (var list in lists)
        {
            // Map list items to full grocery item details
            var listItems = list.Items
                .Select(li => items.FirstOrDefault(g => g.Name == li.ItemName))
                .Where(g => g != null)
                .ToList();

            var execParams = new[] { new RuleParameter("items", typeof(List<GroceryItem>), listItems) };

            var perishableResult = perishableRule.Execute(execParams);
            var budgetResult = budgetRule.Execute(execParams);

            Console.WriteLine($"  List: {list.Name} ({listItems.Count} items)");
            DemoRunner.PrintResult(perishableResult, "  Has perishables");
            DemoRunner.PrintResult(budgetResult, "  Under $30");

            // Show item breakdown
            foreach (var item in listItems)
            {
                var stock = item.InStock ? "in stock" : "OUT OF STOCK";
                Console.WriteLine($"    - {item.Name}: ${item.Price:F2} ({item.Category}, {stock})");
            }
        }
    }

    private static async Task SeedDatabase(GroceryDbContext db)
    {
        var groceryItems = new List<GroceryItem>
        {
            new() { Name = "Milk", Category = "Dairy", Price = 3.49m, InStock = true },
            new() { Name = "Bread", Category = "Bakery", Price = 2.99m, InStock = true },
            new() { Name = "Eggs", Category = "Dairy", Price = 4.29m, InStock = true },
            new() { Name = "Cheese", Category = "Dairy", Price = 5.99m, InStock = false },
            new() { Name = "Apples", Category = "Produce", Price = 1.99m, InStock = true },
            new() { Name = "Chicken", Category = "Meat", Price = 8.99m, InStock = true },
            new() { Name = "Rice", Category = "Pantry", Price = 3.79m, InStock = true },
            new() { Name = "Coffee", Category = "Beverages", Price = 7.99m, InStock = false }
        };

        await db.GroceryItems.AddRangeAsync(groceryItems);

        var weeklyList = new GroceryList
        {
            Name = "Weekly Shopping",
            Items = new List<GroceryListItem>
            {
                new() { ItemName = "Milk", Quantity = 1 },
                new() { ItemName = "Bread", Quantity = 2 },
                new() { ItemName = "Eggs", Quantity = 1 },
                new() { ItemName = "Apples", Quantity = 5 }
            }
        };

        var partyList = new GroceryList
        {
            Name = "Party Prep",
            Items = new List<GroceryListItem>
            {
                new() { ItemName = "Cheese", Quantity = 3 },
                new() { ItemName = "Chicken", Quantity = 2 },
                new() { ItemName = "Rice", Quantity = 1 },
                new() { ItemName = "Coffee", Quantity = 2 }
            }
        };

        await db.GroceryLists.AddRangeAsync(weeklyList, partyList);
        await db.SaveChangesAsync();
    }
}
