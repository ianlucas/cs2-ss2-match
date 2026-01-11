/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Ian Lucas. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *--------------------------------------------------------------------------------------------*/

namespace Match.Get5;

public class Get5PlayerSet
{
    public Dictionary<ulong, string>? AsDictionary { get; set; }
    public List<ulong>? AsList { get; set; }

    public Dictionary<ulong, string>? Get()
    {
        if (AsDictionary != null)
            return AsDictionary;

        if (AsList != null)
        {
            var dictionary = new Dictionary<ulong, string>(AsList.Count);
            foreach (var steamId in AsList)
                dictionary[steamId] = "";
        }

        return null;
    }
}
