This is a very early rewrite of the super slow [RulesEngine](https://github.com/microsoft/RulesEngine) and the only maintained [fork](https://github.com/asulwer/RulesEngine).  Rule wrapper for [DynamicExpresso](https://github.com/dynamicexpresso/DynamicExpresso)

```
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
```