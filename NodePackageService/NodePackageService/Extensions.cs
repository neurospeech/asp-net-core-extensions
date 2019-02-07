using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace NeuroSpeech
{
    public static class NPSStringExtensions
    {

        public static (string, string, string) ParseNPMPath(this string input)
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

            return (package, version, path);
        }

    }
}
