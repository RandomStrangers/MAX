namespace MAX.Orders.Moderation
{
    public class OrdBye : Order
    {
        public override string Name { get { return "Bye"; } }
        public override string Shortcut { get { return ""; } }
        public override string Type { get { return OrderTypes.Moderation; } }
        public override LevelPermission DefaultRank { get { return LevelPermission.Owner; } }
        public override bool MessageBlockRestricted { get { return true; } }
        public override bool UseableWhenJailed { get { return true; } }
        public override void Use(Player p, string message)
        {
            Player[] players = PlayerInfo.Online.Items;
            foreach (Player pl in players)
            {
                if (!string.IsNullOrEmpty(message))
                {
                    pl.Leave(message);
                }
                else
                {
                    string logoutmsg = PlayerInfo.GetLogoutMessage(pl);
                    pl.Leave(logoutmsg);
                }
            }
        }
        public override void Help(Player p)
        {
            if (p.IsSuper)
            {
                p.Message("&T/Bye &H- Makes ALL players leave the server with an optional message");
                return;
            }
            else
            {
                string logoutmsg = PlayerInfo.GetLogoutMessage(p);
                p.Leave(logoutmsg);
            }
        }
    }
}