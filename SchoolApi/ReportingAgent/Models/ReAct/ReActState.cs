using GameAi.Api.ReportingAgent.DTOs;

namespace GameAi.Api.ReportingAgent.Models.ReAct
{
    /// <summary>
    /// Represents the complete state of a ReAct agent execution
    /// </summary>
    public class ReActState
    {
        public string UserQuery { get; set; } = "";
        public List<ReActThought> Thoughts { get; set; } = new();
        public List<ReActAction> Actions { get; set; } = new();
        public List<ReActObservation> Observations { get; set; } = new();
        public AgentDataResult? CollectedData { get; set; }
        public bool ShouldGenerateResponse { get; set; }
        public bool ShouldGeneratePdf { get; set; }
        public int IterationCount { get; set; }
        public const int MaxIterations = 5; // Prevent infinite loops
    }
}

