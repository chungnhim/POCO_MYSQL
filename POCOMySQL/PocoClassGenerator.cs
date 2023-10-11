using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
namespace POCOMySQL
{


    [Flags]
    public enum GeneratorBehavior
    {
        Default = 0x0,
        View = 0x1,
        DapperContrib = 0x2,
        Comment = 0x4
    }

    public static partial class PocoClassGenerator
    {
        #region Property
        private static readonly Dictionary<Type, string> TypeAliases = new Dictionary<Type, string> {
               { typeof(int), "int" },
               { typeof(short), "short" },
               { typeof(byte), "byte" },
               { typeof(byte[]), "byte[]" },
               { typeof(long), "long" },
               { typeof(double), "double" },
               { typeof(decimal), "decimal" },
               { typeof(float), "float" },
               { typeof(bool), "bool" },
               { typeof(string), "string" },
               { typeof(DateTime),"DateTime"},
               { typeof(UInt32),"int"}

       };

        private static readonly Dictionary<Type, string> MySqlTypeAliases = new Dictionary<Type, string> {
               { typeof(int), "MySqlDbType.Int32" },
               { typeof(short), "MySqlDbType.Int16" },
               { typeof(byte), "MySqlDbType.Byte" },
               { typeof(byte[]), "MySqlDbType.VarBinary" },
               { typeof(long), "MySqlDbType.Int32" },
               { typeof(double), "MySqlDbType.Double" },
               { typeof(decimal), "MySqlDbType.Decimal" },
               { typeof(float), "MySqlDbType.Float" },
               { typeof(bool), "MySqlDbType.Bit" },
               { typeof(string), "MySqlDbType.VarChar" },
               { typeof(DateTime),"MySqlDbType.DateTime"},
               { typeof(UInt32),"MySqlDbType.Int32"}

       };
        private static readonly Dictionary<string, string> QuerySqls = new Dictionary<string, string> {
               {"sqlconnection", "select  *  from [{0}] where 1=2" },
               {"sqlceserver", "select  *  from [{0}] where 1=2" },
               {"sqliteconnection", "select  *  from [{0}] where 1=2" },
               {"oracleconnection", "select  *  from \"{0}\" where 1=2" },
               {"mysqlconnection", "select  *  from `{0}` where 1=2" },
               {"npgsqlconnection", "select  *  from \"{0}\" where 1=2" }
       };

        private static readonly Dictionary<string, string> TableSchemaSqls = new Dictionary<string, string> {
               {"sqlconnection", "select TABLE_NAME from INFORMATION_SCHEMA.TABLES where TABLE_TYPE = 'BASE TABLE'" },
               {"sqlceserver", "select TABLE_NAME from INFORMATION_SCHEMA.TABLES  where TABLE_TYPE = 'BASE TABLE'" },
               {"sqliteconnection", "SELECT name FROM sqlite_master where type = 'table'" },
               {"oracleconnection", "select TABLE_NAME from USER_TABLES where table_name not in (select View_name from user_views)" },
               {"mysqlconnection", "select TABLE_NAME from  information_schema.tables where table_type = 'BASE TABLE'" },
               {"npgsqlconnection", "select table_name from information_schema.tables where table_type = 'BASE TABLE'" }
       };


        private static readonly HashSet<Type> NullableTypes = new HashSet<Type> {
               typeof(int),
               typeof(short),
               typeof(long),
               typeof(double),
               typeof(decimal),
               typeof(float),
               typeof(bool),
               typeof(DateTime)
       };
        #endregion

        public static string GenerateAllTables(this System.Data.Common.DbConnection connection, GeneratorBehavior generatorBehavior = GeneratorBehavior.Default)
        {
            if (connection.State != ConnectionState.Open) connection.Open();

            var conneciontName = connection.GetType().Name.ToLower();
            var tables = new List<string>();
            var sql = generatorBehavior.HasFlag(GeneratorBehavior.View) ? TableSchemaSqls[conneciontName].Split("where")[0] : TableSchemaSqls[conneciontName];
            using (var command = connection.CreateCommand(sql))
            using (var reader = command.ExecuteReader())
            {
                while (reader.Read())
                    tables.Add(reader.GetString(0));
            }

            var sb = new StringBuilder();
            sb.AppendLine("namespace Models { ");
            tables.ForEach(table => sb.Append(connection.GenerateClass(
                   string.Format(QuerySqls[conneciontName], table), table, generatorBehavior: generatorBehavior
            )));
            sb.AppendLine("}");
            return sb.ToString();
        }

        public static string GenerateClass(this IDbConnection connection, string sql, GeneratorBehavior generatorBehavior)
             => connection.GenerateClass(sql, null, generatorBehavior);

        public static string GenerateClass(this IDbConnection connection, string sql, string className = null, GeneratorBehavior generatorBehavior = GeneratorBehavior.Default)
        {
            if (connection.State != ConnectionState.Open) connection.Open();

            var builder = new StringBuilder();

            //Get Table Name
            //Fix : [When View using CommandBehavior.KeyInfo will get duplicate columns ¡P Issue #8 ¡P shps951023/PocoClassGenerator](https://github.com/shps951023/PocoClassGenerator/issues/8 )
            var isFromMutiTables = false;
            using (var command = connection.CreateCommand(sql))
            using (var reader = command.ExecuteReader(CommandBehavior.KeyInfo | CommandBehavior.SingleRow))
            {
                var tables = reader.GetSchemaTable().Select().Select(s => s["BaseTableName"] as string).Distinct();
                var tableName = string.IsNullOrWhiteSpace(className) ? tables.First() ?? "Info" : className;

                isFromMutiTables = tables.Count() > 1;

                if (generatorBehavior.HasFlag(GeneratorBehavior.DapperContrib) && !isFromMutiTables)
                    builder.AppendFormat("	[Dapper.Contrib.Extensions.Table(\"{0}\")]{1}", tableName, Environment.NewLine);
                builder.AppendFormat("	public class {0}Dto{1}", tableName.Replace(" ", ""), Environment.NewLine);
                builder.AppendLine("	{");
            }

            //Get Columns 
            var behavior = isFromMutiTables ? (CommandBehavior.SchemaOnly | CommandBehavior.SingleRow) : (CommandBehavior.KeyInfo | CommandBehavior.SingleRow);

            using (var command = connection.CreateCommand(sql))
            using (var reader = command.ExecuteReader(behavior))
            {
                do
                {
                    var schema = reader.GetSchemaTable();
                    foreach (DataRow row in schema.Rows)
                    {
                        var type = (Type)row["DataType"];
                        var name = TypeAliases.ContainsKey(type) ? TypeAliases[type] : type.FullName;
                        var isNullable = (bool)row["AllowDBNull"] && NullableTypes.Contains(type);
                        var collumnName = (string)row["ColumnName"];

                        if (generatorBehavior.HasFlag(GeneratorBehavior.Comment) && !isFromMutiTables)
                        {
                            var comments = new[] { "DataTypeName", "IsUnique", "IsKey", "IsAutoIncrement", "IsReadOnly" }
                                   .Select(s =>
                                   {
                                       if (row[s] is bool && ((bool)row[s]))
                                           return s;
                                       if (row[s] is string && !string.IsNullOrWhiteSpace((string)row[s]))
                                           return string.Format(" {0} : {1} ", s, row[s]);
                                       return null;
                                   }).Where(w => w != null).ToArray();
                            var sComment = string.Join(" , ", comments);

                            builder.AppendFormat("		/// <summary>{0}</summary>{1}", sComment, Environment.NewLine);
                        }

                        if (generatorBehavior.HasFlag(GeneratorBehavior.DapperContrib) && !isFromMutiTables)
                        {
                            var isKey = (bool)row["IsKey"];
                            var isAutoIncrement = (bool)row["IsAutoIncrement"];
                            if (isKey && isAutoIncrement)
                                builder.AppendLine("		[Dapper.Contrib.Extensions.Key]");
                            if (isKey && !isAutoIncrement)
                                builder.AppendLine("		[Dapper.Contrib.Extensions.ExplicitKey]");
                            if (!isKey && isAutoIncrement)
                                builder.AppendLine("		[Dapper.Contrib.Extensions.Computed]");
                        }

                        builder.AppendLine(string.Format("		public {0}{1} {2} {{ get; set; }}", name, isNullable ? "?" : string.Empty, collumnName));
                    }

                    builder.AppendLine("	}");
                    builder.AppendLine();
                } while (reader.NextResult());

                return builder.ToString();
            }
        }
        public static string GenerateClassParameter(this IDbConnection connection, string sql, string className = null, GeneratorBehavior generatorBehavior = GeneratorBehavior.Default)
        {
            if (connection.State != ConnectionState.Open) connection.Open();

            var builder = new StringBuilder();

            //Get Table Name
            //Fix : [When View using CommandBehavior.KeyInfo will get duplicate columns ¡P Issue #8 ¡P shps951023/PocoClassGenerator](https://github.com/shps951023/PocoClassGenerator/issues/8 )
            var isFromMutiTables = false;
            using (var command = connection.CreateCommand(sql))
            using (var reader = command.ExecuteReader(CommandBehavior.KeyInfo | CommandBehavior.SingleRow))
            {
                var tables = reader.GetSchemaTable().Select().Select(s => s["BaseTableName"] as string).Distinct();
                var tableName = string.IsNullOrWhiteSpace(className) ? tables.First() ?? "Info" : className;

                isFromMutiTables = tables.Count() > 1;

                if (generatorBehavior.HasFlag(GeneratorBehavior.DapperContrib) && !isFromMutiTables)
                    builder.AppendFormat("	[Dapper.Contrib.Extensions.Table(\"{0}\")]{1}", tableName, Environment.NewLine);
            }

            //Get Columns 
            var behavior = isFromMutiTables ? (CommandBehavior.SchemaOnly | CommandBehavior.SingleRow) : (CommandBehavior.KeyInfo | CommandBehavior.SingleRow);
            builder.AppendLine("List<MySqlParameter> listParam = new List<MySqlParameter>();");
            using (var command = connection.CreateCommand(sql))
            using (var reader = command.ExecuteReader(behavior))
            {
                do
                {
                    var schema = reader.GetSchemaTable();
                    foreach (DataRow row in schema.Rows)
                    {
                        var type = (Type)row["DataType"];
                        var name = MySqlTypeAliases.ContainsKey(type) ? MySqlTypeAliases[type] : type.FullName;
                        var isNullable = (bool)row["AllowDBNull"] && NullableTypes.Contains(type);
                        var collumnName = (string)row["ColumnName"];
                        builder.AppendLine(string.Format("MySqlParameter {0} = new MySqlParameter(\"@{1}\", {2});",collumnName.ToLower(),collumnName, name));
                        builder.AppendLine(string.Format("{0}.Value = model.{1};", collumnName.ToLower(), collumnName));
                        builder.AppendLine(string.Format("listParam.Add({0});", collumnName.ToLower()));

                       
                    }

                   
                    builder.AppendLine();
                } while (reader.NextResult());

                return builder.ToString();
            }
        }

        public static string GenerateClassModel(this IDbConnection connection, string sql, string className = null, GeneratorBehavior generatorBehavior = GeneratorBehavior.Default)
        {
            if (connection.State != ConnectionState.Open) connection.Open();

            var builder = new StringBuilder();
            var builderFromDto = new StringBuilder();

            //Get Table Name
            //Fix : [When View using CommandBehavior.KeyInfo will get duplicate columns ¡P Issue #8 ¡P shps951023/PocoClassGenerator](https://github.com/shps951023/PocoClassGenerator/issues/8 )
            var isFromMutiTables = false;
            using (var command = connection.CreateCommand(sql))
            using (var reader = command.ExecuteReader(CommandBehavior.KeyInfo | CommandBehavior.SingleRow))
            {
                var tables = reader.GetSchemaTable().Select().Select(s => s["BaseTableName"] as string).Distinct();
                var tableName = string.IsNullOrWhiteSpace(className) ? tables.First() ?? "Info" : className;

                isFromMutiTables = tables.Count() > 1;

                if (generatorBehavior.HasFlag(GeneratorBehavior.DapperContrib) && !isFromMutiTables)
                    builder.AppendFormat("	[Dapper.Contrib.Extensions.Table(\"{0}\")]{1}", tableName, Environment.NewLine);
                builder.AppendFormat("	public class {0}Model {1}", tableName.Replace(" ", ""), Environment.NewLine);
                builder.AppendLine("	{");

                builderFromDto.AppendFormat("public void FromDto({0}Dto objDto)", tableName);
                builderFromDto.AppendLine("	{");
            }

            //Get Columns 
            var behavior = isFromMutiTables ? (CommandBehavior.SchemaOnly | CommandBehavior.SingleRow) : (CommandBehavior.KeyInfo | CommandBehavior.SingleRow);

            using (var command = connection.CreateCommand(sql))
            using (var reader = command.ExecuteReader(behavior))
            {
                do
                {
                    var schema = reader.GetSchemaTable();
                    foreach (DataRow row in schema.Rows)
                    {
                        var type = (Type)row["DataType"];
                        var name = TypeAliases.ContainsKey(type) ? TypeAliases[type] : type.FullName;
                        var isNullable = (bool)row["AllowDBNull"] && NullableTypes.Contains(type);
                        var collumnName = (string)row["ColumnName"];
                        builder.AppendLine(string.Format("		public {0}{1} {2} {{ get; set; }}", name, isNullable ? "?" : string.Empty, collumnName));
                        builderFromDto.AppendLine(string.Format("   {0} = objDto.{0};",collumnName));

                    }
                    //build from dto
                    builderFromDto.AppendLine("	}");
                    builder.Append(builderFromDto);
                    builder.AppendLine();
                    builder.AppendLine("	}");
                    builder.AppendLine();
                } while (reader.NextResult());

                return builder.ToString();
            }
        }


        public static string GenerateSqlInsert(this IDbConnection connection, string sql, string className = null, GeneratorBehavior generatorBehavior = GeneratorBehavior.Default)
        {
            if (connection.State != ConnectionState.Open) connection.Open();

            List<string> colList = new List<string>();
            List<string> paramList  = new List<string>();

            var builder = new StringBuilder();
            var builderParam = new StringBuilder();
            //Get Table Name
            var isFromMutiTables = false;
            using (var command = connection.CreateCommand(sql))
            using (var reader = command.ExecuteReader(CommandBehavior.KeyInfo | CommandBehavior.SingleRow))
            {
                var tables = reader.GetSchemaTable().Select().Select(s => s["BaseTableName"] as string).Distinct();
                var tableName = string.IsNullOrWhiteSpace(className) ? tables.First() ?? "Info" : className;

                isFromMutiTables = tables.Count() > 1;

                if (generatorBehavior.HasFlag(GeneratorBehavior.DapperContrib) && !isFromMutiTables)
                    builder.AppendFormat("	[Dapper.Contrib.Extensions.Table(\"{0}\")]{1}", tableName, Environment.NewLine);
                builder.AppendFormat("INSERT INTO {0}{1}", tableName.Replace(" ", ""), Environment.NewLine);
            }
            builder.Append("(");
            builderParam.Append("(");

            //Get Columns 
            var behavior = isFromMutiTables ? (CommandBehavior.SchemaOnly | CommandBehavior.SingleRow) : (CommandBehavior.KeyInfo | CommandBehavior.SingleRow);

            using (var command = connection.CreateCommand(sql))
            using (var reader = command.ExecuteReader(behavior))
            {
                do
                {
                    var schema = reader.GetSchemaTable();
                    foreach (DataRow row in schema.Rows)
                    {
                        var type = (Type)row["DataType"];
                        var name = TypeAliases.ContainsKey(type) ? TypeAliases[type] : type.FullName;
                        var isNullable = (bool)row["AllowDBNull"] && NullableTypes.Contains(type);
                        var collumnName = (string)row["ColumnName"];
                        var isKey  = (bool)row["IsKey"];
                        var isAutoIncrement = (bool)row["IsAutoIncrement"];
                        if(!isKey && !isAutoIncrement)
                        {
                            colList.Add(string.Format("`{0}`", collumnName));
                            paramList.Add(string.Format("@{0}", collumnName));
                        }
                    }
                    builder.Append(string.Join(", ",colList));
                    builderParam.Append(string.Join(", ", paramList));

                    builder.Append(")");
                    builderParam.Append(")");
                    builder.AppendLine();
                    builder.Append("VALUES ");
                    builder.AppendLine();
                    builder.Append(builderParam);
                } while (reader.NextResult());
                return builder.ToString();
            }
        }

        public static string GenerateSqlUpdate(this IDbConnection connection, string sql, string className = null, GeneratorBehavior generatorBehavior = GeneratorBehavior.Default)
        {
            if (connection.State != ConnectionState.Open) connection.Open();

            List<string> colList = new List<string>();
            List<string> paramList = new List<string>();
            List<string> updateList = new List<string>();

            var builder = new StringBuilder();
            var builderParam = new StringBuilder();
            //Get Table Name
            var isFromMutiTables = false;
            using (var command = connection.CreateCommand(sql))
            using (var reader = command.ExecuteReader(CommandBehavior.KeyInfo | CommandBehavior.SingleRow))
            {
                var tables = reader.GetSchemaTable().Select().Select(s => s["BaseTableName"] as string).Distinct();
                var tableName = string.IsNullOrWhiteSpace(className) ? tables.First() ?? "Info" : className;

                isFromMutiTables = tables.Count() > 1;

                if (generatorBehavior.HasFlag(GeneratorBehavior.DapperContrib) && !isFromMutiTables)
                    builder.AppendFormat("	[Dapper.Contrib.Extensions.Table(\"{0}\")]{1}", tableName, Environment.NewLine);
                builder.AppendFormat("UPDATE {0} SET {1}", tableName.Replace(" ", ""), Environment.NewLine);
            }


            //Get Columns 
            var behavior = isFromMutiTables ? (CommandBehavior.SchemaOnly | CommandBehavior.SingleRow) : (CommandBehavior.KeyInfo | CommandBehavior.SingleRow);

            using (var command = connection.CreateCommand(sql))
            using (var reader = command.ExecuteReader(behavior))
            {
                do
                {
                    var schema = reader.GetSchemaTable();
                    foreach (DataRow row in schema.Rows)
                    {
                        var type = (Type)row["DataType"];
                        var name = TypeAliases.ContainsKey(type) ? TypeAliases[type] : type.FullName;
                        var isNullable = (bool)row["AllowDBNull"] && NullableTypes.Contains(type);
                        var collumnName = (string)row["ColumnName"];
                        var isKey = (bool)row["IsKey"];
                        var isAutoIncrement = (bool)row["IsAutoIncrement"];
                        if (!isKey && !isAutoIncrement)
                        {
                            updateList.Add(string.Format("`{0}` = @{1}", collumnName, collumnName));
                        }
                        else
                        {
                            paramList.Add(string.Format("`{0}` = @{1}", collumnName, collumnName));
                        }

                    }
                    builder.Append(string.Join(", \r\n", updateList));
                    builder.AppendLine();
                    builder.Append("WHERE ");
                    builder.AppendLine();
                    builder.Append(string.Join(" And ", paramList));
                    builder.AppendLine();
                } while (reader.NextResult());
                return builder.ToString();
            }
        }


        #region Private
        private static string[] Split(this string text, string splitText) => text.Split(new[] { splitText }, StringSplitOptions.None);
        private static IDbCommand CreateCommand(this IDbConnection connection, string sql)
        {
            var cmd = connection.CreateCommand();
            cmd.CommandText = sql;
            return cmd;
        }
        #endregion
    }
}
