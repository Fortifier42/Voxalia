//
// This file is part of the game Voxalia, created by FreneticXYZ.
// This code is Copyright (C) 2016 FreneticXYZ under the terms of the MIT license.
// See README.md or LICENSE.txt for contents of the MIT license.
// If these are not available, see https://opensource.org/licenses/MIT
//

using Voxalia.Shared;
using Voxalia.ClientGame.EntitySystem;
using Voxalia.ClientGame.GraphicsSystems.LightingSystem;

namespace Voxalia.ClientGame.NetworkSystem.PacketsIn
{
    public class FlashLightPacketIn: AbstractPacketIn
    {
        public void Destroy(Entity ent)
        {
            SpotLight sl = null;
            if (ent is CharacterEntity)
            {
                sl = ((CharacterEntity)ent).Flashlight;
                ((CharacterEntity)ent).Flashlight = null;
            }
            if (sl != null)
            {
                sl.Destroy();
                TheClient.MainWorldView.Lights.Remove(sl);
            }
        }

        public override bool ParseBytesAndExecute(byte[] data)
        {
            if (data.Length != 8 + 1 + 4 + 24)
            {
                return false;
            }
            long EID = Utilities.BytesToLong(Utilities.BytesPartial(data, 0, 8));
            bool enabled = (data[8] & 1) == 1;
            float distance = Utilities.BytesToFloat(Utilities.BytesPartial(data, 8 + 1, 4));
            Location color = Location.FromDoubleBytes(data, 8 + 1 + 4);
            Entity ent = TheClient.TheRegion.GetEntity(EID);
            if (ent == null || !(ent is CharacterEntity))
            {
                return false;
            }
            Destroy(ent);
            if (enabled)
            {
                SpotLight sl = new SpotLight(ent.GetPosition(), distance, color, Location.UnitX, 45);
                sl.Direction = ((CharacterEntity)ent).ForwardVector();
                sl.Reposition(ent.GetPosition());
                ((CharacterEntity)ent).Flashlight = sl;
                TheClient.MainWorldView.Lights.Add(sl);
            }
            return true;
        }
    }
}
