/*
    Copyright 2010 MCSharp team (Modified for use with MCZall/MCLawl/MCGalaxy)
    
    Dual-licensed under the Educational Community License, Version 2.0 and
    the GNU General Public License, Version 3 (the "Licenses"); you may
    not use this file except in compliance with the Licenses. You may
    obtain a copy of the Licenses at
    
    http://www.opensource.org/licenses/ecl2.php
    http://www.gnu.org/licenses/gpl-3.0.html
    
    Unless required by applicable law or agreed to in writing,
    software distributed under the Licenses are distributed on an "AS IS"
    BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express
    or implied. See the Licenses for the specific language governing
    permissions and limitations under the Licenses.
*/
namespace MAX
{
    public abstract class Addon_Simple : Addon
    {
        public abstract override void Load(bool auto);
        public abstract override void Unload(bool auto);
        public override void Help(Player p)
        {
            p.Message("No help is available for this simple addon.");
        }
        public abstract override string name { get; }
        public override string MAX_Version { get { return Server.Version; } }
        public virtual string Creator { get { return ""; } }
        public override string creator { get { return Creator; } }
        public override string welcome { get { return ""; } }
        public override int build { get { return 1; } }
        public override bool LoadAtStartup { get { return true; } }
    }
}