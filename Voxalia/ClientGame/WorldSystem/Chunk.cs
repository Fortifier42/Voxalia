﻿using System;
using System.Collections.Generic;
using Voxalia.Shared;
using BEPUutilities;
using BEPUphysics.CollisionShapes;
using BEPUphysics.BroadPhaseEntries;
using Voxalia.Shared.Collision;

namespace Voxalia.ClientGame.WorldSystem
{
    public partial class Chunk
    {
        public const int CHUNK_SIZE = 30;

        public Region OwningRegion = null;

        public Location WorldPosition;

        public int CSize = CHUNK_SIZE;

        public int PosMultiplier;

        public Chunk(int posMult)
        {
            PosMultiplier = posMult;
            CSize = CHUNK_SIZE / posMult;
            BlocksInternal = new BlockInternal[CSize * CSize * CSize];
        }

        public BlockInternal[] BlocksInternal;
        
        public int BlockIndex(int x, int y, int z)
        {
            return z * CSize * CSize + y * CSize + x;
        }

        public void SetBlockAt(int x, int y, int z, BlockInternal mat)
        {
            BlocksInternal[BlockIndex(x, y, z)] = mat;
        }

        public BlockInternal GetBlockAt(int x, int y, int z)
        {
            return BlocksInternal[BlockIndex(x, y, z)];
        }

        static Location[] slocs = new Location[] { new Location(1, 0, 0), new Location(-1, 0, 0), new Location(0, 1, 0),
            new Location(0, -1, 0), new Location(0, 0, 1), new Location(0, 0, -1) };

        public void UpdateSurroundingsFully()
        {
            foreach (Location loc in slocs)
            {
                Chunk ch = OwningRegion.GetChunk(WorldPosition + loc);
                if (ch != null)
                {
                    OwningRegion.UpdateChunk(ch);
                }
            }
        }

        public void CalculateLighting()
        {
            if (OwningRegion.TheClient.CVars.r_fallbacklighting.ValueB)
            {
                for (int x = 0; x < CSize; x++)
                {
                    for (int y = 0; y < CSize; y++)
                    {
                        float light = 1f;
                        for (int z = CSize - 1; z >= 0; z--)
                        {
                            BlocksInternal[BlockIndex(x, y, z)].BlockLocalData = (byte)(light * 255);
                            Material mat = (Material)GetBlockAt(x, y, z).BlockMaterial;
                            if (mat.IsOpaque())
                            {
                                light = 0;
                            }
                            if (mat.RendersAtAll())
                            {
                                light /= 1.8f;
                            }
                        }
                    }
                }
            }
        }
        
        public FullChunkObject FCO = null;
        
        public ASyncScheduleItem adding = null;

        public void AddToWorld()
        {
            if (FCO != null)
            {
                return;
            }
            FCO = new FullChunkObject(WorldPosition.ToBVector() * 30, BlocksInternal);
            FCO.CollisionRules.Group = CollisionUtil.Solid;
            OwningRegion.AddChunk(FCO);
        }

        public void Destroy()
        {
            if (FCO != null)
            {
                OwningRegion.RemoveChunkQuiet(FCO);
            }
            if (_VBO != null)
            {
                _VBO.Destroy();
                _VBO = null;
            }
        }

        public bool LOADING = false;
        public bool PROCESSED = false;
        public bool PRED = false;
        public bool DENIED = false;
    }
}
