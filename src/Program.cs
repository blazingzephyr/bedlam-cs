
namespace bedlam;

internal class Program
{
    static void Main()
    {
        CancellationTokenSource source = new CancellationTokenSource();
        Parallel(source.Token);

        source.CancelAfter(15000);
    }

    static async Task Parallel(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            Console.WriteLine("Test");
            await Task.Delay(1000);
        }
    }
}