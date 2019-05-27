// A Build Group contains information on what solutions to build for which platform,
// and how to do so.

#addin nuget:?package=Cake.FileHelpers&version=3.0.0
using System.Linq;
using System.Collections.Generic;

public class BuildGroup
{
    private string _platformId;
    private string _toolVersion;
    private string _solutionPath;
    private IList<BuildConfig> _builds;

    private class BuildConfig
    {
        private string _platform { get; set; }
        private string _configuration { get; set; }
        private string _toolVersion { get; set; }
        private string _programFilesDir { get; set; }
        public BuildConfig(string platform, string configuration, string toolVersion)
        {
            _programFilesDir = EnvironmentVariable("programfiles(x86)");
            if (string.IsNullOrEmpty(_programFilesDir))
            {
                _programFilesDir = EnvironmentVariable("programfiles");
            }
            _platform = platform;
            _configuration = configuration;
            _toolVersion = toolVersion;
        }

        public void Build(string solutionPath)
        {
            Statics.Context.MSBuild(solutionPath, settings => {
                if (_toolVersion == "VS2019")
                {
                    settings.ToolPath = string.Join(
                        _programFilesDir,
                        @"Microsoft Visual Studio\2019\Community\MSBuild\Current\bin\amd64\MSBuild.exe");
                }
                if (_platform != null)
                {
                    settings.SetConfiguration(_configuration).WithProperty("Platform", _platform);
                }
                else
                {
                    settings.Configuration = _configuration;
                }
            });
        }
    }

    public BuildGroup(string platformId, string toolVersion)
    {
        _platformId = platformId;
        _toolVersion = toolVersion;
        var reader = ConfigFile.CreateReader();
        _builds = new List<BuildConfig>();
        while (reader.Read())
        {
            if (reader.Name == "buildGroup")
            {
                var buildGroup = new XmlDocument();
                var node = buildGroup.ReadNode(reader);
                ApplyBuildGroupNode(node);
            }
        }
    }

    public void ExecuteBuilds()
    {
        Statics.Context.NuGetRestore(_solutionPath);
        foreach (var buildConfig in _builds)
        {
            buildConfig.Build(_solutionPath);
        }
    }

    private void ApplyBuildGroupNode(XmlNode buildGroup)
    {
        if (buildGroup.Attributes.GetNamedItem("platformId").Value != _platformId)
        {
            return;
        }
        _solutionPath = buildGroup.Attributes.GetNamedItem("solutionPath").Value;
        for (int i = 0; i < buildGroup.ChildNodes.Count; ++i)
        {
            var childNode = buildGroup.ChildNodes.Item(i);
            if (childNode.Name == "build")
            {
                var platform = childNode.Attributes.GetNamedItem("platform")?.Value;
                var configuration = childNode.Attributes.GetNamedItem("configuration")?.Value;
                _builds.Add(new BuildConfig(platform, configuration, _toolVersion));
            }
        }
    }
}
