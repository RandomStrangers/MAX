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
using MAX.Blocks.Physics;
using MAX.Maths;
using System;
using System.Collections.Generic;


namespace MAX.Orders.Building
{
    public class OrdRestartPhysics : Order
    {
        public override string Name { get { return "RestartPhysics"; } }
        public override string Shortcut { get { return "rp"; } }
        public override string Type { get { return OrderTypes.Building; } }
        public override bool MuseumUsable { get { return false; } }
        public override LevelPermission DefaultRank { get { return LevelPermission.AdvBuilder; } }
        public override bool SuperUseable { get { return false; } }

        public override void Use(Player p, string message, OrderData data)
        {
            PhysicsArgs extraInfo = default;
            message = message.ToLower();
            if (message.Length > 0 && !ParseArgs(p, message, ref extraInfo)) return;

            p.Message("Place or break two blocks to determine the edges.");
            p.MakeSelection(2, "Selecting region for &SRestart physics", extraInfo, DoRestart);
        }

        public bool ParseArgs(Player p, string message, ref PhysicsArgs args)
        {
            string[] parts = message.SplitSpaces();
            if (parts.Length % 2 == 1)
            {
                p.Message("Number of parameters must be even");
                Help(p); return false;
            }
            byte type = 0, value = 0;
            byte extBits = 0;

            if (parts.Length >= 2)
            {
                if (!Parse(p, parts[0], parts[1], ref type, ref value, ref extBits)) return false;
                args.Type1 = type; args.Value1 = value;
            }
            if (parts.Length >= 4)
            {
                if (!Parse(p, parts[2], parts[3], ref type, ref value, ref extBits)) return false;
                args.Type2 = type; args.Value2 = value;
            }
            if (parts.Length >= 6)
            {
                p.Message("You can only use up to two types of physics."); return false;
            }

            args.ExtBlock = extBits;
            return true;
        }

        public bool Parse(Player p, string name, string arg, ref byte type, ref byte value, ref byte isExt)
        {
            if (name.CaselessEq("revert"))
            {
                if (!OrderParser.GetBlock(p, arg, out ushort block)) return false;

                type = PhysicsArgs.Revert; value = (byte)block;
                isExt = (byte)(block >> Block.ExtendedShift);
                return true;
            }

            if (!OrderParser.GetByte(p, arg, "Value", ref value)) return false;

            switch (name)
            {
                case "drop": type = PhysicsArgs.Drop; return true;
                case "explode": type = PhysicsArgs.Explode; return true;
                case "dissipate": type = PhysicsArgs.Dissipate; return true;
                case "wait": type = PhysicsArgs.Wait; return true;
                case "rainbow": type = PhysicsArgs.Rainbow; return true;
            }
            p.Message(name + " type is not supported.");
            return false;
        }

        public bool DoRestart(Player p, Vec3S32[] m, object state, ushort block)
        {
            PhysicsArgs args = (PhysicsArgs)state;
            List<int> buffer = new List<int>();

            for (int y = Math.Min(m[0].Y, m[1].Y); y <= Math.Max(m[0].Y, m[1].Y); y++)
                for (int z = Math.Min(m[0].Z, m[1].Z); z <= Math.Max(m[0].Z, m[1].Z); z++)
                    for (int x = Math.Min(m[0].X, m[1].X); x <= Math.Max(m[0].X, m[1].X); x++)
                    {
                        if (!p.level.IsAirAt((ushort)x, (ushort)y, (ushort)z, out int index))
                        {
                            buffer.Add(index);
                        }
                    }

            if (args.Raw == 0)
            {
                if (buffer.Count > Server.Config.PhysicsRestartNormLimit)
                {
                    p.Message("Cannot restart more than " + Server.Config.PhysicsRestartNormLimit + " blocks.");
                    p.Message("Tried to restart " + buffer.Count + " blocks.");
                    return false;
                }
            }
            else if (buffer.Count > Server.Config.PhysicsRestartLimit)
            {
                p.Message("Tried to add physics to " + buffer.Count + " blocks.");
                p.Message("Cannot add physics to more than " + Server.Config.PhysicsRestartLimit + " blocks.");
                return false;
            }

            foreach (int index1 in buffer)
            {
                p.level.AddCheck(index1, true, args);
            }
            p.Message("Activated " + buffer.Count + " blocks.");
            return true;
        }

        public override void Help(Player p)
        {
            p.Message("/restartphysics ([type] [num]) ([type2] [num2]) - Restarts every physics block in an area");
            p.Message("[type] will set custom physics for selected blocks");
            p.Message("Possible [types]: drop, explode, dissipate, wait, rainbow, revert");
            p.Message("/rp revert takes block names");
        }
    }
}