﻿using System;
using System.Collections.Generic;
using Voxalia.Shared;
using Voxalia.ServerGame.ServerMainSystem;
using BEPUphysics;
using BEPUutilities;
using BEPUphysics.Settings;
using Voxalia.ServerGame.EntitySystem;
using Voxalia.ServerGame.JointSystem;
using Voxalia.ServerGame.NetworkSystem;
using Voxalia.ServerGame.NetworkSystem.PacketsOut;
using BEPUutilities.Threading;
using Voxalia.ServerGame.WorldSystem.SimpleGenerator;
using System.Threading;
using System.Threading.Tasks;
using BEPUphysics.BroadPhaseEntries;
using BEPUphysics.CollisionShapes.ConvexShapes;
using Voxalia.Shared.Collision;
using Voxalia.ServerGame.ItemSystem;
using Voxalia.ServerGame.ItemSystem.CommonItems;

namespace Voxalia.ServerGame.WorldSystem
{
    public class Region // TODO: Split into partial files!
    {
        /// <summary>
        /// How much time has elapsed since the last tick started.
        /// </summary>
        public double Delta;

        public List<Entity> GetTargets(string target)
        {
            List<Entity> ents = new List<Entity>();
            for (int i = 0; i < Targetables.Count; i++)
            {
                if (Targetables[i].GetTargetName() == target)
                {
                    ents.Add((Entity)Targetables[i]);
                }
            }
            return ents;
        }

        public void Trigger(string target, Entity ent, Entity user)
        {
            for (int i = 0; i < Targetables.Count; i++)
            {
                if (Targetables[i].GetTargetName() == target)
                {
                    Targetables[i].Trigger(ent, user);
                }
            }
        }

        public List<EntityTargettable> Targetables = new List<EntityTargettable>();

        /// <summary>
        /// All spawnpoint-type entities that exist on this server.
        /// </summary>
        public List<SpawnPointEntity> SpawnPoints = new List<SpawnPointEntity>();

        public List<InternalBaseJoint> Joints = new List<InternalBaseJoint>();

        public List<PlayerEntity> Players = new List<PlayerEntity>();

        /// <summary>
        /// All entities that exist on this server.
        /// </summary>
        public List<Entity> Entities = new List<Entity>();

        /// <summary>
        /// All entities that exist on this server and must tick.
        /// </summary>
        public List<Entity> Tickers = new List<Entity>();

        long jID = 0;

        public double GlobalTickTime = 0;

        public void AddJoint(InternalBaseJoint joint)
        {
            Joints.Add(joint);
            joint.One.Joints.Add(joint);
            joint.Two.Joints.Add(joint);
            joint.JID = jID;
            joint.Enabled = true;
            if (joint is BaseJoint)
            {
                BaseJoint pjoint = (BaseJoint)joint;
                pjoint.CurrentJoint = pjoint.GetBaseJoint();
                PhysicsWorld.Add(pjoint.CurrentJoint);
            }
            SendToAll(new AddJointPacketOut(joint));
        }

        public void DestroyJoint(InternalBaseJoint joint)
        {
            Joints.Remove(joint);
            joint.One.Joints.Remove(joint);
            joint.Two.Joints.Remove(joint);
            joint.Enabled = false;
            if (joint is BaseJoint)
            {
                BaseJoint pjoint = (BaseJoint)joint;
                if (pjoint.CurrentJoint != null)
                {
                    PhysicsWorld.Remove(pjoint.CurrentJoint);
                }
            }
            SendToAll(new DestroyJointPacketOut(joint));
        }

        public long cID = 0;

        public Dictionary<string, Entity> JointTargets = new Dictionary<string, Entity>();

        public void ChunkSendToAll(AbstractPacketOut packet, Location cpos)
        {
            if (cpos.IsNaN())
            {
                for (int i = 0; i < Players.Count; i++)
                {
                    Players[i].Network.SendPacket(packet);
                }
            }
            else
            {
                for (int i = 0; i < Players.Count; i++)
                {
                    if (Players[i].CanSeeChunk(cpos))
                    {
                        Players[i].Network.SendPacket(packet);
                    }
                }
            }
        }

        public void SendToAll(AbstractPacketOut packet)
        {
            for (int i = 0; i < Players.Count; i++)
            {
                Players[i].Network.SendPacket(packet);
            }
        }

        public void Broadcast(string message)
        {
            SysConsole.Output(OutputType.INFO, "[Broadcast] " + message);
            for (int i = 0; i < Players.Count; i++)
            {
                Players[i].Network.SendMessage(message);
            }
        }

        public void SpawnEntity(Entity e) // TODO: Move me to World
        {
            if (e.IsSpawned)
            {
                return;
            }
            JointTargets.Remove(e.JointTargetID);
            JointTargets.Add(e.JointTargetID, e);
            Entities.Add(e);
            e.IsSpawned = true;
            e.EID = cID++;
            if (e.Ticks)
            {
                Tickers.Add(e);
            }
            if (e is EntityTargettable)
            {
                Targetables.Add((EntityTargettable)e);
            }
            e.TheRegion = this;
            AbstractPacketOut packet = null;
            if (e is PhysicsEntity && !(e is PlayerEntity))
            {
                ((PhysicsEntity)e).SpawnBody();
                if (e.NetworkMe)
                {
                    packet = new SpawnPhysicsEntityPacketOut((PhysicsEntity)e);
                }
            }
            else if (e is PrimitiveEntity)
            {
                ((PrimitiveEntity)e).Spawn();
            }
            if (e is SpawnPointEntity)
            {
                SpawnPoints.Add((SpawnPointEntity)e);
            }
            else if (e is PointLightEntity)
            {
                packet = new SpawnLightPacketOut((PointLightEntity)e);
            }
            else if (e is BulletEntity)
            {
                packet = new SpawnBulletPacketOut((BulletEntity)e);
            }
            else if (e is PrimitiveEntity)
            {
                if (e.NetworkMe)
                {
                    packet = new SpawnPrimitiveEntityPacketOut((PrimitiveEntity)e);
                }
            }
            if (packet != null)
            {
                SendToAll(packet);
            }
            if (e is PlayerEntity)
            {
                TheServer.Players.Add((PlayerEntity)e);
                Players.Add((PlayerEntity)e);
                for (int i = 0; i < TheServer.Networking.Strings.Strings.Count; i++)
                {
                    ((PlayerEntity)e).Network.SendPacket(new NetStringPacketOut(TheServer.Networking.Strings.Strings[i]));
                }
                ((PlayerEntity)e).SpawnBody();
                packet = new SpawnPhysicsEntityPacketOut((PlayerEntity)e);
                for (int i = 0; i < Players.Count; i++)
                {
                    if (Players[i] != e)
                    {
                        Players[i].Network.SendPacket(packet);
                    }
                }
                ((PlayerEntity)e).Network.SendPacket(new YourEIDPacketOut(e.EID));
                ((PlayerEntity)e).Network.SendPacket(new CVarSetPacketOut(TheServer.CVars.g_timescale, TheServer));
                ((PlayerEntity)e).SetAnimation("human/stand/idle01", 0);
                ((PlayerEntity)e).SetAnimation("human/stand/idle01", 1);
                ((PlayerEntity)e).SetAnimation("human/stand/idle01", 2);
                for (int i = 0; i < Entities.Count - 2; i++)
                {
                    if (Entities[i] is PhysicsEntity)
                    {
                        if (Entities[i].NetworkMe)
                        {
                            ((PlayerEntity)e).Network.SendPacket(new SpawnPhysicsEntityPacketOut((PhysicsEntity)Entities[i]));
                        }
                    }
                    else if (Entities[i] is PointLightEntity)
                    {
                        ((PlayerEntity)e).Network.SendPacket(new SpawnLightPacketOut((PointLightEntity)Entities[i]));
                    }
                    else if (Entities[i] is BulletEntity)
                    {
                        ((PlayerEntity)e).Network.SendPacket(new SpawnBulletPacketOut((BulletEntity)Entities[i]));
                    }
                    else if (Entities[i] is PrimitiveEntity)
                    {
                        if (Entities[i].NetworkMe)
                        {
                            ((PlayerEntity)e).Network.SendPacket(new SpawnPrimitiveEntityPacketOut((PrimitiveEntity)Entities[i]));
                        }
                    }
                }
            }
        }

        public void DespawnEntity(Entity e)
        {
            if (!e.IsSpawned)
            {
                return;
            }
            JointTargets.Remove(e.JointTargetID);
            Entities.Remove(e);
            e.IsSpawned = false;
            if (e.Ticks)
            {
                Tickers.Remove(e);
            }
            if (e is EntityTargettable)
            {
                Targetables.Remove((EntityTargettable)e);
            }
            if (e is PhysicsEntity)
            {
                ((PhysicsEntity)e).DestroyBody();
            }
            else if (e is PrimitiveEntity)
            {
                ((PrimitiveEntity)e).Destroy();
            }

            if (e is SpawnPointEntity)
            {
                SpawnPoints.Remove((SpawnPointEntity)e);
            }
            else if (e is PlayerEntity)
            {
                TheServer.Players.Remove((PlayerEntity)e);
                Players.Remove((PlayerEntity)e);
                ((PlayerEntity)e).Kick("Despawned!");
            }
            if (e.NetworkMe)
            {
                SendToAll(new DespawnEntityPacketOut(e.EID));
            }
        }

        // TODO: Rework, update
        public void LoadMapFromString(string data)
        {
            for (int i = 0; i < Entities.Count; i++)
            {
                if (!(Entities[i] is PlayerEntity))
                {
                    DespawnEntity(Entities[i]);
                    i--;
                }
            }
            data = data.Replace('\r', '\n').Replace('\t', '\n').Replace("\n", "");
            int start = 0;
            for (int i = 0; i < data.Length; i++)
            {
                if (data[i] == '{')
                {
                    string objo = data.Substring(start, i - start).Trim();
                    int obj_start = i + 1;
                    for (int x = i + 1; x < data.Length; x++)
                    {
                        if (data[x] == '}')
                        {
                            try
                            {
                                string objdat = data.Substring(obj_start, x - obj_start);
                                LoadObj(objo, objdat);
                            }
                            catch (Exception ex)
                            {
                                SysConsole.Output(OutputType.ERROR, "Invalid entity " + objo + ": " + ex.ToString());
                            }
                            i = x;
                            break;
                        }
                    }
                    start = i + 1;
                }
            }
            // TODO: Respawn all players
        }

        // TODO: Rework, update
        public void LoadObj(string name, string dat)
        {
            string[] dats = dat.Split(';');
            if (name == "general")
            {
                for (int i = 0; i < dats.Length; i++)
                {
                    if (dats[i].Length <= 0)
                    {
                        continue;
                    }
                    string trimmed = dats[i].Trim();
                    if (trimmed.Length == 0)
                    {
                        continue;
                    }
                    string[] datum = trimmed.Split(':');
                    if (datum.Length != 2)
                    {
                        throw new Exception("Invalid key '" + dats[i] + "'!");
                    }
                    string det = datum[1].Trim().Replace("&nl", "\n").Replace("&sc", ";").Replace("&amp", "&");
                    switch (datum[0])
                    {
                        case "ambient":
                            // TODO: ambient = Utilities.StringToLocation(det);
                            break;
                        case "music":
                            // TODO: music = det;
                            break;
                        default:
                            throw new Exception("Invalid key: " + datum[0].Trim() + "!");
                    }
                }
                return;
            }
            if (name.StartsWith("joint_"))
            {
                InternalBaseJoint joint;
                switch (name.Substring("joint_".Length))
                {
                    case "ballsocket":
                        joint = new JointBallSocket(null, null, Location.Zero);
                        break;
                    default:
                        throw new Exception("Invalid joint type '" + name + "'!");
                }
                for (int i = 0; i < dats.Length; i++)
                {
                    if (dats[i].Length <= 0)
                    {
                        continue;
                    }
                    string trimmed = dats[i].Trim();
                    if (trimmed.Length == 0)
                    {
                        continue;
                    }
                    string[] datum = trimmed.Split(':');
                    if (datum.Length != 2)
                    {
                        throw new Exception("Invalid key '" + dats[i] + "'!");
                    }
                    string det = datum[1].Trim().Replace("&nl", "\n").Replace("&sc", ";").Replace("&amp", "&");
                    bool applied = false;
                    try
                    {
                        applied = joint.ApplyVar(this, datum[0].Trim(), det);
                    }
                    catch (Exception)
                    {
                        // Ignore for now
                    }
                    if (!applied)
                    {
                        throw new Exception("Invalid key: " + datum[0].Trim() + " === " + det + "!");
                    }
                }
                if (joint.One != null && joint.Two != null)
                {
                    AddJoint(joint);
                }
                else
                {
                    SysConsole.Output(OutputType.WARNING, "Invalid joint " + name + ": Invalid targets!");
                }
            }
            else
            {
                Entity e;
                switch (name) // TODO: Registry
                {
                    case "cube":
                        e = new CubeEntity(new Location(1, 1, 1), this, 0f);
                        break;
                    case "pointlight":
                        e = new PointLightEntity(this);
                        break;
                    case "spawn":
                        e = new SpawnPointEntity(this);
                        break;
                    case "model":
                        e = new ModelEntity("", this);
                        break;
                    case "triggergeneric":
                        e = new TriggerGenericEntity(new Location(1, 1, 1), this);
                        break;
                    case "targetscriptrunner":
                        e = new TargetScriptRunnerEntity(this);
                        break;
                    case "targetposition":
                        e = new TargetPositionEntity(this);
                        break;
                    case "functrack":
                        e = new FuncTrackEntity(new Location(1, 1, 1), this, 0);
                        break;
                    default:
                        throw new Exception("Invalid entity type '" + name + "'!");
                }
                for (int i = 0; i < dats.Length; i++)
                {
                    if (dats[i].Length <= 0)
                    {
                        continue;
                    }
                    string trimmed = dats[i].Trim();
                    if (trimmed.Length == 0)
                    {
                        continue;
                    }
                    string[] datum = trimmed.Split(':');
                    if (datum.Length != 2)
                    {
                        throw new Exception("Invalid key '" + dats[i] + "'!");
                    }
                    string det = datum[1].Trim().Replace("&nl", "\n").Replace("&sc", ";").Replace("&amp", "&");
                    bool applied = false;
                    try
                    {
                        applied = e.ApplyVar(datum[0].Trim(), det);
                    }
                    catch (Exception)
                    {
                        // Ignore for now
                    }
                    if (!applied)
                    {
                        throw new Exception("Invalid key: " + datum[0].Trim() + "!");
                    }
                }
                e.Recalculate();
                SpawnEntity(e);
            }
        }

        public int MaxViewRadiusInChunks = 4;

        /// <summary>
        /// The physics world in which all physics-related activity takes place.
        /// </summary>
        public Space PhysicsWorld;

        public YAMLConfiguration Config;

        Thread MainThread;

        int MainThreadID;

        public void CheckThreadValidity()
        {
            if (Thread.CurrentThread.ManagedThreadId != MainThreadID)
            {
                throw new Exception("Called a critical method on the wrong thread!");
            }
        }

        public void BuildWorld()
        {
            MainThread = Thread.CurrentThread;
            MainThreadID = MainThread.ManagedThreadId;
            ParallelLooper pl = new ParallelLooper();
            for (int i = 0; i < Environment.ProcessorCount; i++)
            {
                pl.AddThread();
            }
            CollisionDetectionSettings.AllowedPenetration = 0.01f;
            PhysicsWorld = new Space(pl);
            PhysicsWorld.TimeStepSettings.MaximumTimeStepsPerFrame =  10;
            PhysicsWorld.ForceUpdater.Gravity = new Vector3(0, 0, -9.8f * 3f / 2f);
            Collision = new CollisionUtil(PhysicsWorld);
            string fname = "saves/" + Name + "/region.yml";
            if (Program.Files.Exists(fname))
            {
                Config = new YAMLConfiguration(Program.Files.ReadText(fname));
            }
            else
            {
                Config = new YAMLConfiguration("");
            }
            Config.Changed += new EventHandler(configChanged);
            Config.Set("general.IMPORTANT_NOTE", "Edit this configuration at your own risk!");
            Config.Set("general.name", Name);
            Config.Default("general.seed", Utilities.UtilRandom.Next(short.MaxValue * 2));
            CFGEdited = true;
            Seed = (short)Config.ReadInt("general.seed", 100);
            Random seedGen = new Random(Seed);
            Seed2 = (short)(seedGen.Next(short.MaxValue * 2) - short.MaxValue);
            bool bval = false;
            LoadRegion(new Location(-MaxViewRadiusInChunks * 30), new Location(MaxViewRadiusInChunks * 30), true, () =>
            {
                bval = true;
            });
            int c = 0;
            while (!AllChunksLoadedFully(out c) || !bval)
            {
                TheServer.Schedule.RunAllSyncTasks(0.016); // TODO: Separate per-world scheduler // Also don't freeze the entire server just because we're waiting on chunks >.>
                Thread.Sleep(16);
            }
            SysConsole.Output(OutputType.INIT, "Finished building chunks! Now have " + LoadedChunks.Count + " chunks!");
        }

        bool CFGEdited = false;

        void configChanged(object sender, EventArgs e)
        {
            CFGEdited = true;
        }

        public void AddChunk(FullChunkObject mesh)
        {
            CheckThreadValidity();
            if (mesh == null)
            {
                return;
            }
            PhysicsWorld.Add(mesh);
        }

        public void RemoveChunkQuiet(FullChunkObject mesh)
        {
            CheckThreadValidity();
            if (mesh == null)
            {
                return;
            }
            PhysicsWorld.Remove(mesh);
        }

        public bool SpecialCaseRayTrace(Location start, Location dir, float len, MaterialSolidity considerSolid, Func<BroadPhaseEntry, bool> filter, out RayCastResult rayHit)
        {
            CheckThreadValidity();
            Ray ray = new Ray(start.ToBVector(), dir.ToBVector());
            RayCastResult best = new RayCastResult(new RayHit() { T = len }, null);
            bool hA = false;
            if (considerSolid.HasFlag(MaterialSolidity.FULLSOLID))
            {
                RayCastResult rcr;
                if (PhysicsWorld.RayCast(ray, len, filter, out rcr))
                {
                    best = rcr;
                    hA = true;
                }
            }
            AABB box = new AABB();
            box.Min = start;
            box.Max = start;
            box.Include(start + dir * len);
            foreach (KeyValuePair<Location, Chunk> chunk in LoadedChunks)
            {
                if (chunk.Value == null || chunk.Value.FCO == null)
                {
                    continue;
                }
                if (!box.Intersects(new AABB() { Min = chunk.Value.WorldPosition * 30, Max = chunk.Value.WorldPosition * 30 + new Location(30, 30, 30) }))
                {
                    continue;
                }
                RayHit temp;
                if (chunk.Value.FCO.RayCast(ray, len, null, considerSolid, out temp))
                {
                    hA = true;
                    if (temp.T < best.HitData.T)
                    {
                        best.HitData = temp;
                        best.HitObject = chunk.Value.FCO;
                    }
                }
            }
            rayHit = best;
            return hA;
        }

        public bool SpecialCaseConvexTrace(ConvexShape shape, Location start, Location dir, float len, MaterialSolidity considerSolid, Func<BroadPhaseEntry, bool> filter, out RayCastResult rayHit)
        {
            CheckThreadValidity();
            RigidTransform rt = new RigidTransform(start.ToBVector(), BEPUutilities.Quaternion.Identity);
            BEPUutilities.Vector3 sweep = (dir * len).ToBVector();
            RayCastResult best = new RayCastResult(new RayHit() { T = len }, null);
            bool hA = false;
            if (considerSolid.HasFlag(MaterialSolidity.FULLSOLID))
            {
                RayCastResult rcr;
                if (PhysicsWorld.ConvexCast(shape, ref rt, ref sweep, filter, out rcr))
                {
                    best = rcr;
                    hA = true;
                }
            }
            sweep = dir.ToBVector();
            AABB box = new AABB();
            box.Min = start;
            box.Max = start;
            box.Include(start + dir * len);
            foreach (KeyValuePair<Location, Chunk> chunk in LoadedChunks)
            {
                if (chunk.Value == null || chunk.Value.FCO == null)
                {
                    continue;
                }
                if (!box.Intersects(new AABB() { Min = chunk.Value.WorldPosition * 30, Max = chunk.Value.WorldPosition * 30 + new Location(30, 30, 30) }))
                {
                    continue;
                }
                RayHit temp;
                if (chunk.Value.FCO.ConvexCast(shape, ref rt, ref sweep, len, considerSolid, out temp))
                {
                    hA = true;
                    if (temp.T < best.HitData.T)
                    {
                        best.HitData = temp;
                        best.HitObject = chunk.Value.FCO;
                    }
                }
            }
            rayHit = best;
            return hA;
        }

        private bool AllChunksLoadedFully(out int c)
        {
            c = 0;
            CheckThreadValidity();
            foreach (Chunk chunk in LoadedChunks.Values)
            {
                if (chunk.LOADING && !chunk.ISCUSTOM)
                {
                    c++;
                }
            }
            return c == 0;
        }

        public void LoadRegion(Location min, Location max, bool announce = true, Action callback = null)
        {
            RegionLoader rl = new RegionLoader() { region = this };
            TheServer.Schedule.StartASyncTask(() =>
            {
                rl.LoadRegion(min, max);
                if (announce)
                {
                    SysConsole.Output(OutputType.INIT, "Initially loaded " + rl.Count + " chunks...");
                }
                if (callback != null)
                {
                    callback.Invoke();
                }
            });
        }

        public short Seed;

        public short Seed2;

        void OncePerSecondActions()
        {
            foreach (Chunk o in LoadedChunks.Values)
            {
                if (o.LastEdited >= 0)
                {
                    Chunk to = o;
                    Action cb = null;/*
                    if (ChunksToDestroy.Contains(o))
                    {
                        cb = () =>
                        {
                            TheServer.Schedule.ScheduleSyncTask(() =>
                            {
                                if (ChunksToDestroy.Contains(to) && to.LastEdited < 0) // TODO: Handle rare case
                                {
                                    ChunksToDestroy.Remove(to);
                                    LoadedChunks.Remove(to.WorldPosition);
                                }
                            });
                        };
                    }*/
                    o.SaveToFile(cb);
                }
                // TODO: If distant from all players, unload
            }
            if (CFGEdited)
            {
                string cfg = Config.SaveToString();
                TheServer.Schedule.StartASyncTask(() =>
                {
                    Program.Files.WriteText("saves/" + Name + "/region.yml", cfg);
                });
            }
        }

        double opsat;

        public void Tick(double delta)
        {
            CheckThreadValidity();
            Delta = delta;
            GlobalTickTime += Delta;
            if (Delta <= 0)
            {
                return;
            }
            opsat += Delta;
            while (opsat > 1.0)
            {
                opsat -= 1.0;
                OncePerSecondActions();
            }
            PhysicsWorld.Update((float)delta); // TODO: More specific settings?
            // TODO: Async tick
            for (int i = 0; i < Tickers.Count; i++)
            {
                Tickers[i].Tick();
            }
            for (int i = 0; i < Joints.Count; i++) // TODO: Optimize!
            {
                if (Joints[i].Enabled && Joints[i] is BaseFJoint)
                {
                    ((BaseFJoint)Joints[i]).Solve();
                }
            }
        }

        public CollisionUtil Collision;

        public Location GravityNormal = new Location(0, 0, -1);

        public string Name = null;

        public Server TheServer = null;

        public Dictionary<Location, Chunk> LoadedChunks = new Dictionary<Location, Chunk>();

        public Material GetBlockMaterial(Location pos)
        {
            CheckThreadValidity();
            Chunk ch = LoadChunk(ChunkLocFor(pos));
            int x = (int)Math.Floor(pos.X) - (int)ch.WorldPosition.X * 30;
            int y = (int)Math.Floor(pos.Y) - (int)ch.WorldPosition.Y * 30;
            int z = (int)Math.Floor(pos.Z) - (int)ch.WorldPosition.Z * 30;
            return (Material)ch.GetBlockAt(x, y, z).BlockMaterial;
        }

        public BlockInternal GetBlockInternal(Location pos)
        {
            CheckThreadValidity();
            Chunk ch = LoadChunk(ChunkLocFor(pos));
            int x = (int)Math.Floor(pos.X) - (int)ch.WorldPosition.X * 30;
            int y = (int)Math.Floor(pos.Y) - (int)ch.WorldPosition.Y * 30;
            int z = (int)Math.Floor(pos.Z) - (int)ch.WorldPosition.Z * 30;
            return ch.GetBlockAt(x, y, z);
        }

        public void SetBlockMaterial(Location pos, Material mat, byte dat = 0, byte locdat = (byte)BlockFlags.EDITED, bool broadcast = true, bool regen = true, bool override_protection = false)
        {
            CheckThreadValidity();
            Chunk ch = LoadChunk(ChunkLocFor(pos));
            lock (ch.EditSessionLock)
            {
                int x = (int)Math.Floor(pos.X) - (int)ch.WorldPosition.X * 30;
                int y = (int)Math.Floor(pos.Y) - (int)ch.WorldPosition.Y * 30;
                int z = (int)Math.Floor(pos.Z) - (int)ch.WorldPosition.Z * 30;
                if (!override_protection && ((BlockFlags)ch.GetBlockAt(x, y, z).BlockLocalData).HasFlag(BlockFlags.PROTECTED))
                {
                    return;
                }
                ch.SetBlockAt(x, y, z, new BlockInternal((ushort)mat, dat, locdat));
                ch.LastEdited = GlobalTickTime;
                if (regen)
                {
                    ch.AddToWorld();
                    TrySurroundings(ch, pos, x, y, z);
                }
                if (broadcast)
                {
                    // TODO: Send per-person based on chunk awareness details
                    ChunkSendToAll(new BlockEditPacketOut(new Location[] { pos }, new Material[] { mat }, new byte[] { dat }), ch.WorldPosition);
                }
            }
        }

        public void TrySurroundings(Chunk ch, Location pos, int x, int y, int z) // TODO: Clean!
        {
            CheckThreadValidity();
            if (x == 0)
            {
                ch = GetChunk(ChunkLocFor(pos + new Location(-1, 0, 0)));
                if (ch != null && !ch.LOADING)
                {
                    ch.AddToWorld();
                }
            }
            if (y == 0)
            {
                ch = GetChunk(ChunkLocFor(pos + new Location(0, -1, 0)));
                if (ch != null && !ch.LOADING)
                {
                    ch.AddToWorld();
                }
            }
            if (z == 0)
            {
                ch = GetChunk(ChunkLocFor(pos + new Location(0, 0, -1)));
                if (ch != null && !ch.LOADING)
                {
                    ch.AddToWorld();
                }
            }
            if (x == Chunk.CHUNK_SIZE - 1)
            {
                ch = GetChunk(ChunkLocFor(pos + new Location(1, 0, 0)));
                if (ch != null && !ch.LOADING)
                {
                    ch.AddToWorld();
                }
            }
            if (y == Chunk.CHUNK_SIZE - 1)
            {
                ch = GetChunk(ChunkLocFor(pos + new Location(0, 1, 0)));
                if (ch != null && !ch.LOADING)
                {
                    ch.AddToWorld();
                }
            }
            if (z == Chunk.CHUNK_SIZE - 1)
            {
                ch = GetChunk(ChunkLocFor(pos + new Location(0, 0, 1)));
                if (ch != null && !ch.LOADING)
                {
                    ch.AddToWorld();
                }
            }
        }

        public Location[] FellLocs = new Location[] { new Location(0, 0, 1), new Location(1, 0, 0), new Location(0, 1, 0), new Location(-1, 0, 0), new Location(0, -1, 0) };

        public void BreakNaturally(Location pos, bool regentrans = true, int max_subbreaks = 5)
        {
            pos = pos.GetBlockLocation();
            Chunk ch = LoadChunk(ChunkLocFor(pos));
            lock (ch.EditSessionLock)
            {
                int x = (int)Math.Floor(pos.X) - (int)ch.WorldPosition.X * 30;
                int y = (int)Math.Floor(pos.Y) - (int)ch.WorldPosition.Y * 30;
                int z = (int)Math.Floor(pos.Z) - (int)ch.WorldPosition.Z * 30;
                BlockInternal bi = ch.GetBlockAt(x, y, z);
                if (((BlockFlags)bi.BlockLocalData).HasFlag(BlockFlags.PROTECTED))
                {
                    return;
                }
                Material mat = (Material)bi.BlockMaterial;
                ch.BlocksInternal[ch.BlockIndex(x, y, z)].BlockLocalData |= (byte)BlockFlags.PROTECTED;
                if (mat != (ushort)Material.AIR)
                {
                    if (max_subbreaks > 0
                        && !((BlockFlags)bi.BlockLocalData).HasFlag(BlockFlags.EDITED)
                        && (mat == Material.LOG || mat == Material.LEAVES1))
                    {
                        foreach (Location loc in FellLocs)
                        {
                            Material m2 = GetBlockMaterial(pos + loc);
                            if (m2 == Material.LOG || m2 == Material.LEAVES1)
                            {
                                BreakNaturally(pos + loc, regentrans, max_subbreaks - 1);
                            }
                        }
                    }
                    // TODO: Activate any nearby physents!
                    ch.SetBlockAt(x, y, z, new BlockInternal((ushort)Material.AIR, 0, (byte)BlockFlags.EDITED));
                    ch.LastEdited = GlobalTickTime;
                    if (regentrans)
                    {
                        ch.AddToWorld();
                        TrySurroundings(ch, pos, x, y, z);
                        ChunkSendToAll(new BlockEditPacketOut(new Location[] { pos }, new Material[] { Material.AIR }, new byte[] { 0 }), ch.WorldPosition);
                    }
                    BlockItemEntity bie = new BlockItemEntity(this, mat, bi.BlockData, pos);
                    SpawnEntity(bie);
                }
            }
        }

        public Location GetBlockLocation(Location worldPos)
        {
            return new Location(Math.Floor(worldPos.X), Math.Floor(worldPos.Y), Math.Floor(worldPos.Z));
        }

        public Location ChunkLocFor(Location worldPos)
        {
            worldPos.X = Math.Floor(worldPos.X / 30.0);
            worldPos.Y = Math.Floor(worldPos.Y / 30.0);
            worldPos.Z = Math.Floor(worldPos.Z / 30.0);
            return worldPos;
        }

        static Location[] slocs = new Location[] { new Location(1, 0, 0), new Location(-1, 0, 0), new Location(0, 1, 0),
            new Location(0, -1, 0), new Location(0, 0, 1), new Location(0, 0, -1) };

        public List<Chunk> ChunksToDestroy = new List<Chunk>();
        
        public Chunk LoadChunkNoPopulate(Location cpos)
        {
            CheckThreadValidity();
            Chunk chunk;
            if (LoadedChunks.TryGetValue(cpos, out chunk))
            {
                return chunk;
            }
            chunk = new Chunk();
            chunk.LOADING = true;
            chunk.POPULATING = true;
            chunk.ISCUSTOM = true;
            chunk.OwningRegion = this;
            chunk.WorldPosition = cpos;
            LoadedChunks.Add(cpos, chunk);
            if (Program.Files.Exists(chunk.GetFileName()))
            {
                PopulateChunk(chunk, true);
                chunk.POPULATING = false;
                chunk.ISCUSTOM = false;
                AddChunkToWorld(chunk);
            }
            ChunksToDestroy.Add(chunk);
            chunk.LastEdited = GlobalTickTime;
            return chunk;
        }

        public Chunk LoadChunk(Location cpos)
        {
            CheckThreadValidity();
            Chunk chunk;
            if (LoadedChunks.TryGetValue(cpos, out chunk))
            {
                if (chunk.ISCUSTOM)
                {
                    chunk.ISCUSTOM = false;
                    ChunksToDestroy.Remove(chunk);
                    PopulateChunk(chunk, false);
                    AddChunkToWorld(chunk);
                }
                if (chunk.POPULATING)
                {
                    throw new Exception("Non-custom chunk was still loading when grabbed?!");
                }
                return chunk;
            }
            chunk = new Chunk();
            chunk.LOADING = true;
            chunk.POPULATING = true;
            chunk.OwningRegion = this;
            chunk.WorldPosition = cpos;
            LoadedChunks.Add(cpos, chunk);
            PopulateChunk(chunk, true);
            AddChunkToWorld(chunk);
            return chunk;
        }

        public void AddChunkToWorld(Chunk chunk)
        {
            CheckThreadValidity();
            chunk.AddToWorld();
            chunk.LOADING = false;
            foreach (Location loc in slocs)
            {
                Chunk ch = GetChunk(chunk.WorldPosition + loc);
                if (ch != null && !ch.LOADING)
                {
                    ch.AddToWorld();
                }
            }
        }

        /// <summary>
        /// Designed for startup time.
        /// TODO: Non-background and/or actually asyncify...
        /// </summary>
        public void LoadChunk_Background(Location cpos, Action<bool> callback = null)
        {
            TheServer.Schedule.ScheduleSyncTask(() =>
            {
                CheckThreadValidity();
                Chunk ch;
                if (LoadedChunks.TryGetValue(cpos, out ch))
                {
                    if (!ch.ISCUSTOM)
                    {
                        if (callback != null)
                        {
                            callback.Invoke(true);
                        }
                        return;
                    }
                    else
                    {
                        ch.ISCUSTOM = false;
                        ChunksToDestroy.Remove(ch);
                        PopulateChunk(ch, false);
                        ch.AddToWorld();
                        ch.LOADING = false;
                        if (callback != null)
                        {
                            callback.Invoke(false);
                        }
                        return;
                    }
                }
                ch = new Chunk();
                ch.LOADING = true;
                ch.OwningRegion = this;
                ch.WorldPosition = cpos;
                LoadedChunks.Add(cpos, ch);
                // TheServer.Schedule.StartASyncTask(() =>
                // {
                PopulateChunk(ch, true); // TODO: Make asyncable!
                ch.AddToWorld();
                ch.LOADING = false;
                if (callback != null)
                {
                    callback.Invoke(false);
                }
            });
            // });
        }
        
        public Chunk GetChunk(Location cpos)
        {
            Chunk chunk;
            if (LoadedChunks.TryGetValue(cpos, out chunk))
            {
                if (chunk.ISCUSTOM)
                {
                    return null;
                }
                return chunk;
            }
            return null;
        }

        public BlockInternal GetBlockInternal_NoLoad(Location pos)
        {
            Chunk ch = GetChunk(ChunkLocFor(pos));
            if (ch == null)
            {
                return BlockInternal.AIR;
            }
            int x = (int)Math.Floor(pos.X) - (int)ch.WorldPosition.X * 30;
            int y = (int)Math.Floor(pos.Y) - (int)ch.WorldPosition.Y * 30;
            int z = (int)Math.Floor(pos.Z) - (int)ch.WorldPosition.Z * 30;
            return ch.GetBlockAt(x, y, z);
        }

        public BlockPopulator Generator = new SimpleGeneratorCore();
        public BiomeGenerator BiomeGen = new SimpleBiomeGenerator();

        public void PopulateChunk(Chunk chunk, bool allowFile)
        {
            CheckThreadValidity();
            try
            {
                if (allowFile && Program.Files.Exists(chunk.GetFileName()))
                {
                    chunk.LoadFromSaveData(Program.Files.ReadBytes(chunk.GetFileName()));
                    if (!chunk.ISCUSTOM)
                    {
                        chunk.POPULATING = false;
                        return;
                    }
                    chunk.ISCUSTOM = false;
                }
            }
            catch (Exception ex)
            {
                SysConsole.Output(OutputType.ERROR, "Loading a chunk: " + ex.ToString());
                return;
            }
            Generator.Populate(Seed, Seed2, chunk);
            chunk.LastEdited = GlobalTickTime;
            chunk.POPULATING = false;
        }

        /// <summary>
        /// Does not return until fully unloaded.
        /// </summary>
        public void UnloadFully()
        {
            CheckThreadValidity();
            // TODO: Transfer all players to another world.
            IntHolder counter = new IntHolder(); // TODO: is IntHolder needed here?
            IntHolder total = new IntHolder(); // TODO: is IntHolder needed here?
            foreach (Chunk chunk in LoadedChunks.Values)
            {
                total.Value++;
                chunk.UnloadSafely(() => { counter.Value++; });
            }
            while (counter.Value < total.Value)
            {
                Thread.Sleep(16);
            }
            OncePerSecondActions();
        }

        public List<Location> GetBlocksInRadius(Location pos, float rad)
        {
            int min = (int)Math.Floor(-rad);
            int max = (int)Math.Ceiling(rad);
            List<Location> posset = new List<Location>();
            for (int x = min; x < max; x++)
            {
                for (int y = min; y < max; y++)
                {
                    for (int z = min; z < max; z++)
                    {
                        Location post = new Location(pos.X + x, pos.Y + y, pos.Z + z);
                        if ((post - pos).LengthSquared() <= rad * rad)
                        {
                            posset.Add(post);
                        }
                    }
                }
            }
            return posset;
        }

        public List<PlayerEntity> GetPlayersInRadius(Location pos, float rad)
        {
            CheckThreadValidity();
            List<PlayerEntity> pes = new List<PlayerEntity>();
            foreach (PlayerEntity pe in Players)
            {
                if ((pe.GetPosition() - pos).LengthSquared() <= rad * rad)
                {
                    pes.Add(pe);
                }
            }
            return pes;
        }

        public List<Entity> GetEntitiesInRadius(Location pos, float rad)
        {
            CheckThreadValidity();
            List<Entity> es = new List<Entity>();
            // TODO: Efficiency
            foreach (Entity e in Entities)
            {
                if ((e.GetPosition() - pos).LengthSquared() <= rad * rad)
                {
                    es.Add(e);
                }
            }
            return es;
        }

        public void Explode(Location pos, float rad = 5f, bool effect = true, bool breakblock = true, bool applyforce = true, bool doDamage = true)
        {
            CheckThreadValidity();
            if (breakblock)
            {
                List<Location> hits = GetBlocksInRadius(pos, rad / 3);
                foreach (Location loc in hits)
                {
                    BreakNaturally(loc, true); // TODO: Regen + transmit in Explode() not Break().
                }
            }
            if (effect)
            {
                ParticleEffectPacketOut pepo = new ParticleEffectPacketOut(ParticleEffectType.EXPLOSION, rad + 30, pos);
                foreach (PlayerEntity pe in GetPlayersInRadius(pos, rad + 30)) // TODO: Better particle view dist
                {
                    pe.Network.SendPacket(pepo);
                }
                // TODO: Sound effect?
            }
            if (applyforce)
            {
                foreach (Entity e in GetEntitiesInRadius(pos, rad * 5)) // TODO: Physent-specific search method?
                {
                    // TODO: Generic entity 'ApplyForce' method
                    if (e is PhysicsEntity)
                    {
                        Location offs = e.GetPosition() - pos;
                        float dpower = (float)((rad * 5) - offs.Length()); // TODO: Efficiency?
                        Vector3 force = new Vector3(rad, rad, rad * 3) * dpower;
                        ((PhysicsEntity)e).Body.ApplyLinearImpulse(ref force);
                    }
                }
            }
            if (doDamage)
            {
                // TODO: DO DAMAGE!
            }
        }

        public PhysicsEntity ItemToEntity(ItemStack item)
        {
            if (item.Info is BlockItem)
            {
                int mat = item.Datum & (255 + 255 * 256);
                int dat = item.Datum & (255 * 256 * 256);
                return new BlockItemEntity(this, (Material)mat, (byte)dat, Location.Zero);
            }
            /*
            if (item.Info is GlowstickItem)
            {
                return null;
            }
            */
            return new ItemEntity(item, this);
        }
    }
}
