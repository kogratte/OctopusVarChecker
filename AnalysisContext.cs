namespace OctopusVarChecker
{
    public class AnalysisContext
    {
        public string ConfigFilePath { get; internal set; }
        public string ProjectName { get; internal set; }

        public AnalysisResult Result { get; set; }

        public AnalysisContext()
        {
            this.Result = new AnalysisResult();
        }
    }
}
