using System;
using Flames.Events.PlayerEvents;
using System.IO;
using System.Threading;
using Flames.Util;
using Flames.Tasks;
namespace Flames
{
    public class Goodbye : Plugin
    {
        public override string creator { get { return "Sparkie ^w^"; } }
        public override string Flames_Version { get { return Server.Version; } }
        public override string name { get { return "Goodbye"; } }

        public override void Load(bool startup)
        {
            Command.Register(new CmdNewGoodbye());
            OnPlayerFinishConnectingEvent.Register(Goodbye2, Priority.High);
        }

        public override void Unload(bool shutdown)
        {
            Command.Unregister(Command.Find("NewGoodbye"));
			OnPlayerFinishConnectingEvent.Unregister(Goodbye2);
        }

        void Goodbye2(Player p)
        {
            if (File.Exists("bye.txt"))
            {
                p.Leave("Failed to connect to the server! It's probably down!", true);
                p.cancelconnecting = true;
            }
            else { 
            }
        }
    }

    public class CmdNewGoodbye : Command2
    {
        public override string name { get { return "NewGoodbye"; } }
        public override string type { get { return "other"; } }
        public override LevelPermission defaultRank { get { return LevelPermission.Owner; } }

        public override void Use(Player p, string message)
        {

            if (message.CaselessContains("true"))
            {
        Player[] players = PlayerInfo.Online.Items;
		foreach(Player p2 in players)
				{
	Command.Find("sendcmd").Use(Player.Flame, p2.name + " leave" + " disconnected");}
                p.Message("Server has been locked");
                if (!File.Exists("bye.txt"))
                {
                    File.Create("bye.txt");
                }
                return;
            }
            else if (message.CaselessContains("false"))
            {
                p.Message("Server has been unlocked");
                if (File.Exists("bye.txt"))
                {
                    AtomicIO.TryDelete("bye.txt");
                }
                return;
            }
            else
            {
                p.Message("%T/NewGoodbye True %S- Locks the server.");
                p.Message("%T/NewGoodbye False %S- Unlocks the server.");
            }
        }

        public override void Help(Player p) 
        {
            p.Message("%T/NewGoodbye True %S- Locks the server.");
            p.Message("%T/NewGoodbye False %S- Unlocks the server.");
        }
    }
	}
