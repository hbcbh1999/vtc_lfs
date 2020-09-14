using System;
using System.Collections.Generic;
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
        public static NpgsqlConnection OpenConnection(UserConfig config)
        {
            var cs = $"Host={config.DatabaseUrl};Port={config.DatabasePort};Username={config.Username};Password={config.Password};Database={config.DatabaseName}";
            var dbConnection = new NpgsqlConnection(cs);
            dbConnection.Open();
            return dbConnection;
        }

        public static void CreateDatabase(NpgsqlConnection connection)
        {
            var cmd = new NpgsqlCommand(Resource1.CreateDatabaseSQL, connection);
            cmd.ExecuteNonQuery();
            CreateTables(connection);
        }

        public static bool CheckIfDatabaseExists(NpgsqlConnection connection)
        {
            var cmd = new NpgsqlCommand(Resource1.CheckIfDatabaseExistsSQL, connection);
            var dbExists = cmd.ExecuteScalar() != null;
            return dbExists;
        }

        public static void CreateTables(NpgsqlConnection connection)
        {
            CreateJobTable(connection);
            CreateMovementTable(connection);
            CreateStateEstimateTable(connection);
        }

        public static void CreateJobTable(NpgsqlConnection connection)
        {
            try
            {
                var cmd = new NpgsqlCommand(Resource1.CreateJobTableSQL, connection);
                cmd.ExecuteNonQuery();
            }
            catch (PostgresException ex)
            {
                Debug.WriteLine(ex.Message);
            }
            
        }

        public static void CreateMovementTable(NpgsqlConnection connection)
        {
            try
            {
                var cmd = new NpgsqlCommand(Resource1.CreateMovementTableSQL, connection);
                cmd.ExecuteNonQuery();
            }
            catch (PostgresException ex)
            {
                Debug.WriteLine(ex.Message);
            }

        }

        public static void CreateStateEstimateTable(NpgsqlConnection connection)
        {
            try
            {
                var cmd = new NpgsqlCommand(Resource1.CreateStateEstimateTableSQL, connection);
                cmd.ExecuteNonQuery();
            }
            catch (PostgresException ex)
            {
                Debug.WriteLine(ex.Message);
            }

        }

        public static void ResetDatabase(NpgsqlConnection connection)
        {
            var cmdDropStateEstimateTable = new NpgsqlCommand(
                "DROP TABLE IF EXISTS public.stateestimate",
                connection);
            cmdDropStateEstimateTable.ExecuteNonQuery();

            var cmdDropMovementTable = new NpgsqlCommand(
                "DROP TABLE IF EXISTS public.movement",
                connection);
            cmdDropMovementTable.ExecuteNonQuery();

            var cmdDropJobTable = new NpgsqlCommand(
                "DROP TABLE IF EXISTS public.job",
                connection);
            cmdDropJobTable.ExecuteNonQuery();

            CreateTables(connection);
        }

        public static void DeleteDatabaseLogs(NpgsqlConnection connection)
        {
            var cmdDropStateEstimateTable = new NpgsqlCommand(
                "DELETE FROM public.stateestimate",
                connection);
            cmdDropStateEstimateTable.ExecuteNonQuery();

            var cmdDropMovementTable = new NpgsqlCommand(
                "DELETE FROM public.movement",
                connection);
            cmdDropMovementTable.ExecuteNonQuery();

            var cmdDropJobTable = new NpgsqlCommand(
                "DELETE FROM public.job",
                connection);
            cmdDropJobTable.ExecuteNonQuery();
        }

        public static List<Movement> GetMovementsByJob(NpgsqlConnection connection, int jobId)
        {
            var movements = connection.Query<Movement>($"Select * FROM movement WHERE jobid = {jobId}").ToList();
            return movements;
        }

        public static List<BatchVideoJob> GetAllJobs(NpgsqlConnection connection)
        {
            var jobs = connection.Query<BatchVideoJob>("Select * FROM job").ToList();
            return jobs;
        }
    }
}
