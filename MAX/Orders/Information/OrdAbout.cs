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
using MAX.DB;
using MAX.Maths;
using MAX.SQL;
using System;
using System.Collections.Generic;


namespace MAX.Orders.Info
{
    public class OrdAbout : Order
    {
        public override string Name { get { return "About"; } }
        public override string Shortcut { get { return "b"; } }
        public override string Type { get { return OrderTypes.Information; } }
        public override bool MuseumUsable { get { return false; } }
        public override bool SuperUseable { get { return false; } }
        public override OrderDesignation[] Designations
        {
            get { return new[] { new OrderDesignation("BInfo"), new OrderDesignation("WhoDid") }; }
        }
        public override OrderPerm[] ExtraPerms
        {
            get { return new[] { new OrderPerm(LevelPermission.AdvBuilder, "can see portal/MB data of a block") }; }
        }

        public override void Use(Player p, string message, OrderData data)
        {
            p.Message("Break/Build a block to display information.");
            p.MakeSelection(1, "Selecting location for &SBlock info", data, PlacedMark);
        }

        public bool PlacedMark(Player p, Vec3S32[] marks, object state, ushort block)
        {
            ushort x = (ushort)marks[0].X, y = (ushort)marks[0].Y, z = (ushort)marks[0].Z;
            block = p.level.GetBlock(x, y, z);
            p.RevertBlock(x, y, z);
            Dictionary<int, string> names = new Dictionary<int, string>();

            p.Message("Retrieving block change records..");

            bool foundAny = false;
            ListFromDatabase(p, ref foundAny, x, y, z);
            using (IDisposable rLock = p.level.BlockDB.Locker.AccquireRead(30 * 1000))
            {
                if (rLock != null)
                {
                    p.level.BlockDB.FindChangesAt(x, y, z,
                                                  entry => OutputEntry(p, ref foundAny, names, entry));
                }
                else
                {
                    p.Message("&WUnable to accquire read lock on BlockDB after 30 seconds, aborting.");
                    return false;
                }
            }

            if (!foundAny) p.Message("No block change records found for this block.");
            ushort raw = Block.IsPhysicsType(block) ? block : Block.ToRaw(block);
            string blockName = Block.GetName(p, block);
            p.Message("Block ({0}, {1}, {2}): &f{3} = {4}&S.", x, y, z, raw, blockName);

            OrderData data = (OrderData)state;
            if (HasExtraPerm(data.Rank, 1))
            {
                BlockDBChange.OutputMessageBlock(p, block, x, y, z);
                BlockDBChange.OutputPortal(p, block, x, y, z);
            }
            Server.DoGC();
            return true;
        }

        public static void ListFromDatabase(Player p, ref bool foundAny, ushort x, ushort y, ushort z)
        {
            if (!Database.TableExists("Block" + p.level.name)) return;

            List<string[]> entries = Database.GetRows("Block" + p.level.name, "Username,TimePerformed,Deleted,Type",
                                                      "WHERE X=@0 AND Y=@1 AND Z=@2", x, y, z);

            if (entries.Count > 0) foundAny = true;
            BlockDBEntry entry = default;
            entry.OldRaw = Block.Invalid;

            foreach (string[] row in entries)
            {
                DateTime time = Database.ParseDBDate(row[1]).ToUniversalTime();
                TimeSpan delta = time - BlockDB.Epoch;
                entry.TimeDelta = (int)delta.TotalSeconds;
                entry.Flags = BlockDBFlags.ManualPlace;

                byte flags = ParseFlags(row[2]);
                if ((flags & 1) == 0)
                { // block was placed
                    entry.NewRaw = byte.Parse(row[3]);
                    if ((flags & 2) != 0) entry.Flags |= BlockDBFlags.NewExtended;
                }
                BlockDBChange.Output(p, row[0], entry);
            }
        }

        public static byte ParseFlags(string value)
        {
            // This used to be a 'deleted' boolean, so we need to make sure we account for that
            if (value.CaselessEq("true")) return 1;
            if (value.CaselessEq("false")) return 0;
            return byte.Parse(value);
        }

        public static void OutputEntry(Player p, ref bool foundAny, Dictionary<int, string> names, BlockDBEntry entry)
        {
            if (!names.TryGetValue(entry.PlayerID, out string name))
            {
                name = NameConverter.FindName(entry.PlayerID);
                names[entry.PlayerID] = name;
            }
            foundAny = true;
            BlockDBChange.Output(p, name, entry);
        }

        public override void Help(Player p)
        {
            p.Message("&T/About");
            p.Message("&HOutputs the change/edit history for a block.");
        }
    }
}