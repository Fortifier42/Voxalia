﻿using Voxalia.ServerGame.EntitySystem;
using Voxalia.Shared;
using Voxalia.Shared.Collision;

namespace Voxalia.ServerGame.NetworkSystem.PacketsOut
{
    class SpawnPhysicsEntityPacketOut: AbstractPacketOut
    {
        public SpawnPhysicsEntityPacketOut(PhysicsEntity e)
        {
            ID = 2;
            BlockGroupEntity bge = null;
            Location hs = Location.Zero;
            if (e is BlockGroupEntity)
            {
                bge = (BlockGroupEntity)e;
                hs = new Location(bge.XWidth, bge.YWidth, bge.ZWidth);
            }
            // TODO: LOL PLS CLEAN
            Data = new byte[4 + 12 + 12 + 16 + 12 + 8 + 4 + 12 + 1 + 
                (e is GlowstickEntity ? 4: (e is BlockGroupEntity ? bge.Blocks.Length * 4 + 1 :(e is BlockItemEntity ? 4: (e is ModelEntity ? 4 + 1: (e is GrenadeEntity ? 0: 0))))) + 4 + 1];
            Utilities.FloatToBytes(e.GetMass()).CopyTo(Data, 0);
            e.GetPosition().ToBytes().CopyTo(Data, 4);
            e.GetVelocity().ToBytes().CopyTo(Data, 4 + 12);
            Utilities.QuaternionToBytes(e.GetOrientation()).CopyTo(Data, 4 + 12 + 12);
            e.GetAngularVelocity().ToBytes().CopyTo(Data, 4 + 12 + 12 + 16);
            Utilities.LongToBytes(e.EID).CopyTo(Data, 4 + 12 + 12 + 16 + 12);
            Utilities.FloatToBytes(e.GetFriction()).CopyTo(Data, 4 + 12 + 12 + 16 + 12 + 8);
            // TODO: Unique gravity, restitution, etc. properties?
            // TODO: handle different e-types cleanly
            if (e is CharacterEntity)
            {
                // TODO: Use this slot?
                new Location(0, 0, 0).ToBytes().CopyTo(Data, 4 + 12 + 12 + 16 + 12 + 8 + 4);
            }
            else if (e is ModelEntity)
            {
                ((ModelEntity)e).scale.ToBytes().CopyTo(Data, 4 + 12 + 12 + 16 + 12 + 8 + 4);
            }
            else if (e is BlockGroupEntity)
            {
                hs.ToBytes().CopyTo(Data, 4 + 12 + 12 + 16 + 12 + 8 + 4);
            }
            else
            {
                // TODO: Warning message?
                new Location(1, 1, 1).ToBytes().CopyTo(Data, 4 + 12 + 12 + 16 + 12 + 8 + 4);
            }
            // TODO: LOL PLS CLEAN
            Data[4 + 12 + 12 + 16 + 12 + 8 + 4 + 12] = (byte)(e is GlowstickEntity ? 5:
                (e is GrenadeEntity ? 6 :
                (e is SlimeEntity ? 7 :
                (e is BlockGroupEntity ? 4:
                (e is CharacterEntity ? 1 :
                (e is BlockItemEntity ? 4:
                (e is ModelEntity ? 2: 0)))))));
            int start = 4 + 12 + 12 + 16 + 12 + 8 + 4 + 12 + 1;
            if (e is BlockItemEntity)
            {
                BlockItemEntity bie = (BlockItemEntity)e;
                Utilities.UshortToBytes(bie.Original.BlockMaterial).CopyTo(Data, start);
                Data[start + 2] = bie.Original.BlockData;
                Data[start + 3] = bie.Original.BlockPaint;
            }
            else if (e is ModelEntity)
            {
                ModelEntity me = (ModelEntity)e;
                NetStringManager strings = me.TheServer.Networking.Strings;
                Utilities.IntToBytes(strings.IndexForString(me.model)).CopyTo(Data, start);
                Data[start + 4] = (byte)me.mode;
            }
            else if (e is BlockGroupEntity)
            {
                for (int i = 0; i < bge.Blocks.Length; i++)
                {
                    Utilities.UshortToBytes(bge.Blocks[i].BlockMaterial).CopyTo(Data, start + i * 2);
                    Data[start + bge.Blocks.Length * 2 + i] = bge.Blocks[i].BlockData;
                    Data[start + bge.Blocks.Length * 3 + i] = bge.Blocks[i].BlockPaint;
                }
                Data[start + bge.Blocks.Length * 4] = (byte)bge.TraceMode;
            }
            else if (e is GlowstickEntity)
            {
                Utilities.IntToBytes(((GlowstickEntity)e).Color.ToArgb()).CopyTo(Data, start);
            }
            Utilities.FloatToBytes(e.GetBounciness()).CopyTo(Data, Data.Length - (4 + 1));
            Data[Data.Length - 1] = (byte)(e.Visible ? 1 : 0);
            if (e.CGroup == CollisionUtil.Solid)
            {
                Data[Data.Length - 1] |= 2;
            }
            else if (e.CGroup == CollisionUtil.NonSolid)
            {
                Data[Data.Length - 1] |= 4;
            }
            else if (e.CGroup == CollisionUtil.Item)
            {
                Data[Data.Length - 1] |= 2 | 4;
            }
            else if (e.CGroup == CollisionUtil.Player)
            {
                Data[Data.Length - 1] |= 8;
            }
            else if (e.CGroup == CollisionUtil.Water)
            {
                Data[Data.Length - 1] |= 2 | 8;
            }
            else if (e.CGroup == CollisionUtil.WorldSolid)
            {
                Data[Data.Length - 1] |= 2 | 4 | 8;
            }
        }
    }
}
