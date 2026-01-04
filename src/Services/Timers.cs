/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Ian Lucas. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *--------------------------------------------------------------------------------------------*/

using System.Collections.Concurrent;

namespace Match;

public static class Timers
{
    private static readonly ConcurrentDictionary<string, CancellationTokenSource> _timers = [];
    private const float ChatInterval = 15.0f;

    public static void ClearAll()
    {
        foreach (var name in _timers.Keys)
            Clear(name);
    }

    public static void Clear(string name)
    {
        if (_timers.TryRemove(name, out var timer))
        {
            timer.Cancel();
            timer.Dispose();
        }
    }

    public static void Set(string name, float interval, Action callback)
    {
        Clear(name);
        _timers[name] = Swiftly.Core.Scheduler.DelayBySeconds(interval, callback);
    }

    public static void SetEveryChatInterval(string name, Action callback)
    {
        Clear(name);
        var timer = Swiftly.Core.Scheduler.DelayAndRepeatBySeconds(0, ChatInterval, callback);
        Swiftly.Core.Scheduler.StopOnMapChange(timer);
        _timers[name] = timer;
    }

    public static void SetEverySecond(string name, Action callback)
    {
        Clear(name);
        var timer = Swiftly.Core.Scheduler.DelayAndRepeatBySeconds(0, 1, callback);
        Swiftly.Core.Scheduler.StopOnMapChange(timer);
        _timers[name] = timer;
    }
}
