using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using CommandLine;
using CommandLine.Text;
using JB.AerialDownloader.Commands;
using JB.AerialDownloader.Options;

namespace JB.AerialDownloader
{
    class Program
    {
        /// <summary>
        /// The cancellation token source used for ctrl+c handling
        /// </summary>
        private static readonly CancellationTokenSource CancellationTokenSource = new CancellationTokenSource();

        static async Task<int> Main(string[] args)
        {
            int exitCode = Constants.DefaultSuccessCode;

            try
            {
                Console.CancelKeyPress += OnConsole_CancelKeyPress;

                var commandlineParser = new Parser(settings =>
                {
                    settings.IgnoreUnknownArguments = true;
                });

                var parserResult = commandlineParser.ParseArguments<DownloadAerialMoviesOptions>(args);
                if (parserResult.Tag == ParserResultType.Parsed)
                {
                    exitCode = await parserResult.MapResult(
                        downloadAerialMoviesOptions =>
                        {
                            var command = new DownloadAerialMoviesCommand();

                            return command.ExecuteAndReturnExitCode(downloadAerialMoviesOptions,
                                CancellationTokenSource.Token);
                        },
                        errors => Task.FromResult(Constants.DefaultErrorCode)).ConfigureAwait(false);
                }
                else
                {
                    var helpText = HelpText.AutoBuild(parserResult);
                    helpText.Heading = GetHelpTextHeading();

                    Console.Error.Write(helpText);

                    exitCode = Constants.DefaultErrorCode;
                }
            }
            catch (OperationCanceledException)
            {
                var currentConsoleForegroundColor = Console.ForegroundColor;
                try
                {
                    Console.WriteLine();
                    Console.ForegroundColor = ConsoleColor.Yellow;

                    Console.WriteLine("The operation was cancelled.");

                    exitCode = Constants.DefaultSuccessCode;
                }
                finally
                {
                    Console.ForegroundColor = currentConsoleForegroundColor;
                }
            }
            catch (AggregateException aggregateException)
            {
                var currentConsoleForegroundColor = Console.ForegroundColor;
                try
                {
                    Console.WriteLine();
                    Console.ForegroundColor = ConsoleColor.Red;

                    Console.WriteLine("Something bad happened:");

                    foreach (var exception in aggregateException.Flatten().InnerExceptions)
                    {
                        Console.WriteLine("- {0}", exception.Message);
                    }

                    exitCode = Constants.DefaultErrorCode;
                }
                finally
                {
                    Console.ForegroundColor = currentConsoleForegroundColor;
                }
            }
            catch (Exception exception)
            {
                var currentConsoleForegroundColor = Console.ForegroundColor;
                try
                {
                    Console.WriteLine();
                    Console.ForegroundColor = ConsoleColor.Red;

                    Console.WriteLine("Something bad happened: {0}", exception);

                    exitCode = Constants.DefaultErrorCode;
                }
                finally
                {
                    Console.ForegroundColor = currentConsoleForegroundColor;
                }
            }

            return exitCode;
        }

        /// <summary>
        /// Gets the help text heading.
        /// </summary>
        /// <returns></returns>
        private static string GetHelpTextHeading()
        {
            var title = typeof(Program).Assembly.GetAssemblyAttribute<AssemblyTitleAttribute>();
            var version = typeof(Program).Assembly.GetName().Version;

            return $"{title.Title} v{version}";
        }

        /// <summary>
        /// Called when [CTRL+C] is pressed.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="consoleCancelEventArgs">The <see cref="ConsoleCancelEventArgs"/> instance containing the event data.</param>
        private static void OnConsole_CancelKeyPress(object sender, ConsoleCancelEventArgs consoleCancelEventArgs)
        {
            CancellationTokenSource?.Cancel(); // signal cancellation to command(s)

            Console.WriteLine();
            Console.WriteLine("CTRL+C pressed, waiting for pending actions and downloads to finish...");

            consoleCancelEventArgs.Cancel = true; // cancel process termination
        }
    }
}
