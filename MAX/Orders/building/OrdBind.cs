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


namespace MAX.Orders.Building
{
    public class OrdBind : Order
    {
        public override string Name { get { return "Bind"; } }
        public override string Type { get { return OrderTypes.Building; } }
        public override LevelPermission DefaultRank { get { return LevelPermission.AdvBuilder; } }
        public override bool SuperUseable { get { return false; } }

        public override void Use(Player p, string message, OrderData data)
        {
            if (message.Length == 0) { Help(p); return; }
            string[] args = message.SplitSpaces();
            if (args.Length > 2) { Help(p); return; }

            if (args[0].CaselessEq("clear"))
            {
                for (int b = 0; b < p.BlockBindings.Length; b++)
                {
                    p.BlockBindings[b] = (ushort)b;
                }
                p.Message("All bindings were unbound.");
                return;
            }

            if (!OrderParser.GetBlock(p, args[0], out ushort src)) return;
            if (Block.IsPhysicsType(src))
            {
                p.Message("Physics blocks cannot be bound to another block."); return;
            }

            if (args.Length == 2)
            {
                if (!OrderParser.GetBlockIfAllowed(p, args[1], "bind a block to", out ushort dst)) return;

                p.BlockBindings[src] = dst;
                p.Message("{0} bound to {1}", Block.GetName(p, src), Block.GetName(p, dst));
            }
            else
            {
                if (p.BlockBindings[src] == src)
                {
                    p.Message("{0} is not bound.", Block.GetName(p, src)); return;
                }
                p.BlockBindings[src] = src;
                p.Message("Unbound {0}.", Block.GetName(p, src));
            }
        }

        public override void Help(Player p)
        {
            p.Message("&T/Bind [block] [replacement block]");
            p.Message("&HCauses [replacement] to be placed, whenever you place [block].");
            p.Message("&T/Bind [block] &H- Removes binding for [block].");
            p.Message("&T/Bind clear &H- Clears all binds.");
        }
    }
}