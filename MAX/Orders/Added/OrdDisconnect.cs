
namespace MAX.Orders.Misc
{
    public class OrdDisconnect : Order
    {
        public override string Name { get { return "Disconnect"; } }
        public override string Shortcut { get { return "leave"; } }
        public override string Type { get { return OrderTypes.Other; } }
        public override bool MessageBlockRestricted { get { return true; } }
        public override bool SuperUseable { get { return false; } }

        public override bool UseableWhenJailed { get { return true; } }
        public override void Use(Player p, string message)
        {
            p.Leave(message);
        }
        public override void Help(Player p)
        {
            p.Message("&T/Disconnect (message) &H- Leaves the server with an optional message.");
        }
    }
}