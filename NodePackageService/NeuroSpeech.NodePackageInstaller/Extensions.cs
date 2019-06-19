using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace NeuroSpeech
{

    /// <summary>
    /// Creates PackagePathSegments from string
    /// </summary>
    public struct PackagePathSegments : IEquatable<PackagePathSegments>
    {
        /// <summary>
        /// Returns only package name including scope
        /// </summary>
        public string Package;

        /// <summary>
        /// Returns only version if any
        /// </summary>
        public string Version;

        /// <summary>
        /// Returns parsed path after package
        /// </summary>
        public string Path;

        /// <summary>
        /// Returns package name and version in {Package}@{Version} format if version is present
        /// </summary>
        public string PackageAndVersion => 
            string.IsNullOrWhiteSpace(Version) ?
                Package :
                $"{Package}@{Version}";

        public PackagePathSegments(string p, string v, string path)
        {
            this.Package = p;
            this.Version = v;
            this.Path = path;
        }

        public static implicit operator PackagePathSegments (string value) {
            return NPSStringExtensions.ParseNPMPath(value);    
        }

        public override bool Equals(object obj)
        {
            return obj is PackagePathSegments && Equals((PackagePathSegments)obj);
        }

        public bool Equals(PackagePathSegments other)
        {
            return Package == other.Package &&
                   Version == other.Version &&
                   Path == other.Path;
        }

        public override int GetHashCode()
        {
            return Package.GetHashCode();
        }

        public static bool operator == (PackagePathSegments a, PackagePathSegments b)
        {
            return a.Package == b.Package && a.Version == b.Version && a.Path == b.Path;
        }

        public static bool operator != (PackagePathSegments a, PackagePathSegments b)
        {
            return !(a.Package == b.Package && a.Version == b.Version && a.Path == b.Path);
        }

        public (string package, string version, string path) Deconstruct
        {
            get {
                return (Package, Version, Path);
            }
        }

    }

    public static class NPSStringExtensions
    {

        public static PackagePathSegments ParseNPMPath(this string input)
        {
            string package = "";
            string version = "";
            string path = "";
            if (input.StartsWith("@"))
            {
                var (scope, packagePath) = input.ExtractTill("/");

                (package, path) = packagePath.ExtractTill("/");
                (package, version) = package.ExtractTill("@");
                package = scope + "/" + package;
            }
            else
            {
                (package, path) = input.ExtractTill("/");
                (package, version) = package.ExtractTill("@");
            }

            if(version.StartsWith("v"))
            {
                version = version.Substring(1);
            }

            return new PackagePathSegments (package, version, path);
        }

    }
}
