/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Ian Lucas. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *--------------------------------------------------------------------------------------------*/

using SwiftlyS2.Shared.Commands;
using SwiftlyS2.Shared.GameEvents;
using SwiftlyS2.Shared.Memory;
using SwiftlyS2.Shared.Misc;

namespace Match;

public class BaseState
{
    public virtual string Name { get; set; } = "default_state";
    protected bool _matchCancelled = false;
    private readonly List<Guid> _commands = [];
    private readonly List<Guid> _gameEvents = [];
    private readonly List<Action> _coreEvents = [];
    private readonly List<Action> _nativeHooks = [];

    public virtual void Load() { }

    public virtual void Unload()
    {
        Timers.ClearAll();
        foreach (var cleanup in _coreEvents)
            cleanup();
        foreach (var cleanup in _nativeHooks)
            cleanup();
        foreach (var guid in _commands)
            Swiftly.Core.Command.UnregisterCommand(guid);
        foreach (var guid in _gameEvents)
            Swiftly.Core.GameEvent.Unhook(guid);
    }

    protected void RegisterCommand(
        List<string> commandNames,
        ICommandService.CommandListener handler
    )
    {
        _commands.AddRange(Swiftly.Core.Command.Register(commandNames, handler));
    }

    protected void HookGameEvent<T>(
        IGameEventService.GameEventHandler<T> handler,
        HookMode mode = HookMode.Post
    )
        where T : IGameEvent<T>
    {
        _gameEvents.Add(
            mode == HookMode.Pre
                ? Swiftly.Core.GameEvent.HookPre(handler)
                : Swiftly.Core.GameEvent.HookPost(handler)
        );
    }

    protected void HookCoreEvent<THandler>(THandler handler)
        where THandler : Delegate
    {
        var eventName = typeof(THandler).Name;
        var eventSource = Swiftly.Core.Event;
        var eventInfo =
            eventSource.GetType().GetEvent(eventName) ?? throw new ArgumentException(
                $"Event '{eventName}' not found on type '{eventSource.GetType().Name}'"
            );
        eventInfo.AddEventHandler(eventSource, handler);
        _coreEvents.Add(() => eventInfo.RemoveEventHandler(eventSource, handler));
    }

    protected void AddHook<TDelegate>(
        IUnmanagedFunction<TDelegate> fn,
        Func<Func<TDelegate>, TDelegate> handler
    )
        where TDelegate : Delegate
    {
        var guid = fn.AddHook(handler);
        _nativeHooks.Add(() => fn.RemoveHook(guid));
    }
}
