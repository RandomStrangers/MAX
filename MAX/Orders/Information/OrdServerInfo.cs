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
using MAX.Platform;
using MAX.SQL;
using System;
using System.Diagnostics;
using System.Threading;

namespace MAX.Orders.Info
{
    public class OrdServerInfo : Order
    {
        public override string Name { get { return "ServerInfo"; } }
        public override string Shortcut { get { return "SInfo"; } }
        public override string Type { get { return OrderTypes.Information; } }
        public override bool UseableWhenJailed { get { return true; } }
        public override OrderDesignation[] Designations
        {
            get { return new[] { new OrderDesignation("Host"), new OrderDesignation("ZAll") }; }
        }
        public override OrderPerm[] ExtraPerms
        {
            get { return new[] { new OrderPerm(LevelPermission.Admin, "can see server CPU and memory usage") }; }
        }
        public string UpdateIntervalMessage()
        {
            if (Server.Config.PositionUpdateInterval <= 0)
            {
                return "  Player positions are updated &aconstantly&S.";
            }
            else
            {
                return "  Player positions are updated &a" +
                    (1000 / Server.Config.PositionUpdateInterval) + " &Stimes/second.";
            }
        }
        public override void Use(Player p, string message, OrderData data)
        {
            int count = Database.CountRows("Players");
            p.Message("About &b{0}&S", Server.Config.Name);
            p.Message("  &a{0} &Splayers total. (&a{1} &Sonline, &8{2} banned&S)",
                      count, PlayerInfo.GetOnlineCanSee(p, data.Rank).Count, Group.BannedRank.Players.Count);
            p.Message("  &a{0} &Slevels total (&a{1} &Sloaded). Currency is &3{2}&S.",
                      LevelInfo.AllMapFiles().Length, LevelInfo.Loaded.Count, Server.Config.Currency);

            TimeSpan up = DateTime.UtcNow - Server.StartTime;
            p.Message("  Been up for &a{0}&S, running &b{1} &a{2} &f" + Updater.SourceURL,
                      up.Shorten(true), Server.SoftwareName, Server.Version);

            p.Message(UpdateIntervalMessage());

            string owner = Server.Config.OwnerName;
            if (!owner.CaselessEq("Notch") && !owner.CaselessEq("the owner"))
            {
                p.Message("  Owner is &3{0}", owner);
            }

            if (HasExtraPerm(data.Rank, 1)) OutputResourceUsage(p);
        }

        public static DateTime startTime;
        public static ProcInfo startUsg;

        public static void OutputResourceUsage(Player p)
        {
            Process proc = Process.GetCurrentProcess();
            p.Message("Measuring resource usage...one second");
            IOperatingSystem os = IOperatingSystem.DetectOS();

            if (startTime == default)
            {
                startTime = DateTime.UtcNow;
                startUsg = os.MeasureResourceUsage(proc, false);
            }

            CPUTime allBeg = os.MeasureAllCPUTime();
            ProcInfo begUsg = os.MeasureResourceUsage(proc, false);

            // measure CPU usage over one second
            Thread.Sleep(1000);
            ProcInfo endUsg = os.MeasureResourceUsage(proc, true);
            CPUTime allEnd = os.MeasureAllCPUTime();
            p.Message("&a{0}% &SCPU usage now, &a{1}% &Soverall",
                MeasureCPU(begUsg.ProcessorTime, endUsg.ProcessorTime, TimeSpan.FromSeconds(1)),
                MeasureCPU(startUsg.ProcessorTime, endUsg.ProcessorTime, DateTime.UtcNow - startTime));

            ulong idl = allEnd.IdleTime - allBeg.IdleTime;
            ulong sys = allEnd.ProcessorTime - allBeg.ProcessorTime;
            double cpu = sys * 100.0 / (sys + idl);
            int cores = Environment.ProcessorCount;
            p.Message("  &a{0}% &Sby all processes across {1} CPU core{2}",
                double.IsNaN(cpu) ? "(unknown)" : cpu.ToString("F2"),
                cores, cores.Plural());

            // Private Bytes = memory the process has reserved just for itself
            int memory = (int)Math.Round(endUsg.PrivateMemorySize / 1048576.0);
            p.Message("&a{0} &Sthreads, using &a{1} &Smegabytes of memory",
                endUsg.NumThreads, memory);
        }

        public static string MeasureCPU(TimeSpan beg, TimeSpan end, TimeSpan interval)
        {
            //if (end < beg) return "0.00"; // TODO: Can this ever happen
            int cores = Math.Max(1, Environment.ProcessorCount);

            TimeSpan used = end - beg;
            double elapsed = 100.0 * (used.TotalSeconds / interval.TotalSeconds);
            return (elapsed / cores).ToString("F2");
        }

        public override void Help(Player p)
        {
            p.Message("&T/ServerInfo");
            p.Message("&HDisplays the server information.");
        }
    }
}