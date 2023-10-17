using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
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
                        builder.AppendLine(string.Format("MySqlParameter {0} = new MySqlParameter(\"@{1}\", {2});", collumnName.ToLower(), collumnName, name));
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
                        builderFromDto.AppendLine(string.Format("   {0} = objDto.{0};", collumnName));

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
            List<string> paramList = new List<string>();

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
                        var isKey = (bool)row["IsKey"];
                        var isAutoIncrement = (bool)row["IsAutoIncrement"];
                        if (!isKey && !isAutoIncrement)
                        {
                            colList.Add(string.Format("`{0}`", collumnName));
                            paramList.Add(string.Format("@{0}", collumnName));
                        }
                    }
                    builder.Append(string.Join(", ", colList));
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

        #region Create all file

        public static string GenerateClassDtoToFile(this IDbConnection connection, string sql, string className = null, GeneratorBehavior generatorBehavior = GeneratorBehavior.Default)
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

                TextInfo textInfo = new CultureInfo("en-US", false).TextInfo;
                tableName = textInfo.ToTitleCase(tableName);

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

        public static string GenerateClassModelToFile(this IDbConnection connection, string sql, string className = null, GeneratorBehavior generatorBehavior = GeneratorBehavior.Default)
        {
            if (connection.State != ConnectionState.Open) connection.Open();

            var builderView = new StringBuilder();
            var builderCreate = new StringBuilder();
            var builderEdit = new StringBuilder();

            //Get Table Name
            //Fix : [When View using CommandBehavior.KeyInfo will get duplicate columns ¡P Issue #8 ¡P shps951023/PocoClassGenerator](https://github.com/shps951023/PocoClassGenerator/issues/8 )
            var isFromMutiTables = false;
            using (var command = connection.CreateCommand(sql))
            using (var reader = command.ExecuteReader(CommandBehavior.KeyInfo | CommandBehavior.SingleRow))
            {
                var tables = reader.GetSchemaTable().Select().Select(s => s["BaseTableName"] as string).Distinct();
                var tableName = string.IsNullOrWhiteSpace(className) ? tables.First() ?? "Info" : className;

                TextInfo textInfo = new CultureInfo("en-US", false).TextInfo;
                tableName = textInfo.ToTitleCase(tableName);

                isFromMutiTables = tables.Count() > 1;

                if (generatorBehavior.HasFlag(GeneratorBehavior.DapperContrib) && !isFromMutiTables)
                {
                    builderView.AppendFormat("	[Dapper.Contrib.Extensions.Table(\"{0}\")]{1}", tableName, Environment.NewLine);
                    builderCreate.AppendFormat("	[Dapper.Contrib.Extensions.Table(\"{0}\")]{1}", tableName, Environment.NewLine);
                    builderEdit.AppendFormat("	[Dapper.Contrib.Extensions.Table(\"{0}\")]{1}", tableName, Environment.NewLine);
                }

                builderView.AppendFormat("	public class View{0}Model{1}", tableName.Replace(" ", ""), Environment.NewLine);
                builderView.AppendLine("	{");

                builderCreate.AppendFormat("	public class Create{0}Model{1}", tableName.Replace(" ", ""), Environment.NewLine);
                builderCreate.AppendLine("	{");

                builderEdit.AppendFormat("	public class Edit{0}Model{1}", tableName.Replace(" ", ""), Environment.NewLine);
                builderEdit.AppendLine("	{");

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

                            builderView.AppendFormat("		/// <summary>{0}</summary>{1}", sComment, Environment.NewLine);
                            builderCreate.AppendFormat("		/// <summary>{0}</summary>{1}", sComment, Environment.NewLine);
                            builderEdit.AppendFormat("		/// <summary>{0}</summary>{1}", sComment, Environment.NewLine);
                        }

                        if (generatorBehavior.HasFlag(GeneratorBehavior.DapperContrib) && !isFromMutiTables)
                        {
                            var isKey = (bool)row["IsKey"];
                            var isAutoIncrement = (bool)row["IsAutoIncrement"];
                            if (isKey && isAutoIncrement)
                            {
                                builderView.AppendLine("		[Dapper.Contrib.Extensions.Key]");
                                builderCreate.AppendLine("		[Dapper.Contrib.Extensions.Key]");
                                builderEdit.AppendLine("		[Dapper.Contrib.Extensions.Key]");
                            }

                            if (isKey && !isAutoIncrement)
                            {
                                builderView.AppendLine("		[Dapper.Contrib.Extensions.ExplicitKey]");
                                builderCreate.AppendLine("		[Dapper.Contrib.Extensions.ExplicitKey]");
                                builderEdit.AppendLine("		[Dapper.Contrib.Extensions.ExplicitKey]");
                            }
                            if (!isKey && isAutoIncrement)
                            {
                                builderView.AppendLine("		[Dapper.Contrib.Extensions.Computed]");
                                builderCreate.AppendLine("		[Dapper.Contrib.Extensions.Computed]");
                                builderEdit.AppendLine("		[Dapper.Contrib.Extensions.Computed]");
                            }

                        }

                        builderView.AppendLine(string.Format("		public {0}{1} {2} {{ get; set; }}", name, isNullable ? "?" : string.Empty, collumnName));
                        builderCreate.AppendLine(string.Format("		public {0}{1} {2} {{ get; set; }}", name, isNullable ? "?" : string.Empty, collumnName));
                        builderEdit.AppendLine(string.Format("		public {0}{1} {2} {{ get; set; }}", name, isNullable ? "?" : string.Empty, collumnName));
                    }

                    builderView.AppendLine("	}");
                    builderView.AppendLine();

                    builderCreate.AppendLine("	}");
                    builderCreate.AppendLine();

                    builderEdit.AppendLine("	}");
                    builderEdit.AppendLine();


                } while (reader.NextResult());

                return builderView.ToString() + builderCreate.ToString() + builderEdit.ToString();
            }
        }


        public static string GenerateClassServiceToFile(this IDbConnection connection, string sql, string className = null, GeneratorBehavior generatorBehavior = GeneratorBehavior.Default)
        {
            if (connection.State != ConnectionState.Open) connection.Open();

            var builderView = new StringBuilder();
            var builderCreate = new StringBuilder();
            var builderEdit = new StringBuilder();
            var tableSelect = "";



            //Get Table Name
            //Fix : [When View using CommandBehavior.KeyInfo will get duplicate columns ¡P Issue #8 ¡P shps951023/PocoClassGenerator](https://github.com/shps951023/PocoClassGenerator/issues/8 )
            var isFromMutiTables = false;
            using (var command = connection.CreateCommand(sql))
            using (var reader = command.ExecuteReader(CommandBehavior.KeyInfo | CommandBehavior.SingleRow))
            {
                var tables = reader.GetSchemaTable().Select().Select(s => s["BaseTableName"] as string).Distinct();
                var tableName = string.IsNullOrWhiteSpace(className) ? tables.First() ?? "Info" : className;

                TextInfo textInfo = new CultureInfo("en-US", false).TextInfo;
                tableName = textInfo.ToTitleCase(tableName);
                tableSelect = tableName;

                isFromMutiTables = tables.Count() > 1;
            }

            //Get Columns 
            var behavior = isFromMutiTables ? (CommandBehavior.SchemaOnly | CommandBehavior.SingleRow) : (CommandBehavior.KeyInfo | CommandBehavior.SingleRow);

            var nameDtoModel = $"{tableSelect}Dto";
            var nameCreateModel = $"Create{tableSelect}Model";
            var nameViewModel = $"View{tableSelect}Model";
            var nameEditModel = $"Edit{tableSelect}Model";


            //create interface
            builderView.AppendLine($"\t\tpublic interface I{tableSelect}Service");
            builderView.AppendLine("\t\t{");
            builderView.AppendLine($"\t\t\tpublic Task<{nameDtoModel}> Get{tableSelect}ById(int id);");
            builderView.AppendLine($"\t\t\tpublic Task<int> Insert{tableSelect}({nameDtoModel} obj{nameDtoModel});");
            builderView.AppendLine($"\t\t\tpublic Task<int> Update{tableSelect}({nameDtoModel} obj{nameDtoModel});");
            builderView.AppendLine($"\t\t\tpublic Task<int> Delete{tableSelect}(int id);");
            builderView.AppendLine($"\t\t\tpublic Task<DataTableViewModel<{nameViewModel}>> Get{tableSelect}Search(FilterTableParams tableParams, string search = \"\", DateTime? fromDate = null, DateTime? toDate = null);");
            builderView.AppendLine("\t\t}");

            builderView.AppendLine($"\t\tpublic class {tableSelect}Service : I{tableSelect}Service");
            builderView.AppendLine("\t\t{");
            //Add contructor
            builderView.AppendLine("\t\t\tprivate readonly IMySqlServerRepository repository;");
            builderView.AppendLine($"\t\t\tpublic {tableSelect}Service(IMySqlServerRepository repository)");
            builderView.AppendLine("\t\t\t{");
            builderView.AppendLine("\t\t\t\tthis.repository = repository;");
            builderView.AppendLine("\t\t\t}");
            //function get
            builderView.AppendLine($"\t\t\tpublic async Task<{nameDtoModel}> Get{tableSelect}ById(int id)");
            builderView.AppendLine("\t\t\t{");
            builderView.AppendLine("\t\t\t\tusing (var dbContext = await repository.OpenConnectionAsync())");
            builderView.AppendLine("\t\t\t\t{");
            builderView.AppendLine($"\t\t\t\t\t var query = @\"SELECT * FROM {tableSelect.ToLower()} where id=@id\";");
            builderView.AppendLine($"\t\t\t\t\t return (await dbContext.QueryAsync<{nameDtoModel}>(query, new {{@id = id}})).FirstOrDefault();");
            builderView.AppendLine("\t\t\t\t}");
            builderView.AppendLine("\t\t\t}");
            //end function get

            //function insert
            builderView.AppendLine($"\t\t\tpublic async Task<int> Insert{tableSelect}({nameDtoModel} obj{nameDtoModel})");
            builderView.AppendLine("\t\t\t{");
            builderView.AppendLine("\t\t\t\tusing (var dbContext = await repository.OpenConnectionAsync())");
            builderView.AppendLine("\t\t\t\t{");
            //get sqlupdate
            var sqlInsert = connection.GenerateSqlInsert(sql);

            builderView.AppendLine($"\t\t\t\t\t var query = @\"{sqlInsert}\";");
            builderView.AppendLine($"\t\t\t\t\t return await dbContext.ExecuteAsync(query, obj{nameDtoModel});");
            builderView.AppendLine("\t\t\t\t}");
            builderView.AppendLine("\t\t\t}");
            //end function insert

            //function update
            builderView.AppendLine($"\t\t\tpublic async Task<int> Update{tableSelect}({nameDtoModel} obj{nameDtoModel})");
            builderView.AppendLine("\t\t\t{");
            builderView.AppendLine("\t\t\t\tusing (var dbContext = await repository.OpenConnectionAsync())");
            builderView.AppendLine("\t\t\t\t{");
            //get sqlupdate
            var sqlUpdate = connection.GenerateSqlUpdate(sql);

            builderView.AppendLine($"\t\t\t\t\t var query = @\"{sqlUpdate}\";");
            builderView.AppendLine($"\t\t\t\t\t return await dbContext.ExecuteAsync(query, obj{nameDtoModel});");
            builderView.AppendLine("\t\t\t\t}");
            builderView.AppendLine("\t\t\t}");
            //end function update

            //function delete
            builderView.AppendLine($"\t\t\tpublic async Task<int> Delete{tableSelect}(int id)");
            builderView.AppendLine("\t\t\t{");
            builderView.AppendLine("\t\t\t\tusing (var dbContext = await repository.OpenConnectionAsync())");
            builderView.AppendLine("\t\t\t\t{");
            builderView.AppendLine($"\t\t\t\t\t var query = @\"DELETE FROM {tableSelect.ToLower()} where id=@id\";");
            builderView.AppendLine($"\t\t\t\t\t return await dbContext.ExecuteAsync(query, new {{@id = id}});");
            builderView.AppendLine("\t\t\t\t}");
            builderView.AppendLine("\t\t\t}");
            //end function delete

            //function search
            builderView.AppendLine($"\t\t\tpublic async Task<DataTableViewModel<{nameViewModel}>> Get{tableSelect}Search(FilterTableParams tableParams, string search = \"\", DateTime? fromDate = null, DateTime? toDate = null)");
            builderView.AppendLine("\t\t\t{");
            builderView.AppendLine("\t\t\t\tusing (var dbContext = await repository.OpenConnectionAsync())");
            builderView.AppendLine("\t\t\t\t{");

            builderView.AppendLine($"\t\t\t\t\tvar searchStr = \"\";");

            builderView.AppendLine($"\t\t\t\t\tif (fromDate == null) {{ fromDate = DateTime.UtcNow.Date; }}");
            builderView.AppendLine($"\t\t\t\t\tif (toDate == null) {{ toDate = DateTime.UtcNow.Date.AddDays(1); }}");
            builderView.AppendLine($"\t\t\t\t\tif (!string.IsNullOrEmpty(search)){{ if (searchStr.Length > 0) {{ searchStr = searchStr + \" AND \"; }} }}");

            builderView.AppendLine($"\t\t\t\t\tif (fromDate != null)");
            builderView.AppendLine($"\t\t\t\t\t{{");
            builderView.AppendLine($"\t\t\t\t\t\tif (searchStr.Length > 0) {{ searchStr = searchStr + \" AND \"; }}");
            builderView.AppendLine($"\t\t\t\t\t\tif (toDate != null)");
            builderView.AppendLine($"\t\t\t\t\t\t{{");
            builderView.AppendLine($"\t\t\t\t\t\tsearchStr = searchStr + \"  t1.CreatedOn >= CONVERT_TZ(@FromDate,'+08:00','+00:00') And t1.CreatedOn < CONVERT_TZ(@ToDate,'+08:00','+00:00') \";");
            builderView.AppendLine($"\t\t\t\t\t\t}}");
            builderView.AppendLine($"\t\t\t\t\t\telse");
            builderView.AppendLine($"\t\t\t\t\t\t{{");
            builderView.AppendLine($"\t\t\t\t\t\tsearchStr = searchStr + \" t1.CreatedOn >= CONVERT_TZ(@FromDate,'+08:00','+00:00') \";");
            builderView.AppendLine($"\t\t\t\t\t\t}}");
            builderView.AppendLine($"\t\t\t\t\t\t}}");

            builderView.AppendLine($"\t\t\t\t\t/*paging*/");
            builderView.AppendLine($"\t\t\t\t\tvar fetchStr = $\"LIMIT  {{tableParams.Offset}} , {{tableParams.PageSize}}\";");

            builderView.AppendLine($"\t\t\t\t\t/*Sort*/");
            builderView.AppendLine($"\t\t\t\t\tvar sortStr =  string.Empty;");

            builderView.AppendLine($"\t\t\t\t\tswitch (tableParams.SortColum)");
            builderView.AppendLine($"\t\t\t\t\t{{");
            builderView.AppendLine($"\t\t\t\t\t\tcase 0:");
            builderView.AppendLine($"\t\t\t\t\t\tsortStr = \" ORDER BY t1.Id desc \";");
            builderView.AppendLine($"\t\t\t\t\t\tbreak;");
            builderView.AppendLine($"\t\t\t\t\t}}");


            builderView.AppendLine($"\t\t\t\t\tif (searchStr.Trim().Length > 0)");
            builderView.AppendLine($"\t\t\t\t\t{{");
            builderView.AppendLine($"\t\t\t\t\tsearchStr = \" WHERE \" + searchStr;");
            builderView.AppendLine($"\t\t\t\t\t}}");

            builderView.AppendLine($"\t\t\t\t\t var query = @$\"SELECT t1.* FROM {tableSelect.ToLower()} t1 {{searchStr}} {{sortStr}} {{fetchStr}};" +
                                    $"  \r\n \t\t\t\t\t\t SELECT COUNT(1) FROM {tableSelect.ToLower()} t1 {{searchStr}}; \";");

            builderView.AppendLine($"\t\t\t\t\tvar result = dbContext.QueryMultiple(query, new {{@search = search, @FromDate = fromDate.Value.Date, @ToDate = toDate.Value.Date.AddDays(1)}});");

            builderView.AppendLine($"\t\t\t\t\tvar list = result.Read<{nameViewModel}>().ToList();");
            builderView.AppendLine($"\t\t\t\t\tvar total = result.Read<int>().FirstOrDefault();");
            builderView.AppendLine($"\t\t\t\t\tvar tuple = new Tuple<int, List<{nameViewModel}>>(total, list);");

            builderView.AppendLine($"\t\t\t\t\t  return new DataTableViewModel<{nameViewModel}>(tableParams.draw, tuple.Item1, tuple.Item1, tuple.Item2);");
            builderView.AppendLine("\t\t\t\t}");
            builderView.AppendLine("\t\t\t}");
            //end function search

            builderView.AppendLine("\t\t}");

            return builderView.ToString();

        }


        public static string GenerateClassControllerToFile(this IDbConnection connection, string sql, string className = null, GeneratorBehavior generatorBehavior = GeneratorBehavior.Default)
        {
            if (connection.State != ConnectionState.Open) connection.Open();

            var builderView = new StringBuilder();
            var tableSelect = "";

            //Get Table Name
            //Fix : [When View using CommandBehavior.KeyInfo will get duplicate columns ¡P Issue #8 ¡P shps951023/PocoClassGenerator](https://github.com/shps951023/PocoClassGenerator/issues/8 )
            var isFromMutiTables = false;
            using (var command = connection.CreateCommand(sql))
            using (var reader = command.ExecuteReader(CommandBehavior.KeyInfo | CommandBehavior.SingleRow))
            {
                var tables = reader.GetSchemaTable().Select().Select(s => s["BaseTableName"] as string).Distinct();
                var tableName = string.IsNullOrWhiteSpace(className) ? tables.First() ?? "Info" : className;

                TextInfo textInfo = new CultureInfo("en-US", false).TextInfo;
                tableName = textInfo.ToTitleCase(tableName);
                tableSelect = tableName;

                isFromMutiTables = tables.Count() > 1;
            }

            //Get Columns 
            var behavior = isFromMutiTables ? (CommandBehavior.SchemaOnly | CommandBehavior.SingleRow) : (CommandBehavior.KeyInfo | CommandBehavior.SingleRow);

            var nameDtoModel = $"{tableSelect}Dto";
            var nameCreateModel = $"Create{tableSelect}Model";
            var nameViewModel = $"View{tableSelect}Model";
            var nameEditModel = $"Edit{tableSelect}Model";

            builderView.AppendLine($"\t\t[Authorize]");
            builderView.AppendLine($"\t\tpublic class {tableSelect}Controller : BaseController");
            builderView.AppendLine("\t\t{");
            //contructor
            builderView.AppendLine($"\t\t\tprivate readonly ILogger<{tableSelect}Controller> _logger;");
            builderView.AppendLine($"\t\t\tprivate readonly I{tableSelect}Service _{tableSelect.ToLower()}Service;");
            builderView.AppendLine("\t\t\tprivate readonly IMapper mapper;");
            builderView.AppendLine($"\t\t\tpublic {tableSelect}Controller(I{tableSelect}Service {tableSelect.ToLower()}Service, ILogger<{tableSelect}Controller> _logger, IMapper _mapper, IHttpContextAccessor _httpContextAccessor)");
            builderView.AppendLine("\t\t\t{");
            builderView.AppendLine("\t\t\t\t _logger = _logger;");
            builderView.AppendLine("\t\t\t\tthis.mapper = _mapper;");
            builderView.AppendLine($"\t\t\t\tthis._{tableSelect.ToLower()}Service = {tableSelect.ToLower()}Service;");
            builderView.AppendLine("\t\t\t}");
            builderView.AppendLine("\t\t\t");

            // end contructor

            //index

            builderView.AppendLine("\t\t\tpublic IActionResult Index()");
            builderView.AppendLine("\t\t\t{");
            builderView.AppendLine("\t\t\t\treturn View();");
            builderView.AppendLine("\t\t\t}");
            //end index

            //Search function
            builderView.AppendLine($"\t\t\tpublic async Task<JsonResult> {tableSelect}Search(int draw = 1, int start = 0, int length = 10, string search = \"\", DateTime? fromDate = null, DateTime? toDate = null)");
            builderView.AppendLine("\t\t\t{");
            builderView.AppendLine("\t\t\t\tvar tableParams = new FilterTableParams(HttpContext, start, length, draw);");
            builderView.AppendLine($"\t\t\t\tvar result = await _{tableSelect.ToLower()}Service.Get{tableSelect}Search(tableParams, search, fromDate, toDate);");
            builderView.AppendLine("\t\t\t\treturn new JsonResult(result);");
            builderView.AppendLine("\t\t\t}");
            //end Search function

            //View function
            builderView.AppendLine($"\t\t\tpublic async Task<IActionResult> View{tableSelect}(int? id)");
            builderView.AppendLine("\t\t\t{");
            builderView.AppendLine("\t\t\t\tif (id == null || (id.HasValue && id.Value < 0))");
            builderView.AppendLine($"\t\t\t\treturn this.RedirectToAction(\"NotFound\", \"ErrorPage\");");
            builderView.AppendLine($"\t\t\t\tvar objModel = await _{tableSelect.ToLower()}Service.Get{tableSelect}ById(id.Value);");
            builderView.AppendLine($"\t\t\t\treturn View(objModel);");
            builderView.AppendLine("\t\t\t}");
            //end View function

            //Create function
            builderView.AppendLine("[HttpGet]");
            builderView.AppendLine($"\t\t\tpublic IActionResult Create{tableSelect}()");
            builderView.AppendLine("\t\t\t{");
            builderView.AppendLine($"\t\t\t\t{nameCreateModel} objModel = new {nameCreateModel}();");
            builderView.AppendLine($"\t\t\t\treturn View(objModel);");
            builderView.AppendLine("\t\t\t}");

            builderView.AppendLine("[HttpPost]");
            builderView.AppendLine("[ValidateAntiForgeryToken]");
            builderView.AppendLine($"\t\t\tpublic async Task<IActionResult> Create{tableSelect}({nameCreateModel} model)");
            builderView.AppendLine("\t\t\t{");
            builderView.AppendLine($"\t\t\t\tif (ModelState.IsValid)");
            builderView.AppendLine($"\t\t\t\t{{");

            builderView.AppendLine($"\t\t\t\t\t var user = User;");
            builderView.AppendLine($"\t\t\t\t\t var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier);");
            builderView.AppendLine($"\t\t\t\t\t var usernameClaim = user.FindFirst(ClaimTypes.Name);");
            builderView.AppendLine($"\t\t\t\t\t var userId = user.FindFirst(CustomClaimTypes.MerchantId).Value;");

            builderView.AppendLine($"\t\t\t\t\t var dto = mapper.Map<{nameCreateModel}, {nameDtoModel}>(model);");
            builderView.AppendLine($"\t\t\t\t\t dto.CreatedByUserID = Convert.ToInt32(userId);");
            builderView.AppendLine($"\t\t\t\t\t dto.CreatedOn = DateTime.UtcNow;");
            builderView.AppendLine($"\t\t\t\t\t var insertMerchant = await _{tableSelect.ToLower()}Service.Insert{tableSelect}(dto);");
            builderView.AppendLine($"\t\t\t\t\t return this.RedirectToAction(\"Index\", \"{tableSelect}\");");

            builderView.AppendLine($"\t\t\t\t}}");

            builderView.AppendLine($"\t\t\t\treturn View(model);");

            builderView.AppendLine("\t\t\t}");

            //end Create function

            //Update function
            builderView.AppendLine("[HttpGet]");
            builderView.AppendLine($"\t\t\tpublic async Task<IActionResult> Edit{tableSelect}(int? id)");
            builderView.AppendLine("\t\t\t{");
            builderView.AppendLine("\t\t\t\tif (id == null || (id.HasValue && id.Value < 0))");
            builderView.AppendLine($"\t\t\t\treturn this.RedirectToAction(\"NotFound\", \"ErrorPage\");");

            builderView.AppendLine($"\t\t\t\tvar dto = await _{tableSelect.ToLower()}Service.Get{tableSelect}ById(id.Value);");
            builderView.AppendLine($"\t\t\t\tvar objModel = mapper.Map<{nameDtoModel}, {nameEditModel}>(dto);");
            builderView.AppendLine($"\t\t\t\treturn View(objModel);");
            builderView.AppendLine("\t\t\t}");

            builderView.AppendLine("[HttpPost]");
            builderView.AppendLine("[ValidateAntiForgeryToken]");
            builderView.AppendLine($"\t\t\tpublic async Task<IActionResult> Edit{tableSelect}({nameEditModel} model)");
            builderView.AppendLine("\t\t\t{");
            builderView.AppendLine($"\t\t\t\tif (ModelState.IsValid)");
            builderView.AppendLine($"\t\t\t\t{{");

            builderView.AppendLine($"\t\t\t\t\t var user = User;");
            builderView.AppendLine($"\t\t\t\t\t var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier);");
            builderView.AppendLine($"\t\t\t\t\t var usernameClaim = user.FindFirst(ClaimTypes.Name);");
            builderView.AppendLine($"\t\t\t\t\t var userId = user.FindFirst(CustomClaimTypes.MerchantId).Value;");

            builderView.AppendLine($"\t\t\t\t\t var dto = mapper.Map<{nameEditModel}, {nameDtoModel}>(model);");
            builderView.AppendLine($"\t\t\t\t\t dto.UpdatedByUserID = Convert.ToInt32(userId);");
            builderView.AppendLine($"\t\t\t\t\t dto.UpdatedOn = DateTime.UtcNow;");
            builderView.AppendLine($"\t\t\t\t\t var update{tableSelect} = await _{tableSelect.ToLower()}Service.Update{tableSelect}(dto);");
            builderView.AppendLine($"\t\t\t\t\t return this.RedirectToAction(\"Index\", \"{tableSelect}\");");

            builderView.AppendLine($"\t\t\t\t}}");

            builderView.AppendLine($"\t\t\t\treturn View(model);");

            builderView.AppendLine("\t\t\t}");

            //end Update function

            //Delete function
            builderView.AppendLine("[HttpPost]");
            builderView.AppendLine("[ValidateAntiForgeryToken]");
            builderView.AppendLine($"\t\t\tpublic async Task<JsonResult> Delete{tableSelect}(int? id)");
            builderView.AppendLine("\t\t\t{");
            builderView.AppendLine("\t\t\t\tif (id == null || (id.HasValue && id.Value < 0))");
            builderView.AppendLine($"\t\t\t\treturn ErrorToClient(\"Delete {tableSelect} select not success.\");");
            builderView.AppendLine($"\t\t\t\tvar objModel = await _{tableSelect.ToLower()}Service.Delete{tableSelect}(id.Value);");
            builderView.AppendLine($"\t\t\t\treturn SuccessToClient(\"Delete {tableSelect} select successfully.\");");
            builderView.AppendLine("\t\t\t}");
            //end Delete function

            builderView.AppendLine("\t\t}");
            return builderView.ToString();

        }

        public static string GenerateClassCreateCSHtmlToFile(this IDbConnection connection, string sql, string className = null, GeneratorBehavior generatorBehavior = GeneratorBehavior.Default)
        {
            if (connection.State != ConnectionState.Open) connection.Open();

            var builder = new StringBuilder();
            var tableSelect = "";
            //Get Table Name
            //Fix : [When View using CommandBehavior.KeyInfo will get duplicate columns ¡P Issue #8 ¡P shps951023/PocoClassGenerator](https://github.com/shps951023/PocoClassGenerator/issues/8 )
            var isFromMutiTables = false;
            using (var command = connection.CreateCommand(sql))
            using (var reader = command.ExecuteReader(CommandBehavior.KeyInfo | CommandBehavior.SingleRow))
            {
                var tables = reader.GetSchemaTable().Select().Select(s => s["BaseTableName"] as string).Distinct();
                var tableName = string.IsNullOrWhiteSpace(className) ? tables.First() ?? "Info" : className;

                TextInfo textInfo = new CultureInfo("en-US", false).TextInfo;
                tableName = textInfo.ToTitleCase(tableName);
                tableSelect = tableName;
                isFromMutiTables = tables.Count() > 1;

                //if (generatorBehavior.HasFlag(GeneratorBehavior.DapperContrib) && !isFromMutiTables)
                //builder.AppendFormat("	[Dapper.Contrib.Extensions.Table(\"{0}\")]{1}", tableName, Environment.NewLine);
                //builder.AppendFormat("	public class {0}Dto{1}", tableName.Replace(" ", ""), Environment.NewLine);
                //builder.AppendLine("	{");
            }

            //builder.AppendLine($"@model EVSystem.Admin.Portal.Models.Stations.CreateStationModel");

            builder.AppendLine($"@{{");
            builder.AppendLine($"\tViewBag.Title = \"Admin Portal {tableSelect}\";");
            builder.AppendLine($"\tViewBag.pageTitle = \"{tableSelect}\";");
            builder.AppendLine($"\tViewBag.pTitle = \"Create {tableSelect} Information\";");
            builder.AppendLine($"\tLayout = \"~/Views/Shared/_Layout.cshtml\";");
            builder.AppendLine($"}}");

            builder.AppendLine($"<div class=\"row\">");
            builder.AppendLine($"\t<div class=\"col-lg-12\">");
            builder.AppendLine($"\t\t<div class=\"card\">");
            builder.AppendLine($"\t\t\t<div class=\"card-body\">");
            builder.AppendLine($"\t\t\t\t<div class=\"form-group row mb-4\">\r\n                    <label class=\"card-title col-sm-6 col-form-label\">{tableSelect} Information</label>\r\n                </div>");
            builder.AppendLine($"\t\t\t\t@using (Html.BeginForm(\"Create{tableSelect}\", \"{tableSelect}\", FormMethod.Post, new {{ id = \"FormCreate{tableSelect}Information\", @class = \"needs-validation\", novalidate = \"novalidate\" }}))");
            builder.AppendLine($"\t\t\t\t{{");
            builder.AppendLine($"\t\t\t\t\t@Html.AntiForgeryToken()");


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

                        //builder.AppendLine(string.Format("public {0}{1} {2} {{ get; set; }}", name, isNullable ? "?" : string.Empty, collumnName));

                        builder.AppendLine($"\t\t\t\t\t<div class=\"form-group row mb-4\">");

                        builder.AppendLine($"\t\t\t\t\t\t<label class=\"col-sm-3 col-form-label\">{collumnName}</label>");
                        builder.AppendLine($"\t\t\t\t\t\t<div class=\"col-sm-9\">");
                        builder.AppendLine($"\t\t\t\t\t\t\t@Html.TextBoxFor(m => m.{collumnName}, new {{@class = \"form-control\"}})");
                        builder.AppendLine($"\t\t\t\t\t\t\t@Html.ValidationMessageFor(m => m.{collumnName}, null, new {{@class = \"text-danger\"}})");
                        builder.AppendLine($"\t\t\t\t\t\t</div>");
                        builder.AppendLine($"\t\t\t\t\t </div>");

                    }

                    //builder.AppendLine("	}");
                    //builder.AppendLine();
                } while (reader.NextResult());


                builder.AppendLine($"\t\t\t\t\t<div class=\"form-group row mb-4\">");

                builder.AppendLine($"\t\t\t\t\t\t<div class=\"col-md-10\">");
                builder.AppendLine($"\t\t\t\t\t\t\t<a href=\"@Url.Action(\"Index\",\"{tableSelect}\")\" class=\"btn btn-primary\">Back to {tableSelect} List</a>");
                builder.AppendLine($"\t\t\t\t\t\t</div>");
                builder.AppendLine($"\t\t\t\t\t\t<div class=\"col-md-2\">");
                builder.AppendLine($"\t\t\t\t\t\t\t <button type=\"submit\" class=\"btn btn-primary\">\r\n                                Save\r\n                            </button>");
                builder.AppendLine($"\t\t\t\t\t\t</div>");

                builder.AppendLine($"\t\t\t\t\t </div>");

                builder.AppendLine($"\t\t\t\t}}");

                builder.AppendLine($"</div>\r\n        </div>\r\n    </div>\r\n</div>");

                builder.AppendLine($"@section scripts{{");
                builder.AppendLine($"<script src=\"~/assets/libs/parsleyjs/parsley.min.js\"></script>");
                builder.AppendLine($"<script src=\"~/assets/js/pages/form-validation.init.js\"></script>");
                builder.AppendLine($"}}");
                return builder.ToString();
            }
        }

        public static string GenerateClassEditCSHtmlToFile(this IDbConnection connection, string sql, string className = null, GeneratorBehavior generatorBehavior = GeneratorBehavior.Default)
        {
            if (connection.State != ConnectionState.Open) connection.Open();

            var builder = new StringBuilder();
            var tableSelect = "";
            //Get Table Name
            //Fix : [When View using CommandBehavior.KeyInfo will get duplicate columns ¡P Issue #8 ¡P shps951023/PocoClassGenerator](https://github.com/shps951023/PocoClassGenerator/issues/8 )
            var isFromMutiTables = false;
            using (var command = connection.CreateCommand(sql))
            using (var reader = command.ExecuteReader(CommandBehavior.KeyInfo | CommandBehavior.SingleRow))
            {
                var tables = reader.GetSchemaTable().Select().Select(s => s["BaseTableName"] as string).Distinct();
                var tableName = string.IsNullOrWhiteSpace(className) ? tables.First() ?? "Info" : className;

                TextInfo textInfo = new CultureInfo("en-US", false).TextInfo;
                tableName = textInfo.ToTitleCase(tableName);
                tableSelect = tableName;
                isFromMutiTables = tables.Count() > 1;

                //if (generatorBehavior.HasFlag(GeneratorBehavior.DapperContrib) && !isFromMutiTables)
                //builder.AppendFormat("	[Dapper.Contrib.Extensions.Table(\"{0}\")]{1}", tableName, Environment.NewLine);
                //builder.AppendFormat("	public class {0}Dto{1}", tableName.Replace(" ", ""), Environment.NewLine);
                //builder.AppendLine("	{");
            }

            //builder.AppendLine($"@model EVSystem.Admin.Portal.Models.Stations.CreateStationModel");

            builder.AppendLine($"@{{");
            builder.AppendLine($"\tViewBag.Title = \"Admin Portal {tableSelect}\";");
            builder.AppendLine($"\tViewBag.pageTitle = \"{tableSelect}\";");
            builder.AppendLine($"\tViewBag.pTitle = \"Create {tableSelect} Information\";");
            builder.AppendLine($"\tLayout = \"~/Views/Shared/_Layout.cshtml\";");
            builder.AppendLine($"}}");

            builder.AppendLine($"<div class=\"row\">");
            builder.AppendLine($"\t<div class=\"col-lg-12\">");
            builder.AppendLine($"\t\t<div class=\"card\">");
            builder.AppendLine($"\t\t\t<div class=\"card-body\">");
            builder.AppendLine($"\t\t\t\t<div class=\"form-group row mb-4\">\r\n                    <label class=\"card-title col-sm-6 col-form-label\">{tableSelect} Information</label>\r\n                </div>");
            builder.AppendLine($"\t\t\t\t@using (Html.BeginForm(\"Edit{tableSelect}\", \"{tableSelect}\", FormMethod.Post, new {{ id = \"FormCreate{tableSelect}Information\", @class = \"needs-validation\", novalidate = \"novalidate\" }}))");
            builder.AppendLine($"\t\t\t\t{{");
            builder.AppendLine($"\t\t\t\t\t@Html.AntiForgeryToken()");


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

                        //builder.AppendLine(string.Format("public {0}{1} {2} {{ get; set; }}", name, isNullable ? "?" : string.Empty, collumnName));

                        builder.AppendLine($"\t\t\t\t\t<div class=\"form-group row mb-4\">");

                        builder.AppendLine($"\t\t\t\t\t\t<label class=\"col-sm-3 col-form-label\">{collumnName}</label>");
                        builder.AppendLine($"\t\t\t\t\t\t<div class=\"col-sm-9\">");
                        builder.AppendLine($"\t\t\t\t\t\t\t@Html.TextBoxFor(m => m.{collumnName}, new {{@class = \"form-control\"}})");
                        builder.AppendLine($"\t\t\t\t\t\t\t@Html.ValidationMessageFor(m => m.{collumnName}, null, new {{@class = \"text-danger\"}})");
                        builder.AppendLine($"\t\t\t\t\t\t</div>");
                        builder.AppendLine($"\t\t\t\t\t </div>");

                    }

                    //builder.AppendLine("	}");
                    //builder.AppendLine();
                } while (reader.NextResult());


                builder.AppendLine($"\t\t\t\t\t<div class=\"form-group row mb-4\">");

                builder.AppendLine($"\t\t\t\t\t\t<div class=\"col-md-10\">");
                builder.AppendLine($"\t\t\t\t\t\t\t<a href=\"@Url.Action(\"Index\",\"{tableSelect}\")\" class=\"btn btn-primary\">Back to {tableSelect} List</a>");
                builder.AppendLine($"\t\t\t\t\t\t</div>");
                builder.AppendLine($"\t\t\t\t\t\t<div class=\"col-md-2\">");
                builder.AppendLine($"\t\t\t\t\t\t\t <button type=\"submit\" class=\"btn btn-primary\">\r\n                                Save\r\n                            </button>");
                builder.AppendLine($"\t\t\t\t\t\t</div>");

                builder.AppendLine($"\t\t\t\t\t </div>");

                builder.AppendLine($"\t\t\t\t}}");

                builder.AppendLine($"</div>\r\n        </div>\r\n    </div>\r\n</div>");

                builder.AppendLine($"@section scripts{{");
                builder.AppendLine($"<script src=\"~/assets/libs/parsleyjs/parsley.min.js\"></script>");
                builder.AppendLine($"<script src=\"~/assets/js/pages/form-validation.init.js\"></script>");
                builder.AppendLine($"}}");
                return builder.ToString();
            }
        }


        public static string GenerateClassViewCSHtmlToFile(this IDbConnection connection, string sql, string className = null, GeneratorBehavior generatorBehavior = GeneratorBehavior.Default)
        {
            if (connection.State != ConnectionState.Open) connection.Open();

            var builder = new StringBuilder();
            var tableSelect = "";
            //Get Table Name
            //Fix : [When View using CommandBehavior.KeyInfo will get duplicate columns ¡P Issue #8 ¡P shps951023/PocoClassGenerator](https://github.com/shps951023/PocoClassGenerator/issues/8 )
            var isFromMutiTables = false;
            using (var command = connection.CreateCommand(sql))
            using (var reader = command.ExecuteReader(CommandBehavior.KeyInfo | CommandBehavior.SingleRow))
            {
                var tables = reader.GetSchemaTable().Select().Select(s => s["BaseTableName"] as string).Distinct();
                var tableName = string.IsNullOrWhiteSpace(className) ? tables.First() ?? "Info" : className;

                TextInfo textInfo = new CultureInfo("en-US", false).TextInfo;
                tableName = textInfo.ToTitleCase(tableName);
                tableSelect = tableName;
                isFromMutiTables = tables.Count() > 1;

                //if (generatorBehavior.HasFlag(GeneratorBehavior.DapperContrib) && !isFromMutiTables)
                //builder.AppendFormat("	[Dapper.Contrib.Extensions.Table(\"{0}\")]{1}", tableName, Environment.NewLine);
                //builder.AppendFormat("	public class {0}Dto{1}", tableName.Replace(" ", ""), Environment.NewLine);
                //builder.AppendLine("	{");
            }

            //builder.AppendLine($"@model EVSystem.Admin.Portal.Models.Stations.CreateStationModel");

            builder.AppendLine($"@{{");
            builder.AppendLine($"\tViewBag.Title = \"Admin Portal {tableSelect}\";");
            builder.AppendLine($"\tViewBag.pageTitle = \"{tableSelect}\";");
            builder.AppendLine($"\tViewBag.pTitle = \"Create {tableSelect} Information\";");
            builder.AppendLine($"\tLayout = \"~/Views/Shared/_Layout.cshtml\";");
            builder.AppendLine($"}}");

            builder.AppendLine($"<div class=\"row\">");
            builder.AppendLine($"\t<div class=\"col-lg-12\">");
            builder.AppendLine($"\t\t<div class=\"card\">");
            builder.AppendLine($"\t\t\t<div class=\"card-body\">");
            builder.AppendLine($"\t\t\t\t<div class=\"form-group row mb-4\">\r\n                    <label class=\"card-title col-sm-6 col-form-label\">{tableSelect} Information</label>\r\n                </div>");
            builder.AppendLine($"\t\t\t\t@using (Html.BeginForm(\"View{tableSelect}\", \"{tableSelect}\", FormMethod.Post, new {{ id = \"FormCreate{tableSelect}Information\", @class = \"needs-validation\", novalidate = \"novalidate\" }}))");
            builder.AppendLine($"\t\t\t\t{{");
            builder.AppendLine($"\t\t\t\t\t@Html.AntiForgeryToken()");


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

                        //builder.AppendLine(string.Format("public {0}{1} {2} {{ get; set; }}", name, isNullable ? "?" : string.Empty, collumnName));

                        builder.AppendLine($"\t\t\t\t\t<div class=\"form-group row mb-4\">");

                        builder.AppendLine($"\t\t\t\t\t\t<label class=\"col-sm-3 col-form-label\">{collumnName}</label>");
                        builder.AppendLine($"\t\t\t\t\t\t<div class=\"col-sm-9\">");
                        builder.AppendLine($"\t\t\t\t\t\t\t@Html.TextBoxFor(m => m.{collumnName}, new {{@class = \"form-control\" , @readonly = \"readonly\"}})");
                        builder.AppendLine($"\t\t\t\t\t\t\t@Html.ValidationMessageFor(m => m.{collumnName}, null, new {{@class = \"text-danger\"}})");
                        builder.AppendLine($"\t\t\t\t\t\t</div>");
                        builder.AppendLine($"\t\t\t\t\t </div>");

                    }

                    //builder.AppendLine("	}");
                    //builder.AppendLine();
                } while (reader.NextResult());


                builder.AppendLine($"\t\t\t\t\t<div class=\"form-group row mb-4\">");

                builder.AppendLine($"\t\t\t\t\t\t<div class=\"col-md-10\">");
                builder.AppendLine($"\t\t\t\t\t\t\t<a href=\"@Url.Action(\"Index\",\"{tableSelect}\")\" class=\"btn btn-primary\">Back to {tableSelect} List</a>");
                builder.AppendLine($"\t\t\t\t\t\t</div>");
                builder.AppendLine($"\t\t\t\t\t </div>");

                builder.AppendLine($"\t\t\t\t}}");

                builder.AppendLine($"</div>\r\n        </div>\r\n    </div>\r\n</div>");

                builder.AppendLine($"@section scripts{{");
                builder.AppendLine($"<script src=\"~/assets/libs/parsleyjs/parsley.min.js\"></script>");
                builder.AppendLine($"<script src=\"~/assets/js/pages/form-validation.init.js\"></script>");
                builder.AppendLine($"}}");
                return builder.ToString();
            }
        }



        #endregion
    }
}
