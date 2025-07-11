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
using MAX.Drawing.Ops;
using MAX.Levels.IO;
using MAX.Maths;
using System.IO;


namespace MAX.Orders.Moderation
{
    public class OrdRestoreSelection : Order
    {
        public override string Name { get { return "RS"; } }
        public override string Shortcut { get { return "RestoreSelection"; } }
        public override string Type { get { return OrderTypes.Moderation; } }
        public override bool MuseumUsable { get { return false; } }
        public override LevelPermission DefaultRank { get { return LevelPermission.Operator; } }

        public override void Use(Player p, string message, OrderData data)
        {
            if (message.Length == 0) { Help(p); return; }
            if (!Formatter.ValidMapName(p, message)) return;

            string path = LevelInfo.BackupFilePath(p.level.name, message);
            if (File.Exists(path))
            {
                p.Message("Select two corners for restore.");
                p.MakeSelection(2, "Selecting region for &SRestore", path, DoRestore);
            }
            else
            {
                p.Message("Backup {0} does not exist.", message);
                LevelOperations.OutputBackups(p, p.level);
            }
        }

        public bool DoRestore(Player p, Vec3S32[] marks, object state, ushort block)
        {
            string path = (string)state;
            Level source = IMapImporter.Decode(path, "templevel", false);

            RestoreSelectionDrawOp op = new RestoreSelectionDrawOp
            {
                Source = source
            };
            if (DrawOpPerformer.Do(op, null, p, marks)) return false;

            // Not high enough draw limit
            source.Dispose();
            return false;
        }

        public override void Help(Player p)
        {
            p.Message("&T/RestoreSelection [backup name]");
            p.Message("&HRestores a previous backup of the current selection");
        }
    }
}