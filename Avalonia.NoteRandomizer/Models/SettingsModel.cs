using Newtonsoft.Json;

namespace Avalonia.NoteRandomizer.Models;

public class SettingsModel
{
    [JsonProperty("ProgramName")] public int ProgramName { get; set; }

    public override string ToString()
    {
        return JsonConvert.SerializeObject(this, Formatting.Indented);
    }
}