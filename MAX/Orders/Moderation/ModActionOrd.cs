﻿/*
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
using MAX.DB;
using System.Collections.Generic;
using System.IO;
using System.Net;


namespace MAX.Orders.Moderation
{

    /// <summary> Provides common helper methods for moderation orders. </summary>
    public static class ModActionOrd
    {

        /// <summary> Expands @[rule number] to the actual rule with that number. </summary>
        public static string ExpandReason(Player p, string reason)
        {
            string expanded = TryExpandReason(reason, out int ruleNum);
            if (expanded != null) return expanded;

            Dictionary<int, string> sections = GetRuleSections();
            p.Message("No rule has number \"{0}\". Current rule numbers are: {1}",
                      ruleNum, sections.Keys.Join(n => n.ToString()));
            return null;
        }

        public static string TryExpandReason(string reason, out int ruleNum)
        {
            ruleNum = 0;
            if (reason.Length == 0 || reason[0] != '@') return reason;

            reason = reason.Substring(1);
            if (!int.TryParse(reason, out ruleNum)) return "@" + reason;

            // Treat @num as a shortcut for rule #num
            Dictionary<int, string> sections = GetRuleSections();
            sections.TryGetValue(ruleNum, out string rule); return rule;
        }

        public static Dictionary<int, string> GetRuleSections()
        {
            Dictionary<int, string> sections = new Dictionary<int, string>();
            if (!File.Exists(Paths.RulesFile)) return sections;

            List<string> rules = Utils.ReadAllLinesList(Paths.RulesFile);
            foreach (string rule in rules)
                ParseRule(rule, sections);
            return sections;
        }

        public static void ParseRule(string rule, Dictionary<int, string> sections)
        {
            int ruleNum = -1;
            rule = Colors.Strip(rule);

            for (int i = 0; i < rule.Length; i++)
            {
                char c = rule[i];
                bool isNumber = c >= '0' && c <= '9';
                bool isLetter = (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z');
                if (!isNumber && !isLetter) continue;
                // Found start of a word, but didn't find a number - assume this is a non-numbered rule
                if (isLetter && ruleNum == -1) return;

                if (isNumber)
                { // e.g. line is: 1) Do not do X
                    if (ruleNum == -1) ruleNum = 0;
                    ruleNum *= 10;
                    ruleNum += c - '0';
                }
                else
                {
                    sections[ruleNum] = rule.Substring(i);
                    return;
                }
            }
        }

        public static void ChangeOnlineRank(Player who, Group newRank)
        {
            who.group = newRank;
            who.AllowBuild = who.level.BuildAccess.CheckAllowed(who);
            if (who.hidden && who.hideRank < who.Rank) who.hideRank = who.Rank;

            who.SetColor(PlayerInfo.DefaultColor(who));
            who.SetPrefix();

            Entities.DespawnEntities(who, false);
            who.Session.SendSetUserType(who.UserType());

            who.SendCurrentBlockPermissions();
            Entities.SpawnEntities(who, false);
            CheckBlockBindings(who);

            who.CheckIsUnverified();
        }

        /// <summary> Changes the rank of the given player from the old to the new rank. </summary>
        public static void ChangeRank(string name, Group oldRank, Group newRank,
                                        Player who, bool saveToNewRank = true)
        {
            if (who != null) ChangeOnlineRank(who, newRank);
            Server.reviewlist.Remove(name);

            oldRank.Players.Remove(name);
            oldRank.Players.Save();

            if (!saveToNewRank) return;
            newRank.Players.Add(name);
            newRank.Players.Save();
        }

        public static void CheckBlockBindings(Player who)
        {
            ushort block = who.ModeBlock;
            if (block != Block.Invalid && !OrderParser.IsBlockAllowed(who, "place", block))
            {
                who.ModeBlock = Block.Invalid;
                who.Message("   Hence, &b{0} &Smode was turned &cOFF",
                            Block.GetName(who, block));
            }

            for (int b = 0; b < who.BlockBindings.Length; b++)
            {
                block = who.BlockBindings[b];
                if (block == b) continue;

                if (!OrderParser.IsBlockAllowed(who, "place", block))
                {
                    who.BlockBindings[b] = (ushort)b;
                    who.Message("   Hence, binding for &b{0} &Swas unbound",
                                Block.GetName(who, (ushort)b));
                }
            }
        }

        public static Group CheckTarget(Player p, OrderData data, string action, string target)
        {
            if (p.name.CaselessEq(target))
            {
                p.Message("You cannot {0} yourself", action); return null;
            }

            Group group = PlayerInfo.GetGroup(target);
            if (!Order.CheckRank(p, data, target, group.Permission, action, false)) return null;
            return group;
        }


        /// <summary> Finds the matching name(s) for the input name,
        /// and requires a confirmation message for non-existent players. </summary>
        public static string FindName(Player p, string action, string ord,
                                        string ordSuffix, string name, ref string reason)
        {
            if (!Formatter.ValidPlayerName(p, name)) return null;
            string match = MatchName(p, ref name);
            string confirmed = IsConfirmed(reason);
            if (confirmed != null) reason = confirmed;

            if (match != null)
            {
                // Does matching name exactly equal name given by user?
                if (Server.ToRawUsername(match).CaselessEq(Server.ToRawUsername(name)))
                    return match;

                // Not an exact match, user might have made a mistake
                p.Message("1 player matches \"{0}\": {1}", name, match);
            }

            if (confirmed != null) return name;
            string msgReason = string.IsNullOrEmpty(reason) ? "" : " " + reason;
            p.Message("If you still want to {0} \"{1}\", use &T/{3} {1}{4}{2} confirm",
                           action, name, msgReason, ord, ordSuffix);
            return null;
        }

        public static string MatchName(Player p, ref string name)
        {
            Player target = PlayerInfo.FindMatches(p, name, out int matches);
            if (matches > 1) return null;
            if (matches == 1) { name = target.name; return name; }

            p.Message("Searching PlayerDB...");
            return PlayerDB.MatchNames(p, name);
        }

        public static string IsConfirmed(string reason)
        {
            if (reason == null) return null;
            if (reason.CaselessEq("confirm"))
                return "";
            if (reason.CaselessEnds(" confirm"))
                return reason.Substring(0, reason.Length - " confirm".Length);
            return null;
        }


        public static bool ValidIP(string str)
        {
            // IPAddress.TryParse returns "0.0.0.123" for "123", we do not want that behaviour
            return str.IndexOf(':') >= 0 || str.Split('.').Length == 4;
        }

        /// <summary> Attempts to either parse the message directly as an IP,
        /// or finds the IP of the account whose name matches the message. </summary>
        /// <remarks> "@input" can be used to always find IP by matching account name. <br/>
        /// Warns the player if the input matches both an IP and an account name. </remarks>
        public static string FindIP(Player p, string message, string ord, out string name)
        {
            name = null;

            if (IPAddress.TryParse(message, out IPAddress _) && ValidIP(message))
            {
                string account = Server.FromRawUsername(message);
                // TODO ip.ToString()
                if (PlayerDB.FindName(account) == null) return message;

                // ClassiCube.net used to allow registering accounts with . anywhere in name,
                //  so some older names can be parsed as valid IPs. Warn in this case
                p.Message("Note: \"{0}\" is both an IP and an account name. "
                          + "If you meant the account, use &T/{1} @{0}", message, ord);
                return message;
            }

            if (message[0] == '@') message = message.Remove(0, 1);
            Player who = PlayerInfo.FindMatches(p, message);
            if (who != null) { name = who.name; return who.ip; }

            p.Message("Searching PlayerDB..");
            name = PlayerDB.FindOfflineIPMatches(p, message, out string dbIP);
            return dbIP;
        }
    }
}