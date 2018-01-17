using Octopus.Client.Model;
using System;
using System.Collections.Generic;
using System.Linq;

public class VariableViewModel
{
    public string VariableSetName { get; set; }
    public string Name { get; set; }
    public string Value { get; set; }
    public string Scope { get; set; }

    public VariableViewModel(VariableResource variable, string variableSetName, Dictionary<String, String> scopeNames)
    {
        Name = variable.Name;
        Value = variable.Value;
        VariableSetName = variableSetName;

        var nonLookupRoles =
            variable.Scope.Where(s => s.Key != ScopeField.Environment & s.Key != ScopeField.Machine & s.Key != ScopeField.Channel & s.Key != ScopeField.Action)
                .ToDictionary(dict => dict.Key, dict => dict.Value);

        foreach (var scope in nonLookupRoles)
        {
            if (string.IsNullOrEmpty(Scope))
                Scope = String.Join(",", scope.Value, Scope);
        }

        var lookupRoles =
            variable.Scope.Where(s => s.Key == ScopeField.Environment || s.Key == ScopeField.Machine || s.Key == ScopeField.Channel || s.Key == ScopeField.Action)
                .ToDictionary(dict => dict.Key, dict => dict.Value);

        foreach (var role in lookupRoles)
        {
            foreach (var scope in role.Value)
            {
                Scope = String.Join(",", scopeNames[scope], Scope);
            }
        }

    }
}