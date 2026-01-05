namespace GameAi.Api.ReportingAgent.Models.ReAct
{
    /// <summary>
    /// Represents the result/observation after executing an action
    /// </summary>
    public class ReActObservation
    {
        public string ToolName { get; set; } = "";
        public object? Result { get; set; }
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
    }
}

