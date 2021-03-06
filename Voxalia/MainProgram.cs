//
// This file is part of the game Voxalia, created by FreneticXYZ.
// This code is Copyright (C) 2016 FreneticXYZ under the terms of the MIT license.
// See README.md or LICENSE.txt for contents of the MIT license.
// If these are not available, see https://opensource.org/licenses/MIT
//

using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using Voxalia.Shared;
using Voxalia.ServerGame.ServerMainSystem;
using Voxalia.ClientGame.ClientMainSystem;
using System.IO;
using System.Threading;
using System.Globalization;
using Voxalia.Shared.Files;

namespace Voxalia
{
    /// <summary>
    /// Central program entry point.
    /// </summary>
    class MainProgram
    {
        /// <summary>
        /// A handle for the console window.
        /// </summary>
        public static IntPtr ConsoleHandle;
        
        /// <summary>
        /// Central program entry point.
        /// Decides whether to lauch the server or the client.
        /// </summary>
        /// <param name="args">The command line arguments.</param>
        static void Main(string[] args)
        {
            ConsoleHandle = Process.GetCurrentProcess().MainWindowHandle;
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
            CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
            CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;
            SysConsole.Init();
            StringBuilder arger = new StringBuilder();
            for (int i = 0; i < args.Length; i++)
            {
                arger.Append(args[i]).Append(' ');
            }
            try
            {
                Program.Init();
                if (args.Length > 0 && args[0] == "server")
                {
                    string[] targs = new string[args.Length - 1];
                    Array.Copy(args, 1, targs, 0, targs.Length);
                    Server.Init(targs);
                }
                else
                {
                    Client.Init(arger.ToString());
                }
            }
            catch (Exception ex)
            {
                if (ex is ThreadAbortException)
                {
                    Console.WriteLine("Forced shutdown - terminating process.");
                    Environment.Exit(0);
                    return;
                }
                SysConsole.Output(ex);
                File.WriteAllText("GLOBALERR_" + DateTime.Now.ToFileTimeUtc().ToString() + ".txt", ex.ToString() + "\n\n" + Environment.StackTrace);
            }
            SysConsole.ShutDown();
            Console.WriteLine("Final shutdown - terminating process.");
            Environment.Exit(0);
        }
    }
}
