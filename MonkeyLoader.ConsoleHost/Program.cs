using System.IO.Pipes;

namespace MonkeyLoader.ConsoleHost
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            var appName = args.FirstOrDefault() ?? "MonkeyLoader";
            var pipeName = $"MonkeyLoader.ConsoleHost.{appName}";

            using var pipeServer = new NamedPipeServerStream(pipeName, PipeDirection.In);
            var reader = new StreamReader(pipeServer);

            Console.Title = $"{appName} - MonkeyLoader Console";
            Console.WriteLine("Welcome to MonkeyLoader!");
            Console.WriteLine($"Waiting for input on pipe: {pipeName}");

            while (true)
            {
                pipeServer.WaitForConnection();

                try
                {
                    var i = 0;
                    while (i >= 0)
                    {
                        i = reader.Read();

                        if (i >= 0)
                            Console.Write(Convert.ToChar(i));
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                    //error handling code here
                }
            }
        }
    }
}