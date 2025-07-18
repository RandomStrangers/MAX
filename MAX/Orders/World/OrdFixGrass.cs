﻿/*
    Copyright 2010 MCLawl Team - Written by Valek (Modified for use with MCForge)
 
   
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
using MAX.Drawing.Ops;
using MAX.Maths;


namespace MAX.Orders.World
{
    public class OrdFixGrass : Order
    {
        public override string Name { get { return "FixGrass"; } }
        public override string Shortcut { get { return "fg"; } }
        public override string Type { get { return OrderTypes.World; } }
        public override bool MuseumUsable { get { return false; } }
        public override LevelPermission DefaultRank { get { return LevelPermission.Operator; } }
        public override bool SuperUseable { get { return false; } }

        public override void Use(Player p, string message, OrderData data)
        {
            FixGrassDrawOp op = new FixGrassDrawOp();

            if (message.Length == 0)
            {
                op.FixDirt = true;
                op.FixGrass = true;
            }
            else if (message.CaselessEq("light"))
            {
                op.LightMode = true;
            }
            else if (message.CaselessEq("grass"))
            {
                op.FixGrass = true;
            }
            else if (message.CaselessEq("dirt"))
            {
                op.FixDirt = true;
            }
            else
            {
                Help(p); return;
            }

            p.Message("Place or break two blocks to determine the bounds within which grass/dirt is fixed");
            p.MakeSelection(2, "Selecting corners for &SFixGrass", op, DoFixGrass);
        }

        public bool DoFixGrass(Player p, Vec3S32[] marks, object state, ushort block)
        {
            FixGrassDrawOp op = (FixGrassDrawOp)state;
            op.AlwaysUsable = true;

            DrawOpPerformer.Do(op, null, p, marks, false);
            return false;
        }

        public override void Help(Player p)
        {
            p.Message("&T/FixGrass &H- Turns grass with something on top to dirt, and turns dirt with nothing on top to grass");
            p.Message("&T/FixGrass light &H- Only dirt/grass in sunlight becomes grass");
            p.Message("&T/FixGrass grass &H- Turns grass with something on top to dirt");
            p.Message("&T/FixGrass dirt &H- Turns dirt with nothing on top to grass");
        }
    }
}