/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Ian Lucas. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *--------------------------------------------------------------------------------------------*/

using SwiftlyS2.Shared.Players;

namespace Match;

public static class Game
{
    public static readonly List<PlayerTeam> Teams = [];
    public static readonly PlayerTeam Team1;
    public static readonly PlayerTeam Team2;
    public static string? Id { get; } = null;
    public static bool IsClinchSeries { get; } = true;
    public static BaseState State { get; } = new();
    public static bool IsLoadedFromFile { get; } = false;
    public static bool IsSeriesStarted { get; } = false;
    public static bool IsFirstMapRestarted { get; } = false;
    public static Team? KnifeRoundWinner { get; }
    public static MapEndResult? MapEndResult { get; } = null;

    static Game()
    {
        var team1 = new PlayerTeam(Team.T);
        var team2 = new PlayerTeam(Team.CT);
        team1.Opposition = team2;
        team2.Opposition = team1;
        Teams = [team1, team2];
        Team1 = team1;
        Team2 = team2;
    }
}
