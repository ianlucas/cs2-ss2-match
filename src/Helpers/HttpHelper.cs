/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Ian Lucas. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *--------------------------------------------------------------------------------------------*/

using System.Text;
using System.Text.Json;

namespace Match;

public static class HttpHelper
{
    private static readonly HttpClient _httpClient = new();

    public static async void SendJson(
        string url,
        object data,
        Dictionary<string, string>? headers = null
    )
    {
        try
        {
            using var content = new StringContent(
                JsonSerializer.Serialize(data),
                Encoding.UTF8,
                "application/json"
            );
            using var request = new HttpRequestMessage(HttpMethod.Post, url) { Content = content };
            if (headers != null)
                foreach (var header in headers)
                    request.Headers.TryAddWithoutValidation(header.Key, header.Value);
            using var response = await _httpClient.SendAsync(request);
        }
        catch { }
    }
}
