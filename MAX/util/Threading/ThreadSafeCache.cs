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
using MAX.Tasks;
using System;
using System.Collections.Generic;

namespace MAX.Util
{
    public class ThreadSafeCache
    {
        public static ThreadSafeCache DBCache = new ThreadSafeCache();

        public object locker = new object();
        public Dictionary<string, object> items = new Dictionary<string, object>();
        public Dictionary<string, DateTime> access = new Dictionary<string, DateTime>();

        public object GetLocker(string key)
        {
            lock (locker)
            {
                if (!items.TryGetValue(key, out object value))
                {
                    value = new object();
                    items[key] = value;
                }

                access[key] = DateTime.UtcNow;
                return value;
            }
        }


        public void CleanupTask(SchedulerTask _)
        {
            List<string> free = null;
            DateTime now = DateTime.UtcNow;

            lock (locker)
            {
                foreach (KeyValuePair<string, DateTime> kvp in access)
                {
                    // Has the cached item last been accessed in 5 minutes?
                    if ((now - kvp.Value).TotalMinutes <= 5) continue;

                    if (free == null) free = new List<string>();
                    free.Add(kvp.Key);
                }

                if (free == null) return;
                foreach (string key in free)
                {
                    items.Remove(key);
                    access.Remove(key);
                }
            }
        }
    }
}