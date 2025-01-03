using ZwiftPlayConsoleApp.BLE;
using ZwiftPlayConsoleApp.Utils;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ZwiftPlayConsoleApp.Configuration;

public class Config
{
    public bool SendKeys { get; set; } = false;
    public bool UseMapping { get; set; } = false;
    public string MappingFilePath { get; set; } = "Configuration/TPVirtual.json";
    public KeyboardMapping KeyboardMapping { get; set; } = new();

    public void LoadMappingFile()
    {
        if (!File.Exists(MappingFilePath))
        {
            Console.WriteLine($"Mapping file not found: {MappingFilePath}");
            Environment.Exit(1);
        }

        try
        {
            var json = File.ReadAllText(MappingFilePath);
            var options = new JsonSerializerOptions
            {
                AllowTrailingCommas = true,
                ReadCommentHandling = JsonCommentHandling.Skip
            };

            var mapping = JsonSerializer.Deserialize<KeyboardMapping>(json, options);
            
            if (mapping == null)
            {
                Console.WriteLine("Mapping file is empty or invalid JSON format");
                Environment.Exit(1);
            }

            if (!mapping.ButtonToKeyMap.Any())
            {
                Console.WriteLine("No key mappings found in the file");
                Environment.Exit(1);
            }

            foreach (var kvp in mapping.ButtonToKeyMap)
            {
                if (!Enum.IsDefined(typeof(ZwiftPlayButton), kvp.Key))
                {
                    Console.WriteLine($"Invalid button name: {kvp.Key}");
                    Environment.Exit(1);
                }
            }

            KeyboardMapping = mapping;
        }
        catch (JsonException ex)
        {
            Console.WriteLine($"Error parsing mapping file: {ex.Message}", ex);
            Environment.Exit(1);
        }
    }
}
public class AppSettings
{
    public int DefaultScanTimeoutMs { get; set; }
    public int DefaultRequiredDeviceCount { get; set; }
    public int DefaultConnectionTimeoutMs { get; set; }
    public int DefaultTaskDelay { get; set; }
    public string QuitKey { get; set; } = string.Empty;
}

public class KeyboardMapping
{
    private Dictionary<ZwiftPlayButton, KeyMapping> _buttonToKeyMap = new();
    
    [JsonConverter(typeof(ButtonMappingConverter))]
    public Dictionary<ZwiftPlayButton, KeyMapping> ButtonToKeyMap 
    { 
        get => _buttonToKeyMap;
        set => _buttonToKeyMap = value;
    }
}

public class ButtonMappingConverter : JsonConverter<Dictionary<ZwiftPlayButton, KeyMapping>>
{
    public override Dictionary<ZwiftPlayButton, KeyMapping> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var result = new Dictionary<ZwiftPlayButton, KeyMapping>();
        
        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
            {
                break;
            }
            
            var buttonName = reader.GetString();
            reader.Read();
            var keyValue = reader.TokenType == JsonTokenType.String ? 
                reader.GetString() : 
                reader.GetByte().ToString();
                
            if (keyValue != null && Enum.TryParse<ZwiftPlayButton>(buttonName, out var button))
            {
                result[button] = new KeyMapping(KeyboardKeys.GetKeyCode(keyValue), keyValue);
            }
        }
        
        return result;
    }

    public override void Write(Utf8JsonWriter writer, Dictionary<ZwiftPlayButton, KeyMapping> value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        foreach (var kvp in value)
        {
            writer.WritePropertyName(kvp.Key.ToString());
            writer.WriteStringValue(kvp.Value.OriginalMapping);
        }
        writer.WriteEndObject();
    }
}