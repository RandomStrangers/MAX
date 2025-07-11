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
using MAX.Drawing.Ops;
using MAX.Maths;
using System;

namespace MAX.Orders.Building
{
    public class OrdSphere : DrawOrd
    {
        public override string Name { get { return "Sphere"; } }
        public override string Shortcut { get { return "sp"; } }
        public override LevelPermission DefaultRank { get { return LevelPermission.AdvBuilder; } }
        public override OrderDesignation[] Designations
        {
            get
            {
                return new[] { new OrderDesignation("SphereH", "hollow"),
                    new OrderDesignation("sph", "hollow"), new OrderDesignation("Circle", "circle" ),
                    new OrderDesignation("CircleH", "hollowcircle") };
            }
        }
        public override string PlaceMessage { get { return "Place a block for the centre, then another for the radius."; } }

        public override DrawMode GetMode(string[] parts)
        {
            string msg = parts[0];
            if (msg.CaselessEq("solid")) return DrawMode.solid;
            if (msg.CaselessEq("hollow")) return DrawMode.hollow;
            if (msg.CaselessEq("circle")) return DrawMode.circle;
            if (msg.CaselessEq("hollowcircle")) return DrawMode.hcircle;
            return DrawMode.normal;
        }

        public override DrawOp GetDrawOp(DrawArgs dArgs)
        {
            switch (dArgs.Mode)
            {
                case DrawMode.hollow: return new AdvHollowSphereDrawOp();
                case DrawMode.circle: return new EllipsoidDrawOp();
                case DrawMode.hcircle: return new EllipsoidHollowDrawOp();
            }
            return new AdvSphereDrawOp();
        }

        public override void GetMarks(DrawArgs dArgs, ref Vec3S32[] m)
        {
            Vec3S32 p0 = m[0];
            Vec3S32 radius = GetRadius(dArgs.Mode, m);
            m[0] = p0 - radius; m[1] = p0 + radius;
        }


        public override void GetBrush(DrawArgs dArgs)
        {
            if (dArgs.Mode == DrawMode.solid) dArgs.BrushName = "Normal";
            dArgs.BrushArgs = dArgs.Message.Splice(dArgs.ModeArgsCount, 0);
        }

        public static Vec3S32 GetRadius(DrawMode mode, Vec3S32[] m)
        {
            int dx = Math.Abs(m[0].X - m[1].X);
            int dy = Math.Abs(m[0].Y - m[1].Y);
            int dz = Math.Abs(m[0].Z - m[1].Z);

            bool circle = mode == DrawMode.circle || mode == DrawMode.hcircle;
            if (!circle)
            {
                int R = (int)Math.Sqrt(dx * dx + dy * dy + dz * dz);
                return new Vec3S32(R, R, R);
            }
            else if (dx >= dy && dz >= dy)
            {
                int R = (int)Math.Sqrt(dx * dx + dz * dz);
                return new Vec3S32(R, 0, R);
            }
            else if (dz >= dx)
            {
                int R = (int)Math.Sqrt(dy * dy + dz * dz);
                return new Vec3S32(0, R, R);
            }
            else
            {
                int R = (int)Math.Sqrt(dx * dx + dy * dy);
                return new Vec3S32(R, R, 0);
            }
        }

        public override void Help(Player p)
        {
            p.Message("&T/Sphere <brush args>");
            p.Message("&HCreates a sphere, with first point as centre, and second for radius");
            p.Message("&T/Sphere [mode] <brush args>");
            p.Message("&HModes: &fsolid/hollow/circle/hollowcircle");
            p.Message(BrushHelpLine);
        }
    }
}