﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Frenetic.TagHandlers;
using Frenetic.TagHandlers.Objects;
using Voxalia.ServerGame.TagSystem.TagObjects;
using Voxalia.ServerGame.ServerMainSystem;
using Voxalia.ServerGame.EntitySystem;
using Voxalia.ServerGame.WorldSystem;
using Voxalia.Shared;

namespace Voxalia.ServerGame.TagSystem.TagBases
{
    class BulletEntityTagBase : TemplateTagBase
    {
        // <--[tagbase]
        // @Base bullet_entity[<BulletEntityTag>]
        // @Group Entities
        // @ReturnType BulletEntityTag
        // @Returns the bullet entity with the given entity ID.
        // -->
        Server TheServer;

        public BulletEntityTagBase(Server tserver)
        {
            Name = "bullet_entity";
            TheServer = tserver;
        }

        public override string Handle(TagData data)
        {
            long eid;
            string input = data.GetModifier(0).ToLower();
            if (long.TryParse(input, out eid))
            {
                foreach (Region r in TheServer.LoadedRegions)
                {
                    foreach (Entity e in r.Entities)
                    {
                        if (e.EID == eid && e is BulletEntity)
                        {
                            return new BulletEntityTag((BulletEntity)e).Handle(data.Shrink());
                        }
                    }
                }
            }
            return new TextTag("&{NULL}").Handle(data.Shrink());
        }
    }
}