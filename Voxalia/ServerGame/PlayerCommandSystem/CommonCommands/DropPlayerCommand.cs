﻿using Voxalia.Shared;
using Voxalia.ServerGame.EntitySystem;
using Voxalia.ServerGame.ItemSystem;

namespace Voxalia.ServerGame.PlayerCommandSystem.CommonCommands
{
    class DropPlayerCommand : AbstractPlayerCommand
    {
        public DropPlayerCommand()
        {
            Name = "drop";
            Silent = true;
        }

        public override void Execute(PlayerCommandEntry entry)
        {
            int it = entry.Player.Items.cItem;
            if (entry.InputArguments.Count > 0)
            {
                it = Utilities.StringToInt(entry.InputArguments[0]);
            }
            ItemStack stack = entry.Player.Items.GetItemForSlot(it);
            if (stack.IsBound)
            {
                if (stack.Info == entry.Player.TheServer.Items.GetInfoFor("open_hand")
                    && entry.Player.GrabJoint != null)
                {
                    entry.Player.TheRegion.DestroyJoint(entry.Player.GrabJoint);
                    entry.Player.GrabJoint = null;
                    return;
                }
                entry.Player.Network.SendMessage("^1Can't drop this."); // TODO: Language, entry.output, etc.
                return;
            }
            PhysicsEntity ie = entry.Player.TheRegion.ItemToEntity(stack);
            // TODO: Animate player
            Location fvel = entry.Player.ForwardVector();
            ie.SetPosition(entry.Player.GetEyePosition() + fvel);
            ie.SetOrientation(entry.Player.GetOrientation());
            ie.SetVelocity(fvel);
            entry.Player.TheRegion.SpawnEntity(ie);
            entry.Player.Items.RemoveItem(entry.Player.Items.cItem);
        }
    }
}
