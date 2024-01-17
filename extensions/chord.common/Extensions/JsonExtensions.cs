using System.Text.Json;

namespace Chord.Common.Extensions;


internal static class JsonExtensions
{
    public static Dictionary<TKey, TValue> ReadDictionary<TKey, TValue>(this ref Utf8JsonReader reader)
        where TValue : notnull
        where TKey : notnull
    {
        if (reader.TokenType != JsonTokenType.StartObject)
        {
            throw new JsonException("Expected StartObject token for options.");
        }

        var dict = new Dictionary<TKey, TValue>();

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
            {
                break;
            }

            if (reader.TokenType == JsonTokenType.PropertyName)
            {
                var key = reader.ReadValue<TKey>();
                reader.Read(); // Move to the value
                var value = reader.ReadValue<TValue>();
                dict[key] = value;
            }
        }
        return dict;
    }

    public static TValue ReadValue<TValue>(this ref Utf8JsonReader reader) where TValue : notnull
    {
        var boxed = (object)(typeof(TValue) switch
        {
            Type t when t == typeof(bool) => reader.TokenType is not JsonTokenType.True or JsonTokenType.False
                ? throw new JsonException("Expected boolean")
                : reader.GetBoolean(),

            Type t when t == typeof(int) => reader.TokenType != JsonTokenType.Number
                ? throw new JsonException("Expected number")
                : reader.GetInt32(),

            Type t when t == typeof(string) => reader.GetNonNullOrWhiteSpaceString(),

            _ => throw new JsonException($"Unexpected type {typeof(TValue)}")
        });

        if (boxed is TValue value)
            return value;

        throw new JsonException($"Unexpected type {typeof(TValue)}");
    }

    public static string GetNonNullOrWhiteSpaceString(this ref Utf8JsonReader reader)
    {
        if (reader.TokenType != JsonTokenType.String && reader.TokenType != JsonTokenType.PropertyName)
            throw new JsonException($"Expected string: {reader.TokenType}");

        var value = reader.GetString();
        if (string.IsNullOrWhiteSpace(value))
            throw new JsonException("Expected non-null, non-whitespace string");

        return value;
    }
}