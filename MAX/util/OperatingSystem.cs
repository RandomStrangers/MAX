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
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace MAX.Platform
{
    /// <summary> Summarises resource usage of all CPU cores in the system </summary>
    public struct CPUTime
    {
        /// <summary> Total time spent being idle / not executing code </summary>
        public ulong IdleTime;
        /// <summary> Total time spent executing code in Kernel mode </summary>
        public ulong KernelTime;
        /// <summary> Total time spent executing code in User mode </summary>
        public ulong UserTime;

        /// <summary> Total time spent executing code </summary>
        public ulong ProcessorTime { get { return KernelTime + UserTime; } }
    }

    /// <summary> Summarises resource usage of current process </summary>
    public struct ProcInfo
    {
        public TimeSpan ProcessorTime;
        public long PrivateMemorySize;
        public int NumThreads;
    }

    public class IOperatingSystem
    {
        /// <summary> Whether the operating system currently being run on is Windows </summary>
        public virtual bool IsWindows { get; }
        public virtual string StandaloneName { get { return "UNSUPPORTED"; } }

        public virtual void Init() { }

        /// <summary> Attempts to restart the current process </summary>
        /// <remarks> Does not return if the restart is performed in-place
        /// (since the current process image is replaced) </remarks>
        public virtual void RestartProcess()
        {
            Process.Start(Server.GetRestartPath());
        }


        /// <summary> Measures CPU use by all processes in the system </summary>
        public virtual CPUTime MeasureAllCPUTime()
        {
            CPUTime def = new CPUTime()
            {
                KernelTime = 1,
                IdleTime = 1,
                UserTime = 1,
            };
            return def;
        }

        /// <summary> Measures resource usage by the current process </summary>
        public virtual ProcInfo MeasureResourceUsage(Process proc, bool all)
        {
            ProcInfo info = default;

            info.ProcessorTime = proc.TotalProcessorTime;
            if (all)
            {
                info.PrivateMemorySize = proc.PrivateMemorySize64;
                info.NumThreads = proc.Threads.Count;
            }
            return info;
        }


        public static IOperatingSystem detectedOS;
        public static IOperatingSystem DetectOS()
        {
            detectedOS = detectedOS ?? DoDetectOS();
            return detectedOS;
        }
        public static bool IsWindowsPlatform(PlatformID platform)
        {
            switch (platform)
            {
                case PlatformID.Win32S:
                    return true;
                case PlatformID.WinCE:
                    return true;
                case PlatformID.Win32Windows:
                    return true;
                case PlatformID.Win32NT:
                    return true;
                case PlatformID.Xbox:
                    return true; // Xbox 360 is based on Windows 2000, so return true
                default:
                    return false;
            }
        }
        public static unsafe IOperatingSystem DoDetectOS()
        {
            PlatformID platform = Environment.OSVersion.Platform;
            IOperatingSystem winOS = new WindowsOS();
            IOperatingSystem unixOS = new UnixOS();
            IOperatingSystem linuxOS = new LinuxOS();
            IOperatingSystem mac = new MacOS();
            if (IsWindowsPlatform(platform))
            {
                return winOS;
            }
            else if (platform == PlatformID.MacOSX)
            {
                return new MacOS();
            }
            else
            {
                sbyte* utsname = stackalloc sbyte[8192];
                uname(utsname);
                string kernel = new string(utsname);
                if (kernel.CaselessContains("linux"))
                {
                    return linuxOS;
                }
                else if (kernel.CaselessContains("freeBSD"))
                {
                    return new FreeBSD_OS();
                }
                else if (kernel.CaselessContains("netBSD"))
                {
                    return new NetBSD_OS();
                }
                else if (kernel.CaselessContains("darwin"))
                {
                    return mac;
                }
                else
                {
                    return unixOS;
                }
            }
        }
        [DllImport("libc")]
        public static extern unsafe void uname(sbyte* uname_struct);
    }

    public class WindowsOS : IOperatingSystem
    {
        public override bool IsWindows { get { return true; } }
        public override CPUTime MeasureAllCPUTime()
        {
            CPUTime all = default;
            GetSystemTimes(out all.IdleTime, out all.KernelTime, out all.UserTime);
            // https://docs.microsoft.com/en-us/windows/win32/api/processthreadsapi/nf-processthreadsapi-getsystemtimes
            // lpKernelTime - "... This time value also includes the amount of time the system has been idle."
            all.KernelTime -= all.IdleTime;
            return all;
        }

        [DllImport("kernel32.dll")]
        public static extern int GetSystemTimes(out ulong idleTime, out ulong kernelTime, out ulong userTime);
    }

    public class UnixOS : IOperatingSystem
    {
        public override bool IsWindows { get { return false; } }

        public override void RestartProcess()
        {
            if (!Server.TLIMode) { base.RestartProcess(); return; }

            RestartInPlace();
            // If restarting in place fails, it's better to let the server die
            //  instead of allowing a new instance to be spun up which will
            //  be spammed with constant errors
        }

        public virtual void RestartInPlace()
        {
            // With using normal Process.Start with mono, after Environment.Exit
            //  is called, all FDs (including standard input) are also closed.
            // Unfortunately, this causes the new server process to constantly error with
            //   Type: IOException
            //   Message: Invalid handle to path "server_folder_path/[Unknown]"
            //   Trace:   at System.IO.FileStream.ReadData (System.Runtime.InteropServices.SafeHandle safeHandle, System.Byte[] buf, System.Int32 offset, System.Int32 count) [0x0002d]
            //     at System.IO.FileStream.ReadInternal (System.Byte[] dest, System.Int32 offset, System.Int32 count) [0x00026]
            //     at System.IO.FileStream.Read (System.Byte[] array, System.Int32 offset, System.Int32 count) [0x000a1] 
            //     at System.IO.StreamReader.ReadBuffer () [0x000b3]
            //     at System.IO.StreamReader.Read () [0x00028]
            //     at System.TermInfoDriver.GetCursorPosition () [0x0000d]
            //     at System.TermInfoDriver.ReadUntilConditionInternal (System.Boolean haltOnNewLine) [0x0000e]
            //     at System.TermInfoDriver.ReadLine () [0x00000]
            //     at System.ConsoleDriver.ReadLine () [0x00000]
            //     at System.Console.ReadLine () [0x00013]
            //     at MAX.TerminalLoop () [0x00002]
            // (this errors multiple times a second and can quickly fill up tons of disk space)
            // And also causes console to be spammed with '1R3;1R3;1R3;' or '363;1R;363;1R;'
            //
            // Note this issue does NOT happen with GUI mode for some reason - and also
            // don't want to use excevp in GUI mode, otherwise the X socket FDs pile up
            //
            //
            // a similar issue occurs with dotnet, but errors with this instead
            //  "IOException with 'I/O error' message
            //     ...
            //     at System.IO.StdInReader.ReadKey()

            // try to exec using actual runtime path first
            //   e.g. /usr/bin/mono-sgen, /home/test/.dotnet/dotnet
            string exe = GetProcessExePath();
            execvp(exe, new string[] { exe, Server.RestartPath, null });
            Console.WriteLine("execvp {0} failed: {1}", exe, Marshal.GetLastWin32Error());
        }

        [DllImport("libc", SetLastError = true)]
        public static extern int execvp(string path, string[] argv);

        public static string GetProcessExePath()
        {
            return Process.GetCurrentProcess().MainModule.FileName;
        }


        public override CPUTime MeasureAllCPUTime() { return default; }

        [DllImport("libc", SetLastError = true)]
        public static extern unsafe int sysctlbyname(string name, void* oldp, IntPtr* oldlenp, IntPtr newp, IntPtr newlen);
    }

    public class LinuxOS : UnixOS
    {
        public override string StandaloneName
        {
            get { return IntPtr.Size == 8 ? "nix64" : "nix32"; }
        }

        public override void Init()
        {
            base.Init();
        }


        // https://stackoverflow.com/questions/15145241/is-there-an-equivalent-to-the-windows-getsystemtimes-function-in-linux
        public override CPUTime MeasureAllCPUTime()
        {
            using (StreamReader r = new StreamReader("/proc/stat"))
            {
                string line = r.ReadLine();
                if (line.StartsWith("cpu ")) return ParseCpuLine(line);
            }

            return default;
        }
        public static bool AreAllTrue(List<bool> bools)
        {
            if (bools == null || !bools.Any())
            {
                return false;
            }
            return bools.All(Bool => EqualityComparer<bool>.Default.Equals(Bool, true));
        }
        public static CPUTime ParseCpuLine(string line)
        {
            // "cpu  [USER TIME] [NICE TIME] [SYSTEM TIME] [IDLE TIME] [I/O WAIT TIME] [IRQ TIME] [SW IRQ TIME]"
            line = line.Replace("  ", " ");
            string[] bits = line.SplitSpaces();
            List<bool> bools = new List<bool>()
            {
                ulong.TryParse(bits[1], out ulong user),
                ulong.TryParse(bits[2], out ulong nice),
                ulong.TryParse(bits[3], out ulong kern),
                ulong.TryParse(bits[4], out ulong idle),
            };
            CPUTime all;
            if (AreAllTrue(bools))
            {
                all = new CPUTime()
                {
                    UserTime = user + nice,
                    KernelTime = kern,
                    IdleTime = idle,
                };
            }
            else
            {
                all = new CPUTime()
                {
                    UserTime = 10,
                    KernelTime = 10,
                    IdleTime = 10,
                };
                Logger.Log(LogType.Warning, "Failed to parse CPU time!");
            }
            // TODO interrupt time too?
            return all;
        }

        public override void RestartInPlace()
        {
            try
            {
                // try to restart using process's original command line arguments so that they are preserved
                // e.g. for "mono --debug MAX.exe"
                string exe = GetProcessExePath();
                string[] args = GetProcessCommandLineArgs();
                execvp(exe, args);
            }
            catch (Exception ex)
            {
                Logger.LogError("Restarting process", ex);
            }

            base.RestartInPlace();
        }

        public static string[] GetProcessCommandLineArgs()
        {
            // /proc/self/cmdline returns the command line arguments
            //   of the process separated by NUL characters
            using (StreamReader r = new StreamReader("/proc/self/cmdline"))
            {
                string[] args = r.ReadToEnd().Split('\0');
                // last argument will be a 0 length string - replace with null for execvp
                args[args.Length - 1] = null;
                return args;
            }
        }
    }

    public class FreeBSD_OS : UnixOS
    {
        // https://stackoverflow.com/questions/5329149/using-system-calls-from-c-how-do-i-get-the-utilization-of-the-cpus
        public override unsafe CPUTime MeasureAllCPUTime()
        {
            const int CPUSTATES = 5;

            UIntPtr* states = stackalloc UIntPtr[CPUSTATES];
            IntPtr size = (IntPtr)(CPUSTATES * IntPtr.Size);
            sysctlbyname("kern.cp_time", states, &size, IntPtr.Zero, IntPtr.Zero);

            CPUTime all;
            all.UserTime = states[0].ToUInt32() + states[1].ToUInt32(); // CP_USER + CP_NICE
            all.KernelTime = states[2].ToUInt32(); // CP_SYS
            all.IdleTime = states[4].ToUInt32(); // CP_IDLE
            // TODO interrupt time too?
            return all;
        }
    }

    public class NetBSD_OS : UnixOS
    {
        // https://man.netbsd.org/sysctl.7
        public override unsafe CPUTime MeasureAllCPUTime()
        {
            const int CPUSTATES = 5;

            uint* states = stackalloc uint[CPUSTATES];
            IntPtr size = (IntPtr)(CPUSTATES * sizeof(uint));
            sysctlbyname("kern.cp_time", states, &size, IntPtr.Zero, IntPtr.Zero);

            CPUTime all;
            all.UserTime = states[0] + states[1]; // CP_USER + CP_NICE
            all.KernelTime = states[2]; // CP_SYS
            all.IdleTime = states[4]; // CP_IDLE
            // TODO interrupt time too?
            return all;
        }
    }

    public class MacOS : UnixOS
    {
        public override string StandaloneName
        {
            get { return IntPtr.Size == 8 ? "mac64" : "mac32"; }
        }


        // https://stackoverflow.com/questions/20471920/how-to-get-total-cpu-idle-time-in-objective-c-c-on-os-x
        // /usr/include/mach/host_info.h, /usr/include/mach/machine.h, /usr/include/mach/mach_host.h
        public override CPUTime MeasureAllCPUTime()
        {
            uint[] info = new uint[4]; // CPU_STATE_MAX
            uint count = 4; // HOST_CPU_LOAD_INFO_COUNT 
            int flavor = 3; // HOST_CPU_LOAD_INFO
            host_statistics(mach_host_self(), flavor, info, ref count);

            CPUTime all;
            all.IdleTime = info[2]; // CPU_STATE_IDLE
            all.UserTime = info[0] + info[3]; // CPU_STATE_USER + CPU_STATE_NICE
            all.KernelTime = info[1]; // CPU_STATE_SYSTEM
            return all;
        }

        [DllImport("libc")]
        public static extern IntPtr mach_host_self();
        [DllImport("libc")]
        public static extern int host_statistics(IntPtr port, int flavor, uint[] info, ref uint count);
    }
}