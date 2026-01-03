/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Ian Lucas. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *--------------------------------------------------------------------------------------------*/

namespace Match;

public class BaseState
{
    public virtual string Name { get; set; } = "default_state";

    public virtual void Load() { }

    public virtual void Unload() { }
}
