using Demo.Models;
using RoslynRules.Extensions;
using RoslynRules.Models;
using RoslynRules.Templates;
using System.Diagnostics;
using System.Dynamic;
using System.Text.Json;

namespace Demo
{
    /// <summary>
    /// Comprehensive demo showcasing all RoslynRules features.
    /// 12 static methods, each demonstrating a different capability.
    /// </summary>
    internal class Program
    {
        private static List<Customer> _customers = new();
        private static int _section = 0;

        static async Task Main(string[] args)
        {
            LoadCustomers();

            await RunDemo("Basic Predicates", BasicPredicates);
            await RunDemo("Rule Chaining", RuleChaining);
            await RunDemo("Child Rules", ChildRules);
            await RunDemo("Async Expressions", AsyncExpressions);
            await RunDemo("Custom Types", CustomTypes);
            await RunDemo("ExpandoObject", ExpandoObjectDemo);
            await RunDemo("JSON Round-Trip", JsonRoundTrip);
            await RunDemo("Workflow vs RuleBatch", WorkflowVsRuleBatch);
            await RunDemo("Template Instantiation", TemplateInstantiation);
            await RunDemo("Caching", CachingDemo);
            await RunDemo("Priority Ordering", PriorityOrdering);
            await RunDemo("Lifecycle Events", LifecycleEvents);

            Console.WriteLine();
            Console.WriteLine("=== All demos completed ===");
        }

        private static void LoadCustomers()
        {
            var json = File.ReadAllText("Data/Customers.json");
            var doc = JsonDocument.Parse(json);
            _customers = doc.RootElement.GetProperty("customers").EnumerateArray()
                .Select(c => new Customer
                {
                    Id = Guid.Parse(c.GetProperty("id").GetString()!),
                    Name = c.GetProperty("name").GetString()!,
                    Age = c.GetProperty("age").GetInt32(),
                    Email = c.GetProperty("email").GetString()!,
                    IsActive = c.GetProperty("isActive").GetBoolean(),
                    IsVip = c.GetProperty("isVip").GetBoolean(),
                    CreatedDate = c.GetProperty("createdDate").GetDateTime(),
                    Tags = c.GetProperty("tags").EnumerateArray().Select(t => t.GetString()!).ToList(),
                    Orders = c.GetProperty("orders").EnumerateArray().Select(o => new Order
                    {
                        Id = o.GetProperty("id").GetInt32(),
                        Total = o.GetProperty("total").GetDouble(),
                        Items = o.GetProperty("items").EnumerateArray().Select(i => i.GetString()!).ToList()
                    }).ToList()
                }).ToList();
        }

        private static async Task RunDemo(string title, Func<Task> action)
        {
            _section++;
            Console.WriteLine();
            Console.WriteLine($"=== {_section}. {title} ===");
            var sw = Stopwatch.StartNew();
            try
            {
                await action();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] {ex.Message}");
            }
            sw.Stop();
            Console.WriteLine($"Completed in {sw.Elapsed.TotalMilliseconds:F2}ms");
        }

        // ─── 1. Basic Predicates ───
        private static Task BasicPredicates()
        {
            var rule = new Rule
            {
                Description = "Adult customer",
                Expression = "customer.Age >= 18"
            };

            var adult = _customers.First(c => c.Age >= 18);
            var minor = _customers.First(c => c.Age < 18);

            var param = new RuleParameter("customer", typeof(Customer), adult);
            var result = rule.Execute(param);
            PrintResult(result, "Adult customer check");

            param = new RuleParameter("customer", typeof(Customer), minor);
            result = rule.Execute(param);
            PrintResult(result, "Minor customer check");

            // Regex email validation
            var emailRule = new Rule
            {
                Description = "Valid email",
                Expression = "System.Text.RegularExpressions.Regex.IsMatch(customer.Email, @\"^[^@]+@[^@]+\\.[^@]+$\")"
            };
            param = new RuleParameter("customer", typeof(Customer), adult);
            result = emailRule.Execute(param);
            PrintResult(result, "Email format check");

            return Task.CompletedTask;
        }

        // ─── 2. Rule Chaining ───
        private static Task RuleChaining()
        {
            var discountRule = new Rule
            {
                Description = "Apply VIP discount",
                Expression = "customer.IsVip == true",
                Action = "customer.Name = \"[VIP] \" + customer.Name"
            };

            var vip = _customers.First(c => c.IsVip);
            var regular = _customers.First(c => !c.IsVip);

            var param = new RuleParameter("customer", typeof(Customer), vip);
            var result = discountRule.Execute(param);
            PrintResult(result, $"VIP discount applied to {vip.Name}");

            param = new RuleParameter("customer", typeof(Customer), regular);
            result = discountRule.Execute(param);
            PrintResult(result, $"Non-VIP skipped for {regular.Name}");

            return Task.CompletedTask;
        }

        // ─── 3. Child Rules ───
        private static Task ChildRules()
        {
            var parent = new Rule
            {
                Description = "Premium customer validation",
                Expression = "customer.IsVip == true",
                ChildRules = new List<Rule>
                {
                    new Rule { Description = "Has orders", Expression = "customer.Orders.Count > 0" },
                    new Rule { Description = "Account active", Expression = "customer.IsActive == true" }
                }
            };

            var premium = _customers.First(c => c.IsVip && c.Orders.Count > 0 && c.IsActive);
            var param = new RuleParameter("customer", typeof(Customer), premium);
            var result = parent.Execute(param);
            PrintResult(result, $"Premium check for {premium.Name}");

            if (result.FirstFailure is not null)
                Console.WriteLine($"  First failure: {result.FirstFailure.Value.RuleDescription}");

            return Task.CompletedTask;
        }

        // ─── 4. Async Expressions ───
        private static async Task AsyncExpressions()
        {
            var rule = new Rule
            {
                Description = "Async adult check",
                Expression = "await Task.FromResult(customer.Age >= 18)"
            };

            var customer = _customers.First();
            var param = new RuleParameter("customer", typeof(Customer), customer);
            var result = await rule.ExecuteAsync(param);
            PrintResult(result, "Async age check");
        }

        // ─── 5. Custom Types ───
        private static Task CustomTypes()
        {
            var rule = new Rule
            {
                Description = "Has premium orders",
                Expression = "customer.Orders.Any(o => o.Total > 200)"
            };

            var bigSpender = _customers.First(c => c.Orders.Any(o => o.Total > 200));
            var param = new RuleParameter("customer", typeof(Customer), bigSpender);
            var result = rule.Execute(param);
            PrintResult(result, $"Big spender check ({bigSpender.Name})");

            return Task.CompletedTask;
        }

        // ─── 6. ExpandoObject ───
        private static Task ExpandoObjectDemo()
        {
            dynamic dyn = new ExpandoObject();
            dyn.name = "Dynamic User";
            dyn.score = 95;

            var rule = new Rule
            {
                Description = "High score",
                Expression = "data.score >= 90"
            };

            var param = new RuleParameter("data", typeof(ExpandoObject), (ExpandoObject)dyn);
            var result = rule.Execute(param);
            PrintResult(result, "Dynamic score check");

            return Task.CompletedTask;
        }

        // ─── 7. JSON Round-Trip ───
        private static Task JsonRoundTrip()
        {
            var wf = new Workflow
            {
                Description = "JSON test",
                Rules = new List<Rule>
                {
                    new Rule { Description = "Active check", Expression = "customer.IsActive == true" }
                }
            };

            var json = JsonRuleLoader.Serialize(wf);
            var restored = JsonRuleLoader.DeserializeWorkflow(json);

            var customer = _customers.First(c => c.IsActive);
            var param = new RuleParameter("customer", typeof(Customer), customer);
            var result = restored.Execute(param).First();
            PrintResult(result, "Round-trip workflow");

            return Task.CompletedTask;
        }

        // ─── 8. Workflow vs RuleBatch ───
        private static Task WorkflowVsRuleBatch()
        {
            var wf = new Workflow
            {
                Description = "Batch comparison",
                Rules = new List<Rule>
                {
                    new Rule { Description = "Rule 1", Expression = "customer.Age >= 18" },
                    new Rule { Description = "Rule 2", Expression = "customer.IsActive == true" },
                    new Rule { Description = "Rule 3", Expression = "customer.Orders.Count > 0" }
                }
            };

            var customer = _customers.First(c => c.Age >= 18 && c.IsActive && c.Orders.Count > 0);
            var param = new RuleParameter("customer", typeof(Customer), customer);

            var sw = Stopwatch.StartNew();
            var seq = wf.Execute(param).ToArray();
            sw.Stop();
            Console.WriteLine($"  Sequential: {seq.Length} results in {sw.Elapsed.TotalMilliseconds:F3}ms");

            sw.Restart();
            var par = wf.ExecuteParallel(param);
            sw.Stop();
            Console.WriteLine($"  Parallel:   {par.Length} results in {sw.Elapsed.TotalMilliseconds:F3}ms");

            return Task.CompletedTask;
        }

        // ─── 9. Template Instantiation ───
        private static Task TemplateInstantiation()
        {
            var template = new RuleTemplate
            {
                Description = "Age threshold check",
                Expression = "customer.Age >= {minAge}",
            };
            template.Placeholders.Add("minAge", PlaceholderKind.Value);

            var values = new Dictionary<string, object> { ["minAge"] = 21 };
            var param = new RuleParameter("customer", typeof(Customer), _customers.First(c => c.Age >= 21));
            var rule = template.Instantiate(values, new RoslynRules.Compiler.ExpressionCompiler(), new[] { param }, Array.Empty<string>());
            var result = rule.Execute(param);
            PrintResult(result, "Template: age >= 21");

            return Task.CompletedTask;
        }

        // ─── 10. Caching ───
        private static Task CachingDemo()
        {
            var rule = new Rule
            {
                Description = "Cached check",
                Expression = "customer.IsActive == true",
                CacheDuration = TimeSpan.FromSeconds(5)
            };

            var customer = _customers.First(c => c.IsActive);
            var param = new RuleParameter("customer", typeof(Customer), customer);

            var sw = Stopwatch.StartNew();
            var r1 = rule.Execute(param);
            var first = sw.Elapsed.TotalMilliseconds;

            sw.Restart();
            var r2 = rule.Execute(param);
            var cached = sw.Elapsed.TotalMilliseconds;

            PrintResult(r1, $"First call ({first:F3}ms)");
            PrintResult(r2, $"Cached call ({cached:F3}ms)");

            return Task.CompletedTask;
        }

        // ─── 11. Priority Ordering ───
        private static Task PriorityOrdering()
        {
            var wf = new Workflow
            {
                Description = "Priority test",
                Rules = new List<Rule>
                {
                    new Rule { Description = "Last", Expression = "true", Priority = 10 },
                    new Rule { Description = "First", Expression = "true", Priority = 100 },
                    new Rule { Description = "Middle", Expression = "true", Priority = 50 }
                }
            };

            var param = new RuleParameter("customer", typeof(Customer), _customers.First());
            var results = wf.Execute(param).ToArray();

            foreach (var r in results)
                Console.WriteLine($"  [{r.Success}] {r.RuleDescription}");

            return Task.CompletedTask;
        }

        // ─── 12. Lifecycle Events ───
        private static Task LifecycleEvents()
        {
            var rule = new Rule
            {
                Description = "Evented rule",
                Expression = "customer.IsActive == true"
            };
            rule.OnRuleExecuted += (sender, args) =>
                Console.WriteLine($"  Event: '{args.Rule.Description}' executed in {args.Elapsed.TotalMilliseconds:F2}ms");

            var customer = _customers.First(c => c.IsActive);
            var param = new RuleParameter("customer", typeof(Customer), customer);
            var result = rule.Execute(param);
            PrintResult(result, "Lifecycle event check");

            return Task.CompletedTask;
        }

        private static void PrintResult(RuleResult result, string description)
        {
            var color = result.Success ? ConsoleColor.Green : ConsoleColor.Red;
            var status = result.Success ? "PASS" : "FAIL";
            Console.ForegroundColor = color;
            Console.WriteLine($"  [{status}] {description}");
            Console.ResetColor();
        }
    }
}
