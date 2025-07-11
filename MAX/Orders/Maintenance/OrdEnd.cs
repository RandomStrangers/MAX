using Context = System.Environment;

namespace MAX.Orders.Maintenance
{
    public class OrdEnd : Order
    {
        public override string Name { get { return "End"; } }
        public override string Type { get { return OrderTypes.Moderation; } }
        public override LevelPermission DefaultRank { get { return LevelPermission.Owner; } }
        public override void Use(Player p, string message, OrderData data)
        {
            End(p);
        }
        public static void End(Player p)
        {
            if (!CheckPerms(p))
            {
                p.Message("Only MAX or the Server Owner can end the server.");
                return;
            }
            Context.Exit(0);
        }
        public static bool CheckPerms(Player p)
        {
            if (p.IsMAX)
            {
                return true;
            }
            if (Server.Config.OwnerName.CaselessEq("notch") ||
                Server.Config.OwnerName.CaselessEq("the owner"))
            {
                return false;
            }
            return p.name.CaselessEq(Server.Config.OwnerName);
        }
        public override void Help(Player p)
        {
            p.Message("&T/End &H- Kills the server");
        }
    }
}