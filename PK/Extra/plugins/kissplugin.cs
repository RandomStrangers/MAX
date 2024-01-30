using System;
using Flames.Modules.Relay.Discord;
using Flames;
using Flames.Commands;
using Flames.DB;

namespace Kiss
{
    public sealed class KissPlugin : Plugin
    {
        public override string name { get { return "Kiss"; } }

        public const string EXTRA_KEY = "__Kiss_Name";


        public override void Load(bool startup)
        {
            Command.Register(new CmdAccept1());
            Command.Register(new CmdDeny1());
            Command.Register(new CmdKiss1());
        }

        public override void Unload(bool shutdown)
        {
            Command.Unregister(Command.Find("Accept1"));
            Command.Unregister(Command.Find("Deny1"));
            Command.Unregister(Command.Find("Kiss1"));

        }


        public static Player CheckProposal(Player p)
        {
            string name = p.Extras.GetString(KissPlugin.EXTRA_KEY);
            if (name == null)
            {
                p.Message("You do not have a pending kissing proposal."); return null;
            }

            Player src = PlayerInfo.FindExact(name);
            if (src == null)
            {
                p.Message("The person who proposed to kiss you isn't online."); return null;
            }
            return src;
        }
    }

    public sealed class CmdAccept1 : Command
    {
        public override string name { get { return "Accept1"; } }
        public override string type { get { return "fun"; } }

        public override void Use(Player p, string message)
        {
            Player proposer = KissPlugin.CheckProposal(p);
            if (proposer == null) return;
            p.Message("&bYou &aaccepted &b{0}&b's proposal", p.FormatNick(proposer));
            DiscordPlugin.Bot.SendPublicMessage($"{proposer.color}{proposer.DisplayName}%S gave {p.color}{p.DisplayName}%S a kiss on the lips! O&c///%SO");
            Chat.MessageFrom(p, $"{proposer.color}{proposer.DisplayName}%S gave {p.color}{p.DisplayName}%S a kiss on the lips! O&c///%SO");
        }

        public override void Help(Player p)
        {
            p.Message("&T/Accept1 &H- Accepts a pending kissing proposal.");
        }
    }

    public class CmdDeny1 : Command
    {
        public override string name { get { return "Deny1"; } }
        public override string type { get { return "fun"; } }

        public override void Use(Player p, string message)
        {
            Player proposer = KissPlugin.CheckProposal(p);
            if (proposer == null) return;
            Chat.MessageFrom(p, $"{p.color}{p.DisplayName}%S &Sdenied {proposer.color}{proposer.DisplayName}&S's request.");

            p.Message("&bYou &cdenied &b{0}&b's proposal", p.FormatNick(proposer));
        }

        public override void Help(Player p)
        {
            p.Message("&T/Deny1 &H- Denies a pending kiss proposal.");
        }
    }

    public sealed class CmdKiss1 : Command
    {
        public override string name { get { return "kiss1"; } }
        public override string type { get { return "fun"; } }

        public override void Use(Player p, string message)
        {

            Player partner = PlayerInfo.FindMatches(p, message);
            if (partner == null) return;
            partner.Extras[KissPlugin.EXTRA_KEY] = p.name;
            //DiscordPlugin.Bot.SendPublicMessage($"{p.color}{p.DisplayName}%S is asking {partner.color}{partner.DisplayName}%S for permission to kiss on the lips! O&c///%SO");
            partner.Message($"{p.color}{p.DisplayName}%S is asking {partner.color}{partner.DisplayName}%S for permission to kiss on the lips! O&c///%SO");
            partner.Message("&bTo accept their proposal type &a/Accept1");
            partner.Message("&bOr to deny it, type &c/Deny1");
        }

        public override void Help(Player p)
        {
            p.Message("&T/Kiss1 [player]");
            p.Message("&HAsks the given player permission to kiss them on the lips O&c///&HO.");
        }
    }
}