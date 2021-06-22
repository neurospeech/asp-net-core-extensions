using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Linq;
using NeuroSpeech.EFCoreLiveMigration;

namespace NeuroSpeech.EFCore.Mock
{
    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class OriginalSqlDatabase<T>
    where T : DbContext
    {

        public DirectoryInfo TempPath { get; private set; }

        public string DbFile { get; set; }
        public string LogFile { get; set; }

        public string DBName { get; set; }

        public OriginalSqlDatabase(Func<string> getVersion, bool create = false)
        {

            if (create)
            {
                CreateDb(getVersion());
            }
            else
            {
                DbFile = Path.GetTempFileName();
                LogFile = Path.GetTempFileName();

                File.Copy(_Original.DbFile, DbFile, true);
                File.Copy(_Original.LogFile, LogFile, true);

                DBName = $"{typeof(T).Name}{string.Join("", Guid.NewGuid().ToByteArray().Select(x => x.ToString("x2")))}";

                SqlHelper.Execute($"CREATE DATABASE [{DBName}] ON PRIMARY (NAME = {DBName}_data, FILENAME='{DbFile}') LOG ON (NAME={DBName}_Log, FILENAME='{LogFile}') FOR ATTACH");

            }
        }

        private void CreateDb(string version)
        {
            var v = version.Replace(".", "_");
            var n = typeof(T).FullName;
            TempPath = new System.IO.DirectoryInfo($"{Path.GetTempPath()}\\{n}");
            if (!TempPath.Exists)
                TempPath.Create();

            DbFile = $"{TempPath.FullName}\\DB_{v}.mdf";
            LogFile = $"{TempPath.FullName}\\DB_{v}.ldf";

            if (File.Exists(DbFile))
                return;

            // delete others..
            try
            {
                foreach (var file in TempPath.EnumerateFiles())
                {
                    if (file.Exists)
                    {
                        try
                        {
                            file.Delete();
                        }
                        catch (Exception ex)
                        {
                            Trace.WriteLine(ex.ToString());
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.ToString());
            }

            string DBName = $"{typeof(T).Name}_{v}";

            // create....
            SqlHelper.Execute($"CREATE DATABASE [{DBName}] ON PRIMARY (NAME = {DBName}_data, FILENAME='{DbFile}') LOG ON (NAME={DBName}_Log, FILENAME='{LogFile}')");
            SqlConnectionStringBuilder sqlCnstr = CreateConnectionStringBuilder(DBName);

            MockDatabaseContext.Current.ConnectionString = sqlCnstr.ToString();
            Exception lastError = null;
            try
            {
                DbContextOptionsBuilder<T> options = new DbContextOptionsBuilder<T>();
                options.UseSqlServer(sqlCnstr.ConnectionString);

                using (var db = (T)Activator.CreateInstance(typeof(T), options.Options))
                {

                    db.MigrateSqlServer().Migrate();

                    //db.Talents.FirstOrDefault();
                    //var p = db.GetType().GetProperties().Where(x => x.PropertyType.Name.StartsWith("DbSet")).FirstOrDefault();
                    //var en = p.GetValue(db) as IEnumerable<object>;
                    //foreach (var item in en)
                    //{
                    //    break;
                    //}

                }
            }
            catch (Exception ex)
            {
                lastError = ex;
            }

            // detach...
            SqlHelper.Execute("USE master;");
            SqlHelper.Execute($"ALTER DATABASE [{DBName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;");
            SqlHelper.ExecuteSP($"EXEC MASTER.dbo.sp_detach_db @dbname", new KeyValuePair<string, object>("@dbname", DBName));

            if (lastError != null)
            {
                throw new InvalidOperationException("Database creation failed", lastError);
            }

            Trace.WriteLine("Database recreated successfully");
        }

        protected virtual SqlConnectionStringBuilder CreateConnectionStringBuilder(string DBName)
        {
            var sqlCnstr = new SqlConnectionStringBuilder()
            {
                DataSource = "(localdb)\\MSSQLLocalDB",
                //sqlCnstr.AttachDBFilename = t;
                InitialCatalog = DBName,
                IntegratedSecurity = true,
                ApplicationName = "EntityFramework"
            };
            return sqlCnstr;
        }

        private static OriginalSqlDatabase<T> _Original = null;
        private static object lockObject = new object();
        public static OriginalSqlDatabase<T> GetInstance(Func<string> getVersion)
        {
            lock (lockObject)
            {
                if (_Original == null)
                {
                    _Original = new OriginalSqlDatabase<T>(getVersion, true);
                }
            }
            OriginalSqlDatabase<T> od = new OriginalSqlDatabase<T>(null);
            return od;
        }

    }
}
