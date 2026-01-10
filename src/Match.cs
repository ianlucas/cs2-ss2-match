/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Ian Lucas. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *--------------------------------------------------------------------------------------------*/

using SwiftlyS2.Shared;
using SwiftlyS2.Shared.GameEventDefinitions;
using SwiftlyS2.Shared.Plugins;

namespace Match;

[PluginMetadata(
    Id = "Match",
    Version = "1.0.0",
    Name = "Match",
    Author = "Ian Lucas",
    Description = "Matches with Get5 compatibility."
)]
public partial class Match(ISwiftlyCore core) : BasePlugin(core)
{
    public bool PendingInternalPush = true;
    public bool DidKickBots = false;
    public static readonly List<string> LoadMatchCmds = ["match_load", "get5_loadmatch"];

    public override void Load(bool hotReload)
    {
        Swiftly.Initialize();
        ConVars.Initialize();
        Cstv.Initialize();
        Core.Event.OnConVarValueChanged += OnConVarValueChanged;
        Core.Event.OnMapLoad += OnMapLoad;
        Core.Event.OnTick += OnTick;
        Core.Event.OnClientSteamAuthorize += OnClientSteamAuthorize;
        Core.Event.OnClientDisconnected += OnClientDisconnected;
        Core.GameEvent.HookPost<EventPlayerChat>(OnPlayerChat);
        Natives.CCSGameRules_ChangeTeam.AddHook(OnChangeTeam);
        Natives.CCSBotManager_MaintainBotQuota.AddHook(OnMaintainBotQuota);
        Core.Command.Register(LoadMatchCmds, OnMatchLoadCommand);
        Core.Command.Register(["match_status"], OnMatchStatusCommand);
        Core.Command.Register(["sw_start"], OnStartCommand);
        Core.Command.Register(["sw_restart"], OnRestartCommand);
        Core.Command.Register(["sw_map"], OnMapCommand);
        Directory.CreateDirectory(Core.GetConfigPath());
    }

    public override void Unload()
    {
        Cstv.Shutdown();
    }
}
