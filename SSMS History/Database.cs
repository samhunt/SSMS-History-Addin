/*
 * Logs all T-SQL queries to the table referenced in the config.
 * Copyright (C) 2013 Sam Hunt
 * 
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 * 
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License
 * along with this program.  If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Collections.Generic;
using System.Text;
using System.Data.SqlClient;
using System.Xml;
using System.Xml.Linq;
using System.ServiceProcess;

namespace SSMSHistory
{
    public class Database
    {
        private static string _xmlFile = "config.xml";
        private static string _server;
        private static string _databaseName;
        private static string _tableName;
        private static string _username;
        private static string _password;
        private static string _connectionString;
        private static bool   _running = false;

        public Database()
        {
            _xmlFile = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().CodeBase) + "\\" +_xmlFile;
            _xmlFile = _xmlFile.Replace("file:\\", "");
            LoadConfig();

            if (_server.Equals(".")) _server = "(local)"; // these mean the same thing

            if (_server.Equals("(local)"))
            {
                try
                {
                    ServiceController sc = new ServiceController("MSSQLSERVER");

                    if (sc.Status == ServiceControllerStatus.Running)
                    {
                        _running = true;
                    }
                }
                catch (Exception)
                {
                }
            }
            else
            {
                _running = true;
            }

            if (_running)
            {
                if (!DatabaseExists() && _server.Equals("(local)"))
                {
                    CreateDatabase();
                }
                else if (!DatabaseExists())
                {
                    _running = false;
                    return;
                }
                _connectionString += "Database=" + _databaseName + ";";
                if (!TableExists())
                {
                    CreateTables();
                }
            }
        }

        private void LoadConfig()
        {
            //bool populated = false;

            //check if the file already exists, if it exists load the file, if not, create the new file and populate the file.
            XDocument document;
            XElement root = null;

            if (System.IO.File.Exists(_xmlFile))
            {
                document = XDocument.Load(_xmlFile);
                root = document.Root;

                if (root.Name.LocalName.Equals("config"))
                {
                    if (root.HasElements)
                    {
                        _server = GetElement(root, "server");
                        _databaseName = GetElement(root, "databaseName");
                        _tableName = GetElement(root, "tableName");
                        _username = GetElement(root, "username");
                        _password = GetElement(root, "password");
                        document.Save(_xmlFile);
                    }
                }
            }else
            {
                CreateConfig();
                LoadConfig();
            }
            _connectionString = "server=" + _server + ";User ID =" + _username + ";Password=" + _password + ";";
        }

        private string GetElement(XElement node, string element)
        {
            var elem = node.Element(element);
            string result = elem.Value;
            if(result == null) //ask the user for the correct value and add the element to the file, then return the value.
            {
                XElement xElement = new XElement(element, "test");
                node.Add(xElement);
            }
            
            return result;
        }

        private XElement CreateElement(InputBoxForm form, string element)
        {
            string value;

            switch (element)
            {
                case "server":
                    value = form.Server();
                    break;
                case "databaseName":
                    value = form.Database();
                    break;
                case "tableName":
                    value = form.Table();
                    break;
                case "username":
                    value = form.Username();
                    break;
                case "password":
                    value = form.Password();
                    break;
                default:
                    value = "";
                    break;
            }
            return new XElement(element, value);
        }


        private void CreateConfig()
        {
            InputBoxForm form = new InputBoxForm();
            form.ShowDialog();

            XElement server = CreateElement(form, "server");
            XElement databaseName = CreateElement(form, "databaseName");
            XElement tableName = CreateElement(form, "tableName");
            XElement username = CreateElement(form, "username");
            XElement password = CreateElement(form, "password");

            XDocument document = new XDocument(new XElement("config"));
            document.Root.Add(server);
            document.Root.Add(databaseName);
            document.Root.Add(tableName);
            document.Root.Add(username);
            document.Root.Add(password);
            document.Save(_xmlFile);
        }

        public bool Running()
        {
            if (_server.Equals("(local)") && _running)
            {
                try
                {
                    System.ServiceProcess.ServiceController sc = new System.ServiceProcess.ServiceController("MSSQLSERVER");

                    if (sc.Status == System.ServiceProcess.ServiceControllerStatus.Running)
                    {
                        _running = true;
                    }
                    else
                    {
                        _running = false;
                    }

                }
                catch (Exception)
                {
                    _running = false;
                }
            }
            else
            {
                _running = true;
            }

            return _running;
        }

        public void InsertQuery(string query_text, string file_contents, string file_name, string server, string server_dot, string db, string user_name)
        {
            string query = "INSERT @tableName (" +
                           "       query," +
                           "       file_contents," +
                           "       file_name," +
                           "       server," +
                           "       server_dot," +
                           "       db," +
                           "       user_name)" +
                           "SELECT @query_text," +
                           "       @file_contents," +
                           "       @file_name," +
                           "       @server," +
                           "       @server_dot," +
                           "       @db," +
                           "       @user_name";

            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@tableName", _tableName);
                cmd.Parameters.AddWithValue("@query_text", query_text);
                cmd.Parameters.AddWithValue("@file_contents", file_contents);
                cmd.Parameters.AddWithValue("@file_name", file_name);
                cmd.Parameters.AddWithValue("@server", server);
                cmd.Parameters.AddWithValue("@server_dot", server_dot);
                cmd.Parameters.AddWithValue("@db", db);
                cmd.Parameters.AddWithValue("@user_name", user_name);

                try
                {
                    conn.Open();
                    cmd.ExecuteNonQuery();
                    conn.Close();
                }
                catch
                {
                    _running = false;
                }
                //if (DatabaseExists() && TableExists()) //overparanoid error checking
                //{
                //    conn.Open();
                //    cmd.ExecuteNonQuery();
                //    conn.Close();
                //}
            }
        }

        private void CreateTables()
        {
            string query = "CREATE TABLE @tableName (" +
                           "       id              NUMERIC(18,0) IDENTITY," +
                           "       query           VARCHAR(Max)," +
                           "       file_contents   VARCHAR(Max)," +
                           "       file_name       VARCHAR(100)," +
                           "       server          VARCHAR(100)," +
                           "       server_dot      VARCHAR(100)," +
                           "       db              VARCHAR(100)," +
                           "       user_name       VARCHAR(100)," +
                           "       host_name       VARCHAR(100) DEFAULT host_name()," +
                           "       insert_datetime DATETIME DEFAULT getdate())";

            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                try
                {
                    conn.Open();
                    SqlCommand cmd = new SqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@tableName", _tableName);
                    cmd.ExecuteNonQuery();

                    conn.Close();
                }
                catch
                {
                    _running = false;
                }
            }
        }

        private void CreateDatabase()
        {
            try
            {
                if (!System.IO.Directory.Exists("C:\\Database"))
                {
                    System.IO.Directory.CreateDirectory("C:\\Database");
                }

                string query = "CREATE DATABASE @databaseName ON PRIMARY (" +
                               "NAME = @databaseName," +
                               "FILENAME = 'C:\\Database\\@databaseName" + "eData.mdf'," +
                               "SIZE = 4096KB, FILEGROWTH = 10%)" +
                               "LOG ON (NAME = @databaseName " + "_Log," +
                               "FILENAME = 'C:\\Database\\@databaseName" + "Log.ldf'," +
                               "SIZE = 4MB," +
                               "FILEGROWTH = 10%)";

                System.Diagnostics.Trace.Write(query);

                using (SqlConnection conn = new SqlConnection(_connectionString))
                {
                    conn.Open();
                    SqlCommand cmd = new SqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@databaseName", _databaseName);
                    cmd.ExecuteNonQuery();

                    conn.Close();
                }
            }
            catch
            {
                _running = false;
            }
        }

        private bool TableExists()
        {
            string query = "SELECT object_id from sys.objects WHERE name = @tableName AND type = 'U';";
            bool exists;
            try
            {
                using (SqlConnection conn = new SqlConnection(_connectionString))
                {
                    conn.Open();

                    SqlCommand cmd = new SqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@tableName", _tableName);
                    try
                    {
                        exists = ((int)cmd.ExecuteScalar() > 0);
                    }
                    catch (Exception)
                    {
                        exists = false;
                    }

                    conn.Close();
                }
            }
            catch
            {
                exists = false;
            }
            return exists;
        }

        private bool DatabaseExists()
        {
            string query = "SELECT database_id FROM sys.databases WHERE Name = @database";
            bool exists;
            try
            {
                using (SqlConnection conn = new SqlConnection(_connectionString))
                {
                    conn.Open();

                    SqlCommand cmd = new SqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@database", _databaseName);
                    try
                    {
                        exists = ((int)cmd.ExecuteScalar() > 0);
                    }
                    catch (Exception)
                    {
                        exists = false;
                    }

                    conn.Close();
                }
            }
            catch
            {
                exists = false;
            }
            return exists;
        }
    }
}
