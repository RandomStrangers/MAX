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
    public class OrdPyramid : DrawOrd
    {
        public override string Name { get { return "Pyramid"; } }
        public override string Shortcut { get { return "pd"; } }

        public override DrawMode GetMode(string[] parts)
        {
            string mode = parts[0];
            if (mode.CaselessEq("solid")) return DrawMode.solid;
            if (mode.CaselessEq("hollow")) return DrawMode.hollow;
            if (mode.CaselessEq("reverse")) return DrawMode.reverse;
            return DrawMode.normal;
        }

        public override DrawOp GetDrawOp(DrawArgs dArgs)
        {
            switch (dArgs.Mode)
            {
                case DrawMode.hollow: return new PyramidHollowDrawOp();
                case DrawMode.reverse: return new PyramidReverseDrawOp();
            }
            return new PyramidSolidDrawOp();
        }

        public override void Help(Player p)
        {
            p.Message("&T/Pyramid <brush args>");
            p.Message("&HDraws a square pyramid, using two points for the base.");
            p.Message("&T/Pyramid [mode] <brush args>");
            p.Message("&HModes: &fsolid/hollow/reverse");
            p.Message(BrushHelpLine);
        }
    }
}