using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Npgsql;

namespace VTC.Common
{
    public class DatabaseAccess
    {
        public static NpgsqlConnection OpenConnection()
        {
            var cs = "Host=localhost;Port=5434;Username=postgres;Password=password;Database=roadometry";
            var dbConnection = new NpgsqlConnection(cs);
            dbConnection.Open();
            return dbConnection;
        }

    }
}
