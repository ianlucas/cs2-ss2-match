/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Ian Lucas. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *--------------------------------------------------------------------------------------------*/

using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace Match.Get5;

public class Get5Maplist
{
    public List<string>? AsList { get; set; }
    public Get5MapListFromList? AsObject { get; set; }

    public List<string>? Get()
    {
        try
        {
            if (AsList != null)
                return AsList;
            var name = AsObject?.Fromfile ?? "";
            if (!name.EndsWith(".json"))
                name += ".json";
            var filepath = Swiftly.Core.GetConfigPath($"/{name}");
            if (!File.Exists(filepath))
                filepath = Swiftly.Core.GetCSGOPath(filepath);
            return JsonSerializer.Deserialize<List<string>>(File.ReadAllText(filepath));
        }
        catch (Exception ex)
        {
            Swiftly.Core.Logger.LogWarning($"Error reading match map list file: {ex.Message}");
            return null;
        }
    }
}
