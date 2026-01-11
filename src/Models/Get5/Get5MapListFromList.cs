/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Ian Lucas. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *--------------------------------------------------------------------------------------------*/

using System.Text.Json.Serialization;

namespace Match.Get5;

public class Get5MapListFromList
{
    [JsonPropertyName("fromfile")]
    public required string Fromfile { get; set; }
}
