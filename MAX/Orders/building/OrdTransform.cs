﻿/*
    Copyright 2015 MCGalaxy
        
    Dual-licensed under the Educational Community License, Version 2.0 and
    the GNU General Public License, Version 3 (the "Licenses"); you may
    not use this file except in compliance with the Licenses. You may
    obtain a copy of the Licenses at
    
    https://opensource.org/license/ecl-2-0/
    https://www.gnu.org/licenses/gpl-3.0.html
    
    Unless required by applicable law or agreed to in writing,
    software distributed under the Licenses are distributed on an "AS IS"
    BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express
    or implied. See the Licenses for the specific language governing
    permissions and limitations under the Licenses.
 */
using MAX.Drawing.Transforms;

namespace MAX.Orders.Building
{
    public class OrdTransform : Order
    {
        public override string Name { get { return "Transform"; } }
        public override string Type { get { return OrderTypes.Building; } }
        public override LevelPermission DefaultRank { get { return LevelPermission.AdvBuilder; } }
        public override bool SuperUseable { get { return false; } }
        public override OrderDesignation[] Designations
        {
            get { return new[] { new OrderDesignation("Transforms", "list"), new OrderDesignation("Scale", "scale") }; }
        }

        public override void Use(Player p, string message, OrderData data)
        {
            if (message.Length == 0)
            {
                p.Message("Your current transform is: " + p.Transform.Name); return;
            }
            string[] args = message.SplitSpaces(2);
            TransformFactory transform = TransformFactory.Find(args[0]);

            if (IsListOrder(args[0]))
            {
                List(p);
            }
            else if (transform == null)
            {
                p.Message("No transform found with name \"{0}\".", args[0]);
                List(p);
            }
            else
            {
                p.Message("Set your transform to: " + transform.Name);
                Transform instance = transform.Construct(p, args.Length == 1 ? "" : args[1]);
                if (instance == null) return;
                p.Transform = instance;
            }
        }

        public static void List(Player p)
        {
            p.Message("&HAvailable transforms: &f" + TransformFactory.Transforms.Join(t => t.Name));
        }

        public override void Help(Player p)
        {
            p.Message("&T/Transform [name] <transform args>");
            p.Message("&HSets your current transform to the transform with that name.");
            p.Message("&T/Help Transform [name]");
            p.Message("&HOutputs the help for the transform with that name.");
            List(p);
        }

        public override void Help(Player p, string message)
        {
            TransformFactory transform = TransformFactory.Find(message);
            if (transform == null)
            {
                p.Message("No transform found with name \"{0}\".", message);
                List(p);
            }
            else
            {
                p.MessageLines(transform.Help);
            }
        }
    }
}