﻿using Frenetic.CommandSystem;
using Voxalia.ClientGame.ClientMainSystem;

namespace Voxalia.ClientGame.CommandSystem.UICommands
{
    /// <summary>
    /// A command to run.
    /// </summary>
    class SprintCommand : AbstractCommand
    {
        public Client TheClient;

        public SprintCommand(Client tclient)
        {
            TheClient = tclient;
            Name = "sprint";
            Description = "Makes the player sprint.";
            Arguments = "";
        }

        public override void Execute(CommandEntry entry)
        {
            if (entry.Marker == 0)
            {
                entry.Bad("Must use +, -, or !");
            }
            else if (entry.Marker == 1)
            {
                TheClient.Player.Sprint = true;
            }
            else if (entry.Marker == 2)
            {
                TheClient.Player.Sprint = false;
            }
            else if (entry.Marker == 3)
            {
                TheClient.Player.Sprint = !TheClient.Player.Sprint;
            }
        }
    }
}