//
// This file is part of the game Voxalia, created by FreneticXYZ.
// This code is Copyright (C) 2016 FreneticXYZ under the terms of the MIT license.
// See README.md or LICENSE.txt for contents of the MIT license.
// If these are not available, see https://opensource.org/licenses/MIT
//

using System.Collections.Generic;
using Voxalia.ServerGame.ItemSystem.CommonItems;
using Voxalia.Shared;
using FreneticScript;

namespace Voxalia.ServerGame.ItemSystem
{
    public class ItemInfoRegistry
    {
        public Dictionary<string, BaseItemInfo> Infos;

        public BaseItemInfo Generic;

        public ItemInfoRegistry()
        {
            Infos = new Dictionary<string, BaseItemInfo>();
            // TODO: Organize - alphabetical? Maybe folders too.
            Register(new ExplodobowItem());
            Register(new BowItem());
            Register(new FlashLightItem());
            Register(new FlashantilightItem());
            Register(new HookItem());
            Register(new OpenHandItem());
            Register(new PistolGunItem());
            Register(new RifleGunItem());
            Register(new ShotgunGunItem());
            Register(new BulletItem());
            Register(new FistItem());
            Register(new BlockItem());
            Register(new SunAnglerItem());
            Register(new BreadcrumbItem());
            Register(new GlowstickItem());
            Register(new StructureCreateItem());
            Register(new StructurePasteItem());
            Register(new SmokemachineItem());
            Register(new SmokegrenadeItem());
            Register(new ExplosivegrenadeItem());
            Register(new PaintbrushItem());
            Register(new PaintbombItem());
            Register(new PickaxeItem());
            Register(new JetpackItem());
            Register(new FuelItem());
            Register(new ParachuteItem());
            Register(new SledgehammerItem());
            Register(new ManipulatorItem());
            Register(new StructureSelectorItem());
            Register(new CustomBlockItem());
            Register(new HatCannonItem());
            Register(new SuctionRayItem());
            Register(new PushRayItem());
            Register(new WingsItem());
            Register(Generic = new GenericItem());
        }

        public void Register(BaseItemInfo info)
        {
            Infos.Add(info.Name.ToLowerFast(), info);
        }

        public BaseItemInfo GetInfoFor(string name)
        {
            BaseItemInfo bii;
            if (Infos.TryGetValue(name.ToLowerFast(), out bii))
            {
                return bii;
            }
            SysConsole.Output(OutputType.WARNING, "Using generic item for " + name);
            return Generic;
        }
    }
}
