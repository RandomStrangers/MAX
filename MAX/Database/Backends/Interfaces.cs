/*
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
using System;

namespace MAX.SQL
{
    /// <summary> Abstracts a connection to a SQL database </summary>
    public class ISqlConnection : IDisposable
    {
        public virtual ISqlTransaction BeginTransaction()
        {
            return null;
        }
        public virtual ISqlOrder CreateOrder(string sql)
        {
            return null;
        }

        public virtual void Open()
        {
        }
        public virtual void ChangeDatabase(string name)
        {
        }
        public virtual void Close()
        {
        }
        public virtual void Dispose()
        {
        }
    }

    /// <summary> Abstracts a SQL order/statement </summary>
    public class ISqlOrder : IDisposable
    {
        public virtual void ClearParameters()
        {
        }
        public virtual void AddParameter(string name, object value)
        {
        }

        public virtual void Prepare()
        {
        }
        /// <summary> Executes this order and returns the number of rows affected </summary>
        public virtual int ExecuteNonQuery()
        {
            return 0;
        }
        /// <summary> Executes this order and returns an ISqlReader for reading the results </summary>
        public virtual ISqlReader ExecuteReader()
        {
            return null;
        }
        public virtual void Dispose()
        {
        }
    }

    public class ISqlTransaction : IDisposable
    {
        public virtual void Commit()
        {
        }
        public virtual void Rollback()
        {
        }
        public virtual void Dispose()
        {
        }
    }

    /// <summary> Abstracts iterating over the results from executing a SQL order </summary>
    public class ISqlReader : ISqlRecord, IDisposable
    {
        public virtual int RowsAffected { get; }
        public virtual void Close()
        {
        }
        public virtual bool Read()
        {
            return false;
        }
        public virtual void Dispose()
        {
        }
    }

    public class ISqlRecord
    {
        public virtual int FieldCount { get; }
        public virtual string GetName(int i)
        {
            return null;
        }
        public virtual int GetOrdinal(string name)
        {
            return 0;
        }

        public virtual byte[] GetBytes(int i)
        {
            return null;
        }
        public virtual bool GetBoolean(int i)
        {
            return false;
        }
        public virtual int GetInt32(int i)
        {
            return 0;
        }
        public virtual long GetInt64(int i)
        {
            return 0;
        }
        public virtual double GetDouble(int i)
        {
            return 0;
        }
        public virtual string GetString(int i)
        {
            return null;
        }
        public virtual DateTime GetDateTime(int i)
        {
            return DateTime.UtcNow;
        }
        public virtual bool IsDBNull(int i)
        {
            return false;
        }

        public virtual object GetValue(int i)
        {
            return null;
        }
        public virtual string GetStringValue(int col)
        {
            return null;
        }
        public virtual string DumpValue(int col)
        {
            return null;
        }


        public string GetText(int col)
        {
            return IsDBNull(col) ? "" : GetString(col);
        }

        public string GetText(string name)
        {
            int col = GetOrdinal(name);
            return IsDBNull(col) ? "" : GetString(col);
        }

        public int GetInt(string name)
        {
            int col = GetOrdinal(name);
            return IsDBNull(col) ? 0 : GetInt32(col);
        }

        public long GetLong(string name)
        {
            int col = GetOrdinal(name);
            return IsDBNull(col) ? 0 : GetInt64(col);
        }

        public static string Quote(string value)
        {
            if (value.IndexOf('\'') >= 0) // escape '
                value = value.Replace("'", "''");
            return "'" + value + "'";
        }
    }
}