/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Ian Lucas. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *--------------------------------------------------------------------------------------------*/

namespace Match;

public class MapEndResult
{
    public required Map Map { get; set; }
    public required bool IsSeriesOver { get; set; }
    public PlayerTeam? Winner { get; set; }
}
