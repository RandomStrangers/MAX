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
using MAX.Maths;

namespace MAX.Orders.Building
{
    public class OrdDraw : DrawOrd
    {
        public override string Name { get { return "Draw"; } }

        public override int MarksCount { get { return 1; } }
        public override string SelectionType { get { return "origin"; } }
        public override string PlaceMessage { get { return "Place a block to determine the origin."; } }

        public override DrawMode GetMode(string[] parts)
        {
            string msg = parts[0];
            if (msg.CaselessEq("cone")) return DrawMode.cone;
            if (msg.CaselessEq("hcone")) return DrawMode.hcone;
            if (msg.CaselessEq("icone")) return DrawMode.icone;
            if (msg.CaselessEq("hicone")) return DrawMode.hicone;
            if (msg.CaselessEq("pyramid")) return DrawMode.pyramid;
            if (msg.CaselessEq("hpyramid")) return DrawMode.hpyramid;
            if (msg.CaselessEq("ipyramid")) return DrawMode.ipyramid;
            if (msg.CaselessEq("hipyramid")) return DrawMode.hipyramid;
            if (msg.CaselessEq("sphere")) return DrawMode.sphere;
            if (msg.CaselessEq("hsphere")) return DrawMode.hsphere;
            if (msg.CaselessEq("volcano")) return DrawMode.volcano;
            if (msg.CaselessEq("cylinder")) return DrawMode.hollow;
            return DrawMode.normal;
        }

        public override DrawOp GetDrawOp(DrawArgs dArgs)
        {
            DrawOp op = null;
            switch (dArgs.Mode)
            {
                case DrawMode.cone: op = new ConeDrawOp(); break;
                case DrawMode.hcone: op = new AdvHollowConeDrawOp(); break;
                case DrawMode.icone: op = new ConeDrawOp(true); break;
                case DrawMode.hicone: op = new AdvHollowConeDrawOp(true); break;
                case DrawMode.pyramid: op = new AdvPyramidDrawOp(); break;
                case DrawMode.hpyramid: op = new AdvHollowPyramidDrawOp(); break;
                case DrawMode.ipyramid: op = new AdvPyramidDrawOp(true); break;
                case DrawMode.hipyramid: op = new AdvHollowPyramidDrawOp(true); break;
                case DrawMode.sphere: op = new AdvSphereDrawOp(); break;
                case DrawMode.hsphere: op = new AdvHollowSphereDrawOp(); break;
                case DrawMode.volcano: op = new AdvVolcanoDrawOp(); break;
                case DrawMode.hollow: op = new CylinderDrawOp(); break;
            }
            if (op == null) { Help(dArgs.Player); return null; }

            AdvDrawMeta meta = new AdvDrawMeta();
            bool success = false;
            string[] args = dArgs.Message.SplitSpaces();
            Player p = dArgs.Player;

            if (UsesHeight(dArgs))
            {
                if (args.Length < 3)
                {
                    p.Message("You need to provide the radius and the height for the {0}.", args[0]);
                }
                else
                {
                    success = OrderParser.GetInt(p, args[1], "radius", ref meta.radius, 0, 2000)
                        && OrderParser.GetInt(p, args[2], "height", ref meta.height, 0, 2000);
                }
            }
            else
            {
                if (args.Length < 2)
                {
                    p.Message("You need to provide the radius for the {0}.", args[0]);
                }
                else
                {
                    success = OrderParser.GetInt(p, args[1], "radius", ref meta.radius, 0, 2000);
                }
            }

            if (!success) return null;
            dArgs.Meta = meta;
            return op;
        }

        public override void GetMarks(DrawArgs dArgs, ref Vec3S32[] m)
        {
            AdvDrawMeta meta = (AdvDrawMeta)dArgs.Meta;
            int radius = meta.radius;

            Vec3S32 P = m[0];
            m = new Vec3S32[] {
                new Vec3S32(P.X - radius, P.Y, P.Z - radius),
                new Vec3S32(P.X + radius, P.Y, P.Z + radius),
            };

            if (UsesHeight(dArgs))
            {
                m[1].Y += meta.height - 1;
            }
            else
            {
                m[0].Y -= radius; m[1].Y += radius;
            }
        }

        public override void GetBrush(DrawArgs dArgs)
        {
            int argsUsed = UsesHeight(dArgs) ? 3 : 2;
            dArgs.BrushArgs = dArgs.Message.Splice(argsUsed, 0);
        }

        public class AdvDrawMeta { public int radius, height; }

        public static bool UsesHeight(DrawArgs args)
        {
            DrawMode mode = args.Mode;
            return !(mode == DrawMode.sphere || mode == DrawMode.hsphere);
        }

        public override void Help(Player p)
        {
            p.Message("&T/Draw [object] [baseradius] [height] <brush args>");
            p.Message("&T/Draw [object] [radius] <brush args>");
            p.Message("&HDraws an object at the specified point.");
            p.Message("   &HObjects: &fcone/hcone/icone/hicone/cylinder/");
            p.Message("     &fpyramid/hpyramid/ipyramid/hipyramid/volcano");
            p.Message("   &HObjects with only radius: &fsphere/hsphere");
            p.Message("   &HNote 'h' means hollow, 'i' means inverse");
            p.Message(BrushHelpLine);
        }
    }
}