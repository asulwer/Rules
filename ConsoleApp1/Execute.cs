using Rules.Models;
using Demo.Models;
using DynamicExpresso;

namespace Demo
{
    public class Execute : IDemo
    {
        public Task Run(CancellationToken cancellationToken = default)
        {
            var customers = new List<Customer>
            {
                new Customer { Name = "John Doe" },
                new Customer { Name = "Jane Doe" },
                new Customer { Name = "John Smith" },
                new Customer { Name = "Jane Smith" }
            };

            var wf = new Workflow {
                Description = "examples",
                Rules = new List<Rule>
                {
                    new Rule
                    {
                        //IsActive = false,
                        Description = "something descriptive",
                        InExp = "Customer.Name.Contains(\" Doe\")"
                    },
                    new Rule
                    {
                        //IsActive = false,
                        Description = "something descriptive",
                        InExp = "Customer.Name.Contains(\" Doe\")",
                        OutExp = "Customer.Name = Customer.Name.Replace(\" Doe\", string.Empty)"
                    },
                    new Rule
                    {
                        //IsActive = false,
                        Description = "something descriptive",
                        InExp = "Customer.Name.Contains(\" Smith\")",
                        ChildRules = new List<Rule>
                        {
                            new Rule
                            {
                                Description = "test",
                                OutExp = "Customer.Name = Customer.Name.Replace(\" Smith\", string.Empty)"
                            }
                        }
                    }
                }
            };

            foreach (Customer customer in customers)
            {
                Console.WriteLine(customer.Name);

                var parameters = new Parameter[]
                {
                    new Parameter(nameof(Customer), typeof(Customer), customer)
                };

                foreach (var del in wf.Execute(parameters))
                    Console.WriteLine($"{del}");
            }

            return Task.CompletedTask;
        }
    }
}
