/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Ian Lucas. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *--------------------------------------------------------------------------------------------*/

using System.Text;
using System.Text.Json;

namespace Match;

public static class HttpHelper
{
    public static async void SendJson(
        string url,
        object data,
        Dictionary<string, string>? headers = null
    )
    {
        try
        {
            using HttpClient client = new();
            if (headers != null)
                foreach (var header in headers)
                    client.DefaultRequestHeaders.Add(header.Key, header.Value);
            var json = JsonSerializer.Serialize(data);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            await client.PostAsync(url, content);
        }
        catch { }
    }
}
