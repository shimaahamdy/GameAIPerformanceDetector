using GameAi.Api.ReportingAgent.Models.ReAct;

namespace GameAi.Api.ReportingAgent.Services.Contracts
{
    /// <summary>
    /// Interface for tools that the ReAct agent can call
    /// </summary>
    public interface IReActTool
    {
        /// <summary>
        /// Name of the tool (used by AI to identify which tool to call)
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Description of what the tool does (used in AI prompts)
        /// </summary>
        string Description { get; }

        /// <summary>
        /// Execute the tool with given parameters
        /// </summary>
        Task<ReActObservation> ExecuteAsync(Dictionary<string, object> parameters);
    }
}

