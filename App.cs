using CommandLine;
using OctopusVarChecker.Config;
using System;
using System.Collections.Generic;
using System.Linq;
namespace OctopusVarChecker
{
    class Program
    {

        static void Main(string[] args)
        {
            Console.Clear();

            CommandLine.Parser.Default.ParseArguments<Options>(args)
                .WithParsed(opts =>
                {
                    IEnumerable<OctopusProject> projects = new List<OctopusProject>();

                    if (!string.IsNullOrWhiteSpace(opts.OctopusProjectName))
                    {
                        if (string.IsNullOrEmpty(opts.ConfigFilePath))
                        {
                            throw new ArgumentException("--octopus-project-name should be used in combination with --config-file-path");
                        }

                        projects = new List<OctopusProject>
                        {
                            new OctopusProject
                            {
                                Name = opts.OctopusProjectName,
                                Path = opts.ConfigFilePath
                            }
                        };
                    }
                    else
                    {
                        OctopusProjectsSection octopusConfig = System.Configuration.ConfigurationManager.GetSection("OctopusProjects") as OctopusProjectsSection;

                        projects = octopusConfig.Projects;
                    }

                    if (!projects.Any())
                    {
                        Console.WriteLine("No project to analyse. Please add at least one");
                        return;
                    }

                    var analyser = new Analyser(opts);

                    try
                    {
                        projects.ToList().ForEach(project =>
                        {
                            AnalysisResult result = analyser.Run(project);
                            Display(result, opts, project);

                            if (opts.PaginateResults && !projects.Last().Equals(project))
                            {
                                Console.WriteLine("Press enter to display next page...");
                                Console.ReadLine();
                                Console.Clear();
                            }
                        });
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("An error occured");
                    }
                });
        }

        private static void Display(AnalysisResult result, Options opts, OctopusProject project)
        {

            int tableWidth = 140;
            List<string> environments = result.Environments.Keys.ToList();

            Console.WriteLine("Config file : " + project.Path + "/Web.Release.config");
            var tableSeparator = new String('-', tableWidth).ToString();

            environments.ForEach(environment =>
            {
                if (result.Environments[environment].EndsWith("-DMZ") && opts.IgnoreDmz)
                {
                    return;
                }

                Console.WriteLine(tableSeparator);
                DisplayCentered(project.Name + " - Environment : " + result.Environments[environment], tableWidth);
                Console.WriteLine(tableSeparator);
                WriteTableLine(new List<string> { "Missing variables", "Unused variables" }, tableWidth, true);
                Console.WriteLine(tableSeparator);

                var missingVars = result.GetMissingVariablesForEnvironment(environment);
                var unusedVars = result.GetUnusedVariabledForEnvironment(environment);

                var linesCount = Math.Max(missingVars.Count, unusedVars.Count);

                for (var i = 0; i < linesCount; i++)
                {
                    string missingVarText = string.Empty;
                    string unusedVarText = string.Empty;


                    if (i < missingVars.Count)
                    {
                        missingVarText = missingVars[i].Name;
                    }


                    if (i < unusedVars.Count)
                    {
                        unusedVarText = unusedVars[i];
                    }

                    WriteTableLine(new List<string> { missingVarText, unusedVarText }, tableWidth);

                    if (i < missingVars.Count && missingVars[i].Context.Any() && opts.DisplayVariableUsage)
                    {
                        WriteTableLine(new List<string> { "  Usage : ", string.Empty }, tableWidth);

                        missingVars[i].Context.ForEach(c =>
                        {
                            WriteTableLine(new List<string> { "   - " + c,  string.Empty }, tableWidth);
                        });

                        WriteTableLine(new List<string> { string.Empty, string.Empty }, tableWidth);
                    }
                }
                Console.WriteLine(tableSeparator);
                Console.WriteLine("");

                if (opts.PaginateResults && !environments.Last().Equals(environment))
                {
                    Console.WriteLine("Press enter to display next page...");
                    Console.ReadLine();
                    Console.Clear();
                }
            });
        }

        /// <summary>
        /// <------------------- 50 char ---------------->
        ///                     <--> 4 char
        /// |-------------------Toto---------------------|
        /// </summary>
        /// <param name="text"></param>
        /// <param name="tableWidth"></param>
        private static void DisplayCentered(string text, int tableWidth)
        {
            var separator = "|";
            var content = text.PadLeft(tableWidth / 2 - 1 + text.Length / 2);
            content = content.PadRight(tableWidth - 2);
            Console.WriteLine(separator + content + separator);
        }

        private static void WriteTableLine(List<string> columnsData, int tableWidth, bool center = false)
        {
            var separator = "|";

            var columnWidth = tableWidth / columnsData.Count;
            List<string> columnContent = new List<string>();
            columnsData.ForEach(d =>
            {
                string columnText = d;

                if (center)
                {
                    columnText = columnText.PadLeft(columnWidth / 2 + d.Length / 2 - 1);
                }
                else
                {
                    columnText = columnText.PadLeft(d.Length + 1);
                }

                columnText = columnText.PadRight(columnWidth - 2);
                columnContent.Add(separator + columnText + separator);
            });

            Console.WriteLine(columnContent.Aggregate((i, j) => i + j));
        }
    }
}
