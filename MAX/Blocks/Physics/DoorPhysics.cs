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


namespace MAX.Blocks.Physics
{
    public static class DoorPhysics
    {

        public static void Do(Level lvl, ref PhysInfo C)
        {
            if (C.Data.Type1 != PhysicsArgs.Custom)
            {
                C.Data.Data = PhysicsArgs.RemoveFromChecks; return;
            }

            if (C.Data.Data == 0)
            {
                ushort block = (ushort)(C.Data.Value2 | (C.Data.ExtBlock << Block.ExtendedShift));
                bool tdoor = lvl.Props[block].IsTDoor;

                if (tdoor) TDoor(lvl, ref C);
                else Door(lvl, ref C);
            }

            if (C.Data.Data <= C.Data.Value1)
            { // value1 for wait time
                C.Data.Data++;
            }
            else
            {
                PhysicsArgs dArgs = default;
                dArgs.ExtBlock = C.Data.ExtBlock;
                lvl.AddUpdate(C.Index, C.Data.Value2, dArgs);
                C.Data.Data = PhysicsArgs.RemoveFromChecks;
            }
        }

        // Change anys door blocks nearby into air forms
        public static void Door(Level lvl, ref PhysInfo C)
        {
            ushort x = C.X, y = C.Y, z = C.Z;
            ushort block = (ushort)(C.Data.Value2 | (C.Data.ExtBlock << Block.ExtendedShift));
            bool instant = block == Block.Door_Air || block == Block.Door_AirActivatable;

            ActivateablePhysics.DoDoors(lvl, (ushort)(x + 1), y, z, instant);
            ActivateablePhysics.DoDoors(lvl, (ushort)(x - 1), y, z, instant);
            ActivateablePhysics.DoDoors(lvl, x, y, (ushort)(z + 1), instant);
            ActivateablePhysics.DoDoors(lvl, x, y, (ushort)(z - 1), instant);
            ActivateablePhysics.DoDoors(lvl, x, (ushort)(y - 1), z, instant);
            ActivateablePhysics.DoDoors(lvl, x, (ushort)(y + 1), z, instant);

            if (block == Block.Door_Green && lvl.Physics != 5)
            {
                ActivateablePhysics.DoNeighbours(lvl, x, y, z);
            }
        }

        public static void ODoor(Level lvl, ref PhysInfo C)
        {
            ushort x = C.X, y = C.Y, z = C.Z;
            ushort block = C.Block;

            ActivateODoor(lvl, block, (ushort)(x - 1), y, z);
            ActivateODoor(lvl, block, (ushort)(x + 1), y, z);
            ActivateODoor(lvl, block, x, (ushort)(y - 1), z);
            ActivateODoor(lvl, block, x, (ushort)(y + 1), z);
            ActivateODoor(lvl, block, x, y, (ushort)(z - 1));
            ActivateODoor(lvl, block, x, y, (ushort)(z + 1));
            C.Data.Data = PhysicsArgs.RemoveFromChecks;
        }

        public static void ActivateODoor(Level lvl, ushort target, ushort x, ushort y, ushort z)
        {
            ushort block = lvl.GetBlock(x, y, z, out int index);
            block = lvl.Props[block].oDoorBlock;

            if (index >= 0 && block == target)
            {
                lvl.AddUpdate(index, target, true);
            }
        }

        public static void TDoor(Level lvl, ref PhysInfo C)
        {
            ushort x = C.X, y = C.Y, z = C.Z;
            ActivateTDoor(lvl, (ushort)(x - 1), y, z);
            ActivateTDoor(lvl, (ushort)(x + 1), y, z);
            ActivateTDoor(lvl, x, (ushort)(y - 1), z);
            ActivateTDoor(lvl, x, (ushort)(y + 1), z);
            ActivateTDoor(lvl, x, y, (ushort)(z - 1));
            ActivateTDoor(lvl, x, y, (ushort)(z + 1));
        }

        public static void ActivateTDoor(Level lvl, ushort x, ushort y, ushort z)
        {
            ushort block = lvl.GetBlock(x, y, z, out int index);

            if (lvl.Props[block].IsTDoor)
            {
                PhysicsArgs args = ActivateablePhysics.GetTDoorArgs(block);
                lvl.AddUpdate(index, Block.Air, args);
            }
        }
    }
}