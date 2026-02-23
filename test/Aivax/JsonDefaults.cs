using System.Text.Json;
using System.Text.Json.Serialization;

namespace CountTokens_Tester.Aivax;

internal static class JsonDefaults
{
    public static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = false
    };
}
