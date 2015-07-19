﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Voxalia.ClientGame.EntitySystem;
using Voxalia.Shared;
using BEPUphysics.Constraints.TwoEntity;
using BEPUphysics.Constraints.TwoEntity.Joints;

namespace Voxalia.ClientGame.JointSystem
{
    class JointTwist : BaseJoint
    {
        public JointTwist(PhysicsEntity e1, PhysicsEntity e2, Location a1, Location a2)
        {
            Ent1 = e1;
            Ent2 = e2;
            AxisOne = a1;
            AxisTwo = a2;
        }

        public override TwoEntityConstraint GetBaseJoint()
        {
            return new TwistJoint(Ent1.Body, Ent2.Body, AxisOne.ToBVector(), AxisTwo.ToBVector());
        }

        public Location AxisOne;
        public Location AxisTwo;
    }
}