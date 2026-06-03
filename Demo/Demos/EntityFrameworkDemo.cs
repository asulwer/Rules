using Demo.Data;
using Microsoft.EntityFrameworkCore;
using RoslynRules.Extensions;
using RoslynRules.Models;

namespace Demo.Demos;

public static class EntityFrameworkDemo
{
    public static async Task Run()
    {
        // ── Phase 1: Define and persist a workflow to EF ──
        await using var rulesDb = new RulesDbContext();

        var workflow = new Workflow
        {
            Description = "Grocery validation rules",
            Rules = new List<Rule>
            {
                new Rule
                {
                    Description = "Has perishable items",
                    Expression = "items.Any(i => i.Category == \"Dairy\" || i.Category == \"Produce\")"
                },
                new Rule
                {
                    Description = "Under $30 budget",
                    Expression = "items.Sum(i => i.Price) <= 30m"
                },
                new Rule
                {
                    Description = "All items in stock",
                    Expression = "items.All(i => i.InStock == true)"
                }
            }
        };

        // Serialize workflow to JSON and store in EF
        var json = JsonRuleLoader.Serialize(workflow);
        var entity = new WorkflowEntity
        {
            Name = workflow.Description,
            JsonPayload = json
        };
        rulesDb.Workflows.Add(entity);
        await rulesDb.SaveChangesAsync();
        Console.WriteLine($"  Stored workflow '{entity.Name}' to EF (Id: {entity.Id})");

        // ── Phase 2: Load workflow back from EF ──
        var loadedEntity = await rulesDb.Workflows.FirstAsync();
        var restoredWorkflow = JsonRuleLoader.DeserializeWorkflow(loadedEntity.JsonPayload);
        Console.WriteLine($"  Restored workflow with {restoredWorkflow.Rules.Count} rules");

        // ── Phase 3: Seed grocery data ──
        await using var groceryDb = new GroceryDbContext();
        await SeedGroceryData(groceryDb);

        var items = await groceryDb.GroceryItems.ToListAsync();
        var lists = await groceryDb.GroceryLists.Include(l => l.Items).ToListAsync();

        // ── Phase 4: Compile restored workflow ──
        var compileParams = new[] { new RuleParameter("items", typeof(List<GroceryItem>)) };
        restoredWorkflow.Compile(compileParams, null, DemoRunner.ReferenceProvider);

        // ── Phase 5: Execute against each grocery list ──
        foreach (var list in lists)
        {
            var listItems = list.Items
                .Select(li => items.FirstOrDefault(g => g.Name == li.ItemName))
                .Where(g => g != null)
                .Cast<GroceryItem>()
                .ToList();

            var execParams = new[] { new RuleParameter("items", typeof(List<GroceryItem>), listItems) };
            var results = restoredWorkflow.Execute(execParams).ToArray();

            Console.WriteLine();
            Console.WriteLine($"  List: {list.Name} ({listItems.Count} items)");

            foreach (var result in results)
                DemoRunner.PrintResult(result, $"    {result.RuleDescription}");

            foreach (var item in listItems)
                Console.WriteLine($"    - {item.Name}: ${item.Price:F2} ({item.Category}, {(item.InStock ? "in stock" : "OUT")})");
        }
    }

    private static async Task SeedGroceryData(GroceryDbContext db)
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
