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
using Extensibility;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.CommandBars;
using Microsoft.SqlServer.Management.UI.VSIntegration;
using Microsoft.SqlServer.Management.UI.VSIntegration.Editors;
using Microsoft.SqlServer.Management.UI.VSIntegration.ObjectExplorer;
using Microsoft.SqlServer.Management.SqlStudio.Explorer;
using System.Resources;
using System.Reflection;
using System.Globalization;
using System.Windows.Forms;
using SSMSHistory;

namespace SSMSHistory
{
	public class Connect : IDTExtensibility2, IDTCommandTarget
	{
		public void OnConnection(object application, ext_ConnectMode connectMode, object addInInst, ref Array custom)
		{
            _addInInstance = (AddIn)addInInst;
            _applicationObject = (DTE2)_addInInstance.DTE;
            _db = new Database();

            if (_db.Running()) // no point adding the handler if the service is not running
            {
                _commandEvents = _applicationObject.Events.get_CommandEvents("{52692960-56BC-4989-B5D3-94C47A513E8D}", 1); // Query.Execute
                _commandEvents.AfterExecute += new _dispCommandEvents_AfterExecuteEventHandler(OnAfterExecute);
            }
		}

        private void OnAfterExecute(string guid, int id, Object customId, Object customOut)
        {
            QueryExecuted();
        }

        private void QueryExecuted()
        {
            TextDocument textDocument;
            string query, file_contents, file_name, database, server, server_dot, username;
            CurrentlyActiveWndConnectionInfo connection_info;
            textDocument = GetDocument();

            query = GetQueryText(textDocument);
            file_contents = QueryContext(textDocument);
            file_name = _applicationObject.ActiveWindow.Document.FullName;
            connection_info = ServiceCache.ScriptFactory.CurrentlyActiveWndConnectionInfo;
            server = connection_info.UIConnectionInfo.ServerNameNoDot; // connection_info.UIConnectionInfo.ServerName; // connection_info.UIConnectionGroupInfo.Name
            server_dot = connection_info.UIConnectionInfo.ServerName;
            username = connection_info.UIConnectionInfo.UserName;
            database = connection_info.UIConnectionInfo.AdvancedOptions["database"];

            Debug(query, file_contents, file_name, server, username, database);

            if(_db.Running()) //make sure that the service is still running.
                _db.InsertQuery(query, file_contents, file_name, server, server_dot, database, username);
        }

        private TextDocument GetDocument()
        {
            Document document;
            document = _applicationObject.ActiveDocument;

            return (TextDocument)document.Object("TextDocument");
        }

        private string GetQueryText(TextDocument textDocument)
        {
            string query = textDocument.Selection.Text;

            if (query.Length == 0)
                query = QueryContext(textDocument);

            return query;
        }

        private void Debug(string query, string file_contents, string file_name, string server, string username, string database)
        {
            System.Diagnostics.Trace.Write("\nquery run: \"" + query + "\"\n");
            System.Diagnostics.Trace.Write("file_contents: \"" + file_contents + "\"\n");
            System.Diagnostics.Trace.Write("file_name: " + file_name + "\n");
            System.Diagnostics.Trace.Write("server:" + server + "\n");
            System.Diagnostics.Trace.Write("username: " + username + "\n");
            System.Diagnostics.Trace.Write("database: " + database + "\n\n");
        }

        private string QueryContext(TextDocument textDocument)
        {
            EditPoint ep = textDocument.CreateEditPoint(textDocument.StartPoint);
            return ep.GetText(textDocument.EndPoint);
        }

        public Connect() { }
        void Provider_SelectionChanged(object sender, NodesChangedEventArgs args) { }
        void UtilityExplorerContext_CurrentContextChanged(object sender, NodesChangedEventArgs args) { }
        void ObjectExplorerContext_CurrentContextChanged(object sender, NodesChangedEventArgs args) { }
        public void OnDisconnection(ext_DisconnectMode disconnectMode, ref Array custom) { }
        public void OnAddInsUpdate(ref Array custom) { }
        public void OnStartupComplete(ref Array custom) { }
        public void OnBeginShutdown(ref Array custom) { }
        public void QueryStatus(string commandName, vsCommandStatusTextWanted neededText, ref vsCommandStatus status, ref object commandText) { }
        public void Exec(string commandName, vsCommandExecOption executeOption, ref object varIn, ref object varOut, ref bool handled) { }

        private DTE2 _applicationObject;
        private AddIn _addInInstance;
        private CommandEvents _commandEvents;
        private Database _db;
	}
}