using ICSharpCode.SharpZipLib.GZip;
using ICSharpCode.SharpZipLib.Tar;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace NeuroSpeech.Tasks
{
    //public class TarGZExtractTask : IDisposable
    //{

    //    PackagePath packagePath;
    //    readonly string tempRoot;

    //    readonly DirectoryInfo TagFolder;
    //    readonly DirectoryInfo TempFolder;

    //    public TarGZExtractTask(PackagePath pp, string tempRoot)
    //    {
    //        this.tempRoot = tempRoot;
    //        this.packagePath = pp;
    //        this.TagFolder = new DirectoryInfo(pp.TagFolder);
    //        this.TempFolder = new DirectoryInfo(tempRoot + "\\tmp\\" + Guid.NewGuid());

    //    }

    //    public async Task RunAsync()
    //    {
    //        try
    //        {
    //            string url = this.packagePath.PrivateNpmUrl;

    //            await DownloadAsync(url);



    //        }
    //        catch
    //        {
    //            if (TagFolder.Exists)
    //            {
    //                TagFolder.Delete(true);
    //            }
    //            throw;
    //        } finally
    //        {
    //            TempFolder.Delete(true);
    //        }
    //    }

    //    private async Task DownloadAsync(string url)
    //    {
    //        using (var client = new HttpClient())
    //        {
    //            using (var stream = await client.GetStreamAsync(url))
    //            {
    //                using (var ungzStream = new GZipInputStream(stream))
    //                {
    //                    using (var tar = TarArchive.CreateInputTarArchive(ungzStream))
    //                    {
    //                        // tar.ExtractContents(packagePath.TagFolder);

    //                        tar.ExtractContents(TempFolder.FullName);
    //                        var parent = TagFolder.Parent;
    //                        if (!parent.Exists)
    //                        {
    //                            parent.Create();
    //                        }
    //                        Directory.Move(TempFolder.FullName + "\\package", TagFolder.FullName);

    //                    }
    //                }

    //            }
    //        }
    //    }

    //    public void Dispose()
    //    {

    //    }
    //}
}
