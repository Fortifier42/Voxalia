﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using Voxalia.Shared;
using Voxalia.ServerGame.EntitySystem;
using Voxalia.ServerGame.NetworkSystem;
using Voxalia.ServerGame.NetworkSystem.PacketsOut;
using System.Threading;
using FreneticScript;
using FreneticScript.TagHandlers.Common;
using System.Linq;

namespace Voxalia.ServerGame.ServerMainSystem
{
    public partial class Server
    {
        /// <summary>
        /// All player-type entities that exist on this server.
        /// </summary>
        public List<PlayerEntity> Players = new List<PlayerEntity>();

        public List<PlayerEntity> PlayersWaiting = new List<PlayerEntity>();

        public double opsat = 0;

        string SaveStr = null;

        public void Broadcast(string message, string bcolor = null)
        {
            for (int i = 0; i < Players.Count; i++)
            {
                Players[i].Network.SendMessage(message);
            }
            SysConsole.Output(OutputType.INFO, "[Broadcast] " + message, bcolor);
        }

        public void SendToAll(AbstractPacketOut packet)
        {
            for (int i = 0; i < Players.Count; i++)
            {
                Players[i].Network.SendPacket(packet);
            }
        }

        public int TPS = 0;
        int tpsc = 0;

        public void OncePerSecondActions()
        {
            TPS = tpsc;
            tpsc = 0;
            if (CVars.system.Modified)
            {
                CVars.system.Modified = false;
                StringBuilder cvarsave = new StringBuilder(CVars.system.CVarList.Count * 100);
                cvarsave.Append("// THIS FILE IS AUTOMATICALLY GENERATED.\n");
                cvarsave.Append("// This file is run very early in startup, be careful with it!\n");
                cvarsave.Append("debug minimal;\n");
                for (int i = 0; i < CVars.system.CVarList.Count; i++)
                {
                    if (!CVars.system.CVarList[i].Flags.HasFlag(CVarFlag.ServerControl)
                        && !CVars.system.CVarList[i].Flags.HasFlag(CVarFlag.ReadOnly)
                        && !CVars.system.CVarList[i].Flags.HasFlag(CVarFlag.DoNotSave))
                    {
                        string val = CVars.system.CVarList[i].Value;
                        if (val.Contains('\"'))
                        {
                            val = "<{unescape[" + EscapeTagBase.Escape(val) + "]}>";
                        }
                        cvarsave.Append("set \"" + CVars.system.CVarList[i].Name + "\" \"" + val + "\";\n");
                    }
                }
                SaveStr = cvarsave.ToString();
                Thread thread = new Thread(new ThreadStart(SaveCFG));
                thread.Start();
            }
         //   SysConsole.Output(OutputType.INFO, "Tick: " + (TickTimeC / TickTimes) + ", Schedule: " + (ScheduleTimeC / ScheduleTimes)
         //       + ", Physics: " + (PhysicsTimeC / PhysicsTimes) + ", Entity: " + (EntityTimeC / EntityTimes) + " Ticked: " + TickTimes);
            TickTimes = 0;
            TickTimeC = 0;
            ScheduleTimes = 0;
            ScheduleTimeC = 0;
            PhysicsTimes = 0;
            PhysicsTimeC = 0;
            EntityTimes = 0;
            EntityTimeC = 0;
        }

        public void SaveCFG()
        {
            try
            {
                Program.Files.WriteText("serverdefaultsettings.cfg", SaveStr);
            }
            catch (Exception ex)
            {
                SysConsole.Output(OutputType.ERROR, "Saving settings: " + ex.ToString());
            }
        }

        double pts;

        public double Delta;

        public double TickTimeC;
        public double TickTimes;

        public double ScheduleTimeC;
        public double ScheduleTimes;

        public double PhysicsTimeC;
        public double PhysicsTimes;

        public double EntityTimeC;
        public double EntityTimes;

        /// <summary>
        /// The server's primary tick function.
        /// </summary>
        public void Tick(double delta)
        {
            tpsc++;
            Delta = delta * CVars.g_timescale.ValueD;
            try
            {
                Stopwatch sw = new Stopwatch();
                sw.Start();
                opsat += Delta;
                if (opsat >= 1.0)
                {
                    opsat -= 1.0;
                    OncePerSecondActions();
                }
                if (CVars.g_timescale.ValueD != pts) // TODO: Make this CVar per-world
                {
                    for (int i = 0; i < LoadedRegions.Count; i++)
                    {
                        LoadedRegions[i].SendToAll(new CVarSetPacketOut(CVars.g_timescale, this));
                    }
                }
                pts = CVars.g_timescale.ValueD;
                Networking.Tick(Delta); // TODO: Asynchronize network ticking
                ConsoleHandler.CheckInput(); // TODO: Asynchronize command ticking
                Commands.Tick(Delta); // TODO: Asynchronize command ticking
                TickWorlds(Delta); // TODO: Asynchronize world ticking
                Stopwatch schedw = new Stopwatch();
                schedw.Start();
                Schedule.RunAllSyncTasks(Delta);
                schedw.Stop();
                ScheduleTimeC += schedw.Elapsed.TotalMilliseconds;
                ScheduleTimes++;
                sw.Stop();
                TickTimeC += sw.Elapsed.TotalMilliseconds;
                TickTimes++;
            }
            catch (Exception ex)
            {
                Utilities.CheckException(ex);
                SysConsole.Output(OutputType.ERROR, "Tick: " + ex.ToString());
            }
        }
    }
}
