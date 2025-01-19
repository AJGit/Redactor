using System.Diagnostics;

namespace RedactorApi.Util;

public partial class LogUtils(ILogger logger, string className, string functionName) : IDisposable
{
    private readonly ILogger _logger = logger;
    private readonly string _className = className;
    private readonly string _functionName = functionName;
    private long _startTicks;

    [LoggerMessage(LogLevel.Debug, "Entering {className}::{functionName}")]
    static partial void LogStart(ILogger logger, string className, string functionName);

    [LoggerMessage(LogLevel.Debug, "Exiting {className}::{functionName} - {elapsed}ms")]
    static partial void LogEnd(ILogger logger, string className, string functionName, int elapsed);

    public static IDisposable Create(ILogger logger, string className, string functionName)
    {
        var log = new LogUtils(logger, className, functionName);
        return log.Enter();
    }

    public IDisposable Enter()
    {
        _startTicks = Stopwatch.GetTimestamp();
        LogStart(_logger, _className, _functionName);
        return this;
    }

    public void Dispose()
    {
        var elapsed = Stopwatch.GetElapsedTime(_startTicks);
        LogEnd(_logger, _className, _functionName,  elapsed.Milliseconds);
    }
}