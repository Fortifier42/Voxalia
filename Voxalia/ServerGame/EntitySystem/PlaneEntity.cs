﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Voxalia.Shared;
using Voxalia.Shared.Collision;
using Voxalia.ServerGame.WorldSystem;
using BEPUutilities;
using BEPUphysics.Constraints;
using BEPUphysics.Constraints.SingleEntity;
using Voxalia.ServerGame.JointSystem;

namespace Voxalia.ServerGame.EntitySystem
{
    public class PlaneEntity : VehicleEntity
    {
        public PlaneEntity(string pln, Region tregion)
            : base(pln, tregion)
        {
            SetMass(1000);
        }

        public override EntityType GetEntityType()
        {
            return EntityType.HELICOPTER;
        }

        public override byte[] GetSaveBytes()
        {
            // TODO: Save properly!
            return null;
        }

        public bool ILeft = false;
        public bool IRight = false;

        public bool Fast = false;
        public bool Slow = false;

        public float ForwBack = 0;
        public float RightLeft = 0;

        public override void SpawnBody()
        {
            base.SpawnBody();
            Motion = new PlaneMotionConstraint(this);
            TheRegion.PhysicsWorld.Add(Motion);
            Wings = new JointFlyingDisc(this);
            TheRegion.AddJoint(Wings);
            HandleWheels();
        }

        public float FastStrength
        {
            get
            {
                return GetMass() * 25f;
            }
        }

        public float RegularStrength
        {
            get
            {
                return GetMass() * 7f;
            }
        }

        public float SlowStrength
        {
            get
            {
                return GetMass() * -5f;
            }
        }

        public PlaneMotionConstraint Motion;
        
        // TODO: Plane-specific code?
        public JointFlyingDisc Wings;

        public override void Tick()
        {
            // TODO: Raise/lower landing gear if player hits stance button!
            base.Tick();
        }

        public class PlaneMotionConstraint : SingleEntityConstraint // TODO: network!
        {
            PlaneEntity Plane;
            
            public PlaneMotionConstraint(PlaneEntity pln)
            {
                Plane = pln;
                Entity = pln.Body;
            }

            public override void ExclusiveUpdate()
            {
                if (Plane.DriverSeat.Sitter == null)
                {
                    return; // Don't fly when there's nobody driving this!
                }
                // TODO: Special case for motion on land: only push forward if W key is pressed? Or maybe apply that rule in general?
                // Collect the plane's relative vectors
                Vector3 forward = Quaternion.Transform(Vector3.UnitY, Entity.Orientation);
                Vector3 side = Quaternion.Transform(Vector3.UnitX, Entity.Orientation);
                Vector3 up = Quaternion.Transform(Vector3.UnitZ, Entity.Orientation);
                // Engines!
                if (Plane.Fast)
                {
                    Vector3 force = forward * Plane.FastStrength * Delta;
                    entity.ApplyLinearImpulse(ref force);
                }
                else if (Plane.Slow)
                {
                    Vector3 force = forward * Plane.SlowStrength * Delta;
                    entity.ApplyLinearImpulse(ref force);
                }
                else // FlyNormal
                {
                    Vector3 force = forward * Plane.RegularStrength * Delta;
                    entity.ApplyLinearImpulse(ref force);
                }
                entity.ApplyImpulse(forward * 5 + entity.Position, up * Plane.ForwBack * entity.Mass * 2.5f * Delta);
                entity.ApplyImpulse(side * 5 + entity.Position, up * Plane.RightLeft * entity.Mass * 3f * Delta);
                entity.ApplyImpulse(forward * 5 + entity.Position, side * ((Plane.IRight ? 1 : 0) + (Plane.ILeft ? -1 : 0)) * entity.Mass * 3f * Delta);
                // Apply air drag
                Entity.ModifyLinearDamping(0.5f); // TODO: arbitrary constant
                Entity.ModifyAngularDamping(0.8f); // TODO: arbitrary constant
                Entity.ModifyAngularDamping(0.8f); // TODO: arbitrary constant
                // Ensure we're active if flying!
                Entity.ActivityInformation.Activate();
            }

            public override float SolveIteration()
            {
                return 0; // Do nothing
            }

            float Delta;

            public override void Update(float dt)
            {
                Delta = dt;
            }
        }

        public override void HandleInput(CharacterEntity character)
        {
            ILeft = character.ItemLeft;
            IRight = character.ItemRight;
            ForwBack = character.YMove;
            RightLeft = character.XMove;
            Fast = character.Sprint;
            Slow = character.Walk;
        }
    }
}
