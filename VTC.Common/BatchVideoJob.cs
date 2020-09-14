using System;
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

        public void Save(NpgsqlConnection dbConnection)
        {
            try
            {
                var result = new NpgsqlCommand(
                    $"INSERT INTO public.job(videopath,regionconfigurationname,groundtruthpath,timestamp) VALUES('{VideoPath}','{RegionConfiguration.Title}','{GroundTruthPath}','{DateTime.Now}') RETURNING id",
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
