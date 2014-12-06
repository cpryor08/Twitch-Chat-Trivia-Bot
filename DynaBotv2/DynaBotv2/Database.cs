using System;
using System.Data;
using System.Data.SQLite;

namespace DynaBotv2
{
    public class Database
    {
        string dbConnection;
        public Database()
        {
            dbConnection = "Data Source=Database.s3db";
        }
        public DataTable GetDataTable(string Query)
        {
            DataTable dt = new DataTable();
            try
            {
                SQLiteConnection con = new SQLiteConnection(dbConnection);
                con.Open();
                SQLiteCommand cmd = new SQLiteCommand(con);
                cmd.CommandText = Query;
                SQLiteDataReader dr = cmd.ExecuteReader();
                dt.Load(dr);
                dr.Close();
                con.Clone();
            }
            catch { }
            return dt;
        }
        public int ExecuteNonQuery(string Query)
        {
            SQLiteConnection con = new SQLiteConnection(dbConnection);
            con.Open();
            SQLiteCommand cmd = new SQLiteCommand(con);
            cmd.CommandText = Query;
            int rowsUpdated = cmd.ExecuteNonQuery();
            con.Clone();
            return rowsUpdated;
        }
    }
}