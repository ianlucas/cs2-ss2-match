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
        Core.Event.OnConVarValueChanged += OnConVarValueChanged;
        Core.Event.OnMapLoad += OnMapLoad;
        Core.Event.OnTick += OnTick;
        Core.Event.OnClientSteamAuthorize += OnClientSteamAuthorize;
        Core.Event.OnClientDisconnected += OnClientDisconnected;
        Core.GameEvent.HookPost<EventPlayerChat>(OnPlayerChat);
        Natives.CCSGameRules_ChangeTeam.AddHook(OnChangeTeam);
        Natives.CCSBotManager_MaintainBotQuota.AddHook(OnMaintainBotQuota);
        foreach (var cmd in LoadMatchCmds)
            Core.Command.RegisterCommand(cmd, OnMatchLoadCommand, registerRaw: true);
        Core.Command.RegisterCommand("match_status", OnMatchStatusCommand, registerRaw: true);
        Core.Command.RegisterCommand("sw_start", OnStartCommand, registerRaw: true);
        Core.Command.RegisterCommand("sw_restart", OnRestartCommand, registerRaw: true);
        Core.Command.RegisterCommand("sw_map", OnMapCommand, registerRaw: true);
        Directory.CreateDirectory(Core.GetConfigPath());
    }

    public override void Unload() { }
}
