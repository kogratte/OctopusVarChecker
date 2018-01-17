using CommandLine;

namespace OctopusVarChecker
{
    public class Options
    {
        [Option('u', "display-var-usage", Default = false, HelpText = "Display var usage")]
        public bool DisplayVariableUsage { get; set; }


        [Option("octopus-host", Required = false, Default = "", HelpText = "Octopus URL")]
        public string OctopusUrl { get; set; }

        [Option("api-key", Required = false, Default = "", HelpText = "Octopus Api Key")]
        public string ApiKey { get; set; }

        [Option("project-name", Required = false, Default = "", HelpText = "Octopus project name")]
        public string OctopusProjectName { get; set; }

        [Option("config-path", Required = false, Default = "", HelpText = "Path to the web.release.config, without trailing '/'")]
        public string ConfigFilePath { get; set; }

        [Option('p', "paginate-results", Required = false, Default = false, HelpText = "Display results page by page")]

        public bool PaginateResults { get; set; }

        [Option("ignore-dmz", Required = false, Default = false, HelpText = "Ignore _dmz environments")]

        public bool IgnoreDmz { get; set; }
    }
}
