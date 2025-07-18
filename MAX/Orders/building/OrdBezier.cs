﻿/*
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
using MAX.Drawing.Ops;

namespace MAX.Orders.Building
{
    public class OrdBezier : DrawOrd
    {
        public override string Name { get { return "Bezier"; } }
        public override OrderDesignation[] Designations
        {
            get { return new OrderDesignation[] { new OrderDesignation("Curve") }; }
        }

        public override int MarksCount { get { return 3; } }
        public override string SelectionType { get { return "points"; } }
        public override string PlaceMessage { get { return "Place or break two blocks to determine the endpoints, then another for the control point"; } }

        public override DrawOp GetDrawOp(DrawArgs dArgs) { return new BezierDrawOp(); }

        public override void Help(Player p)
        {
            p.Message("&T/Bezier <brush args>");
            p.Message("&HDraws a quadratic bezier curve.");
            p.Message("&HFirst two points specify the endpoints, then another point specifies the control point.");
            p.Message(BrushHelpLine);
        }
    }
}