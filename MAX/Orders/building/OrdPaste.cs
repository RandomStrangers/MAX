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
using MAX.Drawing;
using MAX.Drawing.Brushes;
using MAX.Drawing.Ops;
using MAX.Maths;


namespace MAX.Orders.Building
{
    public class OrdPaste : Order
    {
        public override string Name { get { return "Paste"; } }
        public override string Shortcut { get { return "v"; } }
        public override string Type { get { return OrderTypes.Building; } }
        public override bool MuseumUsable { get { return false; } }
        public override LevelPermission DefaultRank { get { return LevelPermission.AdvBuilder; } }
        public override bool SuperUseable { get { return false; } }
        public override OrderDesignation[] Designations
        {
            get { return new[] { new OrderDesignation("PasteNot", "not"), new OrderDesignation("pn", "not") }; }
        }

        public override void Use(Player p, string message, OrderData data)
        {
            BrushArgs args = new BrushArgs(p, message, Block.Air);
            if (!BrushFactory.Find("Paste").Validate(args)) return;

            p.Message("Place a block in the corner of where you want to paste.");
            p.MakeSelection(1, "Selecting location for &SPaste", args, DoPaste);
        }

        public bool DoPaste(Player p, Vec3S32[] m, object state, ushort block)
        {
            BrushArgs args = (BrushArgs)state;
            Brush brush = BrushFactory.Find("Paste").Construct(args);
            if (brush == null) return false;

            CopyState cState = p.CurrentCopy;
            PasteDrawOp op = new PasteDrawOp
            {
                CopyState = cState
            };

            m[0] += cState.Offset;
            DrawOpPerformer.Do(op, brush, p, m);
            return true;
        }

        public override void Help(Player p)
        {
            p.Message("&T/Paste &H- Pastes the stored copy.");
            p.Message("&T/Paste [block] [block2].. &H- Pastes only the specified blocks from the copy.");
            p.Message("&T/Paste not [block] [block2].. &H- Pastes all blocks from the copy, except for the specified blocks.");
            p.Message("&4BEWARE: &SThe blocks will always be pasted in a set direction");
        }
    }
}