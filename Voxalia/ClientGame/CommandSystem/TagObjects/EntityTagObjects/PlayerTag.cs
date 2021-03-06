//
// This file is part of the game Voxalia, created by FreneticXYZ.
// This code is Copyright (C) 2016 FreneticXYZ under the terms of the MIT license.
// See README.md or LICENSE.txt for contents of the MIT license.
// If these are not available, see https://opensource.org/licenses/MIT
//

using Voxalia.ClientGame.ClientMainSystem;
using FreneticScript.TagHandlers;
using FreneticScript.TagHandlers.Objects;

namespace Voxalia.ClientGame.CommandSystem.TagObjects.EntityTagObjects
{
    public class PlayerTag: TemplateObject
    {
        public Client TheClient;

        public PlayerTag(Client tclient)
        {
            TheClient = tclient;
        }

        /// <summary>
        /// Parse any direct tag input values.
        /// </summary>
        /// <param name="data">The input tag data.</param>
        public override TemplateObject Handle(TagData data)
        {
            if (data.Remaining == 0)
            {
                return this;
            }
            switch (data[0])
            {
                // <--[tag]
                // @Name PlayerTag.held_item_slot
                // @Group Inventory
                // @ReturnType IntegerTag
                // @Returns the slot of the item the player is currently holding (in their QuickBar).
                // -->
                case "held_item_slot":
                    return new IntegerTag(TheClient.QuickBarPos).Handle(data.Shrink());
                default:
                    return new TextTag(ToString()).Handle(data);
            }
        }
    }
}
