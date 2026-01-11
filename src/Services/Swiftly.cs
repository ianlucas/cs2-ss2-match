/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Ian Lucas. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *--------------------------------------------------------------------------------------------*/

using System.Diagnostics;
using Microsoft.Extensions.Logging;
using SwiftlyS2.Shared;

namespace Match;

public static class Swiftly
{
    [SwiftlyInject]
    public static ISwiftlyCore Core { get; set; } = null!;

    public static void Initialize()
    {
        _ = Core;
    }

    public static void Log(string message, bool sendToChat = false)
    {
        if (sendToChat)
            Core.PlayerManager.SendChat(message);
        if (!ConVars.IsVerbose.Value)
            return;
        var stackTrace = new StackTrace();
        var frame = stackTrace.GetFrame(1);
        var method = frame?.GetMethod();
        var className = method?.DeclaringType?.Name;
        var methodName = method?.Name;
        var prefix =
            className != null && methodName != null ? $"{className}::{methodName}" : "Match";
        Core.Logger.LogInformation("{Prefix} {Message}", prefix, message);
    }
}
