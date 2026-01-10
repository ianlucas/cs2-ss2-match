/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Ian Lucas. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *--------------------------------------------------------------------------------------------*/

using SwiftlyS2.Shared;

namespace Match;

public static class ISwiftlyCoreExtensions
{
    extension(ISwiftlyCore self)
    {
        public string GetConfigConVarPath(string path = "")
        {
            return $"addons/swiftlys2/configs/Match{path}";
        }

        public string GetConfigPath(string path = "")
        {
            return self.GetCSGOPath(self.GetConfigConVarPath(path));
        }

        public string GetCSGOPath(string path = "")
        {
            return Path.Combine(self.GameDirectory, "csgo", path);
        }
    }
}
