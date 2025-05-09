
namespace MAX.Orders.Misc
{
    public class OrdDisconnect : Order
    {
        public override string name { get { return "Disconnect"; } }
        public override string shortcut { get { return "leave"; } }
        public override string type { get { return OrderTypes.Other; } }
        public override bool MessageBlockRestricted { get { return true; } }
        public override bool SuperUseable { get { return false; } }

        public override bool UseableWhenFrozen { get { return true; } }
        public override void Use(Player p, string message)
        {
            p.Leave(message);
        }
        public override void Help(Player p)
        {
            p.Message("/Disconnect (message)- Leaves the server with an optional message.");
        }
    }
}