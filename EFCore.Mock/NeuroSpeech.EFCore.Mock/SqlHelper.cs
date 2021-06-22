using Microsoft.Data.SqlClient;
using System.Collections.Generic;

namespace NeuroSpeech.EFCore.Mock
{
    public class SqlHelper
    {


        public static void Execute(string command)
        {
            using (var c = new SqlConnection($"server=(localdb)\\MSSQLLocalDB"))
            {
                c.Open();


                using (var cmd = c.CreateCommand())
                {
                    cmd.CommandText = command;
                    cmd.ExecuteNonQuery();
                }
            }

        }


        public static void ExecuteSP(string command, params KeyValuePair<string, object>[] pairs)
        {
            using (var c = new SqlConnection($"server=(localdb)\\MSSQLLocalDB"))
            {
                c.Open();


                using (var cmd = c.CreateCommand())
                {
                    cmd.CommandText = command;

                    foreach (var v in pairs)
                    {
                        cmd.Parameters.AddWithValue(v.Key, v.Value);
                    }

                    cmd.ExecuteNonQuery();
                }
            }

        }


    }
}
