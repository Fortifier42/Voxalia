//
// This file is part of the game Voxalia, created by FreneticXYZ.
// This code is Copyright (C) 2016 FreneticXYZ under the terms of the MIT license.
// See README.md or LICENSE.txt for contents of the MIT license.
// If these are not available, see https://opensource.org/licenses/MIT
//

using FreneticScript.CommandSystem;
using Voxalia.ClientGame.ClientMainSystem;

namespace Voxalia.ClientGame.CommandSystem.UICommands
{
    class ItemdownCommand : AbstractCommand
    {
        public Client TheClient;
        
        public ItemdownCommand(Client tclient)
        {
            TheClient = tclient;
            Name = "itemdown";
            Description = "Adjust the item (down version).";
            Arguments = "";
        }

        public override void Execute(CommandQueue queue, CommandEntry entry)
        {
            if (entry.Marker == 0)
            {
                queue.HandleError(entry, "Must use +, -, or !");
            }
            else if (entry.Marker == 1)
            {
                TheClient.Player.ItemDown = true;
            }
            else if (entry.Marker == 2)
            {
                TheClient.Player.ItemDown = false;
            }
            else if (entry.Marker == 3)
            {
                TheClient.Player.ItemDown = !TheClient.Player.ItemDown;
            }
        }
    }
}
