using System;

namespace Avalonia.NoteRandomizer.Services;

public class LogCollector
{
    public event Action<string>? LogAdded;

    public void AddLog(string message)
    {
        LogAdded?.Invoke(message);
    }
}