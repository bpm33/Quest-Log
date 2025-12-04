/*
Benjamin Mather
20251204
4.6 Course Project
Database Implementation

Utility class to handle database connection and basic management for the Goal Tracking App.
*/
using System.Data.SQLite;

namespace GoalTrackingApp
{
    public static class SQLiteConnector
    {
        private const string DatabaseName = "GoalTrackingApp.db";

        // Connects to the SQLite database file, creating it if it doesn't exist.
        public static SQLiteConnection? Connect()
        {
            string connectionString = $@"Data Source={DatabaseName};Version=3;";
            SQLiteConnection connection = new SQLiteConnection(connectionString);

            try
            {
                connection.Open();
                Console.WriteLine($"\nSuccessfully connected to {DatabaseName}");
            }
            catch (Exception e)
            {
                Console.WriteLine($"An error occurred during database connection/creation: {e.Message}");
                return null; 
            }
            return connection;
        }

        //Resets the auto-increment counter for a specified table for testing.
        public static void ResetAutoIncrement(SQLiteConnection conn, string tableName)
        {
            string sqlReset = $"DELETE FROM sqlite_sequence WHERE name = '{tableName}'";
            using (SQLiteCommand cmd = new SQLiteCommand(sqlReset, conn))
            {
                cmd.ExecuteNonQuery();
            }
        }
    }
}