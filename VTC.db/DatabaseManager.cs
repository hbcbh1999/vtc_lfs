using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Dapper;
using Npgsql;
using VTC.Common;

namespace VTC.db
{
    public static class DatabaseManager
    {
        public static DbConnection OpenConnection(UserConfig config)
        {
            var cs = $"Host={config.DatabaseUrl};Port={config.DatabasePort};Username={config.Username};Password={config.Password};Database={config.DatabaseName}";
            var dbConnection = new NpgsqlConnection(cs);
            dbConnection.Open();
            return dbConnection;
        }

        public static void CreateDatabase(DbConnection connection)
        {
            var cmd = connection.CreateCommand();
            cmd.CommandText = Resource1.CreateDatabaseSQL;
            cmd.ExecuteNonQuery();
            CreateTables(connection);
        }

        public static bool CheckIfDatabaseExists(DbConnection connection)
        {
            var cmd = connection.CreateCommand();
            cmd.CommandText = Resource1.CheckIfDatabaseExistsSQL;
            var dbExists = cmd.ExecuteScalar() != null;
            return dbExists;
        }

        public static void CreateTables(DbConnection connection)
        {
            CreateJobTable(connection);
            CreateMovementTable(connection);
            CreateStateEstimateTable(connection);
        }

        public static void CreateJobTable(DbConnection connection)
        {
            try
            {
                var cmd = connection.CreateCommand();
                cmd.CommandText = Resource1.CreateJobTableSQL;
                cmd.ExecuteNonQuery();
            }
            catch (PostgresException ex)
            {
                Debug.WriteLine(ex.Message);
            }
            
        }

        public static void CreateMovementTable(DbConnection connection)
        {
            try
            { 
                var cmd = connection.CreateCommand();
                cmd.CommandText = Resource1.CreateMovementTableSQL;
                cmd.ExecuteNonQuery();
            }
            catch (PostgresException ex)
            {
                Debug.WriteLine(ex.Message);
            }

        }

        public static void CreateStateEstimateTable(DbConnection connection)
        {
            try
            {
                var cmd = connection.CreateCommand();
                cmd.CommandText = Resource1.CreateStateEstimateTableSQL;
                cmd.ExecuteNonQuery();
            }
            catch (PostgresException ex)
            {
                Debug.WriteLine(ex.Message);
            }

        }

        public static void ResetDatabase(DbConnection connection)
        {
            var cmdDropStateEstimateTable = connection.CreateCommand();
            cmdDropStateEstimateTable.CommandText = "DROP TABLE IF EXISTS public.stateestimate";
            cmdDropStateEstimateTable.ExecuteNonQuery();

            var cmdDropMovementTable = connection.CreateCommand();
            cmdDropMovementTable.CommandText = "DROP TABLE IF EXISTS public.movement";
            cmdDropMovementTable.ExecuteNonQuery();

            var cmdDropJobTable = connection.CreateCommand();
            cmdDropJobTable.CommandText = "DROP TABLE IF EXISTS public.job";
            cmdDropJobTable.ExecuteNonQuery();

            CreateTables(connection);
        }

        public static void DeleteDatabaseLogs(DbConnection connection)
        {
            var cmdDropStateEstimateTable = connection.CreateCommand();
            cmdDropStateEstimateTable.CommandText = "DELETE FROM public.stateestimate";
            cmdDropStateEstimateTable.ExecuteNonQuery();

            var cmdDropMovementTable = connection.CreateCommand();
            cmdDropMovementTable.CommandText = "DELETE FROM public.movement";
            cmdDropMovementTable.ExecuteNonQuery();

            var cmdDropJobTable = connection.CreateCommand();
            cmdDropJobTable.CommandText = "DELETE FROM public.job";
            cmdDropJobTable.ExecuteNonQuery();
        }

        public static List<Movement> GetMovementsByJob(DbConnection connection, int jobId)
        {
            var movements = connection.Query<Movement>($"SELECT * FROM movement WHERE jobid = {jobId}").ToList();
            return movements;
        }

        public static List<BatchVideoJob> GetAllJobs(DbConnection connection)
        {
            var jobs = connection.Query<BatchVideoJob>("SELECT * FROM job").ToList();
            return jobs;
        }
    }
}
