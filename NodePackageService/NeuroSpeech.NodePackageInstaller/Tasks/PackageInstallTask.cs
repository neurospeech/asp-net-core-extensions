using ICSharpCode.SharpZipLib.GZip;
using ICSharpCode.SharpZipLib.Tar;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace NeuroSpeech.Tasks
{
    public class PackageInstallTask : IDisposable
    {
        private PackagePath packagePath;
        private HttpClient client;
        private List<string> queuedPackages = new List<string>();

        public PackageInstallTask(PackagePath packagePath)
        {
            this.packagePath = packagePath;
            this.client = new HttpClient();
        }

        public void Dispose()
        {
            this.client.Dispose();
        }
        public async Task RunAsync()
        {
            var tempFolder = new DirectoryInfo($"{this.packagePath.Options.TempFolder}\\tmp\\{Guid.NewGuid()}");
            
            await InstallAsync(this.packagePath, tempFolder);

            var tagFolder = tempFolder.GetDirectories()[0];

            var packageTagFolder = new DirectoryInfo(this.packagePath.TagFolder);
            if (!packageTagFolder.Exists)
            {
                packageTagFolder.Create();
            }

            tagFolder.MoveTo(this.packagePath.TagFolder);

            tempFolder.Delete(true);
        }

        private async Task<string> ReadAllTextAsync(string path)
        {
            using(var fs = System.IO.File.OpenRead(path))
            {
                using(var sr = new StreamReader(fs))
                {
                    return await sr.ReadToEndAsync();
                }
            }
        }

        private async Task InstallAsync(PackagePath package, DirectoryInfo tagFolder)
        {

            await DownloadAsync(package, tagFolder);

            tagFolder = tagFolder.GetDirectories()[0];

            // read config..
            var packageConfig = await ReadAllTextAsync(destination + "\\package.json");
            var config = JObject.Parse(packageConfig);

            if(!config.ContainsKey("dependencies"))
            {
                return;
            }
            
            // bundled dependencies must have all the dependencies
            if (config.ContainsKey("bundledDependencies"))
            {
                return;
            }

            var dep = config["dependencies"] as JObject;
            if (dep == null)
            {
                return;
            }
            List<PackagePath> dependencies = new List<PackagePath>();
            foreach(var key in dep.Properties()) {
                string value = key.Value?.ToString();
                if (value==null)
                {
                    continue;
                }

                value = new string(value
                    .SkipWhile(x => !Char.IsDigit(x))
                    .TakeWhile(x => x == '.' || Char.IsDigit(x))
                    .ToArray());

                if (string.IsNullOrWhiteSpace(value))
                {
                    continue;
                }

                string pn = $"{key.Name}@{value}";
                var cp = new PackagePath(package.Options, pn.ParseNPMPath(), true);

                if (queuedPackages.Contains(cp.Package))
                {
                    continue;
                }
                queuedPackages.Add(cp.Package);
                dependencies.Add(cp);
            }

            await Task.WhenAll( dependencies.Select((p) =>
                InstallAsync(p, $"{tagFolder.FullName}\\node_modules\\{p.Package}")
            ) );

        }

        private async Task DownloadAsync(PackagePath package, DirectoryInfo tagFolder)
        {

            try
            {
                using (var stream = await client.GetStreamAsync(package.PrivateNpmUrl))
                {
                    using (var ungzStream = new GZipInputStream(stream))
                    {
                        using (var tar = TarArchive.CreateInputTarArchive(ungzStream))
                        {
                            // tar.ExtractContents(packagePath.TagFolder);

                            tar.ExtractContents(tagFolder.FullName);
                            var parent = tagFolder.Parent;
                            if (!parent.Exists)
                            {
                                parent.Create();
                            }

                            // var tmp = tempFolder.GetDirectories()[0];

                            // Directory.Move(tmp.FullName, tagFolder.FullName);

                        }
                    }

                }

            }
            catch
            {
                if(tagFolder.Exists)
                {
                    tagFolder.Delete(true);
                }
                throw;
            } finally
            {
                //if (tempFolder.Exists)
                //{
                //    tempFolder.Delete(true);
                //}
            }
        }


    }
}
