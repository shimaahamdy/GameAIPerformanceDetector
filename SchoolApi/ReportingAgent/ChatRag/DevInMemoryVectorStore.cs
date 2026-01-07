using GameAi.Api.RAG.Helpers;

namespace GameAi.Api.ReportingAgent.ChatRag
{
    public class DevInMemoryVectorStore
    {
        private readonly List<DevVectorEntry> _vectors = new();

        public void Add(DevVectorEntry entry) => _vectors.Add(entry);

        public IReadOnlyList<DevVectorEntry> GetAll() => _vectors;

        public List<DevVectorEntry> Search(float[] queryEmbedding, int topK = 5)
        {
            return _vectors
                .Select(v => new { Vector = v, Score = VectorMath.CosineSimilarity(queryEmbedding, v.Embedding) })
                .OrderByDescending(x => x.Score)
                .Take(topK)
                .Select(x => x.Vector)
                .ToList();
        }
    }


}

