using Rules.Models;
using Demo.Models;
using System.Text.Json;
using System.Diagnostics;

namespace Demo
{
    /// <summary>
    /// Demonstrates workflow compilation and execution against a JSON customer list.
    /// Tests both sequential and parallel execution modes.
    /// </summary>
    public class JsonLoad_Execute : IDemo
    {
        /// <summary>
        /// Loads customers from JSON, creates a workflow with sample rules,
        /// compiles once, and evaluates all customers (sequential vs parallel benchmark).
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        public Task Run(CancellationToken cancellationToken = default)
        {            
            try
            {
                var sw = Stopwatch.StartNew();

                //create a Workflow
                var wf = new Workflow
                {
                    Description = "Rules which act upon a Customer",
                    Rules = new List<Rule>
                    {
                        new Rule
                        {
                            Description = "Contains",
                            Expression = "Customer.Name.Contains(\"Olivia Esquivel\")"
                        },
                        new Rule
                        {
                            Description = "Contains then Replace",
                            Expression = "Customer.Name.Contains(\"Bridger Wise\")",
                            Action = "Customer.Name = Customer.Name.Replace(\"Bridger Wise\", \"Wise\")"
                        },
                        new Rule
                        {
                            Description = "Contains then child",
                            Action = "Customer.Name = Customer.Name.Replace(\"Mira Christensen\", \"Mira\")",
                            ChildRules = new List<Rule>
                            {
                                new Rule
                                {
                                    Description = "is customer active",
                                    Expression = "Customer.IsActive == true"
                                },
                                new Rule
                                {
                                    Description = "customer contains name",
                                    Expression = "Customer.Name.Contains(\"Mira Christensen\")"
                                }
                            }
                        }
                    }
                };

                // Validate workflow BEFORE compilation
                var validateStart = sw.ElapsedMilliseconds;
                wf.Validate();
                Console.WriteLine($"Validation took: {sw.ElapsedMilliseconds - validateStart}ms");

                // Compile workflow ONCE
                var compileStart = sw.ElapsedMilliseconds;
                var compileParams = new RuleParameter[]
                {
                    new RuleParameter(nameof(Customer), typeof(Customer), default(Customer))
                };
                wf.Compile(compileParams);
                Console.WriteLine($"Compilation took: {sw.ElapsedMilliseconds - compileStart}ms");

                // Now load customers
                string jsonString = File.ReadAllText("Data/Customers.json");
                List<Customer> customers = JsonSerializer.Deserialize<List<Customer>>(jsonString)!;

                int customerCount = customers!.Count;

                // --- SEQUENTIAL EXECUTION ---
                var seqStart = sw.ElapsedMilliseconds;
                foreach (Customer customer in customers!)
                {
                    var parameters = new RuleParameter[]
                    {
                        new RuleParameter(nameof(Customer), typeof(Customer), customer)
                    };
                    var results = wf.Execute(parameters).ToArray();
                }
                var seqTime = sw.ElapsedMilliseconds - seqStart;

                // Reset customers (re-deserialize since actions mutated them)
                jsonString = File.ReadAllText("Data/Customers.json");
                customers = JsonSerializer.Deserialize<List<Customer>>(jsonString)!;

                // --- PARALLEL EXECUTION ---
                var parStart = sw.ElapsedMilliseconds;
                foreach (Customer customer in customers!)
                {
                    var parameters = new RuleParameter[]
                    {
                        new RuleParameter(nameof(Customer), typeof(Customer), customer)
                    };
                    var results = wf.ExecuteParallel(parameters);
                }
                var parTime = sw.ElapsedMilliseconds - parStart;

                Console.WriteLine($"Sequential execution of {customerCount} customers took: {seqTime}ms ({seqTime / (double)customerCount:F4}ms per customer)");
                Console.WriteLine($"Parallel execution of {customerCount} customers took: {parTime}ms ({parTime / (double)customerCount:F4}ms per customer)");
                Console.WriteLine($"Speedup: {seqTime / (double)parTime:F2}x");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner: {ex.InnerException.Message}");
                }
            }

            return Task.CompletedTask;
        }
    }
}
