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
using MAX.Maths;

namespace MAX.Orders.World
{
    public class OrdWarp : Order
    {
        public override string Name { get { return "Warp"; } }
        public override string Type { get { return OrderTypes.World; } }
        public override bool MuseumUsable { get { return false; } }
        public override bool SuperUseable { get { return false; } }
        public override OrderPerm[] ExtraPerms
        {
            get { return new[] { new OrderPerm(LevelPermission.Operator, "can manage warps") }; }
        }

        public override void Use(Player p, string message, OrderData data)
        {
            UseCore(p, message, data, WarpList.Global, "Warp");
        }

        public static void PrintWarp(Player p, Warp warp)
        {
            Vec3S32 pos = warp.Pos.BlockCoords;
            p.Message("{0} - ({1}, {2}, {3}) on {4}",
                      warp.Name, pos.X, pos.Y, pos.Z, warp.Level);
        }

        public void UseCore(Player p, string message, OrderData data,
                               WarpList warps, string group)
        {
            string[] args = message.SplitSpaces();
            string ord = args[0];
            if (ord.Length == 0) { Help(p); return; }
            bool checkExtraPerms = warps == WarpList.Global;

            if (IsListOrder(ord))
            {
                string modifier = args.Length > 1 ? args[1] : "";
                Paginator.Output(p, warps.Items, PrintWarp,
                                 group + " list", group + "s", modifier);
                return;
            }
            else if (args.Length == 1)
            {
                Warp warp = Matcher.FindWarps(p, warps, ord);
                if (warp != null) warps.Goto(warp, p);
                return;
            }

            string name = args[1];
            if (IsCreateOrder(ord))
            {
                if (checkExtraPerms && !CheckExtraPerm(p, data, 1)) return;
                if (warps.Exists(name)) { p.Message("{0} already exists", group); return; }

                warps.Create(name, p);
                p.Message("{0} {1} created.", group, name);
            }
            else if (IsDeleteOrder(ord))
            {
                if (checkExtraPerms && !CheckExtraPerm(p, data, 1)) return;
                Warp warp = Matcher.FindWarps(p, warps, name);
                if (warp == null) return;

                warps.Remove(warp);
                p.Message("{0} {1} deleted.", group, warp.Name);
            }
            else if (IsEditOrder(ord))
            {
                if (checkExtraPerms && !CheckExtraPerm(p, data, 1)) return;
                Warp warp = Matcher.FindWarps(p, warps, name);
                if (warp == null) return;

                warps.Update(warp, p);
                p.Message("{0} {1} moved.", group, warp.Name);
            }
            else if (ord.CaselessEq("goto"))
            {
                Warp warp = Matcher.FindWarps(p, warps, name);
                if (warp != null) warps.Goto(warp, p);
            }
            else
            {
                Help(p);
            }
        }

        public override void Help(Player p)
        {
            p.Message("&T/Warp [name] &H- Move to that warp");
            p.Message("&T/Warp list &H- List all the warps");
            p.Message("&T/Warp create [name] &H- Create a warp at your position");
            p.Message("&T/Warp delete [name] &H- Deletes a warp");
            p.Message("&T/Warp move [name] &H- Moves a warp to your position");
        }
    }
}