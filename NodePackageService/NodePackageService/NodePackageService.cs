using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
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
using Jering.Javascript.NodeJS;

namespace NeuroSpeech
{


    public class NodePackageService: PackageInstallerService
    {

        private NodePackageServiceOptions _Options;

        public NodePackageService(
            IServiceProvider services,
            NodePackageServiceOptions options,
            Func<IServiceProvider, PackagePathSegments, Task<string>> versionProvider = null)
            : base(services, options, versionProvider)
        {
            this._Options = options;
        }

        public async Task<NodePackage> GetInstalledPackageAsync(string path)
        {
            return await this.InstalledPackageAsync(path) as NodePackage;
        }

        protected override NodeInstalledPackage CreatePackage(PackagePath pp)
        {

            //var options = this._Options.NodeServicesOptions ?? new NodeJSProcessOptions()
            //{
            //    ProjectPath = pp.TagFolder,
            //    NodeInstanceOutputLogger = services.GetService<ILogger<NodePackageService>>()
            //};

            ver options = services.Configure<NodeJSProcessOptions>();

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

            var s = services.GetRequiredService<INodeJSService>();

            return new NodePackage {
                Path = pp,
                NodeServices = s
            };
        }

    }

    public class NodePackage: NodeInstalledPackage
    {
        public INodeJSService NodeServices;

        public override void Dispose()
        {
            NodeServices?.Dispose();
        }
    }
}
