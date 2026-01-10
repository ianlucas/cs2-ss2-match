/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Ian Lucas. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *--------------------------------------------------------------------------------------------*/

using System.Text.Json.Serialization;

namespace Match.Get5.Events;

public sealed class OnGameStateChangedEvent : Get5Event
{
    public override string EventName => "game_state_changed";

    [JsonPropertyName("new_state")]
    public string NewState { get; init; } = string.Empty;

    [JsonPropertyName("old_state")]
    public string OldState { get; init; } = string.Empty;

    public static OnGameStateChangedEvent Create(BaseState oldState, BaseState newState) =>
        new() { NewState = newState.Name, OldState = oldState.Name };
}
