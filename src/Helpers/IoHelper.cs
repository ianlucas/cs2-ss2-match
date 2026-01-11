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
        try
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
            using var stream = File.Create(filename);
            JsonSerializer.Serialize(stream, contents);
        }
        catch (Exception ex)
        {
            Swiftly.Log($"Error writing JSON to {filename}: {ex.Message}");
        }
    }
}
