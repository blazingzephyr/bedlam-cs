
namespace bedlam;

internal class Program
{
    static void Main()
    {
        var variable = Environment.GetEnvironmentVariable("Test", EnvironmentVariableTarget.Process);
        while (true)
        {
            Console.WriteLine(variable);
            Thread.Sleep(5000);
        }
    }
}