/*
    Copyright 2010 MCSharp team (Modified for use with MCZall/MCLawl/MCForge)
    
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

namespace MAX.Orders.Building
{
    public class OrdCuboid : DrawOrd
    {
        public override string Name { get { return "Cuboid"; } }
        public override string Shortcut { get { return "z"; } }
        public override OrderDesignation[] Designations
        {
            get
            {
                return new[] { new OrderDesignation("cw", "wire"),
                    new OrderDesignation("ch", "hollow"), new OrderDesignation("Walls", "walls"),
                    new OrderDesignation("box"), new OrderDesignation("hbox", "hollow") };
            }
        }

        public override DrawMode GetMode(string[] parts)
        {
            string msg = parts[0];
            if (msg.CaselessEq("solid")) return DrawMode.solid;
            if (msg.CaselessEq("hollow")) return DrawMode.hollow;
            if (msg.CaselessEq("walls")) return DrawMode.walls;
            if (msg.CaselessEq("holes")) return DrawMode.holes;
            if (msg.CaselessEq("wire")) return DrawMode.wire;
            if (msg.CaselessEq("random")) return DrawMode.random;
            return DrawMode.normal;
        }

        public override DrawOp GetDrawOp(DrawArgs dArgs)
        {
            switch (dArgs.Mode)
            {
                case DrawMode.hollow: return new CuboidHollowsDrawOp();
                case DrawMode.walls: return new CuboidWallsDrawOp();
                case DrawMode.holes: return new CuboidDrawOp();
                case DrawMode.wire: return new CuboidWireframeDrawOp();
                case DrawMode.random: return new CuboidDrawOp();
            }
            return new CuboidDrawOp();
        }

        public override void GetBrush(DrawArgs dArgs)
        {
            if (dArgs.Mode == DrawMode.solid) dArgs.BrushName = "Normal";
            if (dArgs.Mode == DrawMode.holes) dArgs.BrushName = "Checkered";
            if (dArgs.Mode == DrawMode.random) dArgs.BrushName = "Random";
            dArgs.BrushArgs = dArgs.Message.Splice(dArgs.ModeArgsCount, 0);
        }

        public override void Help(Player p)
        {
            p.Message("&T/Cuboid <brush args>");
            p.Message("&HDraws a cuboid between two points.");
            p.Message("&T/Cuboid [mode] <brush args>");
            p.Message("&HModes: &fsolid/hollow/walls/holes/wire/random");
            p.Message(BrushHelpLine);
        }
    }
}