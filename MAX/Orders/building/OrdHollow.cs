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
    public class OrdHollow : DrawOrd
    {
        public override string Name { get { return "Hollow"; } }

        public override DrawOp GetDrawOp(DrawArgs dArgs)
        {
            ushort skip = Block.Invalid;
            if (dArgs.Message.Length > 0)
            {
                if (!OrderParser.GetBlock(dArgs.Player, dArgs.Message, out skip)) return null;
            }

            HollowDrawOp op = new HollowDrawOp
            {
                Skip = skip
            };
            return op;
        }

        public override void GetBrush(DrawArgs dArgs) { dArgs.BrushName = "Normal"; }

        public override void Help(Player p)
        {
            p.Message("&T/Hollow");
            p.Message("&HHollows out an area without flooding it");
            p.Message("&T/Hollow [block]");
            p.Message("&HHollows around [block]");
        }
    }
}