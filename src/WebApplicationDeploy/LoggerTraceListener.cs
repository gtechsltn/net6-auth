using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace WebApplicationDeploy
{
    public class LoggerTraceListener : TraceListener
    {
        private readonly ILogger _logger;

        public LoggerTraceListener(ILogger logger)
        {
            _logger = logger;
        }

        public override void Write(string message)
        {
            _logger.LogInformation(message);
        }

        public override void WriteLine(string message)
        {
            _logger.LogInformation(message);
        }

        public override void WriteLine(string message, string category)
        {
            _logger.LogInformation(category + ": " + message);
        }

        public override void TraceEvent(TraceEventCache eventCache, string source, TraceEventType eventType, int id)
        {
            switch (eventType)
            {
                case TraceEventType.Critical:
                    _logger.LogCritical(id, source);
                    break;

                case TraceEventType.Error:
                    _logger.LogError(id, source);
                    break;

                case TraceEventType.Warning:
                    _logger.LogWarning(id, source);
                    break;

                case TraceEventType.Information:
                    _logger.LogInformation(id, source);
                    break;

                case TraceEventType.Verbose:
                    _logger.LogTrace(id, source);
                    break;

                case TraceEventType.Start:
                    _logger.LogInformation(id, "Start: " + source);
                    break;

                case TraceEventType.Stop:
                    _logger.LogInformation(id, "Stop: " + source);
                    break;

                case TraceEventType.Suspend:
                    _logger.LogInformation(id, "Suspend: " + source);
                    break;

                case TraceEventType.Resume:
                    _logger.LogInformation(id, "Resume: " + source);
                    break;

                case TraceEventType.Transfer:
                    _logger.LogInformation(id, "Transfer: " + source);
                    break;

                default:
                    throw new InvalidOperationException("Impossible");
            }
        }
    }
}