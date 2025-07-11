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
using MAX.Generator;

namespace MAX.Orders.Building
{
    public class OrdMaze : DrawOrd
    {
        public override string Name { get { return "Maze"; } }

        public override DrawOp GetDrawOp(DrawArgs dArgs)
        {
            MazeDrawOp op = new MazeDrawOp
            {
                rng = MapGen.MakeRng(dArgs.Message)
            };
            return op;
        }

        public override void GetBrush(DrawArgs dArgs) { dArgs.BrushName = "Normal"; }

        public override void Help(Player p)
        {
            p.Message("&T/Maze");
            p.Message("&HGenerates a random maze between two points.");
        }
    }
}