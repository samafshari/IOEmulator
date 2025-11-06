using System;
using Neat;

namespace RaytraceDebug;

class Program
{
    static void Main()
    {
        var io = new IOEmulator();
        var qb = new QBasicApi(io);
        var interp = new QBasicInterpreter(qb);
        
        try
        {
            var src = QBasicSamples.Load("RAYTRACE");
            Console.WriteLine("Loaded RAYTRACE.bas, starting execution...");
            interp.Run(src);
            Console.WriteLine("Execution completed successfully!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.GetType().Name}");
            Console.WriteLine($"Message: {ex.Message}");
            Console.WriteLine($"Stack trace:\n{ex.StackTrace}");
            
            if (ex.InnerException != null)
            {
                Console.WriteLine($"\nInner exception: {ex.InnerException.GetType().Name}");
                Console.WriteLine($"Inner message: {ex.InnerException.Message}");
            }
        }
    }
}
