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
using Voxalia.Shared;
using FreneticScript;

namespace Voxalia.ClientGame.NetworkSystem.PacketsIn
{
    public class DefaultSoundPacketIn: AbstractPacketIn
    {
        public override bool ParseBytesAndExecute(byte[] data)
        {
            if (data.Length != 24 + 1 + 1)
            {
                return false;
            }
            Location loc = Location.FromDoubleBytes(data, 0);
            DefaultSound soundtype = (DefaultSound)data[24];
            byte subdat = data[24 + 1];
            switch (soundtype)
            {
                case DefaultSound.BREAK:
                    PlayDefaultBlockSound(loc, (MaterialSound)subdat, 0.75f, 1f);
                    break;
                case DefaultSound.PLACE:
                    PlayDefaultBlockSound(loc, (MaterialSound)subdat, 1.5f, 0.8f);
                    break;
                case DefaultSound.STEP:
                    PlayDefaultBlockSound(loc, (MaterialSound)subdat, 1, 0.5f);
                    break;
                default:
                    return false;
            }
            return true;
        }

        // TODO: Move to a manager class rather than this packet class!
        public void PlayDefaultBlockSound(Location pos, MaterialSound sound, float pitchmod, float volumemod)
        {
            float pitch = (float)(Utilities.UtilRandom.NextDouble() * 0.1 + 1.0 - 0.05);
            float volume = (float)Math.Min((Utilities.UtilRandom.NextDouble() * 0.1 + 1.0 - 0.1) * volumemod, 1.0);
            // TODO: registry of some form?
            switch (sound)
            {
                case MaterialSound.GRASS:
                case MaterialSound.SAND:
                case MaterialSound.LEAVES:
                case MaterialSound.WOOD:
                case MaterialSound.METAL:
                case MaterialSound.DIRT:
                case MaterialSound.STONE:
                case MaterialSound.SNOW:
                case MaterialSound.GLASS:
                case MaterialSound.CLAY:
                case MaterialSound.LIQUID:
                case MaterialSound.SLIME:
                    // TODO: Don't manually search the sound list every time!
                    TheClient.Sounds.Play(TheClient.Sounds.GetSound("sfx/steps/humanoid/" + sound.ToString().ToLowerFast() + (Utilities.UtilRandom.Next(4) + 1)), false, pos, pitch, volume);
                    break;
                default:
                    return;
            }
        }
    }
}
