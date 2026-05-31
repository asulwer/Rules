using System;
using System.Reflection;

class Program {
    static void Main() {
        var gen = Type.GetType("Rules.Compiler.CodeGenerator, Rules");
        var method = gen.GetMethod("Generate", BindingFlags.NonPublic | BindingFlags.Static);
        var result = method.Invoke(null, new object[] { 
            "await Task.FromResult(customer.Age >= 18)", 
            typeof(bool), 
            new[] { "customer" }, 
            new[] { typeof(object) }, 
            new[] { "System.Threading.Tasks" } 
        });
        Console.WriteLine(result);
    }
}
