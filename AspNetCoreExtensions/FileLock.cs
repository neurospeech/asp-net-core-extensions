using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace AspNetCoreExtensions
{
    public class FileLock : IDisposable
    {
        readonly FileInfo file;
        readonly FileStream fs;
        private FileLock(FileInfo file, FileStream fs)
        {
            this.fs = fs;
            this.file = file;
        }

        public static async Task<IDisposable> AcquireAsync(
            string filePath,
            TimeSpan? maxWait = null,
            TimeSpan? poolDelay = null
            )
        {
            FileInfo lockFile = new FileInfo(filePath);

            if (!lockFile.Directory.Exists)
            {
                lockFile.Directory.Create();
            }

            maxWait = maxWait ?? TimeSpan.FromMinutes(15);
            var start = DateTime.UtcNow;

            TimeSpan delay = poolDelay ?? TimeSpan.FromSeconds(5);

            while(true)
            {
                // try to open file...
                try
                {
                    var fs = new FileStream(filePath,
                        FileMode.OpenOrCreate,
                        FileAccess.ReadWrite, FileShare.None);
                    fs.Seek(0, SeekOrigin.Begin);
                    await fs.WriteAsync(new byte[] { 1 });
                    return new FileLock(lockFile, fs);
                } catch 
                {

                }

                await Task.Delay(delay);
                var diff = DateTime.UtcNow - start;
                if (diff > maxWait)
                {
                    throw new TimeoutException();
                }
            }
        }

        public void Dispose()
        {
            fs.Dispose();
            // there is a possibility of other process to acquire
            // lock immediately so we will ignore exceptions here
            try
            {
                file.Delete();
            }
            catch { }
        }
    }
}
