using System.Text.Json;
using Microsoft.Azure.Cosmos;

namespace Transcrib.Shared.Services;

/// <summary>
/// Cosmos DB serializer that uses System.Text.Json so the shared
/// <c>[JsonPropertyName]</c> contract is honoured both by the API responses and
/// by Cosmos persistence (avoiding a Newtonsoft.Json dependency).
/// </summary>
public class SystemTextJsonCosmosSerializer : CosmosSerializer
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);

    public override T FromStream<T>(Stream stream)
    {
        using (stream)
        {
            if (typeof(Stream).IsAssignableFrom(typeof(T)))
            {
                return (T)(object)stream;
            }

            return JsonSerializer.Deserialize<T>(stream, Options)!;
        }
    }

    public override Stream ToStream<T>(T input)
    {
        var stream = new MemoryStream();
        JsonSerializer.Serialize(stream, input, Options);
        stream.Position = 0;
        return stream;
    }
}
