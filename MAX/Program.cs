using MAX.UI;
using System;
using System.IO;
using System.Reflection;
using System.Threading;
using Context = System.Environment;
using Terminal = System.Console;
using TerminalCancelEventArgs = System.ConsoleCancelEventArgs;
using TerminalColor = System.ConsoleColor;
using TerminalSpecialKey = System.ConsoleSpecialKey;
namespace MAX
{
    public static class Program
    {
        public static string FileName = "MAX";
        [STAThread]
        public static void Main(string[] args)
        {
            Terminal.WriteLine(args);
            Terminal.Clear();
            SetCurrentDirectory();
            EnableTLIMode();
            string file = Path.GetFileName(Assembly.GetExecutingAssembly().Location);
            if (!Server.RestartPath.StartsWith(FileName))
            {
                string ext = Path.GetExtension(file);
                bool movedfile = FileIO.TryMove(file, FileName + ext);
                if (movedfile)
                {
                    SetCurrentDirectory();
                    EnableTLIMode();
                    StartTLI();
                }
                else
                {
                    Terminal.WriteLine("Failed to move {0} to {1}. Please ensure you have write permissions in the current directory.", file, FileName);
                    Terminal.WriteLine("You can manually rename the file to " + FileName + " and run it again.");
                    Server.Stop(true, "Wrong file name, expected " + FileName + " but got " + file);
                }
            }
        }
        public static void SetCurrentDirectory()
        {
            string path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            try
            {
                Context.CurrentDirectory = path;
            }
            catch
            {
                Terminal.WriteLine("Failed to set working directory to '{0}', running in current directory..", path);
            }
        }
        public static void EnableTLIMode()
        {
            try
            {
                Server.TLIMode = true;
            }
            catch
            {
            }
            Server.RestartPath = Assembly.GetEntryAssembly().Location;
        }
        public static void StartTLI()
        {
            FileLogger.Init();
            AppDomain.CurrentDomain.UnhandledException += GlobalExHandler;
            try
            {
                Logger.LogHandler += LogMessage;
                Updater.NewerVersionDetected += LogNewerVersionDetected;
                EnableTLIMode();
                Server.Start();
                Terminal.Title = Colors.StripUsed(Server.Config.Name) + " - " + Colors.StripUsed(Server.NameVersioned);
                Terminal.CancelKeyPress += OnCancelKeyPress;
                CheckNameVerification();
                TerminalLoop();
            }
            catch (Exception e)
            {
                Logger.LogError(e);
                FileLogger.Flush(null);
            }
        }
        public static void OnCancelKeyPress(object sender, TerminalCancelEventArgs e)
        {
            switch (e.SpecialKey)
            {
                case TerminalSpecialKey.ControlBreak:
                    Write("&4-- Server shutdown (Ctrl+Break) --");
                    Thread stopThread = Server.Stop(false, Server.Config.DefaultShutdownMessage);
                    stopThread.Join();
                    break;
                case TerminalSpecialKey.ControlC:
                    e.Cancel = true;
                    Write("&4-- Server shutdown (Ctrl+C) --");
                    Server.Stop(false, Server.Config.DefaultShutdownMessage);
                    break;
            }
        }
        public static void LogAndRestart(Exception ex)
        {
            Logger.LogError(ex);
            FileLogger.Flush(null);
            Thread.Sleep(500);
            if (Server.Config.restartOnError)
            {
                Thread stopThread = Server.Stop(true, "Server restart - unhandled error");
                stopThread.Join();
            }
        }
        public static void GlobalExHandler(object sender, UnhandledExceptionEventArgs e)
        {
            Exception ex = (Exception)e.ExceptionObject;
            LogAndRestart(ex);
        }
        public static string CurrentDate()
        {
            return DateTime.Now.ToString("(HH:mm:ss) ");
        }
        public static void LogMessage(LogType type, string message)
        {
            if (!Server.Config.MAXLogging)
            {
                return;
            }
            switch (type)
            {
                case LogType.Error:
                    Write("&4!!!Error" + ExtractErrorMessage(message)
                          + " - See " + FileLogger.ErrorLogPath + " for more details.");
                    break;
                case LogType.BackgroundActivity:
                    break;
                case LogType.Warning:
                    Write("&e" + CurrentDate() + message);
                    break;
                default:
                    Write(CurrentDate() + message);
                    break;
            }
        }
        public static string msgPrefix = Context.NewLine + "Message: ";
        public static string ExtractErrorMessage(string raw)
        {
            int beg = raw.IndexOf(msgPrefix);
            if (beg == -1)
            {
                return "";
            }
            beg += msgPrefix.Length;
            int end = raw.IndexOf(Context.NewLine, beg);
            if (end == -1)
            {
                return "";
            }
            return " (" + raw.Substring(beg, end - beg) + ")";
        }
        public static void CheckNameVerification()
        {
            if (Server.Config.VerifyNames)
            {
                return;
            }
            Write("&4WARNING: Name verification is disabled! This means players can login as anyone, including YOU");
        }
        public static void LogNewerVersionDetected(object sender, EventArgs e)
        {
            Write(Colors.StripUsed(Server.SoftwareNameConst) + " &4update available! Update by replacing with the files from " + Updater.UploadsURL);
        }
        public static void TerminalLoop()
        {
            int eofs = 0;
            while (true)
            {
                try
                {
                    string msg = Terminal.ReadLine();
                    if (msg == null)
                    {
                        eofs++;
                        if (eofs >= 15)
                        {
                            Write("&e** EOF, terminal no longer accepts input **");
                            break;
                        }
                        continue;
                    }
                    msg = msg.Trim();
                    if (msg == "/")
                    {
                        UIHelpers.RepeatOrder();
                    }
                    else if (msg.Length > 0 && msg[0] == '/')
                    {
                        UIHelpers.HandleOrder(msg.Substring(1));
                    }
                    else
                    {
                        UIHelpers.HandleChat(msg);
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex);
                }
            }
        }
        public static void Write(string message)
        {
            int index = 0;
            char col = 'M';
            message = UIHelpers.Format(message);
            while (index < message.Length)
            {
                char curCol = col;
                string part = UIHelpers.OutputPart(ref col, ref index, message);
                if (part.Length == 0)
                {
                    continue;
                }
                TerminalColor color = GetTerminalColor(curCol);
                if (color == TerminalColor.White)
                {
                    Terminal.ResetColor();
                }
                else
                {
                    Terminal.ForegroundColor = color;
                }
                Terminal.Write(part);
            }
            Terminal.ResetColor();
            Terminal.WriteLine();
        }
        public static TerminalColor GetTerminalColor(char c)
        {
            if (c == 'M')
            {
                return TerminalColor.DarkRed;
            }
            if (c == 'S')
            {
                return TerminalColor.White;
            }
            Colors.Map(ref c);
            switch (c)
            {
                case '0':
                    return TerminalColor.DarkGray;
                case '1':
                    return TerminalColor.DarkBlue;
                case '2':
                    return TerminalColor.DarkGreen;
                case '3':
                    return TerminalColor.DarkCyan;
                case '4':
                    return TerminalColor.DarkRed;
                case '5':
                    return TerminalColor.DarkMagenta;
                case '6':
                    return TerminalColor.DarkYellow;
                case '7':
                    return TerminalColor.Gray;
                case '8':
                    return TerminalColor.DarkGray;
                case '9':
                    return TerminalColor.Blue;
                case 'a':
                    return TerminalColor.Green;
                case 'b':
                    return TerminalColor.Cyan;
                case 'c':
                    return TerminalColor.Red;
                case 'd':
                    return TerminalColor.Magenta;
                case 'e':
                    return TerminalColor.Yellow;
                case 'f':
                    return TerminalColor.White;
                default:
                    if (!Colors.IsDefined(c))
                    {
                        return TerminalColor.DarkRed;
                    }
                    return GetTerminalColor(Colors.Get(c).Fallback);
            }
        }
    }
}