using HelperLibrary.Database;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using HelperLibrary.Database.Models;

namespace HelperLibrary.Database
{
    public class SQLCmd
    {
        public static DbConnection Connection { get; set; }
        private static string _db;
        private static string _table;
        private static MySQLCommandBuilder _builder;
        private SQLCommandTypes cmdTpye;


        public SQLCmd(DbConnection connection, SQLCommandTypes cmdtype)
        {
            Connection = connection;
            cmdTpye = cmdtype;
            _builder = new MySQLCommandBuilder(cmdTpye, _db ?? "trading", _table ?? "yahoo_data");
        }

        public SQLCmd IsNotNull(string field)
        {
            _builder.IsNotNull(field);
            return this;
        }

        public static SQLCmd Select(string database, string table)
        {
            CheckConnection();
            _db = database;
            _table = table;
            var cmd = new SQLCmd(Connection, SQLCommandTypes.Select);
            return cmd;
        }

        public static SQLCmd Call()
        {
            CheckConnection();
            var cmd = new SQLCmd(Connection, SQLCommandTypes.Call);
            return cmd;

        }

        public SQLCmd Procedure(string procedureName, params object[] arguments)
        {
            _builder.CallProcedure(procedureName, arguments);
            return this;
        }


        public SQLCmd Equal(params object[] values)
        {
            _builder.Equal(values);
            return this;
        }

        public SQLCmd Less(params object[] values)
        {
            _builder.Less(values);
            return this;
        }

        public SQLCmd Greater(params object[] values)
        {
            _builder.Greater(values);
            return this;
        }

        public SQLCmd Fields(params string[] fields)
        {
            _builder.Fields(fields);
            return this;
        }

        public static IDataReader Query(SQLCmd sqlCmd)
        {
            return DbTools.CreateQuery(sqlCmd, Connection, _builder);
        }

        public IEnumerable<T> QueryObjects<T>()
        {
            using (var rd = Query(this))
            {
                while (rd?.Read() == true)
                    yield return ObjectMapper<T>.Create(rd);
            }
        }
        public T QuerySingle<T>()
        {
            using (var rd = Query(this))
            {
                while (rd.Read())
                {
                    var item = (T)rd[0];
                    return item;
                }
            }
            return default(T);
        }

        public HashSet<string> QueryKeySet()
        {
            using (var rd = Query(this))
            {
                var tmp = new HashSet<string>();
                while (rd.Read())
                {
                    var asof = (DateTime)rd[0];
                    var secId = (int)rd[1];
                    tmp.Add($"{asof.Date}_{secId}");
                }
                return tmp.Count > 0 ? tmp : null;
            }
        }


        public HashSet<string> QueryKeySet(Type type1, Type type2)
        {
            using (var rd = Query(this))
            {
                var tmp = new HashSet<string>();
                while (rd.Read())
                {
                    var item1 = DbTools.CastType(rd[0], type1);
                    var item2 = DbTools.CastType(rd[1], type2);
                    tmp.Add(item1.ToString() + "_" + item2.ToString());
                }
                return tmp.Count > 0 ? tmp : null;
            }
        }


        public HashSet<T> QueryHashSet<T>(bool appendFields = false)
        {
            using (var rd = Query(this))
            {
                var tmp = new HashSet<T>();
                while (rd.Read())
                {
                    if (appendFields)
                    {
                        var tmpAppend = new HashSet<string>();
                        var item1 = (T)rd[0];
                        var item2 = (T)rd[1];
                        tmpAppend.Add(item1.ToString() + "_" + item2.ToString());
                    }
                    else
                    {
                        var item = (T)rd[0];
                        tmp.Add(item);
                    }
                }
                return tmp.Count > 0 ? tmp : null;
            }
        }

        public List<T> QueryList<T>()
        {
            using (var rd = Query(this))
            {
                var tmp = new List<T>();
                while (rd.Read())
                {

                    var item = (T)rd[0];
                    tmp.Add(item);
                }
                return tmp.Count > 0 ? tmp : null;
            }
        }

        public Dictionary<TKey, TValue> QueryDictionary<TKey, TValue>()
        {
            using (var rd = Query(this))
            {
                var tmp = new Dictionary<TKey, TValue>();
                while (rd.Read())
                {

                    var key = (TKey)rd[0];
                    var value = (TValue)rd[1];
                    if (!tmp.ContainsKey(key))
                        tmp.Add(key, value);
                }
                return tmp.Count > 0 ? tmp : null;
            }
        }

        public static SQLCmd Delete(string database, string table)
        {
            CheckConnection();
            _db = database;
            _table = table;
            var cmd = new SQLCmd(Connection, SQLCommandTypes.Delete);
            return cmd;
        }

        public static SQLCmd Update(string database, string table)
        {
            CheckConnection();
            _db = database;
            _table = table;
            var cmd = new SQLCmd(Connection, SQLCommandTypes.Update);
            return cmd;
        }

        public static SQLCmd Insert(string database, string table)
        {
            CheckConnection();
            _db = database;
            _table = table;
            var cmd = new SQLCmd(Connection, SQLCommandTypes.Insert);
            return cmd;
        }


        public SQLCmd InList(string field, IEnumerable<object> secIds)
        {
            _builder.CreateValueTypesCmd(SQLValueTypes.InList, secIds.ToArray(), field);
            return this;
        }

        public SQLCmd Values(params object[] values)
        {
            if (values.Length % 2 != 0)
                throw new ArgumentException("Es wird ein KeyValue-Pair erwartet");
            for (int i = 0; i < values.Length; i += 2)
            {
                if (!(values[i] is string))
                    throw new ArgumentException("Der FieldnameWert muss vom Typ String sein");
            }


            if (cmdTpye == SQLCommandTypes.Update)
            {
                _builder.CreateValueTypesCmd(SQLValueTypes.UpdateValues, values);
            }
            else
            {
                _builder.CreateValueTypesCmd(SQLValueTypes.Values, values);
            }
            return this;
        }



        private static void CheckConnection()
        {
            if (Connection == null)
                throw new Exception("Achtung noch keine Datenbankverbindung hergestellt");
            else if (Connection.State != ConnectionState.Open)
                throw new Exception("Achtung Connection ist nicht offen");
        }

        public static void Execute()
        {
            DbTools.Exec(Connection, _builder);
        }


    }
}