using GameAi.Api.ReportingAgent.DTOs;
using GameAi.Api.ReportingAgent.Models;
using GameAi.Api.ReportingAgent.Models.ReAct;
using GameAi.Api.ReportingAgent.Services.Contracts;
using System.Text;
using System.Text.Json;
using System.Net.Http;

namespace GameAi.Api.ReportingAgent.Services
{
    /// <summary>
    /// ReAct-style agent that reasons about user queries and takes actions using tools
    /// </summary>
    public class ReActAgentService : IChartsAgentService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IEnumerable<IReActTool> _tools;
        private readonly IPdfGenerator _pdfGenerator;

        public ReActAgentService(
            IHttpClientFactory httpClientFactory,
            IEnumerable<IReActTool> tools,
            IPdfGenerator pdfGenerator)
        {
            _httpClientFactory = httpClientFactory;
            _tools = tools;
            _pdfGenerator = pdfGenerator;
        }

        public async Task<ChartsAgentChatResponse> HandleAsync(string userMessage)
        {
            if (string.IsNullOrWhiteSpace(userMessage))
                throw new ArgumentException("User message cannot be null or empty.", nameof(userMessage));

            var state = new ReActState
            {
                UserQuery = userMessage,
                CollectedData = new AgentDataResult()
            };

            // ReAct Loop: Reason → Act → Observe → Repeat
            while (!state.ShouldGenerateResponse && state.IterationCount < ReActState.MaxIterations)
            {
                state.IterationCount++;

                // Step 1: REASON - AI thinks about what to do next
                var thought = await ReasonAsync(state);
                state.Thoughts.Add(thought);

                if (thought.IsComplete)
                {
                    state.ShouldGenerateResponse = true;
                    break;
                }

                if (!string.IsNullOrEmpty(thought.NextAction))
                {
                    // Step 2: ACT - Execute the action using a tool
                    var action = ParseAction(thought.NextAction!, thought.ActionParameters);
                    state.Actions.Add(action);

                    var observation = await ActAsync(action);
                    state.Observations.Add(observation);

                    // Step 3: OBSERVE - Update state with results
                    UpdateStateWithObservation(state, observation);
                }
            }

            // Step 4: Generate final response
            return await GenerateFinalResponseAsync(state);
        }

        /// <summary>
        /// REASON step: AI analyzes the situation and decides what to do next
        /// </summary>
        private async Task<ReActThought> ReasonAsync(ReActState state)
        {
            var client = _httpClientFactory.CreateClient("OpenAI");

            // Build context from previous thoughts, actions, and observations
            var context = BuildReasoningContext(state);

            // Build tool descriptions for AI to choose from
            var toolsDescription = string.Join("\n", _tools.Select(t => 
                $"- {t.Name}: {t.Description}"));

            var systemMessage = new
            {
                role = "system",
                content = $"""
                You are a ReAct-style AI agent that reasons about user queries and takes actions.
                
                Available tools:
                {toolsDescription}
                
                Your task:
                1. Analyze the user query and current state
                2. Decide if you need more information (call a tool) or if you have enough to answer
                3. If you need more info, specify which tool to call and its parameters
                4. If you have enough info, set isComplete=true
                
                Output JSON with:
                - reasoning: Your thought process
                - nextAction: Tool name to call (or null if complete)
                - actionParameters: Dictionary of parameters for the tool (or null)
                - isComplete: true if ready to generate final response
                """
            };

            var userMessage = new
            {
                role = "user",
                content = $"""
                User Query: {state.UserQuery}
                
                Current State:
                {context}
                
                What should I do next?
                """
            };

            var payload = new
            {
                model = "gpt-3.5-turbo-0125",
                messages = new[] { systemMessage, userMessage },
                temperature = 0.3,
                response_format = new { type = "json_object" }
            };

            var response = await HttpRetryHelper.ExecuteWithRetryAsync(
                client,
                () => new HttpRequestMessage(HttpMethod.Post, "v1/chat/completions")
                {
                    Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
                });

            var raw = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                throw new HttpRequestException($"Reasoning step failed: {raw}");

            var aiContent = ExtractJsonContent(raw);
            var thought = JsonSerializer.Deserialize<ReActThought>(aiContent);

            return thought ?? new ReActThought
            {
                Reasoning = "Failed to parse reasoning response",
                IsComplete = true
            };
        }

        /// <summary>
        /// ACT step: Execute the action using the appropriate tool
        /// </summary>
        private async Task<ReActObservation> ActAsync(ReActAction action)
        {
            var tool = _tools.FirstOrDefault(t => t.Name.Equals(action.ToolName, StringComparison.OrdinalIgnoreCase));

            if (tool == null)
            {
                return new ReActObservation
                {
                    ToolName = action.ToolName,
                    Success = false,
                    ErrorMessage = $"Tool '{action.ToolName}' not found"
                };
            }

            return await tool.ExecuteAsync(action.Parameters);
        }

        /// <summary>
        /// OBSERVE step: Update state with observation results
        /// </summary>
        private void UpdateStateWithObservation(ReActState state, ReActObservation observation)
        {
            if (!observation.Success)
                return;

            // Merge data from tools into collected data
            if (observation.Result is AgentDataResult dataResult)
            {
                // Merge metrics
                foreach (var metric in dataResult.Metrics)
                {
                    state.CollectedData!.Metrics[metric.Key] = metric.Value;
                }
            }
            else if (observation.Result is List<ChartDto> charts)
            {
                // Add charts
                state.CollectedData!.Charts.AddRange(charts);
            }
        }

        /// <summary>
        /// Generate the final response after reasoning and acting
        /// </summary>
        private async Task<ChartsAgentChatResponse> GenerateFinalResponseAsync(ReActState state)
        {
            var client = _httpClientFactory.CreateClient("OpenAI");

            // Build summary of what the agent did
            var agentSummary = BuildAgentSummary(state);

            // Format collected data
            var metricsText = state.CollectedData!.Metrics.Any()
                ? string.Join("\n", state.CollectedData.Metrics.Select(kv => $"{kv.Key}: {kv.Value}"))
                : "No metrics available.";

            var chartsText = state.CollectedData.Charts.Any()
                ? string.Join("\n\n", state.CollectedData.Charts.Select(c =>
                    $"- {c.Title} ({c.Type} chart):\n  Labels: {string.Join(", ", c.Labels)}\n  Values: {string.Join(", ", c.Values)}"))
                : "No charts available.";

            var systemMessage = new
            {
                role = "system",
                content = """
                You are a game analytics assistant.
                Generate a clear, concise report based on the data provided.
                Reference specific numbers and insights from the data.
                Do NOT make up numbers - only use what's provided.
                """
            };

            var userMessage = new
            {
                role = "user",
                content = $"""
                User Query: {state.UserQuery}
                
                Agent Process:
                {agentSummary}
                
                Collected Data:
                Metrics:
                {metricsText}
                
                Charts:
                {chartsText}
                
                Generate a comprehensive response to the user's query.
                """
            };

            var payload = new
            {
                model = "gpt-3.5-turbo-0125",
                messages = new[] { systemMessage, userMessage },
                temperature = 0.3
            };

            var response = await HttpRetryHelper.ExecuteWithRetryAsync(
                client,
                () => new HttpRequestMessage(HttpMethod.Post, "v1/chat/completions")
                {
                    Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
                });

            var raw = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                throw new HttpRequestException($"Response generation failed: {raw}");

            var aiText = ExtractTextContent(raw);

            // Determine if PDF is needed
            var needsPdf = state.UserQuery.Contains("pdf", StringComparison.OrdinalIgnoreCase)
                       || state.UserQuery.Contains("report", StringComparison.OrdinalIgnoreCase)
                       || state.UserQuery.Contains("export", StringComparison.OrdinalIgnoreCase);

            ReportDto? pdfReport = null;
            if (needsPdf)
            {
                var pdfBytes = await _pdfGenerator.GeneratePdfAsync(aiText, state.CollectedData.Charts);
                pdfReport = new ReportDto
                {
                    FileName = $"GameReport_{DateTime.UtcNow:yyyyMMdd_HHmmss}.pdf",
                    FileContent = pdfBytes
                };
            }

            return new ChartsAgentChatResponse
            {
                Text = aiText,
                Charts = state.CollectedData.Charts,
                Report = pdfReport
            };
        }

        private string BuildReasoningContext(ReActState state)
        {
            var context = new StringBuilder();

            if (state.Thoughts.Any())
            {
                context.AppendLine("Previous Thoughts:");
                foreach (var thought in state.Thoughts)
                {
                    context.AppendLine($"- {thought.Reasoning}");
                }
            }

            if (state.Actions.Any() && state.Observations.Any())
            {
                context.AppendLine("\nPrevious Actions & Results:");
                for (int i = 0; i < Math.Min(state.Actions.Count, state.Observations.Count); i++)
                {
                    var action = state.Actions[i];
                    var observation = state.Observations[i];
                    context.AppendLine($"- Action: {action.ToolName} → Success: {observation.Success}");
                    if (!observation.Success)
                        context.AppendLine($"  Error: {observation.ErrorMessage}");
                }
            }

            if (state.CollectedData!.Metrics.Any())
            {
                context.AppendLine("\nCollected Metrics:");
                foreach (var metric in state.CollectedData.Metrics)
                {
                    context.AppendLine($"- {metric.Key}: {metric.Value}");
                }
            }

            if (state.CollectedData.Charts.Any())
            {
                context.AppendLine($"\nCollected Charts: {state.CollectedData.Charts.Count} charts available");
            }

            return context.ToString();
        }

        private string BuildAgentSummary(ReActState state)
        {
            var summary = new StringBuilder();
            summary.AppendLine($"Agent executed {state.IterationCount} reasoning cycles.");
            
            if (state.Actions.Any())
            {
                summary.AppendLine($"Actions taken: {string.Join(", ", state.Actions.Select(a => a.ToolName))}");
            }

            return summary.ToString();
        }

        private ReActAction ParseAction(string actionName, Dictionary<string, object>? parameters)
        {
            return new ReActAction
            {
                ToolName = actionName,
                Parameters = parameters ?? new Dictionary<string, object>()
            };
        }

        private static string ExtractJsonContent(string raw)
        {
            try
            {
                using var doc = JsonDocument.Parse(raw);
                var content = doc.RootElement
                    .GetProperty("choices")[0]
                    .GetProperty("message")
                    .GetProperty("content")
                    .GetString() ?? "{}";

                // Extract JSON from content
                var firstBrace = content.IndexOf('{');
                var lastBrace = content.LastIndexOf('}');
                if (firstBrace >= 0 && lastBrace > firstBrace)
                    content = content.Substring(firstBrace, lastBrace - firstBrace + 1);

                return content;
            }
            catch
            {
                return "{}";
            }
        }

        private static string ExtractTextContent(string raw)
        {
            try
            {
                using var doc = JsonDocument.Parse(raw);
                return doc.RootElement
                    .GetProperty("choices")[0]
                    .GetProperty("message")
                    .GetProperty("content")
                    .GetString() ?? "AI returned empty response.";
            }
            catch
            {
                return "AI output could not be parsed.";
            }
        }
    }
}

