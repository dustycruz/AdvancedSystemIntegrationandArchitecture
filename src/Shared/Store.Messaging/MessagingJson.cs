using System.Text.Json;
using System.Text.Json.Serialization;

namespace Store.Messaging;

/// <summary>JSON settings for every message body: camelCase properties and enums as strings.</summary>
public static class MessagingJson
{
    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };
}
