﻿using Voxalia.ClientGame.EntitySystem;
using Voxalia.Shared;
using BEPUphysics.Constraints.TwoEntity;
using BEPUphysics.Constraints.TwoEntity.Joints;
using BEPUphysics.Constraints;

namespace Voxalia.ClientGame.JointSystem
{
    public class JointBallSocket : BaseJoint
    {
        public JointBallSocket(PhysicsEntity e1, PhysicsEntity e2, Location pos)
        {
            Ent1 = e1;
            Ent2 = e2;
            Position = pos;
        }

        public override SolverUpdateable GetBaseJoint()
        {
            return new BallSocketJoint(Ent1.Body, Ent2.Body, Position.ToBVector());
        }

        public Location Position;
    }
}
