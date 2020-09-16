using System;
using System.Data.Common;
using System.Diagnostics;
using System.Drawing;
using Npgsql;
using VTC.Common.RegionConfig;

namespace VTC.Common
{
    public class BatchVideoJob
    {
        public string VideoPath;
        public RegionConfig.RegionConfig RegionConfiguration;
        public string GroundTruthPath;
        public DateTime Timestamp;
        public int Id;

        public void Save(DbConnection dbConnection)
        {
            try
            {
                var cmd = dbConnection.CreateCommand();
                cmd.CommandText =
                    $"INSERT INTO public.job(videopath,regionconfigurationname,groundtruthpath,timestamp) VALUES('{VideoPath}','{RegionConfiguration.Title}','{GroundTruthPath}','{DateTime.Now}') RETURNING id";
                var result = cmd.ExecuteScalar();
                Id = int.Parse(result.ToString());
            }
            catch (Exception e)
            {
                Debug.WriteLine("BatchVideoJob.Save:" + e.Message);
            }
        }
    }
}
