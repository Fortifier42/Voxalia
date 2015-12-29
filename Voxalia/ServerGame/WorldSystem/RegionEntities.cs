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
using BEPUphysics.BroadPhaseEntries.MobileCollidables;
using BEPUphysics.CollisionShapes.ConvexShapes;
using Voxalia.Shared.Collision;
using Voxalia.ServerGame.ItemSystem;
using Voxalia.ServerGame.ItemSystem.CommonItems;

namespace Voxalia.ServerGame.WorldSystem
{
    public partial class Region
    {
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

        public void AddJoint(InternalBaseJoint joint)
        {
            Joints.Add(joint);
            joint.One.Joints.Add(joint);
            joint.Two.Joints.Add(joint);
            joint.JID = jID++;
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

        public void SpawnEntity(Entity e, long eid = -1)
        {
            if (e.IsSpawned)
            {
                return;
            }
            Entities.Add(e);
            e.IsSpawned = true;
            if (eid == -1)
            {
                e.EID = TheServer.AdvanceCID();
            }
            else
            {
                e.EID = eid;
            }
            if (e.Ticks)
            {
                Tickers.Add(e);
            }
            e.TheRegion = this;
            if (e is PhysicsEntity && !(e is PlayerEntity))
            {
                ((PhysicsEntity)e).SpawnBody();
            }
            else if (e is PrimitiveEntity)
            {
                ((PrimitiveEntity)e).Spawn();
            }
            if (e is SpawnPointEntity)
            {
                SpawnPoints.Add((SpawnPointEntity)e);
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
                ((PlayerEntity)e).Network.SendPacket(new YourEIDPacketOut(e.EID));
                ((PlayerEntity)e).Network.SendPacket(new CVarSetPacketOut(TheServer.CVars.g_timescale, TheServer));
                ((PlayerEntity)e).SetAnimation("human/stand/idle01", 0);
                ((PlayerEntity)e).SetAnimation("human/stand/idle01", 1);
                ((PlayerEntity)e).SetAnimation("human/stand/idle01", 2);
            }
        }

        public void DespawnEntity(Entity e)
        {
            if (!e.IsSpawned)
            {
                return;
            }
            Entities.Remove(e);
            e.IsSpawned = false;
            if (e.Ticks)
            {
                Tickers.Remove(e);
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
                Location lpos = Location.NaN;
                if (e is PhysicsEntity)
                {
                    lpos = ((PhysicsEntity)e).lPos;
                }
                else if (e is PrimitiveEntity)
                {
                    lpos = ((PrimitiveEntity)e).lPos;
                }
                DespawnEntityPacketOut desppack = new DespawnEntityPacketOut(e.EID);
                foreach (PlayerEntity player in Players)
                {
                    if (player.ShouldSeePositionPreviously(lpos))
                    {
                        player.Network.SendPacket(desppack);
                    }
                }
            }
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

        public PhysicsEntity ItemToEntity(ItemStack item)
        {
            if (item.Info is BlockItem)
            {
                int mat = item.Datum & (255 + 255 * 256);
                int dat = item.Datum & (255 * 256 * 256);
                return new BlockItemEntity(this, (Material)mat, (byte)dat, Location.Zero);
            }
            if (item.Info is GlowstickItem)
            {
                return new GlowstickEntity(item.DrawColor, this);
            }
            if (item.Info is SmokegrenadeItem)
            {
                return new SmokegrenadeEntity(item.DrawColor, this, item.GetAttributeI("big_smoke", 0) == 0 ? ParticleEffectNetType.SMOKE : ParticleEffectNetType.BIG_SMOKE);
            }
            return new ItemEntity(item, this);
        }

        public Dictionary<EntityType, EntityConstructor> EntityConstructors = new Dictionary<EntityType, EntityConstructor>();

        public EntityConstructor ConstructorFor(EntityType etype)
        {
            EntityConstructor ec;
            if (EntityConstructors.TryGetValue(etype, out ec))
            {
                return ec;
            }
            return null;
        }

        public bool IgnoreEntities(BroadPhaseEntry entry)
        {
            return !(entry is EntityCollidable);
        }

        public void SpawnSmallPlant(string plant, Location opos)
        {
            // TODO: Efficiency!
            ModelEntity me = new ModelEntity("plants/small/" + plant, this);
            Location pos = opos + new Location(0, 0, 1);
            RayCastResult rcr;
            bool h = SpecialCaseRayTrace(pos, -Location.UnitZ, 3, MaterialSolidity.FULLSOLID, IgnoreEntities, out rcr);
            me.SetPosition(h ? new Location(rcr.HitData.Location) : pos);
            Vector3 plantalign = new Vector3(0, 0, 1);
            Vector3 norm = h ? rcr.HitData.Normal : new Vector3(0, 0, 1);
            Quaternion orient = Quaternion.Identity;
            Quaternion.GetQuaternionBetweenNormalizedVectors(ref plantalign, ref norm, out orient);
            me.SetOrientation(orient);
            me.SetPosition(h ? new Location(rcr.HitData.Location) : pos);
            me.CGroup = CollisionUtil.NonSolid;
            SpawnEntity(me);
            me.SetPosition(me.GetPosition() - new Location(Quaternion.Transform(me.offset.ToBVector(), orient)));
            me.ForceNetwork();
        }

        public void SpawnTree(string tree, Location opos)
        {
            // TODO: Efficiency!
            ModelEntity me = new ModelEntity("plants/trees/" + tree, this);
            Location pos = opos + new Location(0, 0, 1);
            /*RayCastResult rcr;
            bool h = SpecialCaseRayTrace(pos, -Location.UnitZ, 50, MaterialSolidity.FULLSOLID, IgnoreEntities, out rcr);
            me.SetPosition(h ? new Location(rcr.HitData.Location) : pos);*/
            Vector3 treealign = new Vector3(0, 1, 0);
            Vector3 norm = /*h ? rcr.HitData.Normal : */new Vector3(0, 0, 1);
            Quaternion orient;
            Quaternion.GetQuaternionBetweenNormalizedVectors(ref treealign, ref norm, out orient);
            me.SetOrientation(orient);
            me.SetPosition(pos);
            SpawnEntity(me);
            me.SetPosition(pos - new Location(norm) - new Location(Quaternion.Transform(me.offset.ToBVector(), orient)));
            me.ForceNetwork();
        }
    }
}