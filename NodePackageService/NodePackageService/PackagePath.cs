using System;
using System.Linq;

namespace NeuroSpeech
{
    public class PackagePath
    {

        public string PrivateNpmUrl => $"{Options.NPMRegistry}{Package}/-/{ID}-{Version}.tgz";

        public readonly bool isPrivate;
        public readonly string Package;
        public readonly string Version;
        public readonly string Path;
        public readonly string TempRoot;

        // private readonly string npmUrlTemplate;

        public string ID => Package.Split("/").Last();

        public string Tag => $"v{Version}";

        public string TagFolder
                => $"{Options.TempFolder}\\npm\\{Package}\\{Tag}";
    
        public NodePackageServiceOptions Options { get; }


        public PackagePath(
            NodePackageServiceOptions options,
            (string, string, string) p,
            bool isPrivate)
        {
            this.Options = options;
            this.isPrivate = isPrivate;
            (this.Package, this.Version, this.Path) = p;
        }
    }
}
