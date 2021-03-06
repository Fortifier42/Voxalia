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
using FreneticScript.CommandSystem;
using FreneticScript.TagHandlers;
using FreneticScript.TagHandlers.Objects;

namespace VoxaliaServerSamplePlugin.SampleCommands
{
    class GreetingCommand: AbstractCommand
    {
        public SamplePlugin ThePlugin;

        public GreetingCommand(SamplePlugin plugin)
        {
            ThePlugin = plugin;
            Name = "greeting";
            Description = "Greets the server.";
            Arguments = "<message>";
            MinimumArguments = 1;
            MaximumArguments = 1;
            ObjectTypes = new List<Func<TemplateObject, TemplateObject>>()
            {
                (input) =>
                {
                    return new TextTag(input.ToString());
                }
            };
        }

        public override void Execute(CommandQueue queue, CommandEntry entry)
        {
            if (entry.Arguments.Count < 1)
            {
                ShowUsage(queue, entry);
                return;
            }
            entry.Good(queue, "'<{color.emphasis}>" + TagParser.Escape(entry.GetArgument(queue, 0)) + "<{color.base}>' to you as well from " + ThePlugin.Name + "!");
        }
    }
}
