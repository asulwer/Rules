using System.Reflection;

namespace Demo
{
    /// <summary>
    /// Entry point that discovers and runs all IDemo implementations in the assembly.
    /// </summary>
    internal class Program
    {
        /// <summary>
        /// Main entry point. Scans the assembly for IDemo implementations,
        /// instantiates each, runs it, and reports execution time.
        /// </summary>
        /// <param name="args">Command line arguments (unused).</param>
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

                            Console.WriteLine($"{type.Name} completed in {(end - start).TotalMilliseconds}ms");
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
