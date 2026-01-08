/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Ian Lucas. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *--------------------------------------------------------------------------------------------*/

using SwiftlyS2.Shared.Misc;

namespace Match;

public static class Cstv
{
    private static string? _filename;

    static Cstv()
    {
        _match.Plugin.AddCommandListener("changelevel", OnChangeLevel);
    }

    public static HookResult OnChangeLevel(CCSPlayerController? _, CommandInfo info)
    {
        if (IsRecording())
        {
            Stop();
            var arguments = info.ArgString.Trim().Replace("\"", "").ToLower();
            Swiftly.Core.Engine.ExecuteCommand($"changelevel \"{arguments}\"");
            return HookResult.Stop;
        }
        return HookResult.Continue;
    }

    public static bool IsRecording() => _filename != null;

    public static void Record(string? filename)
    {
        if (!IsEnabled() || IsRecording() || filename == null)
            return;
        _filename = filename;
        Game.Log($"Demo is being recorded at {filename}.");
        Swiftly.Core.Engine.ExecuteCommand($"tv_record {filename}");
    }

    public static void Stop()
    {
        if (IsRecording())
        {
            _filename = null;
            Game.Log($"Demo is no longer being recorded.");
            Swiftly.Core.Engine.ExecuteCommand("tv_stoprecord");
        }
    }

    public static string? GetFilename() => _filename;

    public static bool IsEnabled() => Swiftly.Core.ConVar.Find<bool>("tv_enable")?.Value == true;

    public static void Set(bool value)
    {
        if (value)
        {
            Swiftly.Core.ConVar.Find<bool>("tv_enable")?.Value = true;
            Swiftly.Core.ConVar.Find<int>("tv_record_immediate")?.Value = 1;
            Swiftly.Core.ConVar.Find<int>("tv_delay")?.Value = ConVars.TvDelay.Value;
        }
        else
            Swiftly.Core.ConVar.Find<bool>("tv_enable")?.Value = false;
    }
}
