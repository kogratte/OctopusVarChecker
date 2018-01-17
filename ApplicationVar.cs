using System.Collections.Generic;

namespace OctopusVarChecker
{
    public enum VarOrigin
    {
        AppVar = 0,
        Lib = 1
    };

    public class ApplicationVar
    {
        public string Name { get; set; }
        public string Value { get; set; }

        public VarOrigin Origin { get; set; }
    }

    public enum VarUsage
    {
        None = 0,
        ConfigFile = 1,
        StepProperty = 2,
        ActionProperty = 4,
        AppSettings = 8
    };

    public class ProcessVar
    {
        public string Name { get; set; }

        public List<string> Context { get; set;  }
        public VarUsage Usage { get; set; }

        public ProcessVar(string name, VarUsage usage, string context = null)
        {
            this.Name = name;
            this.Usage = usage;
            this.Context = new List<string> { context };
        }
    }
}
