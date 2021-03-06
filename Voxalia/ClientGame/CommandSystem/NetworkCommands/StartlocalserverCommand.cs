//
// This file is part of the game Voxalia, created by FreneticXYZ.
// This code is Copyright (C) 2016 FreneticXYZ under the terms of the MIT license.
// See README.md or LICENSE.txt for contents of the MIT license.
// If these are not available, see https://opensource.org/licenses/MIT
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FreneticScript;
using FreneticScript.CommandSystem;
using Voxalia.ClientGame.ClientMainSystem;
using Voxalia.ServerGame.ServerMainSystem;
using System.Threading.Tasks;
using Voxalia.Shared;

namespace Voxalia.ClientGame.CommandSystem.NetworkCommands
{
    public class StartlocalserverCommand: AbstractCommand
    {
        Client TheClient;

        public StartlocalserverCommand(Client tclient)
        {
            TheClient = tclient;
            Name = "startlocalserver";
            Description = "Launches you into a local game server";
            Arguments = "[port]";
            Waitable = true;
        }

        public override void Execute(CommandQueue queue, CommandEntry entry)
        {
            string arg0 = "28010";
            if (entry.Arguments.Count >= 1)
            {
                arg0 = entry.GetArgument(queue, 0);
            }
            if (TheClient.LocalServer != null)
            {
                entry.Good(queue, "Shutting down pre-existing server.");
                TheClient.LocalServer.ShutDown();
                TheClient.LocalServer = null;
            }
            entry.Good(queue, "Generating new server...");
            TheClient.LocalServer = new Server(Utilities.StringToInt(arg0));
            Server.Central = TheClient.LocalServer;
            Action callback = null;
            if (entry.WaitFor && queue.WaitingOn == entry)
            {
                callback = () =>
                {
                    queue.WaitingOn = null;
                };
            }
            Task.Factory.StartNew(() =>
            {
                try
                {
                    TheClient.LocalServer.StartUp(callback);
                }
                catch (Exception ex)
                {
                    Utilities.CheckException(ex);
                    SysConsole.Output("Running local server", ex);
                }
            });
        }
    }
}
