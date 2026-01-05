using System.Text.Json.Serialization;

namespace GameAi.Api.ReportingAgent.Models.ReAct
{
    /// <summary>
    /// Represents a reasoning step where the agent thinks about what to do next
    /// </summary>
    public class ReActThought
    {
        [JsonPropertyName("reasoning")]
        public string Reasoning { get; set; } = "";

        [JsonPropertyName("nextAction")]
        public string? NextAction { get; set; }

        [JsonPropertyName("actionParameters")]
        public Dictionary<string, object>? ActionParameters { get; set; }

        [JsonPropertyName("isComplete")]
        public bool IsComplete { get; set; }
    }
}

