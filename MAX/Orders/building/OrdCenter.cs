/*
    Copyright 2015 MCGalaxy
    
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
using MAX.DB;
using MAX.Maths;


namespace MAX.Orders.Building
{
    public class OrdCenter : Order
    {
        public override string Name { get { return "Center"; } }
        public override string Shortcut { get { return "Centre"; } }
        public override string Type { get { return OrderTypes.Building; } }

        public override void Use(Player p, string message, OrderData data)
        {
            p.Message("Place or break two blocks to determine the edges.");
            p.MakeSelection(2, "Selecting region for &SCenter", null, DoCentre);
        }

        public bool DoCentre(Player p, Vec3S32[] m, object state, ushort block)
        {
            int lenX = m[0].X + m[1].X, lenY = m[0].Y + m[1].Y, lenZ = m[0].Z + m[1].Z;
            int x = lenX / 2, y = lenY / 2, z = lenZ / 2;

            Place(p, x, y, z);
            if ((lenX & 1) == 1) Place(p, x + 1, y, z);
            if ((lenZ & 1) == 1) Place(p, x, y, z + 1);
            if ((lenX & 1) == 1 && (lenZ & 1) == 1) Place(p, x + 1, y, z + 1);

            // Top layer blocks
            if ((lenY & 1) == 1)
            {
                Place(p, x, y + 1, z);
                if ((lenX & 1) == 1) Place(p, x + 1, y + 1, z);
                if ((lenZ & 1) == 1) Place(p, x, y + 1, z + 1);
                if ((lenX & 1) == 1 && (lenZ & 1) == 1) Place(p, x + 1, y + 1, z + 1);
            }

            p.Message("Gold blocks were placed at ({0}, {1}, {2}).", x, y, z);
            return true;
        }

        public static void Place(Player p, int x, int y, int z)
        {
            p.level.UpdateBlock(p, (ushort)x, (ushort)y, (ushort)z, Block.Gold, BlockDBFlags.Drawn);
        }

        public override void Help(Player p)
        {
            p.Message("&T/Center");
            p.Message("&HPlaces gold blocks at the center of your selection");
        }
    }
}