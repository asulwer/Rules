using Demo.Data;
using Microsoft.EntityFrameworkCore;
using RoslynRules.Models;

namespace Demo.Demos;

public static class EntityFrameworkDemo
{
    public static async Task Run()
    {
        await using var rulesDb = new RulesDbContext();
        await using var groceryDb = new GroceryDbContext();

        // ── Explicit Loading: load workflow without any children ──
        var workflow = await rulesDb.Workflows.FirstAsync();
        Console.WriteLine($"  Loaded '{workflow.Description}' (no rules yet)");

        // ── Explicitly load top-level rules ──
        await rulesDb.Entry(workflow)
            .Collection(w => w.Rules)
            .LoadAsync();
        Console.WriteLine($"  Loaded {workflow.Rules.Count} top-level rules");

        // ── Recursively load child rules for each rule ──
        foreach (var rule in workflow.Rules)
        {
            await LoadChildRulesRecursive(rulesDb, rule);
        }

        // Count total rules including nested children
        var totalRules = CountRulesRecursive(workflow.Rules);
        Console.WriteLine($"  Total rules (including nested): {totalRules}");

        // ── Load grocery data ──
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

    /// <summary>
    /// Recursively loads child rules using explicit loading.
    /// Each call queries the database for that rule's children.
    /// </summary>
    private static async Task LoadChildRulesRecursive(RulesDbContext db, Rule rule)
    {
        await db.Entry(rule)
            .Collection(r => r.ChildRules)
            .LoadAsync();

        foreach (var child in rule.ChildRules)
        {
            await LoadChildRulesRecursive(db, child);
        }
    }

    private static int CountRulesRecursive(IEnumerable<Rule> rules)
    {
        return rules.Sum(r => 1 + CountRulesRecursive(r.ChildRules));
    }
}
