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
using FreneticScript.TagHandlers;
using FreneticScript.TagHandlers.Objects;
using Voxalia.ServerGame.TagSystem.TagObjects;
using Voxalia.ServerGame.ServerMainSystem;

namespace Voxalia.ServerGame.TagSystem.TagBases
{
    class ColorTagBase : TemplateTagBase
    {
        // <--[tagbase]
        // @Base color[<ColorTag>]
        // @Group Mathematics
        // @ReturnType ItemTag
        // @Returns the color described by the given input.
        // -->
        public ColorTagBase()
        {
            Name = "color";
        }

        public override TemplateObject Handle(TagData data)
        {
            string cname = data.GetModifier(0);
            ColorTag ctag = ColorTag.For(cname);
            if (ctag == null)
            {
                data.Error("Invalid color '" + TagParser.Escape(cname) + "'!");
                return new NullTag();
            }
            return ctag.Handle(data.Shrink());
        }
    }
}
