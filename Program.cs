using System;
using System.IO;
using FirebirdSql.Data.FirebirdClient;

namespace DbMetaTool
{
    public static class Program
    {
        // Przykładowe wywołania:
        // DbMetaTool build-db --db-dir "C:\db\fb5" --scripts-dir "C:\scripts"
        // DbMetaTool export-scripts --connection-string "..." --output-dir "C:\out"
        // DbMetaTool update-db --connection-string "..." --scripts-dir "C:\scripts"
        public static int Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("Użycie:");
                Console.WriteLine("  build-db --db-dir <ścieżka> --scripts-dir <ścieżka>");
                Console.WriteLine("  export-scripts --connection-string <connStr> --output-dir <ścieżka>");
                Console.WriteLine("  update-db --connection-string <connStr> --scripts-dir <ścieżka>");
                return 1;
            }

            try
            {
                var command = args[0].ToLowerInvariant();

                switch (command)
                {
                    case "build-db":
                        {
                            string dbDir = GetArgValue(args, "--db-dir");
                            string scriptsDir = GetArgValue(args, "--scripts-dir");

                            BuildDatabase(dbDir, scriptsDir);
                            Console.WriteLine("Baza danych została zbudowana pomyślnie.");
                            return 0;
                        }

                    case "export-scripts":
                        {
                            string connStr = GetArgValue(args, "--connection-string");
                            string outputDir = GetArgValue(args, "--output-dir");

                            ExportScripts(connStr, outputDir);
                            Console.WriteLine("Skrypty zostały wyeksportowane pomyślnie.");
                            return 0;
                        }

                    case "update-db":
                        {
                            string connStr = GetArgValue(args, "--connection-string");
                            string scriptsDir = GetArgValue(args, "--scripts-dir");

                            UpdateDatabase(connStr, scriptsDir);
                            Console.WriteLine("Baza danych została zaktualizowana pomyślnie.");
                            return 0;
                        }

                    default:
                        Console.WriteLine($"Nieznane polecenie: {command}");
                        return 1;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Błąd: " + ex.Message);
                return -1;
            }
        }

        private static string GetArgValue(string[] args, string name)
        {
            int idx = Array.IndexOf(args, name);
            if (idx == -1 || idx + 1 >= args.Length)
                throw new ArgumentException($"Brak wymaganego parametru {name}");
            return args[idx + 1];
        }

        /// <summary>
        /// Buduje nową bazę danych Firebird 5.0 na podstawie skryptów.
        /// </summary>
        public static void BuildDatabase(string databaseDirectory, string scriptsDirectory)
        {
            // TODO:
            // 1) Utwórz pustą bazę danych FB 5.0 w katalogu databaseDirectory.
            // 2) Wczytaj i wykonaj kolejno skrypty z katalogu scriptsDirectory
            //    (tylko domeny, tabele, procedury).
            // 3) Obsłuż błędy i wyświetl raport.
            //throw new NotImplementedException();
            if (!Directory.Exists(databaseDirectory))
                Directory.CreateDirectory(databaseDirectory);
            string dbPath = Path.Combine(databaseDirectory, "fb_database.fdb");
            if (File.Exists(dbPath))
                File.Delete(dbPath);
            var cs = new FbConnectionStringBuilder
            {
                DataSource = "localhost",
                Database = dbPath,
                UserID = "SYSDBA",
                Password = "masterkey",
                Charset = "UTF8",
                Port = 3050,
                Dialect = 3
            };
            FbConnection.CreateDatabase(cs.ConnectionString, 8192);
            using var con = new FbConnection(cs.ConnectionString);
            con.Open();

            //string[] folders = Directory.GetDirectories(scriptsDirectory);
            string[] folders = new string[] { scriptsDirectory + @"\Domains", scriptsDirectory + @"\Tables", scriptsDirectory + @"\Procedures" };
            foreach (string folder in folders)
            {
                var scripts = Directory.GetFiles(folder, "*.sql")
                                   .OrderBy(Path.GetFileName)
                                   .ToList();
                foreach (var script in scripts)
                {
                    Console.WriteLine($"  • {Path.GetFileName(script)}");
                    string sql = File.ReadAllText(script);
                    var chunks = SplitFirebirdScript(sql);
                    using var tr = con.BeginTransaction();
                    try
                    {
                        foreach (var chunk in chunks)
                        {
                            using var cmd = new FbCommand(chunk, con, tr);
                            cmd.ExecuteNonQuery();
                        }
                        tr.Commit();
                    }
                    catch (Exception ex)
                    {
                        tr.Rollback();
                        Console.WriteLine($"Błąd skryptu: {ex.Message}");
                        throw;
                    }
                }
            }
        }

        private static string[] SplitFirebirdScript(string script)
        {
            script = script.Replace("\r", "");
            var lines = script.Split('\n');
            string terminator = ";";
            string buffer = "";
            var list = new System.Collections.Generic.List<string>();
            foreach (var line in lines)
            {
                var trimmed = line.Trim();

                var match = System.Text.RegularExpressions.Regex.Match(trimmed, @"SET\s+TERM\s+(.+)\s+;", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                if (match.Success)
                {

                    //list.Add(trimmed);
                    terminator = match.Groups[1].Value;
                    buffer = "";
                    continue;
                }
                if (trimmed.StartsWith("SET TERM")) continue;
                buffer += line + "\n";
                if (trimmed.EndsWith(terminator))
                {
                    var command = buffer.Trim();
                    command = command.Substring(0, command.Length - terminator.Length).Trim();
                    if (!string.IsNullOrWhiteSpace(command))
                        list.Add(command);
                    buffer = "";
                }
            }
            if (!string.IsNullOrWhiteSpace(buffer))
                list.Add(buffer.Trim());
            return list.ToArray();
        }

        /// <summary>
        /// Generuje skrypty metadanych z istniejącej bazy danych Firebird 5.0.
        /// </summary>
        public static void ExportScripts(string connectionString, string outputDirectory)
        {
            // TODO:
            // 1) Połącz się z bazą danych przy użyciu connectionString.
            // 2) Pobierz metadane domen, tabel (z kolumnami) i procedur.
            // 3) Wygeneruj pliki .sql / .json / .txt w outputDirectory.
            //throw new NotImplementedException();
            if (!Directory.Exists(outputDirectory))
                Directory.CreateDirectory(outputDirectory);

            using var con = new FbConnection(connectionString);
            con.Open();

            string domainDir = Path.Combine(outputDirectory, "Domains");
            string tableDir = Path.Combine(outputDirectory, "Tables");
            string procDir = Path.Combine(outputDirectory, "Procedures");

            Directory.CreateDirectory(domainDir);
            Directory.CreateDirectory(tableDir);
            Directory.CreateDirectory(procDir);

            string sqlDomains = @"
        SELECT TRIM(f.RDB$FIELD_NAME) AS NAME,
               f.RDB$FIELD_TYPE,
               f.RDB$FIELD_SUB_TYPE,
               f.RDB$FIELD_LENGTH,
               f.RDB$CHARACTER_LENGTH,
               f.RDB$FIELD_SCALE
        FROM RDB$FIELDS f
        WHERE COALESCE(f.RDB$SYSTEM_FLAG,0) = 0
          AND f.RDB$FIELD_NAME NOT LIKE 'RDB$%'
        ORDER BY f.RDB$FIELD_NAME;
    ";

            using (var cmd = new FbCommand(sqlDomains, con))
            using (var r = cmd.ExecuteReader())
            {
                while (r.Read())
                {
                    string? name = r["NAME"].ToString();
                    string type = BuildFbType(r);

                    string ddl = $"CREATE DOMAIN {name} AS {type};";

                    File.WriteAllText(Path.Combine(domainDir, $"{name}.sql"), ddl);
                }
            }

            string sqlTables = @"
        SELECT TRIM(RDB$RELATION_NAME)
        FROM RDB$RELATIONS
        WHERE COALESCE(RDB$SYSTEM_FLAG,0) = 0
          AND RDB$VIEW_BLR IS NULL
        ORDER BY RDB$RELATION_NAME;
    ";

            using (var cmd = new FbCommand(sqlTables, con))
            using (var r = cmd.ExecuteReader())
            {
                while (r.Read())
                {
                    string table = r.GetString(0);
                    string ddl = ExportTable(con, table);

                    File.WriteAllText(Path.Combine(tableDir, $"{table}.sql"), ddl);
                }
            }

            
            string sqlProc = @"
        SELECT TRIM(RDB$PROCEDURE_NAME),
               RDB$PROCEDURE_SOURCE,
               RDB$PROCEDURE_ID
        FROM RDB$PROCEDURES
        WHERE COALESCE(RDB$SYSTEM_FLAG,0) = 0
        ORDER BY RDB$PROCEDURE_NAME;
    ";

            using (var cmd = new FbCommand(sqlProc, con))
            using (var r = cmd.ExecuteReader())
            {
                while (r.Read())
                {
                    string name = r.GetString(0);
                    string source = (r["RDB$PROCEDURE_SOURCE"]?.ToString() ?? "").Trim();

                    string sqlParams = @"
        SELECT
            TRIM(p.RDB$PARAMETER_NAME) AS NAME,
            p.RDB$PARAMETER_TYPE AS KIND,  -- 0 = IN, 1 = OUT
            f.RDB$FIELD_TYPE,
            f.RDB$FIELD_SUB_TYPE,
            f.RDB$FIELD_LENGTH,
            f.RDB$CHARACTER_LENGTH,
            f.RDB$FIELD_SCALE
        FROM RDB$PROCEDURE_PARAMETERS p
        JOIN RDB$FIELDS f ON p.RDB$FIELD_SOURCE = f.RDB$FIELD_NAME
        WHERE TRIM(p.RDB$PROCEDURE_NAME) = @P
        ORDER BY p.RDB$PARAMETER_TYPE, p.RDB$PARAMETER_NUMBER
    ";

                    var inParams = new List<string>();
                    var outParams = new List<string>();

                    using (var parcmd = new FbCommand(sqlParams, con))
                    {
                        parcmd.Parameters.AddWithValue("@P", name);
                        using var rr = parcmd.ExecuteReader();

                        while (rr.Read())
                        {
                            string? parname = rr["NAME"].ToString();
                            string type = BuildFbType(rr);
                            bool isOut = Convert.ToInt32(rr["KIND"]) == 1;

                            string def = $"{parname} {type}";
                            if (isOut)
                                outParams.Add(def);
                            else
                                inParams.Add(def);
                        }
                    }
                    var sb = new System.Text.StringBuilder();
                    sb.AppendLine("SET TERM ^ ;");
                    sb.AppendLine();
                    sb.AppendLine($"CREATE OR ALTER PROCEDURE {name}");
                    if (inParams.Count > 0)
                        sb.AppendLine("(" + string.Join(",\n ", inParams) + ")");
                    if (outParams.Count > 0)
                    {
                        sb.AppendLine("RETURNS");
                        sb.AppendLine("(" + string.Join(",\n ", outParams) + ")");
                    }
                    sb.AppendLine("AS");
                    sb.AppendLine(source + "^");
                    sb.AppendLine();
                    sb.AppendLine("SET TERM ; ^");
                    string procid = (r["RDB$PROCEDURE_ID"]?.ToString() ?? "").Trim();
                    File.WriteAllText(Path.Combine(procDir, $"{procid}_{name}.sql"), sb.ToString());
                }
            }
        }
        private static string BuildFbType(FbDataReader r)
        {
            int fieldType = Convert.ToInt32(r["RDB$FIELD_TYPE"]);
            int? subType = r["RDB$FIELD_SUB_TYPE"] as int?;
            int typeLen = r["RDB$FIELD_LENGTH"] == DBNull.Value ? 0 : Convert.ToInt32(r["RDB$FIELD_LENGTH"]);
            int charLen = r["RDB$CHARACTER_LENGTH"] == DBNull.Value ? 0 : Convert.ToInt32(r["RDB$CHARACTER_LENGTH"]);
            int scale = r["RDB$FIELD_SCALE"] == DBNull.Value ? 0 : Math.Abs(Convert.ToInt32(r["RDB$FIELD_SCALE"]));

            return fieldType switch
            {
                7 => "SMALLINT",
                8 => "INTEGER",
                10 => "FLOAT",
                12 => "DATE",
                13 => "TIME",
                35 => "TIMESTAMP",
                14 => $"CHAR({charLen})",
                37 => $"VARCHAR({charLen})",
                16 when subType == 1 => $"NUMERIC({typeLen / 4}, {scale})",
                16 when subType == 2 => $"DECIMAL({typeLen / 4}, {scale})",
                16 => "BIGINT",
                27 => "DOUBLE PRECISION",
                261 when subType == 1 => "BLOB SUB_TYPE TEXT",
                261 => "BLOB",
                _ => $"UNKNOWN"
            };
        }
        private static string ExportTable(FbConnection con, string table)
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"CREATE TABLE {table} (");

            string sqlCols = @"
        SELECT TRIM(r.RDB$FIELD_NAME) as NAME,
               f.*
        FROM RDB$RELATION_FIELDS r
        JOIN RDB$FIELDS f ON f.RDB$FIELD_NAME = r.RDB$FIELD_SOURCE
        WHERE r.RDB$RELATION_NAME = @T
        ORDER BY r.RDB$FIELD_POSITION;
    ";

            using var cmd = new FbCommand(sqlCols, con);
            cmd.Parameters.AddWithValue("@T", table);

            using var r = cmd.ExecuteReader();

            var cols = new List<string>();

            while (r.Read())
            {
                string? name = r["NAME"].ToString();
                string type = BuildFbType(r);
                bool nullable = r["RDB$NULL_FLAG"] == DBNull.Value;

                string col = $"  {name} {type}";
                if (!nullable) col += " NOT NULL";

                cols.Add(col);
            }

            sb.AppendLine(string.Join(",\n", cols));
            sb.AppendLine(");");

            return sb.ToString();
        }

        /// <summary>
        /// Aktualizuje istniejącą bazę danych Firebird 5.0 na podstawie skryptów.
        /// </summary>
        public static void UpdateDatabase(string connectionString, string scriptsDirectory)
        {
            // TODO:
            // 1) Połącz się z bazą danych przy użyciu connectionString.
            // 2) Wykonaj skrypty z katalogu scriptsDirectory (tylko obsługiwane elementy).
            // 3) Zadbaj o poprawną kolejność i bezpieczeństwo zmian.
            //throw new NotImplementedException();
            if (!Directory.Exists(scriptsDirectory))
                throw new DirectoryNotFoundException($"Brak katalogu: {scriptsDirectory}");
           

            using var connection = new FbConnection(connectionString);
            connection.Open();
            //string[] folders = Directory.GetDirectories(scriptsDirectory);
            string[] folders = new string[]{ scriptsDirectory+@"\Domains", scriptsDirectory+@"\Tables", scriptsDirectory+@"\Procedures"};
            foreach (string folder in folders)
            {
                 var scripts = Directory
                    .GetFiles(folder, "*.sql")
                    .OrderBy(Path.GetFileName)
                    .ToList();
                foreach (var scriptPath in scripts)
                {
                    string scriptName = Path.GetFileName(scriptPath);
                    string sql = File.ReadAllText(scriptPath);
                    var sqlChunks = SplitFirebirdScript(sql);
                    using var transaction = connection.BeginTransaction();
                    try
                    {
                        foreach (var chunk in sqlChunks)
                        {
                            using var cmd = new FbCommand(chunk, connection, transaction);
                            cmd.ExecuteNonQuery();
                        }

                        transaction.Commit();
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        Console.WriteLine($"Błąd aktualizacji w {scriptName}: {ex.Message}");
                        throw;
                    }
                }
            }
        }
    }
}
