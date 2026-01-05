using GameAi.Api.ReportingAgent.DTOs;

namespace GameAi.Api.ReportingAgent.Services.Contracts
{
    public interface IPdfGenerator
    {
        Task<byte[]> GeneratePdfAsync(string reportText, List<ChartDto> charts);
    }

}
