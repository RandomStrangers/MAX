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
using MAX.Authentication;
using MAX.Blocks;
using MAX.DB;
using MAX.Drawing;
using MAX.Eco;
using MAX.Events.LevelEvents;
using MAX.Events.ServerEvents;
using MAX.Games;
using MAX.Modules.Awards;
using MAX.Network;
using MAX.Orders;
using MAX.Platform;
using MAX.Scripting;
using MAX.SQL;
using MAX.Tasks;
using MAX.Util;
using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Threading;

namespace MAX
{
    public partial class Server
    {
        public static void StartThread(out Thread thread, string name, ThreadStart threadFunc)
        {
            thread = new Thread(threadFunc);

            try
            {
                thread.Name = name;
            }
            catch (Exception e)
            {
                Logger.LogError(e);
            }
            thread.Start();
        }
        //True = cancel event
        //False = don't cancel event
        public static bool Check(string ord, string message)
        {
            MAXOrder?.Invoke(ord, message);
            return cancelorder;
        }

        public static void CheckFile(string file)
        {
            if (File.Exists(file))
            {
                return;
            }
            Logger.Log(LogType.SystemActivity, file + " doesn't exist, Downloading..");
            try
            {
                using (WebClient client = HttpUtil.CreateWebClient())
                {
                    client.DownloadFile(Updater.UpdatesURL + file, file);
                }
                if (File.Exists(file))
                {
                    Logger.Log(LogType.SystemActivity, file + " download succesful!");
                }
            }
            catch (Exception ex)
            {
                Logger.LogError("Downloading " + file + " failed, try again later", ex);
            }
        }

        public static ConfigElement[] serverConfig, levelConfig, zoneConfig;
        public static void Start()
        {
            serverConfig = ConfigElement.GetAll(typeof(ServerConfig));
            levelConfig = ConfigElement.GetAll(typeof(LevelConfig));
            zoneConfig = ConfigElement.GetAll(typeof(ZoneConfig));

            IOperatingSystem.DetectOS().Init();

            StartTime = DateTime.UtcNow;
            Logger.Log(LogType.SystemActivity, "Starting Server");
            ServicePointManager.Expect100Continue = false;
            ForceEnableTLS();

            SQLiteBackend.Instance.LoadDependencies();

            EnsureFilesExist();
            IScripting.Init();

            LoadAllSettings(true);
            InitDatabase();
            Economy.LoadDatabase();

            Background.QueueOnce(LoadMainLevel);
            Background.QueueOnce(LoadAllAddons);
            Background.QueueOnce(LoadAutoloadMaps);
            Background.QueueOnce(UpgradeTasks.UpgradeOldTempranks);
            Background.QueueOnce(UpgradeTasks.UpgradeDBTimeSpent);
            Background.QueueOnce(InitPlayerLists);
            Background.QueueOnce(Pronouns.Init);

            Background.QueueOnce(SetupSocket);
            Background.QueueOnce(InitTimers);
            Background.QueueOnce(InitRest);
            Background.QueueOnce(InitHeartbeat);
            ServerTasks.QueueTasks();
            Background.QueueRepeat(ThreadSafeCache.DBCache.CleanupTask,
                                   null, TimeSpan.FromMinutes(5));

        }

        public static void EnsureDirectoryDoesNotExist(string dir)
        {
            if (Directory.Exists(dir))
            {
                foreach (string file in Directory.GetFiles(dir))
                {
                    FileIO.TryDelete(file);
                }
                Directory.Delete(dir, true);
            }
        }
        public static void ForceEnableTLS()
        {
            // Force enable TLS 1.1/1.2, otherwise checking for updates on Github doesn't work
            try
            {
                ServicePointManager.SecurityProtocol |= (SecurityProtocolType)0x300;
            }
            catch
            {
            }
            try
            {
                ServicePointManager.SecurityProtocol |= (SecurityProtocolType)0xC00;
            }
            catch
            {
            }
        }
        public static void EnsureFilesExist()
        {
            EnsureDirectoryExists("props");
            EnsureDirectoryExists("levels");
            EnsureDirectoryExists("bots");
            EnsureDirectoryExists("text");
            EnsureDirectoryExists("ranks");
            RankInfo.EnsureExists();
            Ban.EnsureExists();
            PlayerDB.EnsureDirectoriesExist();

            EnsureDirectoryExists("extra");
            EnsureDirectoryExists(Paths.WaypointsDir);
            EnsureDirectoryExists("extra/bots");
            EnsureDirectoryExists(Paths.ImportsDir);
            EnsureDirectoryExists("blockdefs");
            EnsureDirectoryDoesNotExist("properties");
        }

        public static void EnsureDirectoryExists(string dir)
        {
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
        }

        public static void LoadAllSettings()
        {
            LoadAllSettings(false);
        }

        // TODO rethink this
        public static void LoadAllSettings(bool orders)
        {
            Colors.Load();
            Designation.LoadCustom();
            BlockDefinition.LoadGlobal();
            ImagePalette.Load();
            SrvProperties.Load();
            if (orders) Order.InitAll();
            AuthService.ReloadDefault();
            Group.LoadAll();
            OrderPerms.Load();
            Block.SetBlocks();
            BlockPerms.Load();
            AwardsList.Load();
            PlayerAwards.Load();
            Economy.Load();
            WarpList.Global.Filename = "extra/warps.save";
            WarpList.Global.Load();
            OrderExtraPerms.Load();
            ProfanityFilter.Init();
            Team.LoadList();
            ChatTokens.LoadCustom();
            SrvProperties.FixupOldPerms();
            CpeExtension.LoadDisabledList();

            TextFile announcementsFile = TextFile.Files["Announcements"];
            announcementsFile.EnsureExists();
            announcements = announcementsFile.GetText();

            OnConfigUpdatedEvent.Call();
        }


        public static object stopLock = new object();
        public static volatile Thread stopThread;
        public static Thread Stop(bool restart, string msg)
        {
            Order.Find("say").Use(Player.MAX, "Goodbye Cruel World!");
            Logger.Log(LogType.Warning, "&4Server is shutting down!");
            shuttingDown = true;
            lock (stopLock)
            {
                if (stopThread != null)
                {
                    return stopThread;
                }
                stopThread = new Thread(() => ShutdownThread(restart, msg));
                stopThread.Start();
                return stopThread;
            }
        }

        public static void ShutdownThread(bool restarting, string msg)
        {
            try
            {
                Logger.Log(LogType.SystemActivity, "Server shutting down ({0})", msg);
            }
            catch (Exception e)
            {
                Logger.LogError(e);
            }

            // Stop accepting new connections and disconnect existing sessions
            Listener.Close();
            try
            {
                Player[] players = PlayerInfo.Online.Items;
                foreach (Player p in players)
                {
                    p.Leave(msg);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex);
            }

            byte[] kick = Packet.Kick(msg, false);
            try
            {
                INetSocket[] pending = INetSocket.pending.Items;
                foreach (INetSocket p in pending)
                {
                    p.Send(kick, SendFlags.None);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex);
            }

            OnShuttingDownEvent.Call(restarting, msg);
            Addon.UnloadAll();


            try
            {
                string autoload = SaveAllLevels();
                if (SetupFinished && !Config.AutoLoadMaps)
                {
                    File.WriteAllText("text/autoload.txt", autoload);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex);
            }

            try
            {
                Logger.Log(LogType.SystemActivity, "Server shutdown completed");
            }
            catch (Exception e)
            {
                Logger.LogError(e);
            }

            try
            {
                FileLogger.Flush(null);
            }
            catch (Exception e)
            {
                Logger.LogError(e);
            }

            if (restarting)
            {
                IOperatingSystem.DetectOS().RestartProcess();
                // TODO: FileLogger.Flush again maybe for if execvp fails?
            }
            Environment.Exit(0);
        }

        public static string SaveAllLevels()
        {
            string autoload = null;
            Level[] loaded = LevelInfo.Loaded.Items;

            foreach (Level lvl in loaded)
            {
                if (!lvl.SaveChanges)
                {
                    Logger.Log(LogType.SystemActivity, "Skipping save for level {0}", lvl.ColoredName);
                    continue;
                }

                autoload = autoload + lvl.name + "=" + lvl.Physics + Environment.NewLine;
                lvl.Save();
                lvl.SaveBlockDBChanges();
            }
            return autoload;
        }


        public static string GetServerDLLPath()
        {
            return GetServerExePath();
        }

        public static string GetRestartPath()
        {
            return RestartPath;
        }
        /// <summary> Returns the full path to the server executable </summary>
        public static string GetServerExePath()
        {
            return RestartPath;
        }
        public static bool checkedOnMono, runningOnMono;
        public static bool RunningOnMono()
        {
            if (!checkedOnMono)
            {
                runningOnMono = Type.GetType("Mono.Runtime") != null;
                checkedOnMono = true;
            }
            return runningOnMono;
        }


        public static void UpdateUrl(string url)
        {
            OnURLChange?.Invoke(url);
        }

        public static void RandomMessage(SchedulerTask task)
        {
            if (PlayerInfo.Online.Count > 0 && announcements.Length > 0)
            {
                Chat.MessageGlobal(announcements[new Random().Next(0, announcements.Length)]);
            }
        }

        public static void SettingsUpdate()
        {
            OnSettingsUpdate?.Invoke();
        }

        public static bool SetMainLevel(string map)
        {
            OnMainLevelChangingEvent.Call(ref map);
            string main = mainLevel != null ? mainLevel.name : Config.MainLevel;
            if (map.CaselessEq(main))
            {
                return false;
            }
            Level lvl = LevelInfo.FindExact(map) ?? LevelActions.Load(Player.MAX, map, false);
            if (lvl == null)
            {
                return false;
            }
            SetMainLevel(lvl);
            return true;
        }

        public static void SetMainLevel(Level lvl)
        {
            Level oldMain = mainLevel;
            mainLevel = lvl;
            oldMain.AutoUnload();
        }

        public static void DoGC()
        {
            Stopwatch sw = Stopwatch.StartNew();
            long start = GC.GetTotalMemory(false);
            GC.Collect();
            GC.WaitForPendingFinalizers();

            long end = GC.GetTotalMemory(false);
            double deltaKB = (start - end) / 1024.0;
            if (deltaKB < 100.0)
            {
                return;
            }
            Logger.Log(LogType.BackgroundActivity, "GC performed in {0:F2} ms (tracking {1:F2} KB, freed {2:F2} KB)",
                       sw.Elapsed.TotalMilliseconds, end / 1024.0, deltaKB);
        }


        // only want ASCII alphanumerical characters for salt
        public static bool AcceptableSaltChar(char c)
        {
            return (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z')
                || (c >= '0' && c <= '9');
        }

        /// <summary> Generates a random salt that is used for calculating mppasses. </summary>
        public static string GenerateSalt()
        {
            RandomNumberGenerator rng = RandomNumberGenerator.Create();
            char[] str = new char[32];
            byte[] one = new byte[1];

            for (ulong i = 0; i < (ulong)str.Length;)
            {
                rng.GetBytes(one);
                if (!AcceptableSaltChar((char)one[0]))
                {
                    continue;
                }
                str[i] = (char)one[0]; i++;
            }
            return new string(str);
        }

        public static System.Text.ASCIIEncoding enc = new System.Text.ASCIIEncoding();
        public static MD5CryptoServiceProvider md5 = new MD5CryptoServiceProvider();
        public static object md5Lock = new object();

        /// <summary> Calculates mppass (verification token) for the given username. </summary>
        public static string CalcMppass(string name, string salt)
        {
            byte[] hash = null;
            lock (md5Lock)
            {
                hash = md5.ComputeHash(enc.GetBytes(salt + name));
            }
            return Utils.ToHexString(hash);
        }

        /// <summary> Converts a formatted username into its original username </summary>
        /// <remarks> If ClassiCubeAccountPlus option is set, removes + </remarks>
        public static string ToRawUsername(string name)
        {
            if (Config.ClassicubeAccountPlus)
            {
                return name.Replace("+", "");
            }
            return name;
        }

        /// <summary> Converts a username into its formatted username </summary>
        /// <remarks> If ClassiCubeAccountPlus option is set, adds trailing + </remarks>
        public static string FromRawUsername(string name)
        {
            if (!Config.ClassicubeAccountPlus)
            {
                return name;
            }
            // NOTE:
            // This is technically incorrect when the server has both
            //   classicube-account-plus enabled and is using authentication service suffixes
            // (e.g. ToRawUsername("Test+$") ==> "Test$", so adding + to end is wrong)
            // But since that is an unsupported combination to run the server in anyways,
            //  I decided that it is not worth complicating the implementation for
            if (!name.Contains("+"))
            {
                name += "+";
            }
            return name;
        }
    }
}