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
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace SSMSHistory
{
    public partial class InputBoxForm : Form
    {
        public InputBoxForm()
        {
            InitializeComponent();
            _server = _database = _table = _username = _password = "";
        }

        public InputBoxForm(string server, string database, string table, string username, string password){
            InitializeComponent();
            
            serverTextBox.Text = server;
            databaseTextBox.Text = database;
            tableTextBox.Text = table;
            usernameTextBox.Text = username;
            passwordTextBox.Text = password;

            _server = _database = _table = _username = _password = "";
        }

        private void InputBox_Load(object sender, EventArgs e)
        {

        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (String.IsNullOrEmpty(serverTextBox.Text) || String.IsNullOrEmpty(databaseTextBox.Text) ||
                    String.IsNullOrEmpty(tableTextBox.Text) || String.IsNullOrEmpty(usernameTextBox.Text) ||
                    String.IsNullOrEmpty(passwordTextBox.Text))
            {
                MessageBox.Show("You must fill out all fields");
            }
            else
            {
                _server = serverTextBox.Text;
                _database = databaseTextBox.Text;
                _table = tableTextBox.Text;
                _username = usernameTextBox.Text;
                _password = passwordTextBox.Text;

                this.Close();
            }

        }

        public string Server() { return _server; }
        public string Database() { return _database; }
        public string Table() { return _table; }
        public string Username() { return _username; }
        public string Password() { return _password; }

        private string _server;
        private string _database;
        private string _table;
        private string _username;
        private string _password;


    }
}
