using System.Reflection;

namespace Demo
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            using (var cts = new CancellationTokenSource())
            {
                var assembly = Assembly.GetExecutingAssembly();
                var demoTypes = assembly.GetTypes().Where(t => typeof(IDemo).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract).ToList();

                foreach (var type in demoTypes)
                {
                    try
                    {
                        if (Activator.CreateInstance(type) is IDemo demo)
                        {
                            Console.WriteLine($"{type.Name} started");

                            DateTime start = DateTime.Now;
                            await demo.Run(cts.Token);
                            DateTime end = DateTime.Now;

                            Console.WriteLine($"{type.Name} completed {(end - start).TotalMilliseconds}ms");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error executing Run on {type.Name}: {ex.Message}");
                    }
                }
            }
        }
    }
}
