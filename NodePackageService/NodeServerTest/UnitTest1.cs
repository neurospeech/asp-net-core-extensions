using System;
using Xunit;
using NeuroSpeech;

namespace NodeServerTest
{
    public class ParseNPMPackageTest
    {
        [Fact]
        public void Parse()
        {
            var (package, version, path) = "root".ParseNPMPath();

            Assert.Equal("", version);
            Assert.Equal("root", package);
            Assert.Equal("", path);

            (package, version, path) = "@root/package".ParseNPMPath();

            Assert.Equal("", version);
            Assert.Equal("@root/package", package);
            Assert.Equal("", path);

        }

        [Fact]
        public void ParseWithVersion()
        {
            var (package, version, path) = "root@1.1".ParseNPMPath();

            Assert.Equal("1.1", version);
            Assert.Equal("root", package);
            Assert.Equal("", path);

            (package, version, path) = "@root/package@1.1".ParseNPMPath();

            Assert.Equal("1.1", version);
            Assert.Equal("@root/package", package);
            Assert.Equal("", path);

        }
    }

    public class ParseNPMPathTest
    {
        [Fact]
        public void WithoutVersion()
        {

            var (package, version, path) = "root/path".ParseNPMPath();

            Assert.Equal("", version);
            Assert.Equal("root", package);
            Assert.Equal("path", path);

            (package, version, path) = "root/path/1/2".ParseNPMPath();

            Assert.Equal("", version);
            Assert.Equal("root", package);
            Assert.Equal("path/1/2", path);
        }

        [Fact]
        public void WithVersion()
        {

            var (package, version, path) = "root@1.1/path".ParseNPMPath();

            Assert.Equal("1.1", version);
            Assert.Equal("root", package);
            Assert.Equal("path", path);

            (package, version, path) = "root@1.1/path/1/2".ParseNPMPath();

            Assert.Equal("1.1", version);
            Assert.Equal("root", package);
            Assert.Equal("path/1/2", path);
        }
    }

    public class ParseScopedNPMPathTest
    {
        [Fact]
        public void WithoutVersion()
        {

            var (package, version, path) = "@root/package/path".ParseNPMPath();

            Assert.Equal("", version);
            Assert.Equal("@root/package", package);
            Assert.Equal("path", path);

            (package, version, path) = "@root/package/path/1/2".ParseNPMPath();

            Assert.Equal("", version);
            Assert.Equal("@root/package", package);
            Assert.Equal("path/1/2", path);
        }

        [Fact]
        public void WithVersion()
        {

            var (package, version, path) = "@root/package@1.1/path".ParseNPMPath();

            Assert.Equal("1.1", version);
            Assert.Equal("@root/package", package);
            Assert.Equal("path", path);

            (package, version, path) = "@root/package@1.1/path/1/2".ParseNPMPath();

            Assert.Equal("1.1", version);
            Assert.Equal("@root/package", package);
            Assert.Equal("path/1/2", path);
        }
    }
}
