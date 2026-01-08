/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Ian Lucas. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *--------------------------------------------------------------------------------------------*/

using SwiftlyS2.Shared.GameEventDefinitions;
using SwiftlyS2.Shared.Misc;

namespace Match;

public class StateWarmup : BaseState
{
    public override void Load()
    {
        Swiftly.Core.EntitySystem.GetGameRules()?.RoundsPlayedThisPhase = 0;
        HookGameEvent<EventItemPickup>(OnItemPickup);
    }

    public static HookResult OnItemPickup(EventItemPickup @event)
    {
        var controller = @event.UserIdController;
        var inGameMoneyServices = controller.InGameMoneyServices;
        if (inGameMoneyServices != null)
        {
            inGameMoneyServices.Account = 16000;
            controller.InGameMoneyServicesUpdated();
        }
        return HookResult.Continue;
    }
}
