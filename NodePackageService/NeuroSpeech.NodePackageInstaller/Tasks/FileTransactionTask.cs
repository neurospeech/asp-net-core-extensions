using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace NeuroSpeech.Tasks
{
    public class FileTransactionTask : IDisposable
    {
        readonly DirectoryInfo folder;
        readonly DirectoryInfo _workingFolder;

        public DirectoryInfo WorkingFolder => this._workingFolder;

        public FileTransactionTask(string folder, string root)
        {
            this.folder = new DirectoryInfo($"{root}\\{folder}");
            this._workingFolder = new DirectoryInfo($"{root}\\tmp\\{Guid.NewGuid()}");

            if (!this._workingFolder.Exists)
            {
                this._workingFolder.Create();
            }
        }

        public void Dispose()
        {
            this._workingFolder.Delete(true);
        }
    }
}
