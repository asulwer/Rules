This is a very early rewrite of the super slow [RulesEngine](https://github.com/microsoft/RulesEngine) and the only maintained [fork](https://github.com/asulwer/RulesEngine).  Rule wrapper for [DynamicExpresso](https://github.com/dynamicexpresso/DynamicExpresso)

### Example - Customer Name Contains (with possible Name replacement)

[Demo](https://github.com/asulwer/Rules/blob/master/Demo/Execute.cs) completed in the following time <img width="205" height="17" alt="image" src="https://github.com/user-attachments/assets/956752fc-f5bd-4679-961c-d65e16fecc04" />

```
//Customer Model
var customers = new List<Customer>
{
    new Customer { Name = "John Doe" },
    new Customer { Name = "Jane Doe" },
    new Customer { Name = "John Smith" },
    new Customer { Name = "Jane Smith" }
};

//create Workflow
var wf = new Workflow {
    Description = "examples", //used to give a description to the workflow
    Rules = new List<Rule> //rules in workflow
    {
        new Rule
        {
            //IsActive = false, //if IsActive is false then it will be skipped
            Description = "something descriptive", //rule description
            InExp = "Customer.Name.Contains(\" Doe\")" //Expression to evaluate and if True then this rule was successul
        },
        new Rule
        {
            //IsActive = false,
            Description = "something descriptive",
            InExp = "Customer.Name.Contains(\" Doe\")",
            OutExp = "Customer.Name = Customer.Name.Replace(\" Doe\", string.Empty)" //Expression to perform if Rule is successful which is determined by success of InExp
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

//evaluate ONE customer at a time against all rules in workflow
foreach (Customer customer in customers)
{
    Console.WriteLine(customer.Name);

    //pass array of Parameter's to Rules for evaluation
    var parameters = new Parameter[]
    {
        new Parameter(nameof(Customer), typeof(Customer), customer)
    };

    foreach (var del in wf.Execute(parameters)) //execute workflow and its rules
        Console.WriteLine($"{del}");
}
```
