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
using System;
using System.IO;
using System.Net;
using MAX.Network;
using MAX.Tasks;

namespace MAX
{
    /// <summary> Checks for and applies software updates. </summary>
    public static class Updater
    {

        public static string SourceURL = "https://github.com/RandomStrangers/MAX";
        public const string UploadsURL = "https://github.com/RandomStrangers/MAX/tree/master/Uploads";
        public static string WikiURL = "https://github.com/ClassiCube/MCGalaxy";
        public const string CurrentVersionURL = "https://github.com/RandomStrangers/MAX/raw/master/Uploads/current.txt";
#if !MAX_DOTNET
        public const string UpdatesURL = "https://github.com/RandomStrangers/MAX/raw/master/Uploads/";
        public const string URL = UpdatesURL + "MAX.exe";
#else
        public const string UpdatesURL = "https://github.com/RandomStrangers/MAX/raw/master/Uploads/dotnet/";
        public const string URL = UpdatesURL + "MAX";
        public static string[] Dependencies = new string[] {
            "MAX.dll", "MySql.Data.dll", "SixLabors.ImageSharp.dll",
            "System.Configuration.ConfigurationManager.dll", "System.Drawing.Common.dll",
            "System.IO.Pipelines.dll", "System.Security.Cryptography.ProtectedData.dll",
            "System.Security.Permissions.dll", "System.Windows.Extensions.dll",
            "BouncyCastle.Crypto.dll", "Google.Protobuf.dll", "K4os.Compression.LZ4.dll",
            "K4os.Compression.LZ4.Streams.dll", "K4os.Hash.xxHash.dll", "ZstdSharp.dll",
            "MAX.deps.json", "MAX.runtimeconfig.json"
        };
        public static string UnixLib = "System.Drawing.Common.dll";
        public static string[] WinCoreLibs = new string[]
        {
            "Microsoft.Win32.SystemEvents.dll", "System.Drawing.Common.dll", "System.Windows.Extensions.dll",
        };
        public static string WinStandardLib = "System.Security.Crytography.ProtectedData.dll";

        public static string[] Win64Libs = new string[]
        {
            "comerr64.dll", "gssapi64.dll", "k5sprt64.dll", "krb5_64.dll","krbcc64.dll"
        };
        public static string[] RuntimeDependencies = new string[]
        {
            "unix/lib/System.Drawing.Common.dll",
            "win/lib/netcoreapp3.0/Microsoft.Win32.SystemEvents.dll",
            "win/lib/netcoreapp3.0/System.Drawing.Common.dll",
            "win/lib/netcoreapp3.0/System.Windows.Extensions.dll",
            "win/lib/netstandard2.0/System.Security.Crytography.ProtectedData.dll",
            "win-x64/native/comerr64.dll",
            "win-x64/native/gssapi64.dll",
            "win-x64/native/k5sprt64.dll",
            "win-x64/native/krb5_64.dll",
            "win-x64/native/krbcc64.dll"
        };
#endif

        public static event EventHandler NewerVersionDetected;

        public static void UpdaterTask(SchedulerTask task)
        {
            UpdateCheck();
            task.Delay = TimeSpan.FromHours(2);
        }

        public static void UpdateCheck()
        {
            if (!Server.Config.CheckForUpdates) return;
            WebClient client = HttpUtil.CreateWebClient();

            try
            {
                string latest = client.DownloadString(CurrentVersionURL);

                if (new Version(Server.Version) >= new Version(latest))
                {
                    Logger.Log(LogType.SystemActivity, "No update found!");
                }
                else if (NewerVersionDetected != null)
                {
                    NewerVersionDetected(null, EventArgs.Empty);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError("Error checking for updates", ex);
            }

            client.Dispose();
        }

        public static void PerformUpdate()
        {
            try
            {
                try
                {
                    DeleteFiles("MAX.update", "prev_MAX.exe");
#if MAX_DOTNET
                    DeleteFiles("runtimes/unix/lib/netcoreapp3.0/prev_System.Drawing.Common.dll",
                    "runtimes/unix/lib/netcoreapp3.0/System.Drawing.Common.dll.update",
                    "runtimes/win/lib/netstandard2.0/prev_System.Security.Cryptography.ProtectedData.dll",
                    "runtimes/win/lib/netstandard2.0/System.Security.Cryptography.ProtectedData.dll.update");
                    foreach (string wincorelib in WinCoreLibs)
                    {
                        DeleteFiles("runtimes/win/lib/netcoreapp3.0/prev_" + wincorelib,
                        "runtimes/win/lib/netcoreapp3.0/" + wincorelib + ".update");
                    }
                    foreach (string Win64Lib in Win64Libs)
                    {
                        DeleteFiles("runtimes/win-x64/native/prev_" + Win64Lib,
                        "runtimes/win-x64/native/" + Win64Lib + ".update");
                    }
                    foreach (string dependency in Dependencies)
                    {
                        DeleteFiles("prev_" + dependency, dependency + ".update");
                    }
#endif
                }
                catch
                {
                }

                WebClient client = HttpUtil.CreateWebClient();
                client.DownloadFile(URL, "MAX.update");
#if MAX_DOTNET
                foreach (string dep in Dependencies)
                {
                    client.DownloadFile(UpdatesURL + dep, dep + ".update");
                }
                client.DownloadFile(UpdatesURL + "runtimes/unix/lib/netcoreapp3.0/System.Drawing.Common.dll",
                  "runtimes/unix/lib/netcoreapp3.0/System.Drawing.Common.dll.update");
                client.DownloadFile(UpdatesURL + "runtimes/win/lib/netstandard2.0/System.Security.Cryptography.ProtectedData.dll",
                  "runtimes/win/lib/netstandard2.0/System.Security.Cryptography.ProtectedData.dll.update");
                foreach (string wincore in WinCoreLibs)
                {
                    client.DownloadFile(UpdatesURL + "runtimes/win/lib/netcoreapp3.0/" + wincore,
                    "runtimes/win/lib/netcoreapp3.0/" + wincore + ".update");
                }
                foreach (string Win64 in Win64Libs)
                {
                    client.DownloadFile(UpdatesURL + "runtimes/win-x64/native/" + Win64,
                    "runtimes/win-x64/native/" + Win64 + ".update");
                }
#endif
                Level[] levels = LevelInfo.Loaded.Items;
                foreach (Level lvl in levels)
                {
                    if (!lvl.SaveChanges) continue;
                    lvl.Save();
                    lvl.SaveBlockDBChanges();
                }

                Player[] players = PlayerInfo.Online.Items;
                foreach (Player pl in players) pl.SaveStats();
                string serverTLI = Server.GetServerExePath();
                AtomicIO.TryMove(serverTLI, "prev_MAX.exe");
                AtomicIO.TryMove("MAX.update", serverTLI);
#if MAX_DOTNET
                foreach (string d in Dependencies)
                {
                    AtomicIO.TryMove(d + ".update", d);
                }
                AtomicIO.TryMove("/runtimes/unix/lib/netcoreapp3.0/System.Drawing.Common.dll.update",
                "runtimes/unix/lib/netcoreapp3.0/System.Drawing.Common.dll");
                AtomicIO.TryMove("/runtimes/win/lib/netstandard2.0/System.Security.Cryptography.ProtectedData.dll.update",
                "/runtimes/win/lib/netstandard2.0/System.Security.Cryptography.ProtectedData.dll");
                foreach (string wincorelib in WinCoreLibs)
                {
                    AtomicIO.TryMove("/runtimes/win/lib/netcoreapp3.0/" + wincorelib + ".update",
                    "/runtimes/win/lib/netcoreapp3.0/" + wincorelib);
                }
                foreach (string Win64Lib in Win64Libs)
                {
                    AtomicIO.TryMove("/runtimes/win-x64/native/" + Win64Lib + ".update",
                    "/runtimes/win-x64/native/" + Win64Lib);
                }
#endif
                Server.Stop(true, "Updating server.");
            }
            catch (Exception ex)
            {
                Logger.LogError("Error performing update", ex);
            }
        }
        public static void DeleteFiles(params string[] paths)
        {
            foreach (string path in paths) { AtomicIO.TryDelete(path); }
        }
    }
}