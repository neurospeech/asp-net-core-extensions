using Microsoft.AspNetCore.NodeServices;
using System;
using System.Collections.Generic;
using System.Linq;

namespace NeuroSpeech
{
    public class NodePackageServiceOptions
    {

        /// <summary>
        /// 
        /// </summary>
        public NodeServicesOptions NodeServicesOptions { get; set; }

        /// <summary>
        /// If set, starts the Node.js instance with the specified environment variables.
        /// </summary>
        public IDictionary<string,string> EnvironmentVariables { get; set; }

        /// <summary>
        /// Folder where node packages will be downloaded
        /// </summary>
        public string TempFolder { get; set; } = "d:\\temp\\ns-npm";


        /// <summary>
        /// NPM Registry used to download packages
        /// </summary>
        public string NPMRegistry { get; set; }


        /// <summary>
        /// White list of packages to execute
        /// </summary>
        public string[] PrivatePackages { get; set; }

        /// <summary>
        /// Time to live, after which NodeServer will dispose
        /// </summary>
        public TimeSpan TTL { get; set; } = TimeSpan.FromHours(1);
    }
}
