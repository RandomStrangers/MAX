/*
    Copyright 2011 MCForge
        
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
    public class OrdOutline : DrawOrd
    {
        public override string Name { get { return "Outline"; } }
        public override LevelPermission DefaultRank { get { return LevelPermission.AdvBuilder; } }

        public override DrawOp GetDrawOp(DrawArgs dArgs)
        {
            Player p = dArgs.Player;
            if (dArgs.Message.Length == 0)
            {
                p.Message("Block name is required."); return null;
            }

            string[] parts = dArgs.Message.SplitSpaces(2);
            // NOTE: Don't need to check if allowed to use block here
            // (OutlineDrawOp skips all blocks that are equal to target)
            if (!OrderParser.GetBlock(p, parts[0], out ushort target)) return null;

            OutlineDrawOp op = new OutlineDrawOp();
            // e.g. testing air 'above' grass - therefore op.Above needs to be false for 'up mode'
            if (dArgs.Mode == DrawMode.up) { op.Layer = false; op.Above = false; }
            if (dArgs.Mode == DrawMode.down) { op.Layer = false; op.Below = false; }
            if (dArgs.Mode == DrawMode.layer) { op.Above = false; op.Below = false; }
            op.Target = target;
            return op;
        }


        public override DrawMode GetMode(string[] parts)
        {
            if (parts.Length == 1) return DrawMode.normal;

            string type = parts[1];
            if (type.CaselessEq("down")) return DrawMode.down;
            if (type.CaselessEq("up")) return DrawMode.up;
            if (type.CaselessEq("layer")) return DrawMode.layer;
            if (type.CaselessEq("all")) return DrawMode.solid;
            return DrawMode.normal;
        }

        public override void GetBrush(DrawArgs dArgs)
        {
            dArgs.BrushArgs = dArgs.Message.Splice(dArgs.ModeArgsCount + 1, 0);
        }

        public override void Help(Player p)
        {
            p.Message("&T/Outline [block] <brush args>");
            p.Message("&HOutlines [block] with output of your current brush.");
            p.Message("&T/Outline [block] [mode] <brush args>");
            p.Message("&HModes: &fall/up/layer/down (default all)");
            p.Message(BrushHelpLine);
        }
    }
}