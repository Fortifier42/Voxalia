﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Frenetic.TagHandlers;
using Frenetic.TagHandlers.Objects;
using Voxalia.ServerGame.TagSystem.TagObjects;
using Voxalia.ServerGame.ServerMainSystem;

namespace Voxalia.ServerGame.TagSystem.TagBases
{
    class ItemTagBase : TemplateTagBase
    {
        public Server TheServer;

        // <--[tagbase]
        // @Base item[<ItemTag>]
        // @ReturnType ItemTag
        // @Returns the item described by the given input.
        // -->
        public ItemTagBase(Server tserver)
        {
            TheServer = tserver;
            Name = "item";
        }

        public override string Handle(TagData data)
        {
            string iname = data.GetModifier(0);
            ItemTag itag = ItemTag.For(TheServer, iname);
            if (itag == null)
            {
                return new TextTag("&{NULL}").Handle(data.Shrink());
            }
            return itag.Handle(data.Shrink());
        }
    }
}