using Pandemonium_Classic_Mod_Manager.Properties;
using Pandemonium_Classic_Mod_Manager;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Net.WebRequestMethods;

namespace Pandemonium_Classic_Mod_Manager.SQLiteDataBase
{
    public class PCUE_Database
    {
        public SQLiteConnection dbConnection = new();
        SQLiteCommand command = new();
        SQLiteDataReader? reader;
        string sqlCommand = string.Empty;
        string dbFilePath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "mods.db");

        public PCUE_Database()
        {
            if (CreateDbFile())
            {
                CreateDbConnection();
                Mods_CreateTable();
                Mods_FillTable();
                Files_CreateTable();
                Files_FillTable();
                Strings_CreateTable();
                Strings_FillTable();
            }
            else
            {
                CreateDbConnection();
                Mods_UpdateRecords();
            }
        }

        public bool CreateDbFile()
        {
            if (!System.IO.File.Exists(dbFilePath))
            {
                SQLiteConnection.CreateFile(dbFilePath);
                return true;
            }
            return false;
        }

        public string CreateDbConnection()
        {
            string strCon = string.Format("Data Source={0};", dbFilePath);
            dbConnection = new SQLiteConnection(strCon);
            dbConnection.Open();
            command = dbConnection.CreateCommand();
            return strCon;
        }

        public bool CheckIfExist(string tableName)
        {
            command.CommandText = "SELECT name FROM sqlite_master WHERE name=\"" + tableName + "\"";
            var result = command.ExecuteScalar();

            return result != null && result.ToString() == tableName;
        }

        public void ExecuteQuery(string sqlCommand)
        {
            SQLiteCommand triggerCommand = dbConnection.CreateCommand();
            triggerCommand.CommandText = sqlCommand;
            triggerCommand.ExecuteNonQuery();
        }

        public bool CheckIfTableContainsData(string tableName)
        {
            command.CommandText = "SELECT COUNT(*) FROM " + tableName;
            var result = command.ExecuteScalar();

            return Convert.ToInt32(result) > 0;
        }

        public void StartTransaction()
        {
            sqlCommand = "BEGIN TRANSACTION";
            command = new SQLiteCommand(sqlCommand, dbConnection);
            command.ExecuteNonQuery();
        }

        public void CommitTransaction()
        {
            sqlCommand = "COMMIT";
            command = new SQLiteCommand(sqlCommand, dbConnection);
            command.ExecuteNonQuery();
        }

        public void Mods_CreateTable()
        {
            if (!CheckIfExist("mods"))
            {
                StartTransaction();

                sqlCommand = "CREATE TABLE mods(name TEXT, installed INT, backup INT)";
                ExecuteQuery(sqlCommand);

                CommitTransaction();
            }
        }

        public void Mods_FillTable()
        {
            if (!CheckIfTableContainsData("mods"))
            {
                StartTransaction();

                foreach (Mod mod in PCUE_ModManager.instance.Mods)
                {
                    sqlCommand = "INSERT INTO mods(name, installed, backup) values (\"" + mod.Name + "\", 0, 0)";
                    command = new SQLiteCommand(sqlCommand, dbConnection);
                    ExecuteQuery(sqlCommand);
                }

                CommitTransaction();
            }
        }

        public void Mods_CompareRecords()
        {
            var modList = PCUE_ModManager.instance.Mods;

            sqlCommand = "SELECT * FROM mods WHERE installed = \"1\"";
            command = new SQLiteCommand(sqlCommand, dbConnection);
            var reader = command.ExecuteReader();

            while (reader.Read())
            {
                var mod = modList.Where(t => t.Name == (string)reader["name"]).FirstOrDefault();
                if (mod != null)
                {
                    mod.Installed = "+";
                    if ((int)reader["backup"] == 1)
                        mod.BackUp = true;
                }
                else
                {
                    mod = new()
                    {
                        Name = (string)reader["name"],
                        Installed = "*"
                    };
                    if ((int)reader["backup"] == 1)
                        mod.BackUp = true;
                    modList.Add(mod);
                }
            }
        }

        public void Mods_UpdateRecords()
        {
            Mods_CompareRecords();

            StartTransaction();

            sqlCommand = "SELECT * FROM mods";
            command = new SQLiteCommand(sqlCommand, dbConnection);
            reader = command.ExecuteReader();

            // Delete Missing And Uninstalled Records
            while (reader.Read())
            {
                string modName = (string)reader["name"];
                if (PCUE_ModManager.instance.Mods.Where(t => t.Name == modName).ToArray().Length == 0)
                {
                    if ((int)reader["installed"] == 0)
                    {
                        sqlCommand = "DELETE FROM mods WHERE name = \"" + modName + "\"";
                        command = new SQLiteCommand(sqlCommand, dbConnection);
                        command.ExecuteNonQuery();
                    }
                }
            }

            // Add New Records
            foreach (Mod mod in PCUE_ModManager.instance.Mods)
            {
                sqlCommand = "SELECT COUNT(name) FROM mods WHERE name = \"" + mod.Name + "\"";
                command = new SQLiteCommand(sqlCommand, dbConnection);
                var result = Convert.ToInt32(command.ExecuteScalar());

                if (result == 0)
                {
                    sqlCommand = "INSERT INTO mods (name, installed, backup) values (\"" + mod.Name + "\", 0, 0)";
                    command = new SQLiteCommand(sqlCommand, dbConnection);
                    command.ExecuteNonQuery();
                }
            }

            CommitTransaction();
        }

        public void Mods_SetInstalled(Mod mod, bool installed, bool backup = false)
        {
            StartTransaction();

            if (installed)
            {
                sqlCommand = "UPDATE mods SET installed = 1 WHERE name = \"" + mod.Name + "\"";
                command = new SQLiteCommand(sqlCommand, dbConnection);
                command.ExecuteNonQuery();
                if (backup)
                {
                    sqlCommand = "UPDATE mods SET backup = 1 WHERE name = \"" + mod.Name + "\"";
                    command = new SQLiteCommand(sqlCommand, dbConnection);
                    command.ExecuteNonQuery();
                }
            }
            else if (!installed)
            {
                sqlCommand = "UPDATE mods SET installed = 0 WHERE name = \"" + mod.Name + "\"";
                command = new SQLiteCommand(sqlCommand, dbConnection);
                command.ExecuteNonQuery();

                sqlCommand = "UPDATE mods SET backup = 0 WHERE name = \"" + mod.Name + "\"";
                command = new SQLiteCommand(sqlCommand, dbConnection);
                command.ExecuteNonQuery();
            }

            CommitTransaction();
        }

        public void Files_CreateTable()
        {
            if (!CheckIfExist("files"))
            {
                StartTransaction();

                sqlCommand = "CREATE TABLE files(mod TEXT, path TEXT)";
                ExecuteQuery(sqlCommand);

                CommitTransaction();
            }
        }

        public void Files_FillTable()
        {
            if (!CheckIfTableContainsData("files"))
            {
                StartTransaction();

                var files = Directory.GetFiles(Settings.Default.backupFolder);

                foreach (string file in files)
                {
                    sqlCommand = "INSERT INTO files (mod, path) values (\"none\", \"" + file + "\")";
                    command = new SQLiteCommand(sqlCommand, dbConnection);
                    command.ExecuteNonQuery();
                }

                CommitTransaction();
            }
        }

        public void Files_AddRecords(string modName, string[] files)
        {
            StartTransaction();

            List<string> records = new();
            for (int i = 0; i < files.Length; i++)
            {
                sqlCommand = "SELECT * FROM files WHERE path = \"" + files[i] + "\"";
                command = new SQLiteCommand(sqlCommand, dbConnection);
                var reader = command.ExecuteReader();

                while (reader.Read())
                {
                    records.Add((string)reader["path"]);
                }
            }

            foreach (string file in files)
            {
                if (records.Contains(file))
                {
                    sqlCommand = "DELETE FROM files WHERE path = \"" + file + "\"";
                    command = new SQLiteCommand(sqlCommand, dbConnection);
                    command.ExecuteNonQuery();
                }

                sqlCommand = "INSERT INTO files (mod, path) values (\"" + modName + "\", \"" + file + "\")";
                command = new SQLiteCommand(sqlCommand, dbConnection);
                command.ExecuteNonQuery();
            }

            CommitTransaction();
        }

        /// <summary>
        /// Gets the files for the chosen mod and removes them from the table
        /// </summary>
        /// <param name="modName"></param>
        /// <returns></returns>

        public string[] Files_TakeRecords(string modName)
        {
            StartTransaction();

            sqlCommand = "SELECT * FROM files WHERE mod = \"" + modName + "\"";
            command = new SQLiteCommand(sqlCommand, dbConnection);
            var reader = command.ExecuteReader();

            var resultList = new List<string>();
            while (reader.Read())
            {
                resultList.Add((string)reader["path"]);
            }

            sqlCommand = "DELETE FROM files WHERE mod = \"" + modName + "\"";
            command = new SQLiteCommand(sqlCommand, dbConnection);
            command.ExecuteNonQuery();

            CommitTransaction();

            return resultList.ToArray();
        }

        public string? Files_CheckForConflicts(string[] fileList)
        {
            sqlCommand = "SELECT * FROM files";
            command = new SQLiteCommand(sqlCommand, dbConnection);
            var reader = command.ExecuteReader();

            var resultList = new List<string>();
            var resultList_Mods = new List<string>();
            while (reader.Read())
            {
                resultList.Add((string)reader["path"]);
                resultList_Mods.Add((string)reader["mod"]);
            }

            foreach (var file in fileList)
            {
                if (resultList.Contains(file))
                {
                    return resultList_Mods[resultList.IndexOf(file)];
                }
            }
            return null;
        }

        public void Strings_CreateTable()
        {
            if (!CheckIfExist("strings"))
            {
                StartTransaction();

                sqlCommand = "CREATE TABLE strings(mod TEXT, section TEXT, key TEXT, string TEXT)";
                ExecuteQuery(sqlCommand);

                CommitTransaction();
            }
        }

        public void Strings_FillTable()
        {
            if (!CheckIfTableContainsData("strings"))
            {
                StartTransaction();

                foreach (var section in PCUE_ModManager.instance.ModdedStrings)
                {
                    foreach (var entry in section.Value)
                    {
                        sqlCommand = "INSERT INTO strings (mod, section, key, string) values (\"none\", \"" + section.Key + "\", \"" + entry.Key + "\", \"" + entry.Value + "\")";
                        command = new SQLiteCommand(sqlCommand, dbConnection);
                        command.ExecuteNonQuery();
                    }
                }

                CommitTransaction();
            }
        }

        public void Strings_AddRecords(string modName, Dictionary<string, Dictionary<string, string>> strings)
        {
            StartTransaction();

            foreach (var section in strings)
            {
                foreach (var entry in section.Value)
                {
                    sqlCommand = "DELETE FROM strings WHERE section = \"" + section.Key + "\"" +
                        " AND key = \"" + entry.Key + "\"";
                    command = new SQLiteCommand(sqlCommand, dbConnection);
                    command.ExecuteNonQuery();

                    sqlCommand = "INSERT INTO strings (mod, section, key, string) values (\"" + modName + "\", \"" + section.Key + "\", \"" + entry.Key + "\", \"" + entry.Value + "\")";
                    command = new SQLiteCommand(sqlCommand, dbConnection);
                    command.ExecuteNonQuery();
                }
            }

            CommitTransaction();
        }

        public Dictionary<string, Dictionary<string, string>> Strings_TakeRecords(string modName)
        {
            StartTransaction();

            sqlCommand = "SELECT * FROM strings WHERE mod = \"" + modName + "\"";
            command = new SQLiteCommand(sqlCommand, dbConnection);
            var reader = command.ExecuteReader();

            var resultList = new Dictionary<string, Dictionary<string, string>>();
            while (reader.Read())
            {
                var section = (string)reader["section"];
                var key = (string)reader["key"];
                var text = (string)reader["text"];
                resultList[section][key] = text;
            }

            sqlCommand = "DELETE FROM strings WHERE mod = \"" + modName + "\"";
            command = new SQLiteCommand(sqlCommand, dbConnection);
            command.ExecuteNonQuery();

            CommitTransaction();

            return resultList;
        }
    }
}
