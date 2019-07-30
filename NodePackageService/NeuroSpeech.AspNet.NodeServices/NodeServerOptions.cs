using NeuroSpeech.AspNet.NodeServices;
using System;
using System.Collections.Generic;
using System.Linq;

namespace NeuroSpeech
{
    public class NodePackageServiceOptions: PackageInstallerOptions
    {

        /// <summary>
        /// 
        /// </summary>
        public NodeServicesOptions NodeServicesOptions { get; set; }

    }
}
