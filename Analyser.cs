using Octopus.Client;
using Octopus.Client.Model;
using OctopusVarChecker.Config;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml;

namespace OctopusVarChecker
{
    public class Analyser
    {
        private const string varPattern = @"#\{([^\}]*)\}";

        private Options Options { get; set; }
        private OctopusRepository OctopusRepository { get; set; }

        public Analyser(Options opts)
        {
            OctopusProjectsSection octopusConfig = System.Configuration.ConfigurationManager.GetSection("OctopusProjects") as OctopusProjectsSection;

            var octopusHost = string.IsNullOrEmpty(opts.OctopusUrl) ? octopusConfig.Host : opts.OctopusUrl;
            var apiKey = string.IsNullOrEmpty(opts.ApiKey) ? octopusConfig.ApiKey : opts.ApiKey;

            this.Options = opts;

            if (string.IsNullOrWhiteSpace(octopusHost))
            {
                throw new ArgumentException("Octopus URL should be provided. See help (--help)");
            }

            if (string.IsNullOrWhiteSpace(apiKey))
            {
                throw new ArgumentException("ApiKey should be provided. See help (--help)");
            }

            var endpoint = new OctopusServerEndpoint(octopusHost, apiKey);
            this.OctopusRepository = new OctopusRepository(endpoint);
        }

        public AnalysisResult Run(OctopusProject project)
        {
                var analysisContext = new AnalysisContext
                {
                    ProjectName = project.Name,
                    ConfigFilePath = project.Path
                };

                this.LoadEnvironments(analysisContext);
                this.AnalyseVariableUsage(analysisContext);
                this.AnalyseDefinedVars(analysisContext);

            return analysisContext.Result;

        }

        private void LoadEnvironments(AnalysisContext context)
        {
            context.Result.Environments = this.OctopusRepository.Environments.FindAll().ToDictionary(x => x.Id, x => x.Name);
        }

        private void AnalyseDefinedVars(AnalysisContext context)
        {
            ProjectResource project = OctopusRepository.Projects.FindByName(context.ProjectName);

            // Les librairies attachées au projet

            List<LibraryVariableSetResource> librarySets = OctopusRepository.LibraryVariableSets.FindMany(lib => project.IncludedLibraryVariableSetIds.Contains(lib.Id));

            foreach (var libraryVariableSetResource in librarySets)
            {
                VariableSetResource variableSet = OctopusRepository.VariableSets.Get(libraryVariableSetResource.VariableSetId);

                variableSet.Variables.ToList().ForEach(v =>
                {
                    foreach (var envId in v.Scope.First().Value)
                    {
                        context.Result.AddDefinedVar(envId, v, VarOrigin.Lib);
                    }
                });
            }

            // Les vars attachées au projet
            OctopusRepository.VariableSets.Get(project.VariableSetId).Variables.ToList().ForEach(v =>
            {
                foreach (var envId in v.Scope.First().Value)
                {
                    context.Result.AddDefinedVar(envId, v, VarOrigin.AppVar);
                }
            });
        }

        private void AnalyseVariableUsage(AnalysisContext context)
        {
            FillProcessVariables(context);
            FillConfigFileVariables(context);
        }

        private void FillProcessVariables(AnalysisContext context)
        {
            ProjectResource project = this.OctopusRepository.Projects.FindByName(context.ProjectName);

            DeploymentProcessResource deployementProcess = OctopusRepository.DeploymentProcesses.Get(project.DeploymentProcessId);
            deployementProcess.Steps.ToList().ForEach(step =>
            {
                step.Actions.ToList().ForEach(action =>
                {
                    if (action.Properties.Any())
                    {
                        action.Properties.Values.Where(p => Regex.IsMatch(p.Value, varPattern)).ToList().ForEach(p =>
                        {
                            foreach (Match match in Regex.Matches(p.Value, varPattern))
                            {
                                context.Result.AddToUsedVar(match.Value, VarUsage.ActionProperty, "on action " + action.Name);
                            }
                        });
                    }
                });

                step.Properties.Values.Where(p => Regex.IsMatch(p.Value, varPattern)).ToList().ForEach(p =>
                {
                    foreach (Match match in Regex.Matches(p.Value, varPattern))
                    {
                        context.Result.AddToUsedVar(match.Value, VarUsage.StepProperty, "on step " + step.Name);
                    }
                });
            });
        }

        private void FillConfigFileVariables(AnalysisContext context)
        {
            var result = new List<ProcessVar>();

            // Variables de config
            var fileContent = File.ReadAllText(context.ConfigFilePath + "/Web.Release.config");

            MatchCollection matches = Regex.Matches(fileContent, varPattern);

            foreach (Match match in matches)
            {
                context.Result.AddToUsedVar(match.Value, VarUsage.ConfigFile, "Web.Release.Config");
            }

            // Aller lire les appSettings
            XmlDocument doc = new XmlDocument();
            doc.Load(context.ConfigFilePath + "/Web.config");
            XmlNode node = doc.DocumentElement.SelectSingleNode("/configuration/appSettings");

            foreach (XmlNode appSettingNode in node.ChildNodes)
            {
                var settingsName = appSettingNode.Attributes["key"]?.InnerText;

                if (!string.IsNullOrEmpty(settingsName))
                {
                    context.Result.AddToUsedVar(settingsName, VarUsage.AppSettings, "AppSettings");
                }
            }
        }
    }
}
