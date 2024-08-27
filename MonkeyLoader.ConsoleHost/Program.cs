using System.IO.Pipes;

namespace MonkeyLoader.ConsoleHost
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            var appName = args.FirstOrDefault() ?? "MonkeyLoader";

            using var pipeServer = new NamedPipeServerStream($"MonkeyLoader.ConsoleHost.{appName}", PipeDirection.In);
            pipeServer.WaitForConnection();

            var reader = new StreamReader(pipeServer);

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
            catch (IOException)
            {
                //error handling code here
            }
        }
    }
}