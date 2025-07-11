﻿/*
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
using MAX.Orders.Building;
using System;
using System.Collections.Generic;


namespace MAX.Orders.Moderation
{
    public class OrdUndoPlayer : Order
    {
        public override string Name { get { return "UndoPlayer"; } }
        public override string Shortcut { get { return "up"; } }
        public override string Type { get { return OrderTypes.Moderation; } }
        public override LevelPermission DefaultRank { get { return LevelPermission.Operator; } }
        public override OrderDesignation[] Designations
        {
            get
            {
                return new[] { new OrderDesignation("XUndo","{args} all"),
                    new OrderDesignation("UndoArea", "-area"), new OrderDesignation("ua", "-area") };
            }
        }

        public override void Use(Player p, string message, OrderData data)
        {
            bool area = message.CaselessStarts("-area");
            if (area)
            {
                message = message.Substring("-area".Length).TrimStart();
            }

            if (CheckSuper(p, message, "player name")) return;
            if (message.Length == 0) { p.Message("You need to provide a player name."); return; }

            string[] parts = message.SplitSpaces();
            int[] ids = GetIds(p, parts, data, out string[] names);
            if (ids == null) return;

            TimeSpan delta = OrdUndo.GetDelta(p, parts[0], parts, 1);
            if (delta == TimeSpan.MinValue) return;

            if (!area)
            {
                Vec3S32[] marks = new Vec3S32[] { Vec3U16.MinVal, Vec3U16.MaxVal };
                UndoPlayer(p, delta, names, ids, marks);
            }
            else
            {
                p.Message("Place or break two blocks to determine the edges.");
                UndoAreaArgs args = new UndoAreaArgs
                {
                    ids = ids,
                    names = names,
                    delta = delta
                };
                p.MakeSelection(2, "Selecting region for &SUndo player", args, DoUndoArea);
            }
        }

        public bool DoUndoArea(Player p, Vec3S32[] marks, object state, ushort block)
        {
            UndoAreaArgs args = (UndoAreaArgs)state;
            UndoPlayer(p, args.delta, args.names, args.ids, marks);
            return false;
        }

        public struct UndoAreaArgs { public string[] names; public int[] ids; public TimeSpan delta; }


        public static void UndoPlayer(Player p, TimeSpan delta, string[] names, int[] ids, Vec3S32[] marks)
        {
            UndoDrawOp op = new UndoDrawOp
            {
                Start = DateTime.UtcNow.Subtract(delta),
                who = names[0],
                ids = ids,
                AlwaysUsable = true
            };

            if (p.IsSuper)
            {
                // undo them across all loaded levels
                Level[] levels = LevelInfo.Loaded.Items;

                foreach (Level lvl in levels)
                {
                    op.Setup(p, lvl, marks);
                    DrawOpPerformer.Execute(p, op, null, marks);
                }
                p.level = null;
            }
            else
            {
                DrawOpPerformer.Do(op, null, p, marks);
            }

            string namesStr = names.Join(name => p.FormatNick(name));
            if (op.found)
            { // TODO bug assumes no other queued drawops
                Chat.MessageGlobal("Undid {1}&S's changes for the past &b{0}", delta.Shorten(true), namesStr);
                Logger.Log(LogType.UserActivity, "Actions of {0} for the past {1} were undone.", names.Join(), delta.Shorten(true));
            }
            else
            {
                p.Message("No changes found by {1} &Sin the past &b{0}", delta.Shorten(true), namesStr);
            }
        }

        public int[] GetIds(Player p, string[] parts, OrderData data, out string[] names)
        {
            int count = Math.Max(1, parts.Length - 1);
            List<int> ids = new List<int>();
            names = new string[count];

            for (int i = 0; i < names.Length; i++)
            {
                names[i] = PlayerDB.MatchNames(p, parts[i]);
                if (names[i] == null) return null;

                Group grp = PlayerInfo.GetGroup(names[i]);
                if (!CheckRank(p, data, names[i], grp.Permission, "undo", false)) return null;
                ids.AddRange(NameConverter.FindIds(names[i]));
            }
            return ids.ToArray();
        }

        public override void Help(Player p)
        {
            p.Message("&T/UndoPlayer [player1] <player2..> <timespan>");
            p.Message("&HUndoes the block changes of [players] in the past <timespan>");
            p.Message("&T/UndoPlayer -area [player1] <player2..> <timespan>");
            p.Message("&HOnly undoes block changes in the specified region.");
            p.Message("&H  If <timespan> is not given, undoes 30 minutes.");
        }
    }
}