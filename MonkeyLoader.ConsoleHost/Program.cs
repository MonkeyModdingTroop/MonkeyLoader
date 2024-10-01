using System.IO.Pipes;

namespace MonkeyLoader.ConsoleHost
{
    internal class Program
    {
        private const char ESC = '\x1b';

        private static void Main(string[] args)
        {
            var appName = args.FirstOrDefault() ?? "MonkeyLoader";
            var pipeName = $"MonkeyLoader.ConsoleHost.{appName}";

            using var pipeServer = new NamedPipeServerStream(pipeName, PipeDirection.In);
            var reader = new StreamReader(pipeServer);

            ConsoleMode.EnsureTerminalProcessing();

            Console.Title = $"{appName} - MonkeyLoader Console";
            Console.WriteLine("Welcome to MonkeyLoader!");
            Console.WriteLine($"Waiting for input on pipe: {pipeName}");

            //var NL = Environment.NewLine; // shortcut
            //var NORMAL = Console.IsOutputRedirected ? "" : "\x1b[39m";
            //var RED = Console.IsOutputRedirected ? "" : "\x1b[91m";
            //var GREEN = Console.IsOutputRedirected ? "" : "\x1b[92m";
            //var YELLOW = Console.IsOutputRedirected ? "" : "\x1b[93m";
            //var BLUE = Console.IsOutputRedirected ? "" : "\x1b[94m";
            //var MAGENTA = Console.IsOutputRedirected ? "" : "\x1b[95m";
            //var CYAN = Console.IsOutputRedirected ? "" : "\x1b[96m";
            //var GREY = Console.IsOutputRedirected ? "" : "\x1b[97m";
            //var BOLD = Console.IsOutputRedirected ? "" : "\x1b[1m";
            //var NOBOLD = Console.IsOutputRedirected ? "" : "\x1b[22m";
            //var UNDERLINE = Console.IsOutputRedirected ? "" : "\x1b[4m";
            //var NOUNDERLINE = Console.IsOutputRedirected ? "" : "\x1b[24m";
            //var REVERSE = Console.IsOutputRedirected ? "" : "\x1b[7m";
            //var NOREVERSE = Console.IsOutputRedirected ? "" : "\x1b[27m";

            //Console.WriteLine($"This is {RED}Red{NORMAL}, {GREEN}Green{NORMAL}, {YELLOW}Yellow{NORMAL}, {BLUE}Blue{NORMAL}, {MAGENTA}Magenta{NORMAL}, {CYAN}Cyan{NORMAL}, {GREY}Grey{NORMAL}! ");
            //Console.WriteLine($"This is {BOLD}Bold{NOBOLD}, {UNDERLINE}Underline{NOUNDERLINE}, {REVERSE}Reverse{NOREVERSE}! ");

            while (true)
            {
                pipeServer.WaitForConnection();

                try
                {
                    var i = 0;
                    var isTermSeq = false;

                    while (true)
                    {
                        i = reader.Read();

                        if (i < 0)
                            break;

                        var c = Convert.ToChar(i);

                        if (!ConsoleMode.IsTerminal)
                        {
                            if (c is ESC)
                            {
                                isTermSeq = true;
                                continue;
                            }

                            if (isTermSeq && c is 'm')
                            {
                                isTermSeq = false;
                                continue;
                            }
                        }

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