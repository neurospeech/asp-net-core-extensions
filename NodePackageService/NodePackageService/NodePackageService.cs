using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.NodeServices;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using NeuroSpeech.Tasks;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using NeuroSpeech.Internal;

namespace NeuroSpeech
{
    internal class NFileLock : IDisposable
    {
        readonly FileInfo file;
        readonly FileStream fs;
        private NFileLock(FileInfo file, FileStream fs)
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

            while (true)
            {
                // try to open file...
                try
                {
                    var fs = new FileStream(filePath,
                        FileMode.OpenOrCreate,
                        FileAccess.ReadWrite, FileShare.None);
                    fs.Seek(0, SeekOrigin.Begin);
                    await fs.WriteAsync(new byte[] { 1 }, 0 , 1);
                    return new NFileLock(lockFile, fs);
                }
                catch
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

    public class NodePackageService
    {

        readonly IServiceProvider services;
        readonly IEnumerable<PackagePath> privatePackages;
        readonly AtomicCache<NodePackage> cache;
        readonly Func<IServiceProvider, PackagePathSegments, Task<string>> versionProvider;
        public NodePackageServiceOptions Options { get; }

        public NodePackageService(
            IServiceProvider services,
            NodePackageServiceOptions options,
            Func<IServiceProvider, PackagePathSegments, Task<string>> versionProvider = null)
        {
            this.versionProvider = versionProvider;
            var reg = options.NPMRegistry.TrimEnd('/') + "/";
            this.Options = options;
            this.Options.NPMRegistry = reg;
            this.privatePackages = options.PrivatePackages.Select( x => {
                return new PackagePath(options, x.ParseNPMPath(), true);
            } );
            this.cache = new AtomicCache<NodePackage>();
            this.services = services;            
        }

        public PackagePath ParsePath(string sp)
        {
            var pps = sp.ParseNPMPath();

            var package = pps.Package;
            var version = pps.Version;

            var existing = this.privatePackages.FirstOrDefault(x => x.Package == package);

            // replace version... if it is empty
            if(string.IsNullOrWhiteSpace(version))
            {
                if (existing != null)
                {
                    version = existing.Version;
                }
            }

            return new PackagePath(this.Options, pps, existing != null);
        }

        protected virtual async Task<PackagePathSegments> ResolveVersion(PackagePathSegments pps)
        {
            var package = pps.Package;
            var version = pps.Version;


            // replace version... if it is empty
            if (string.IsNullOrWhiteSpace(version))
            {
                // find from version provider..
                if (versionProvider != null)
                {
                    version = await versionProvider(services, pps);
                }
                if (string.IsNullOrWhiteSpace(version))
                {
                    var existing = this.privatePackages.FirstOrDefault(x => x.Package == package);
                    if (existing != null)
                    {
                        version = existing.Version;
                    }
                }
            }

            if (!string.IsNullOrWhiteSpace(version))
            {
                pps.Version = version;
            }

            return pps;
        }

        public async Task DownloadAsync(PackagePath packagePath)
        {
            if (!Directory.Exists(packagePath.TagFolder))
            {
                using(var tgz = new PackageInstallTask(packagePath))
                {
                    await tgz.RunAsync();
                }
            }

        }

        private Dictionary<string, bool> loading = new Dictionary<string, bool>();

        public async Task InstallAsync(PackagePath packagePath)
        {


            if (this.Options.UseFileLock)
            {
                using (var fl = await NFileLock.AcquireAsync(
                    packagePath.TagFolder + "_lock",
                    TimeSpan.FromMinutes(15)))
                {
                    if (Directory.Exists(packagePath.TagFolder))
                    {
                        return;
                    }
                    await DownloadAsync(packagePath);
                    return;
                }
            }

            bool wait = false;

            while (true) {
         
                lock(loading)
                {
                    if(loading.ContainsKey(packagePath.TagFolder))
                    {
                        wait = true;
                    } else
                    {
                        wait = false;
                        loading[packagePath.TagFolder] = true;
                        break;
                    }
                }
                if (wait)
                {
                    await Task.Delay(1000);
                }
            }

            try
            {

                if (!Directory.Exists(packagePath.TagFolder))
                {
                    await DownloadAsync(packagePath);
                }

            } finally {
                lock (loading)
                {
                    loading.Remove(packagePath.TagFolder);
                }
            }
        }

        public async Task<NodePackage> GetInstalledPackageAsync(string path)
        {
            PackagePathSegments pps = path;
            pps = await this.ResolveVersion(pps);
            var pp = new PackagePath(this.Options, pps, true);
            return await cache.GetAsync(pp.Package + "@" + pp.Version, async entry => {

                entry.SlidingExpiration = this.Options.TTL;

                await InstallAsync(pp);

                var options = this.Options.NodeServicesOptions ?? new NodeServicesOptions(services)
                {
                    ProjectPath = pp.TagFolder,
                    NodeInstanceOutputLogger = services.GetService<ILogger<NodePackageService>>()
                };

                options.ProjectPath = pp.TagFolder;

                if (this.Options.EnvironmentVariables != null)
                {
                    if (options.EnvironmentVariables == null)
                    {
                        options.EnvironmentVariables = new Dictionary<string, string>();
                    }
                    foreach (var item in this.Options.EnvironmentVariables)
                    {
                        options.EnvironmentVariables[item.Key] = item.Value;
                    }
                }

                var s = NodeServicesFactory.CreateNodeServices(options);

                entry.EvictionCallbacks.Add((c) => {
                    try
                    {
                        s.Dispose();
                    } catch(Exception ex)
                    {
                        Trace.WriteLine(ex);
                    }
                });

                return new NodePackage {
                    Path = pp,
                    NodeServices = s
                };

            });

        }



    }

    public class NodePackage
    {
        public PackagePath Path;
        public INodeServices NodeServices;
    }
}
