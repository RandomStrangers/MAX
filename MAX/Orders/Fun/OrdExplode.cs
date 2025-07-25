/*
    Written by Jack1312
  
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
using MAX.Maths;


namespace MAX.Orders.Fun
{
    public class OrdExplode : Order
    {
        public override string Name { get { return "Explode"; } }
        public override string Shortcut { get { return "ex"; } }
        public override string Type { get { return OrderTypes.Moderation; } }
        public override LevelPermission DefaultRank { get { return LevelPermission.Operator; } }

        public override void Use(Player p, string message, OrderData data)
        {
            if (message.Length == 0) { Help(p); return; }
            if (message.CaselessEq("me")) message = p.name;

            string[] args = message.SplitSpaces();
            Vec3S32 P = p.Pos.BlockCoords;
            if (args.Length == 1)
            {
                Player target = PlayerInfo.FindMatches(p, args[0]);
                if (target == null) return;

                P = target.Pos.BlockCoords;
                if (DoExplode(p, target.level, ref P))
                {
                    p.Message("{0} &Shas been exploded!", p.FormatNick(target));
                }
            }
            else if (args.Length == 3)
            {
                if (!OrderParser.GetCoords(p, args, 0, ref P)) return;

                if (DoExplode(p, p.level, ref P))
                {
                    p.Message("An explosion was made at ({0}, {1}, {2}).", P.X, P.Y, P.Z);
                }
            }
            else
            {
                Help(p);
            }
        }

        public static bool DoExplode(Player p, Level lvl, ref Vec3S32 pos)
        {
            if (lvl.Physics < 3 || lvl.Physics == 5)
            {
                p.Message("&WThe physics on {0} &Ware not sufficient for exploding!", lvl.ColoredName);
                return false;
            }

            pos = lvl.ClampPos(pos);
            ushort x = (ushort)pos.X, y = (ushort)pos.Y, z = (ushort)pos.Z;
            ushort old = lvl.GetBlock(x, y, z);

            if (!lvl.CheckAffect(p, x, y, z, old, Block.TNT)) return false;
            lvl.MakeExplosion(x, y, z, 1);
            return true;
        }

        public override void Help(Player p)
        {
            p.Message("&T/Explode &H- Creates small explosions");
            p.Message("&T/Explode me &H- Explodes at your location");
            p.Message("&T/Explode [Player] &H- Explodes at given's player location");
            p.Message("&T/Explode [x y z] &H- Explodes at the given corordinates");
        }
    }
}