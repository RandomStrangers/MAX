﻿/*
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
using MAX.Drawing.Transforms;

namespace MAX.Orders.Building
{
    public class OrdAbort : Order
    {
        public override string Name { get { return "Abort"; } }
        public override string Shortcut { get { return "a"; } }
        public override string Type { get { return OrderTypes.Building; } }
        public override bool SuperUseable { get { return false; } }

        public override void Use(Player p, string message, OrderData data)
        {
            p.ClearBlockchange();
            p.painting = false;
            p.checkingBotInfo = false;
            p.ordTimer = false;
            p.staticOrders = false;
            p.deleteMode = false;
            p.ModeBlock = Block.Invalid;
            p.onTrain = false;
            p.isFlying = false;
            p.BrushName = "normal";
            p.DefaultBrushArgs = "";
            p.Transform = NoTransform.Instance;

            p.weapon?.Disable();
            p.Message("Every toggle or action was aborted.");
        }

        public override void Help(Player p)
        {
            p.Message("&T/Abort");
            p.Message("&HCancels an action.");
        }
    }
}