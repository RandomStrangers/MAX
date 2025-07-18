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
using MAX.Util;

namespace MAX.Orders.Info
{
    public class OrdFaq : Order
    {
        public override string Name { get { return "FAQ"; } }
        public override string Type { get { return OrderTypes.Information; } }
        public override bool UseableWhenJailed { get { return true; } }

        public override void Use(Player p, string message, OrderData data)
        {
            TextFile faqFile = TextFile.Files["FAQ"];
            faqFile.EnsureExists();

            string[] faq = faqFile.GetText();
            p.Message("&cFAQ&f:");
            foreach (string line in faq)
                p.Message("&f" + line);
        }

        public override void Help(Player p)
        {
            p.Message("&T/FAQ");
            p.Message("&HDisplays frequently asked questions");
        }
    }
}