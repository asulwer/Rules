using Demo.Data;
using Microsoft.EntityFrameworkCore;
using RoslynRules.Models;

namespace Demo.Demos;

public static class EntityFrameworkDemo
{
    public static async Task Run()
    {
        // ── Load workflow from DB ──
        await using var rulesDb = new RulesDbContext();

        var workflow = await rulesDb.Workflows
            .AsNoTracking()
            .Include(w => w.Rules)
            .FirstAsync();

        Console.WriteLine($"  Loaded '{workflow.Description}' from DB ({workflow.Rules.Count} rules)");

        // ── Load grocery data ──
        await using var groceryDb = new GroceryDbContext();

        var items = await groceryDb.GroceryItems.ToListAsync();
        var lists = await groceryDb.GroceryLists.Include(l => l.Items).ToListAsync();

        // ── Compile and execute ──
        var compileParams = new[] { new RuleParameter("items", typeof(List<GroceryItem>)) };
        workflow.Compile(compileParams, null, DemoRunner.ReferenceProvider);

        foreach (var list in lists)
        {
            var listItems = list.Items
                .Select(li => items.FirstOrDefault(g => g.Name == li.ItemName))
                .Where(g => g != null)
                .Cast<GroceryItem>()
                .ToList();

            var execParams = new[] { new RuleParameter("items", typeof(List<GroceryItem>), listItems) };
            var results = workflow.Execute(execParams).ToArray();

            Console.WriteLine();
            Console.WriteLine($"  List: {list.Name} ({listItems.Count} items)");

            foreach (var result in results)
                DemoRunner.PrintResult(result, $"    {result.RuleDescription}");

            foreach (var item in listItems)
                Console.WriteLine($"    - {item.Name}: ${item.Price:F2} ({item.Category}, {(item.InStock ? "in stock" : "OUT")})");
        }
    }
}
