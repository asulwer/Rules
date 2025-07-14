using Rules.Models;
using Demo.Models;
using DynamicExpresso;
using System.Text.Json;

namespace Demo
{
    public class Execute : IDemo
    {
        public Task Run(CancellationToken cancellationToken = default)
        {
            string file = Path.Combine(Directory.GetParent(Directory.GetCurrentDirectory())!.Parent!.Parent!.Parent!.FullName!, "Customers.json");

            string jsonString = File.ReadAllText(file);
            List<Customer> customers = JsonSerializer.Deserialize<List<Customer>>(jsonString)!;

            //create a Workflow
            var wf = new Workflow
            {
                //IsActive = false, //skip this workflow and all its associated Rules
                Description = "Rules which act upon a Customer", //used to give a description to the workflow
                Rules = new List<Rule> //rules in workflow
                {
                    new Rule
                    {
                        //IsActive = false, //if IsActive is false then it will be skipped
                        Description = "Contains", //rule description
                        InExp = "Customer.Name.Contains(\"Olivia Esquivel\")" //Expression to evaluate and if True then this rule was successul
                    },
                    new Rule
                    {
                        //IsActive = false,
                        Description = "Contains then Replace",
                        LocalParameters = { new Parameter("x", typeof(int), 10) },
                        InExp = "Customer.Name.Contains(\"Bridger Wise\") && x==10",
                        OutExp = "Customer.Name = Customer.Name.Replace(\"Bridger Wise\", \"Wise\")" //Expression to perform if Rule is successful which is determined by success of InExp
                    },
                    new Rule
                    {
                        //IsActive = false,
                        Description = "Contains then child",
                        InExp = "Customer.Name.Contains(\"Mira Christensen\")", //if condition is met then run child rule
                        ChildRules = new List<Rule>
                        {
                            new Rule
                            {
                                Description = "Replace",
                                OutExp = "Customer.Name = Customer.Name.Replace(\"Mira Christensen\", \"Mira\")"  //replace string
                            }
                        }
                    }
                }
            };

            //evaluate ONE customer at a time against all enabled rules in workflow
            foreach (Customer customer in customers!)
            {
                Console.WriteLine(customer.Name);

                //pass array of Parameter's to Workflow for evaluation
                var parameters = new Parameter[]
                {
                    new Parameter(nameof(Customer), typeof(Customer), customer)
                };

                foreach (var del in wf.Execute(parameters)) //execute workflow and its rules
                    Console.WriteLine($"{del}");
            }

            return Task.CompletedTask;
        }
    }
}
