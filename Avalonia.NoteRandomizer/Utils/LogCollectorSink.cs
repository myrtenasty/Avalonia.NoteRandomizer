using System.IO;
using Avalonia.NoteRandomizer.Services;
using Serilog.Core;
using Serilog.Events;
using Serilog.Formatting;

namespace Avalonia.NoteRandomizer.Utils;

public class LogCollectorSink : ILogEventSink
{
    private readonly LogCollector _collector;
    private readonly ITextFormatter _textFormatter;

    public LogCollectorSink(LogCollector collector, ITextFormatter textFormatter)
    {
        _collector = collector;
        _textFormatter = textFormatter;
    }

    public void Emit(LogEvent logEvent)
    {
        using (var writer = new StringWriter())
        {
            _textFormatter.Format(logEvent, writer);
            var message = writer.ToString();
            _collector.AddLog(message);
        }
    }
}