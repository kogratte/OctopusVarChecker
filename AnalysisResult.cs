using Octopus.Client.Model;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace OctopusVarChecker
{
    public class AnalysisResult
    {
        private const string varPattern = @"#\{([^\}]*)\}";
        public List<ProcessVar> UsedVariables { get; set; }
        public Dictionary<string, List<ApplicationVar>> DeclaredVariablesByEnvironment { get; private set; }

        public AnalysisResult()
        {
            this.UsedVariables = new List<ProcessVar>();
            this.DeclaredVariablesByEnvironment = new Dictionary<string, List<ApplicationVar>>();
        }

        public void AddToUsedVar(string matchValue, VarUsage usage, string context = null)
        {
            var varName = Regex.Replace(matchValue, varPattern, "$1");

            if (UsedVariables.Any(v => v.Name.Equals(varName)))
            {
                var variable = UsedVariables.Single(v => v.Name.Equals(varName));
                variable.Usage = variable.Usage & usage;
                variable.Context.Add(context);
            }
            else
            {
                UsedVariables.Add(new ProcessVar(varName, usage, context));
            }
        }

        public void AddDefinedVar(string envId, VariableResource variable, VarOrigin origin)
        {
            if (!DeclaredVariablesByEnvironment.Keys.ToList().Contains(envId))
            {
                DeclaredVariablesByEnvironment[envId] = new List<ApplicationVar>();
            }

            DeclaredVariablesByEnvironment[envId].Add(new ApplicationVar
            {
                Name = variable.Name,
                Value = variable.Value,
                Origin = origin
            });
        }

        public Dictionary<string, string> Environments { get; internal set; }

        /// <summary>
        /// Retourne la liste des variables utilisées et non déclarées.
        /// Les variables propres à Octopus sont ignorées (variables systèmes)
        /// </summary>
        /// <param name="environment">Le nom de l'environnement</param>
        /// <returns>La liste des variables dont la définition manque</returns>
        public List<ProcessVar> GetMissingVariablesForEnvironment(string environment)
        {
            var result = new List<ProcessVar>();

            if (DeclaredVariablesByEnvironment.Keys.Contains(environment))
            {
                result = UsedVariables
                       .Where(uv => !DeclaredVariablesByEnvironment[environment].Any(ev => ev.Name.Equals(uv.Name)) && !uv.Name.StartsWith("Octopus."))
                       .Where(uv => uv.Usage != VarUsage.AppSettings) // Les AppSettings ne sont pas forcément à remplacer.
                       .ToList();
            }
            else
            {
                result = UsedVariables.ToList();
            }

            return result;
        }

        /// <summary>
        /// Retourne la liste des variables inutilisées mais déclarées
        /// Ignore dans la réponse les variables rattachées à des collections, car potentiellement usitées par d'autres projets
        /// </summary>
        /// <param name="environment">Le nom de l'environnement</param>
        /// <returns>La liste des noms de variables inutilisées</returns>
        public List<string> GetUnusedVariabledForEnvironment(string environment)
        {
            if (!DeclaredVariablesByEnvironment.Keys.Contains(environment))
            {
                return new List<string>();
            }
            return DeclaredVariablesByEnvironment[environment]
                       .Where(v => v.Origin.Equals(VarOrigin.AppVar))
                       .Where(v => !UsedVariables.Any(uv => uv.Name.Equals(v.Name)))
                       .Select(v => v.Name)
                       .ToList();
        }
    }
}
