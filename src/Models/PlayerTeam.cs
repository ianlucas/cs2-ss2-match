/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Ian Lucas. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *--------------------------------------------------------------------------------------------*/

using SwiftlyS2.Shared.Players;
using SwiftlyS2.Shared.SchemaDefinitions;

namespace Match;

public class PlayerTeam(Team startingTeam)
{
    private PlayerTeam? _opposition;

    public readonly List<PlayerState> Players = [];

    public readonly List<ulong> SurrenderVotes = [];

    public readonly int Index = (byte)startingTeam - 2;

    public Team StartingTeam = startingTeam;

    public PlayerState? InGameLeader = null;

    public string Name = "";

    public string Id = "";

    public bool IsUnpauseMatch = false;

    public bool IsSurrended = false;

    public int SeriesScore = 0;

    public TeamStats Stats = new();

    public PlayerTeam Opposition
    {
        get
        {
            if (_opposition == null)
                throw new ArgumentException("No opposition available.");
            return _opposition;
        }
        set { _opposition = value; }
    }

    public Team CurrentTeam
    {
        get =>
            Swiftly.Core.EntitySystem.GetGameRules()?.AreTeamsPlayingSwitchedSides() == true
                ? StartingTeam.Toggle()
                : StartingTeam;
    }

    public string ServerName =>
        Name == ""
            ? InGameLeader != null
                ? $"team_{InGameLeader.Name}"
                : "\"\""
            : Name;

    public string FormattedName =>
        Name == ""
            ? InGameLeader != null
                ? $"team_{InGameLeader.Name}"
                : Swiftly.Core.Localizer[CurrentTeam == Team.T ? "match.t" : "match.ct"]
            : Name;

    public CCSTeam? Manager =>
        Swiftly
            .Core.EntitySystem.GetAllEntitiesByDesignerName<CCSTeam>("cs_team_manager")
            .FirstOrDefault(t => t.TeamNum == (byte)CurrentTeam);

    public int Score
    {
        get => Manager?.Score ?? 0;
        set { Manager?.Score = value; }
    }

    public void Reset()
    {
        Players.Clear();
        SurrenderVotes.Clear();
        InGameLeader = null;
        Name = "";
        IsUnpauseMatch = false;
        IsSurrended = false;
        SeriesScore = 0;
        Stats = new();
    }

    public void AddPlayer(PlayerState player)
    {
        Players.Add(player);
        InGameLeader ??= player;
    }

    public bool CanAddPlayer()
    {
        return Players.Count < ConVars.PlayersNeededPerTeam.Value;
    }

    public void RemovePlayer(PlayerState player)
    {
        Players.Remove(player);
        if (InGameLeader == player)
            InGameLeader = Players.FirstOrDefault();
    }

    public void SendChat(string message)
    {
        foreach (var player in Players)
            player.Handle?.SendChat(message);
    }
}
