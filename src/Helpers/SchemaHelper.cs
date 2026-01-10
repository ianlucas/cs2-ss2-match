/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Ian Lucas. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *--------------------------------------------------------------------------------------------*/

namespace Match;

public static class SchemaHelper
{
    public static CEconItemSchema? GetItemSchema()
    {
        var ptr = Natives.GetItemSchema.Call();
        var schema = new CEconItemSchema(ptr);
        return schema.IsValid ? schema : null;
    }
}
