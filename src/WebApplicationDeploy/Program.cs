using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using System.Diagnostics;
using System.Text;

namespace WebApplicationDeploy
{
    public class Program
    {
        /// <summary>
        /// https://stackoverflow.com/questions/54537694/how-i-can-transfer-all-messages-from-system-diagnostics-trace-to-ilogger
        /// https://stackoverflow.com/questions/44226554/how-to-enable-trace-logging-in-asp-net-core
        /// https://www.codeproject.com/Articles/5255953/Use-Trace-and-TraceSource-in-NET-Core-Logging
        /// https://andrewlock.net/exploring-dotnet-6-part-10-new-dependency-injection-features-in-dotnet-6/
        /// https://stackoverflow.com/questions/69907525/publishing-a-net-6-project-with-c-sharp-10-implicit-usings-via-visual-studio-20
        /// </summary>
        /// <param name="args"></param>
        public static void Main(string[] args)
        {
            Console.InputEncoding = Encoding.UTF8;
            Console.OutputEncoding = Encoding.UTF8;
            var configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", false, true)
                .Build();

            Log.Logger = new LoggerConfiguration()
                .Enrich.FromLogContext()
                .ReadFrom.Configuration(configuration)
                .CreateLogger();

            using var serviceProvider = new ServiceCollection()
                .AddLogging(builder => builder.AddSerilog())
                .AddSingleton<IFooService, FooService>()
                .BuildServiceProvider();

            var logger = serviceProvider.GetService<ILogger<Program>>();
            var fooService = serviceProvider.GetService<IFooService>();

            Trace.Listeners.Add(new LoggerTraceListener(logger));
            Debug.WriteLine("Test 1");

            try
            {
                Log.Information($"{nameof(Program)} started");
                logger.LogInformation("Log Information");
                logger.LogWarning("Log Warning");
                fooService.DoWork();
                Debug.WriteLine("Test 3");
                Log.Information($"{nameof(Program)} finished");
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Test 4");
                Log.Fatal(ex, "Application start-up failed");
            }
            finally
            {
                Debug.WriteLine("Test 5");
                Log.CloseAndFlush();
            }
        }
    }
}