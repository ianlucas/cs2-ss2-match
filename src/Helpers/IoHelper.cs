/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Ian Lucas. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *--------------------------------------------------------------------------------------------*/

using System.Text.Json;

namespace Match;

public static class IoHelper
{
    public static void WriteJson(string filename, object contents)
    {
        if (File.Exists(filename))
        {
            int version = 1;
            string backupPath;
            do
            {
                backupPath = $"{filename}.{version}";
                version++;
            } while (File.Exists(backupPath));
            File.Copy(filename, backupPath);
        }
        string jsonString = JsonSerializer.Serialize(contents);
        File.WriteAllText(filename, jsonString);
    }
}
