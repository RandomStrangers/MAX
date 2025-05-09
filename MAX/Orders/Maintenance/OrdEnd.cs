using System;

namespace MAX.Orders.Chatting
{
    public sealed class OrdEnd : Order2
    {
        public override string name { get { return "End"; } }
        public override string type { get { return OrderTypes.Other; } }
        public override LevelPermission defaultRank { get { return LevelPermission.Nobody; } }
 
        public override void Use(Player p, string message, OrderData data)
        {
            End(p);
        }
        public static void End(Player p)
        {
            if (!CheckPerms(p))
            {
                p.Message("Only MAX or the Server Owner can end the server."); return;
            }
            Environment.Exit(0);        
        }
        public static bool CheckPerms(Player p)
        {
            if (p.IsMAX) return true;

            if (Server.Config.OwnerName.CaselessEq("Notch")) return false;
            return p.name.CaselessEq(Server.Config.OwnerName);
        }
        public override void Help(Player p)
        {
            p.Message("&T/End &H- Kills the server");
        }
    }
}