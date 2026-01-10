/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Ian Lucas. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *--------------------------------------------------------------------------------------------*/

using System.Text.Json;
using System.Text.Json.Serialization;

namespace Match.Get5;

public class Get5MaplistJsonConverter : JsonConverter<Get5Maplist>
{
    public override Get5Maplist? Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options
    )
    {
        switch (reader.TokenType)
        {
            case JsonTokenType.StartObject:
                var @object = JsonSerializer.Deserialize<Get5MapListFromList>(ref reader, options);
                return new Get5Maplist { AsObject = @object };

            case JsonTokenType.StartArray:
                var list = JsonSerializer.Deserialize<List<string>>(ref reader, options);
                return new Get5Maplist { AsList = list };
        }

        throw new JsonException("Expected an array or an object for Get5Map.");
    }

    public override void Write(
        Utf8JsonWriter writer,
        Get5Maplist value,
        JsonSerializerOptions options
    )
    {
        if (value.AsObject != null)
        {
            JsonSerializer.Serialize(writer, value.AsObject, options);
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
