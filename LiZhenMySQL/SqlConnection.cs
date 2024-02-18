using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LiZhenMySQL
{
    public class SqlConnection
    {
        public MySqlConnection Connection { get; set; }
        public string ConnectStr { get; private set; }
        public string Server { get; set; }
        public string Port { get; set; }
        public string DataBase { get; set; }
        public string User { get; set; }
        public string Password { get; set; }
        public string Exception { get; private set; }
        public ConnectionState State => Connection.State;
        public bool Open()
        {
            if (State == ConnectionState.Closed)
                try
                {
                    Connection.Open();
                    return true;
                }
                catch (Exception e)
                {
                    this.Exception = e.Message;
                    return false;
                }
            else
                return true;
        }
        public bool Close()
        {
            try
            {
                Connection.Close();
                return true;
            }
            catch(Exception e)
            {
                this.Exception = e.Message;
                return false;
            }
        }
        public SqlConnection(string server, string port, string database, string user, string password)
        {
            Server = server;
            Port = port;
            DataBase = database;
            User = user;
            Password = password;
            ConnectStr = string.Format(@"server={0};port={1};database={2};user={3};password={4};CharSet=utf8", Server, Port, DataBase, User, Password);
            Connection = new MySqlConnection(ConnectStr);
        }
    }

}