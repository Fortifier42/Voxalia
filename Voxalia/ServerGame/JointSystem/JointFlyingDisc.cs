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
using System.Threading.Tasks;
using Voxalia.ServerGame.EntitySystem;
using BEPUphysics.Constraints;
using Voxalia.Shared.Collision;

namespace Voxalia.ServerGame.JointSystem
{
    public class JointFlyingDisc : BaseJoint
    {
        public JointFlyingDisc(Entity e)
        {
            One = e;
            Two = e;
        }

        public bool IsAPlane = false;

        public override SolverUpdateable GetBaseJoint()
        {
            return new FlyingDiscConstraint(Ent1.Body) { IsAPlane = IsAPlane };
        }
    }
}
