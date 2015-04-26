﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ShadowOperations.Shared;
using ShadowOperations.ServerGame.EntitySystem;

namespace ShadowOperations.ServerGame.ItemSystem.CommonItems
{
    class OpenHandItem: BaseItemInfo
    {
        public OpenHandItem()
            : base()
        {
            Name = "open_hand";
        }

        public override void PrepItem(EntitySystem.PlayerEntity player, ItemStack item)
        {
        }

        public override void AltClick(EntitySystem.PlayerEntity player, ItemStack item)
        {
            player.Grabbed = null;
        }

        public override void Click(EntitySystem.PlayerEntity player, ItemStack item)
        {
            Location ang = player.GetAngles();
            Location end = player.GetEyePosition() + Utilities.ForwardVector_Deg(ang.X, ang.Y) * 2;
            player.TheServer.PhysicsWorld.Remove(player.Body);
            BEPUphysics.Entities.Entity e = player.TheServer.Collision.CuboidLineTrace(new Location(0.1, 0.1, 0.1), player.GetEyePosition(), end).HitEnt;
            player.TheServer.PhysicsWorld.Add(player.Body);
            if (e != null && ((PhysicsEntity)e.Tag).GetMass() > 0)
            {
                player.Grabbed = (PhysicsEntity)e.Tag;
                player.GrabForce = 100f;
            }
        }

        public override void Use(EntitySystem.PlayerEntity player, ItemStack item)
        {
        }

        public override void SwitchFrom(PlayerEntity player, ItemStack item)
        {
            player.Grabbed = null;
        }

        public override void SwitchTo(PlayerEntity player, ItemStack item)
        {
        }
    }
}
