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
using MAX.Core;
using MAX.Modules.Moderation.Notes;
using MAX.Modules.Security;
using MAX.Relay.Discord;
using MAX.Relay.IRC;
using MAX.Scripting;
using System;
using System.Collections.Generic;

namespace MAX
{
    /// <summary> This class provides for more advanced modification to MAX </summary>
    public abstract partial class Addon
    {
        /// <summary> Hooks into events and initalises states/resources etc </summary>
        /// <param name="auto"> True if addon is being automatically loaded (e.g. on server startup), false if manually. </param>
        public virtual void Load(bool auto) { }

        /// <summary> Unhooks from events and disposes of state/resources etc </summary>
        /// <param name="auto"> True if addon is being auto unloaded (e.g. on server shutdown), false if manually. </param>
        public virtual void Unload(bool auto) { }

        /// <summary> Called when a player does /Help on the addon. Typically tells the player what this addon is about. </summary>
        /// <param name="p"> Player who is doing /Help. </param>
        public virtual void Help(Player p)
        {
            p.Message("No help is available for this addon.");
        }

        /// <summary> Name of the addon. </summary>
        public abstract string Name { get; }
        /// <summary> The oldest version of MAX this addon is compatible with. </summary>
        public virtual string MAX_Version { get { return Server.InternalVersion; } }
        /// <summary> Version of this addon. </summary>
        public virtual int Build { get { return 0; } }
        /// <summary> Message to display once this addon is loaded. </summary>
        public virtual string Welcome { get { return ""; } }
        /// <summary> The creator/author of this addon. (Your name) </summary>
        public virtual string Creator { get { return ""; } }
        /// <summary> Whether or not to auto load this addon on server startup. </summary>
        public virtual bool LoadAtStartup { get { return true; } }


        /// <summary> List of addon/modules included in the server software </summary>
        public static List<Addon> core = new List<Addon>();
        public static List<Addon> custom = new List<Addon>();

        public static Addon FindCustom(string name)
        {
            foreach (Addon a in custom)
            {
                if (a.Name.CaselessEq(name)) return a;
            }
            return null;
        }


        public static void Load(Addon a, bool auto)
        {
            string ver = a.MAX_Version;
            if (!string.IsNullOrEmpty(ver) && new Version(ver) > new Version(Server.InternalVersion))
            {
                string msg = string.Format("Addon '{0}' requires a more recent version of {1}!", a.Name, Server.SoftwareNameConst);
                throw new InvalidOperationException(msg);
            }

            try
            {
                custom.Add(a);

                if (a.LoadAtStartup || !auto)
                {
                    a.Load(auto);
                    Logger.Log(LogType.SystemActivity, "Addon {0} loaded...build: {1}", a.Name, a.Build);
                }
                else
                {
                    Logger.Log(LogType.SystemActivity, "Addon {0} was not loaded, you can load it with /aload", a.Name);
                }

                if (!string.IsNullOrEmpty(a.Welcome)) Logger.Log(LogType.SystemActivity, a.Welcome);
            }
            catch
            {
                if (!string.IsNullOrEmpty(a.Creator)) Logger.Log(LogType.Warning, "You can go bug {0} about {1} failing to load.", a.Creator, a.Name);
                throw;
            }
        }

        public static bool Unload(Addon a)
        {
            bool success = UnloadAddon(a, false);
            if (success)
            {
                custom.Remove(a);
                core.Remove(a);
            }
            return success;
        }

        public static bool UnloadAddon(Addon a, bool auto)
        {
            try
            {
                a.Unload(auto);
                return true;
            }
            catch (Exception ex)
            {
                Logger.LogError("Error unloading addon " + a.Name, ex);
                return false;
            }
        }


        public static void UnloadAll()
        {
            for (int i = 0; i < custom.Count; i++)
            {
                UnloadAddon(custom[i], true);
            }
            custom.Clear();

            for (int i = 0; i < core.Count; i++)
            {
                UnloadAddon(core[i], true);
            }
        }

        public static void LoadAll()
        {
            LoadCoreAddon(new Compiling.CompilerAddon());
            LoadCoreAddon(new CoreAddon());
            LoadCoreAddon(new ServerURLSender());
            LoadCoreAddon(new NotesAddon());
            LoadCoreAddon(new DiscordAddon());
            LoadCoreAddon(new IRCAddon());
            LoadCoreAddon(new IPThrottler());
            IScripting.AutoloadAddons();
        }

        public static void LoadCoreAddon(Addon addon)
        {
            List<string> disabled = Server.Config.DisabledModules;
            if (disabled.CaselessContains(addon.Name)) return;

            addon.Load(true);
            core.Add(addon);
        }
    }
}
