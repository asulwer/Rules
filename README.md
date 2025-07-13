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

```
https://github.com/asulwer/Rules/blob/master/Demo/Execute.cs#L1018-L1069