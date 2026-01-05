namespace GameAi.Api.ReportingAgent.Models.ReAct
{
    /// <summary>
    /// Represents an action the agent wants to take (calling a tool)
    /// </summary>
    public class ReActAction
    {
        public string ToolName { get; set; } = "";
        public Dictionary<string, object> Parameters { get; set; } = new();
    }
}

