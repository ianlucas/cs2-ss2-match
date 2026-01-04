/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Ian Lucas. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *--------------------------------------------------------------------------------------------*/

namespace Match;

public static class TimeHelper
{
    public static long Now() => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

    public static long NowSeconds() => DateTimeOffset.UtcNow.ToUnixTimeSeconds();

    public static string Format(long seconds)
    {
        return $"{seconds / 60}:{seconds % 60:D2}";
    }
}
