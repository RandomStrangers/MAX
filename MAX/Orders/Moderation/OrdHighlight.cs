/*
    Copyright 2011 MCForge
        
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
using MAX.Drawing.Ops;
using MAX.Maths;
using MAX.Network;
using System;


namespace MAX.Orders.Moderation
{
    public class OrdHighlight : Order
    {
        public override string Name { get { return "Highlight"; } }
        public override string Type { get { return OrderTypes.Moderation; } }
        public override bool MuseumUsable { get { return false; } }
        public override LevelPermission DefaultRank { get { return LevelPermission.AdvBuilder; } }
        public override bool SuperUseable { get { return false; } }
        public override OrderDesignation[] Designations
        {
            get { return new OrderDesignation[] { new OrderDesignation("HighlightArea", "area") }; }
        }

        public override void Use(Player p, string message, OrderData data)
        {
            TimeSpan delta = TimeSpan.Zero;
            bool area = message.CaselessStarts("area ");
            if (area) message = message.Substring("area ".Length);

            if (message.Length == 0) message = p.name;
            string[] parts = message.SplitSpaces();

            if (parts.Length >= 2)
            {
                if (!OrderParser.GetTimespan(p, parts[1], ref delta, "highlight the past", "s")) return;
            }
            else
            {
                delta = TimeSpan.FromMinutes(30);
            }

            parts[0] = PlayerDB.MatchNames(p, parts[0]);
            if (parts[0] == null) return;
            int[] ids = NameConverter.FindIds(parts[0]);

            if (!area)
            {
                Vec3S32[] marks = new Vec3S32[] { Vec3U16.MinVal, Vec3U16.MaxVal };
                HighlightPlayer(p, delta, parts[0], ids, marks);
            }
            else
            {
                p.Message("Place or break two blocks to determine the edges.");
                HighlightAreaArgs args = new HighlightAreaArgs
                {
                    ids = ids,
                    who = parts[0],
                    delta = delta
                };
                p.MakeSelection(2, "Selecting region for &SHighlight", args, DoHighlightArea);
            }
        }

        public bool DoHighlightArea(Player p, Vec3S32[] marks, object state, ushort block)
        {
            HighlightAreaArgs args = (HighlightAreaArgs)state;
            HighlightPlayer(p, args.delta, args.who, args.ids, marks);
            return false;
        }

        public struct HighlightAreaArgs { public string who; public int[] ids; public TimeSpan delta; }


        public static void HighlightPlayer(Player p, TimeSpan delta, string who, int[] ids, Vec3S32[] marks)
        {
            HighlightDrawOp op = new HighlightDrawOp
            {
                Start = DateTime.UtcNow.Subtract(delta),
                who = who,
                ids = ids
            };
            op.Setup(p, p.level, marks);

            BufferedBlockSender buffer = new BufferedBlockSender(p);
            op.Perform(marks, null,
                       P =>
                       {
                           int index = p.level.PosToInt(P.X, P.Y, P.Z);
                           buffer.Add(index, P.Block);
                       });
            buffer.Flush();

            if (op.totalChanges > 0)
            {
                p.Message("Highlighting &T{0}&S changes by {1}&S in the past &b{2}",
                           op.totalChanges.ToString("N0"), p.FormatNick(who), delta.Shorten(true));
                p.Message("&WUse /reload to un-highlight");
            }
            else
            {
                p.Message("No changes found by {1} &Sin the past &b{0}",
                           delta.Shorten(true), p.FormatNick(who));
            }
        }

        public override void Help(Player p)
        {
            p.Message("&T/Highlight [player] <timespan>");
            p.Message("&HHighlights blocks changed by [player] in the past <timespan>");
            p.Message("&T/Highlight area [player] <timespan>");
            p.Message("&HOnly highlights in the specified region.");
            p.Message("&H If <timespan> is not given, highlights for last 30 minutes");
            p.Message("&W/Highlight cannot be disabled, use /reload to un-highlight");
        }
    }
}