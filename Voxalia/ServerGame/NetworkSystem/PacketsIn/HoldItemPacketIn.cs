//
// This file is part of the game Voxalia, created by FreneticXYZ.
// This code is Copyright (C) 2016 FreneticXYZ under the terms of the MIT license.
// See README.md or LICENSE.txt for contents of the MIT license.
// If these are not available, see https://opensource.org/licenses/MIT
//

using Voxalia.Shared;
using Voxalia.ServerGame.ItemSystem;
using Voxalia.ServerGame.NetworkSystem.PacketsOut;

namespace Voxalia.ServerGame.NetworkSystem.PacketsIn
{
    public class HoldItemPacketIn: AbstractPacketIn
    {
        public override bool ParseBytesAndExecute(byte[] data)
        {
            if (data.Length != 4)
            {
                return false;
            }
            if (Player.Flags.HasFlag(YourStatusFlags.RELOADING))
            {
                Player.Network.SendPacket(new SetHeldItemPacketOut(Player.Items.cItem));
                return true; // Permit but ignore
            }
            Player.NoteDidAction();
            int dat = Utilities.BytesToInt(data);
            dat = dat % (Player.Items.Items.Count + 1);
            while (dat < 0)
            {
                dat += Player.Items.Items.Count + 1;
            }
            ItemStack old = Player.Items.GetItemForSlot(Player.Items.cItem);
            old.Info.SwitchFrom(Player, old);
            Player.Items.cItem = dat;
            ItemStack newit = Player.Items.GetItemForSlot(Player.Items.cItem);
            newit.Info.SwitchTo(Player, newit);
            return true;
        }
    }
}
