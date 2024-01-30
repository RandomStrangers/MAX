using System;
using Flames;
using Flames.Events;
using Flames.Events.PlayerEvents;
namespace PluginChangeClientName
{
    public class ChangeClientName : Plugin
    {
        public override string creator { get { return "Stella"; } }
        public override string name { get { return "ClientNameChanger"; } }


        public override void Load(bool startup)
        {
			Command.Register(new CmdChangeClient());
		OnPlayerFinishConnectingEvent.Register(ClientNameChange, Priority.High);
        }

        public override void Unload(bool shutdown)
        {
			Command.Unregister(Command.Find("ChangeClient"));
			OnPlayerFinishConnectingEvent.Unregister(ClientNameChange);
        }

        void ClientNameChange(Player p)
        {

            string app3 = p.Session.ClientName();
            string name = p.truename;
	if (app3 == null){ Command.Find("opchat").Use(Player.Flame, name + " connected using Classic 0.28-0.30.");}
         else {            
         Command.Find("opchat").Use(Player.Flame, name + " connected using " + app3 + ".");
}
 if (p.Supports(CpeExt.ExtEntityTeleport)) return;
 if (!p.Supports(CpeExt.ExtEntityTeleport))
            {
              p.Message("&dHey! &fEven though most of the CPE features exist in " + app3 + " , you'd have a better time using the updated ClassiCube client.", true);
            }
        }
    }
    	public sealed class CmdChangeClient : Command {
		public override string name { get { return "ChangeClient"; } }
		public override string type { get { return "other"; } }
        public override LevelPermission defaultRank { get { return LevelPermission.Admin; } }
		public override void Use(Player p, string message) {
		string app =  p.Session.appName;
       	string app2 = p.Session.ClientName();
        p.Session.appName = message;
        app = message;
        app2 = app;
		//Command.Find("server").Use(Player.Flame, "reload");
		p.Message("Changed your client name to " + app + ".");
		Command.Find("pclients").Use(p, "");
		}
		
		public override void Help(Player p) {
			p.Message("&T/ChangeClient [Client] &H- Changes your client name.");
		}
	}
}