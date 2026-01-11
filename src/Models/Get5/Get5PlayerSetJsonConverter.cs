/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Ian Lucas. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *--------------------------------------------------------------------------------------------*/

using System.Text.Json;
using System.Text.Json.Serialization;

namespace Match.Get5;

public class Get5PlayerSetJsonConverter : JsonConverter<Get5PlayerSet>
{
    public override Get5PlayerSet? Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options
    )
    {
        switch (reader.TokenType)
        {
            case JsonTokenType.StartObject:
                var dictionary = JsonSerializer.Deserialize<Dictionary<ulong, string>>(
                    ref reader,
                    options
                );
                return new Get5PlayerSet { AsDictionary = dictionary };

            case JsonTokenType.StartArray:
                var list = JsonSerializer.Deserialize<List<ulong>>(ref reader, options);
                return new Get5PlayerSet { AsList = list };
        }

        throw new JsonException("Expected an array or an object for Get5PlayerSet.");
    }

    public override void Write(
        Utf8JsonWriter writer,
        Get5PlayerSet value,
        JsonSerializerOptions options
    )
    {
        if (value.AsDictionary != null)
        {
            JsonSerializer.Serialize(writer, value.AsDictionary, options);
        }
        else if (value.AsList != null)
        {
            JsonSerializer.Serialize(writer, value.AsList, options);
        }
        else
        {
            writer.WriteNullValue();
        }
    }
}
