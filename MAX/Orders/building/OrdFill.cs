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
using MAX.Maths;
using System;


namespace MAX.Orders.Building
{
    public class OrdFill : DrawOrd
    {
        public override string Name { get { return "Fill"; } }
        public override string Shortcut { get { return "f"; } }
        public override LevelPermission DefaultRank { get { return LevelPermission.AdvBuilder; } }
        public override OrderDesignation[] Designations
        {
            get
            {
                return new[] { new OrderDesignation("F3D"), new OrderDesignation("F2D", "2d"),
                    new OrderDesignation("Fill3D"), new OrderDesignation("Fill2D", "2d") };
            }
        }

        public override int MarksCount { get { return 1; } }
        public override string SelectionType { get { return "origin"; } }
        public override string PlaceMessage { get { return "Place or break a block to mark the area you wish to fill."; } }

        public override DrawMode GetMode(string[] parts)
        {
            string msg = parts[0];
            if (msg.CaselessEq("normal")) return DrawMode.solid;
            if (msg.CaselessEq("up")) return DrawMode.up;
            if (msg.CaselessEq("down")) return DrawMode.down;
            if (msg.CaselessEq("layer")) return DrawMode.layer;
            if (msg.CaselessEq("vertical_x")) return DrawMode.verticalX;
            if (msg.CaselessEq("vertical_z")) return DrawMode.verticalZ;
            if (msg.CaselessEq("2d")) return DrawMode.volcano;
            return DrawMode.normal;
        }

        public override DrawOp GetDrawOp(DrawArgs dArg) { return new FillDrawOp(); }

        public override void GetBrush(DrawArgs dArgs)
        {
            int endCount = 0;
            if (IsConfirmed(dArgs.Message)) endCount++;
            dArgs.BrushArgs = dArgs.Message.Splice(dArgs.ModeArgsCount, endCount);
        }

        public override bool DoDraw(Player p, Vec3S32[] marks, object state, ushort block)
        {
            DrawArgs dArgs = (DrawArgs)state;
            ushort x = (ushort)marks[0].X, y = (ushort)marks[0].Y, z = (ushort)marks[0].Z;
            ushort old = p.level.GetBlock(x, y, z);
            if (!OrderParser.IsBlockAllowed(p, "fill over", old)) return false;

            bool is2D = dArgs.Mode == DrawMode.volcano;
            if (is2D) dArgs.Mode = Calc2DFill(p, marks);

            FillDrawOp op = (FillDrawOp)dArgs.Op;
            op.Positions = FillDrawOp.FloodFill(p, p.level.PosToInt(x, y, z), old, dArgs.Mode);
            int count = op.Positions.Count;

            bool confirmed = IsConfirmed(dArgs.Message), success = true;
            if (count < p.group.DrawLimit && count > p.level.ReloadThreshold && !confirmed)
            {
                p.Message("This fill would affect {0} blocks.", count);
                p.Message("If you still want to fill, type &T/Fill {0} confirm", dArgs.Message);
            }
            else
            {
                success = base.DoDraw(p, marks, state, block);
            }

            if (is2D) dArgs.Mode = DrawMode.volcano;
            op.Positions = null;
            return success;
        }

        public static DrawMode Calc2DFill(Player p, Vec3S32[] marks)
        {
            int lenX = Math.Abs(p.Pos.BlockX - marks[0].X);
            int lenY = Math.Abs(p.Pos.BlockY - marks[0].Y);
            int lenZ = Math.Abs(p.Pos.BlockZ - marks[0].Z);

            if (lenY >= lenX && lenY >= lenZ) return DrawMode.layer;
            return lenX >= lenZ ? DrawMode.verticalX : DrawMode.verticalZ;
        }

        public static bool IsConfirmed(string message)
        {
            return message.CaselessEq("confirm") || message.CaselessEnds(" confirm");
        }

        public override void Help(Player p)
        {
            p.Message("&T/Fill <brush args>");
            p.Message("&HFills the area specified with the output of your current brush.");
            p.Message("&T/Fill [mode] <brush args>");
            p.Message("&HModes: &fnormal/up/down/layer/vertical_x/vertical_z/2d");
            p.Message(BrushHelpLine);
        }
    }
}