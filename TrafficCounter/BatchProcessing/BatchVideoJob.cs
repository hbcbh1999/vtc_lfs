using System;
using System.Diagnostics;
using System.Drawing;
using Npgsql;
using VTC.Common.RegionConfig;

namespace VTC.BatchProcessing
{
    public class BatchVideoJob
    {
        public string VideoPath;
        public RegionConfig RegionConfiguration;
        public string GroundTruthPath;
        public int Id;

        public void Save(NpgsqlConnection dbConnection)
        {
            try
            {
                var result = new NpgsqlCommand(
                    $"INSERT INTO job(videopath,regionconfiguration,groundtruthpath,created_at) VALUES('{VideoPath}','{RegionConfiguration.Title}','{GroundTruthPath}','{DateTime.Now}') RETURNING id",
                    dbConnection).ExecuteScalar();
                Id = int.Parse(result.ToString());
            }
            catch (Exception e)
            {
                Debug.WriteLine("BatchVideoJob.Save:" + e.Message);
            }
        }
    }
}
