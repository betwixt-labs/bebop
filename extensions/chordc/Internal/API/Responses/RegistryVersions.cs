using System.Text.Json;
using System.Text.Json.Serialization;

namespace Chord.Compiler.Internal.API.Responses;

internal sealed record RegistryCatalog(string Name, string Description, string Latest, RegistryVersion[] Versions);
internal sealed record RegistryVersion(string Version, ulong PublishedAt, bool IsPreRelease);

internal sealed class RegistryCatalogConverter : JsonConverter<RegistryCatalog>
{
    public override RegistryCatalog Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
        {
            throw new JsonException();
        }

        string? name = null;
        string? description = null;
        string? latest = null;
        var versions = new List<RegistryVersion>();

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
            {
                if (string.IsNullOrEmpty(name))
                {
                    throw new JsonException("Name cannot be empty.");
                }
                if (string.IsNullOrEmpty(description))
                {
                    throw new JsonException("Description cannot be empty.");
                }
                if (string.IsNullOrEmpty(latest))
                {
                    throw new JsonException("Latest version cannot be empty.");
                }
                return new RegistryCatalog(name, description, latest, [.. versions]);
            }
            if (reader.TokenType == JsonTokenType.PropertyName)
            {
                var propertyName = reader.GetString();
                reader.Read();

                switch (propertyName)
                {
                    case "name":
                        name = reader.GetString();
                        break;
                    case "description":
                        description = reader.GetString();
                        break;
                    case "latest":
                        latest = reader.GetString();
                        break;
                    case "versions":
                        if (reader.TokenType == JsonTokenType.StartObject)
                        {
                            while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
                            {
                                if (reader.TokenType == JsonTokenType.PropertyName)
                                {
                                    var versionNumber = reader.GetString();
                                    if (string.IsNullOrEmpty(versionNumber))
                                    {
                                        throw new JsonException("Version number cannot be empty.");
                                    }
                                    var version = ReadRegistryVersion(ref reader);
                                    versions.Add(version with { Version = versionNumber });
                                }
                            }
                        }
                        break;
                }
            }
        }

        throw new JsonException("JSON format is invalid.");
    }

    private RegistryVersion ReadRegistryVersion(ref Utf8JsonReader reader)
    {
        ulong publishedAt = 0;
        bool isPreRelease = false;

        reader.Read(); // Move to StartObject
        if (reader.TokenType != JsonTokenType.StartObject)
        {
            throw new JsonException();
        }

        while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
        {
            if (reader.TokenType == JsonTokenType.PropertyName)
            {
                var propertyName = reader.GetString();
                reader.Read();

                switch (propertyName)
                {
                    case "publishedAt":
                        publishedAt = reader.GetUInt64();
                        break;
                    case "isPreRelease":
                        isPreRelease = reader.GetBoolean();
                        break;
                }
            }
        }
        return new RegistryVersion(string.Empty, publishedAt, isPreRelease); // Version will be filled later
    }

    public override void Write(Utf8JsonWriter writer, RegistryCatalog value, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }
}