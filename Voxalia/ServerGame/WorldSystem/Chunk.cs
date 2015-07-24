﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Voxalia.Shared;
using BEPUphysics;
using BEPUphysics.CollisionShapes;
using BEPUutilities;
using BEPUphysics.Entities;
using BEPUphysics.BroadPhaseEntries;

namespace Voxalia.ServerGame.WorldSystem
{
    public class Chunk
    {
        public const int CHUNK_SIZE = 30;

        public bool LOADING = true;

        public World OwningWorld = null;

        public Location WorldPosition;

        public Chunk()
        {
            BlocksInternal = new BlockInternal[CHUNK_SIZE * CHUNK_SIZE * CHUNK_SIZE];
        }

        public BlockInternal[] BlocksInternal;

        public int LODBlockIndex(int x, int y, int z, int lod)
        {
            int cs = CHUNK_SIZE / lod;
            return z * CHUNK_SIZE * CHUNK_SIZE * lod + y * CHUNK_SIZE * lod + x * lod;
        }
        
        public int BlockIndex(int x, int y, int z)
        {
            return z * CHUNK_SIZE * CHUNK_SIZE + y * CHUNK_SIZE + x;
        }

        public void SetBlockAt(int x, int y, int z, BlockInternal mat)
        {
            BlocksInternal[BlockIndex(x, y, z)] = mat;
        }

        public BlockInternal GetBlockAt(int x, int y, int z)
        {
            return BlocksInternal[BlockIndex(x, y, z)];
        }

        public StaticMesh CalculateChunkShape()
        {
            List<Vector3> Vertices = new List<Vector3>(CHUNK_SIZE * CHUNK_SIZE * CHUNK_SIZE * 6); // TODO: Make this an array?
            Vector3 ppos = WorldPosition.ToBVector() * 30;
            for (int x = 0; x < CHUNK_SIZE; x++)
            {
                for (int y = 0; y < CHUNK_SIZE; y++)
                {
                    for (int z = 0; z < CHUNK_SIZE; z++)
                    {
                        BlockInternal c = GetBlockAt(x, y, z);
                        if (((Material)c.BlockMaterial).IsSolid())
                        {
                            BlockInternal zp = z + 1 < CHUNK_SIZE ? GetBlockAt(x, y, z + 1) : OwningWorld.GetBlockInternal_NoLoad(Location.FromBVector(ppos) + new Location(x, y, 30));
                            BlockInternal zm = z > 0 ? GetBlockAt(x, y, z - 1) : OwningWorld.GetBlockInternal_NoLoad(Location.FromBVector(ppos) + new Location(x, y, -1));
                            BlockInternal yp = y + 1 < CHUNK_SIZE ? GetBlockAt(x, y + 1, z) : OwningWorld.GetBlockInternal_NoLoad(Location.FromBVector(ppos) + new Location(x, 30, z));
                            BlockInternal ym = y > 0 ? GetBlockAt(x, y - 1, z) : OwningWorld.GetBlockInternal_NoLoad(Location.FromBVector(ppos) + new Location(x, -1, z));
                            BlockInternal xp = x + 1 < CHUNK_SIZE ? GetBlockAt(x + 1, y, z) : OwningWorld.GetBlockInternal_NoLoad(Location.FromBVector(ppos) + new Location(30, y, z));
                            BlockInternal xm = x > 0 ? GetBlockAt(x - 1, y, z) : OwningWorld.GetBlockInternal_NoLoad(Location.FromBVector(ppos) + new Location(-1, y, z));
                            bool zps = ((Material)zp.BlockMaterial).IsSolid() && BlockShapeRegistry.BSD[zp.BlockData].OccupiesTOP();
                            bool zms = ((Material)zm.BlockMaterial).IsSolid() && BlockShapeRegistry.BSD[zm.BlockData].OccupiesBOTTOM();
                            bool xps = ((Material)xp.BlockMaterial).IsSolid() && BlockShapeRegistry.BSD[xp.BlockData].OccupiesXP();
                            bool xms = ((Material)xm.BlockMaterial).IsSolid() && BlockShapeRegistry.BSD[xm.BlockData].OccupiesXM();
                            bool yps = ((Material)yp.BlockMaterial).IsSolid() && BlockShapeRegistry.BSD[yp.BlockData].OccupiesYP();
                            bool yms = ((Material)ym.BlockMaterial).IsSolid() && BlockShapeRegistry.BSD[ym.BlockData].OccupiesYM();
                            Vector3 pos = new Vector3(ppos.X + x, ppos.Y + y, ppos.Z + z);
                            List<Vector3> vecsi = BlockShapeRegistry.BSD[c.BlockData].GetVertices(pos, xps, xms, yps, yms, zps, zms);
                            Vertices.AddRange(vecsi);
                        }
                    }
                }
            }
            if (Vertices.Count == 0)
            {
                return null;
            }
            int[] inds = new int[Vertices.Count];
            for (int i = 0; i < Vertices.Count; i++)
            {
                inds[i] = i;
            }
            Vector3[] vecs = Vertices.ToArray();
            StaticMesh sm = new StaticMesh(vecs, inds);
            return sm;
        }

        StaticMesh worldObject = null;

        public ASyncScheduleItem adding = null;
        
        public void AddToWorld(Action callback = null)
        {
            if (adding != null)
            {
                ASyncScheduleItem item = OwningWorld.TheServer.Schedule.AddASyncTask(() => AddInternal(callback));
                adding = adding.ReplaceOrFollowWith(item);
            }
            else
            {
                adding = OwningWorld.TheServer.Schedule.StartASyncTask(() => AddInternal(callback));
            }
        }

        void AddInternal(Action callback)
        {
            StaticMesh tworldObject = CalculateChunkShape();
            OwningWorld.TheServer.Schedule.ScheduleSyncTask(() =>
            {
                if (worldObject != null)
                {
                    OwningWorld.PhysicsWorld.Remove(worldObject);
                }
                worldObject = tworldObject;
                if (worldObject != null)
                {
                    OwningWorld.PhysicsWorld.Add(worldObject);
                }
                if (callback != null)
                {
                    callback.Invoke();
                }
            });
        }
    }
}
