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
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace MAX.SQL 
{    
    /// <summary> Abstracts a SQL based database management system </summary>
    public abstract class IDatabaseBackend 
    {        
        /// <summary> Whether this backend enforces the character length in VARCHAR columns </summary>
        public abstract bool EnforcesTextLength { get; }
        /// <summary> Whether this backend enforces integer limits based on column types </summary>
        public abstract bool EnforcesIntegerLimits { get; } 
        /// <summary> Whether this backend supports multiple database schemas </summary>
        public abstract bool MultipleSchema { get; }
        public abstract string EngineName { get; }

        public abstract ISqlConnection CreateConnection();

        /// <summary> Suffix required after a WHERE clause for caseless string comparison. </summary>
        public string CaselessWhereSuffix { get; protected set; }
        /// <summary> Suffix required after a LIKE clause for caseless string comparison. </summary>        
        public string CaselessLikeSuffix { get; protected set; }

        
        /// <summary> Downloads and/or moves required DLLs </summary>
        public abstract void LoadDependencies();
        /// <summary> Creates the schema for this database (if required). </summary>
        public abstract void CreateDatabase();
        
        protected internal virtual void ParseCreate(ref string ord) { }
        
        protected static List<string> GetStrings(string sql, params object[] args) {
            List<string> values = new List<string>();
            Database.Iterate(sql, 
                            record => values.Add(record.GetText(0)), 
                            args);
            return values;
        }
        
        
        // == Higher level table management functions ==
        
        /// <summary> Returns whether a table (case sensitive) exists by that name. </summary>
        public abstract bool TableExists(string table);

        /// <summary> Returns a list of all tables in this database. </summary>
        public abstract List<string> AllTables();
        
        /// <summary> Returns a list of the column names in the given table. </summary>
        public abstract List<string> ColumnNames(string table);
        
        /// <summary> Returns SQL for renaming the source table to the given name. </summary>
        public abstract string RenameTableSql(string srcTable, string dstTable);
        
        /// <summary> Returns SQL for creating a new table (unless it already exists). </summary>
        public virtual string CreateTableSql(string table, ColumnDesc[] columns) {
            StringBuilder sql = new StringBuilder();
            sql.AppendLine("CREATE TABLE if not exists `" + table + "` (");
            CreateTableColumns(sql, columns);
            sql.AppendLine(");");
            return sql.ToString();
        }
        
        protected abstract void CreateTableColumns(StringBuilder sql, ColumnDesc[] columns);
        
        /// <summary> Returns SQL for completely removing the given table. </summary>
        public virtual string DeleteTableSql(string table) {
            return "DROP TABLE if exists `" + table + "`";
        }
        
        /// <summary> Prints/dumps the table schema of the given table. </summary>
        public abstract void PrintSchema(string table, TextWriter w);
        
        /// <summary> Returns SQL for adding a new column to the given table. </summary>
        /// <remarks> Note colAfter is only a hint - some database backends ignore this. </remarks>
        public abstract string AddColumnSql(string table, ColumnDesc col, string colAfter);

        
        // == Higher level functions ==

        /// <summary> Returns SQL for copying all the rows from the source table into the destination table. </summary>
        public virtual string CopyAllRowsSql(string srcTable, string dstTable) {
            return "INSERT INTO `" + dstTable + "` SELECT * FROM `" + srcTable + "`";
        }
        
        /// <summary> Returns SQL for reading rows from the given table. </summary>
        public virtual string ReadRowsSql(string table, string columns, string modifier) {
            string sql = "SELECT " + columns + " FROM `" + table + "`";
            if (modifier.Length > 0) sql += " " + modifier;
            return sql;
        }
        
        /// <summary> Returns SQL for updating rows for the given table. </summary>
        public virtual string UpdateRowsSql(string table, string columns, string modifier) {
            string sql = "UPDATE `" + table + "` SET " + columns;
            if (modifier.Length > 0) sql += " " + modifier;
            return sql;
        }
        
        /// <summary> Returns SQL for deleting rows for the given table. </summary>
        public virtual string DeleteRowsSql(string table, string modifier) {
            string sql = "DELETE FROM `" + table + "`";
            if (modifier.Length > 0) sql += " " + modifier;
            return sql;
        }

        /// <summary> Returns SQL for adding a row to the given table. </summary>
        public virtual string AddRowSql(string table, string columns, int numArgs) {
            return InsertSql("INSERT INTO", table, columns, numArgs);
        }
        
        /// <summary> Returns SQL for adding or replacing a row (same primary key) in the given table. </summary>
        public abstract string AddOrReplaceRowSql(string table, string columns, int numArgs);
      
        
        protected string InsertSql(string ord, string table, string columns, int numArgs) {
            StringBuilder sql = new StringBuilder(ord);
            sql.Append(" `").Append(table).Append("` ");
            sql.Append('(').Append(columns).Append(')');
            
            string[] names = GetNames(numArgs);
            sql.Append(" VALUES (");
            for (int i = 0; i < numArgs; i++) 
            {
                sql.Append(names[i]);
                if (i < numArgs - 1) sql.Append(", ");
                else sql.Append(")");
            }
            return sql.ToString();
        }
        
        
        #region Raw SQL functions
        
        /// <summary> Executes an SQL order and returns the number of affected rows. </summary>
        public int Execute(string sql, object[] parameters, bool createDB) {
            int rows = 0;
        	
            using (ISqlConnection conn = CreateConnection()) {
                conn.Open();
                if (!createDB && MultipleSchema)
                    conn.ChangeDatabase(Server.Config.MySQLDatabaseName);
                
                using (ISqlOrder ord = conn.CreateOrder(sql)) {
                    FillParams(ord, parameters);
                    rows = ord.ExecuteNonQuery();
                }
                conn.Close();
            }
            return rows;
        }

        /// <summary> Excecutes an SQL query, invoking a callback on the returned rows one by one. </summary>        
        public int Iterate(string sql, object[] parameters, ReaderCallback callback) {
            int rows = 0;
        	
            using (ISqlConnection conn = CreateConnection()) {
                conn.Open();
                if (MultipleSchema)
                    conn.ChangeDatabase(Server.Config.MySQLDatabaseName);
                
                using (ISqlOrder ord = conn.CreateOrder(sql)) {
                    FillParams(ord, parameters);
                    using (ISqlReader reader = ord.ExecuteReader()) {
                        while (reader.Read()) { callback(reader); rows++; }
                    }
                }
                conn.Close();
            }
            return rows;
        }
        
        
        /// <summary> Sets the SQL order's parameter values to the given arguments </summary>
        public static void FillParams(ISqlOrder ord, object[] parameters) {
            if (parameters == null || parameters.Length == 0) return;
            
            string[] names = GetNames(parameters.Length);
            for (int i = 0; i < parameters.Length; i++) 
            {
                ord.AddParameter(names[i], parameters[i]);
            }
        }
        
        volatile static string[] ids;
        internal static string[] GetNames(int count) {
            // Avoid allocation overhead from string concat every query by caching
            string[] names = ids;
            if (names == null || count > names.Length) {
                names = new string[count];
                for (int i = 0; i < names.Length; i++) { names[i] = "@" + i; }
                ids = names;
            }
            return names;
        }
        #endregion
    }
}