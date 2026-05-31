using Rules.Compiler;
using System;

class Program {
    static void Main() {
        var gen = typeof(Rules.Compiler.CodeGenerator);
        var method = gen.GetMethod("Generate", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
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
