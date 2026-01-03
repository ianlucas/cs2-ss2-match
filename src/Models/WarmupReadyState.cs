/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Ian Lucas. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *--------------------------------------------------------------------------------------------*/

namespace Match;

public class StateWarmupReady : StateWarmup
{
    public override string Name => "warmup";
    public static readonly List<string> ReadyCmds = ["css_ready", "css_r", "css_pronto"];
    public static readonly List<string> UnreadyCmds = ["css_unready", "css_ur", "css_naopronto"];
    private long _warmupStart = 0;

    public override void Load()
    {
        base.Load();
    }

    public override void Unload()
    {
        base.Unload();
    }
}
