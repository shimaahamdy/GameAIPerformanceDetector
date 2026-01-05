namespace GameAi.Api.RAG
{
    public class VectorStore
    {
        private readonly List<RagVector> _vectors = new();

        public void Add(RagVector vector)
        {
            _vectors.Add(vector);
        }

        public IReadOnlyList<RagVector> GetAll()
        {
            return _vectors;
        }

 
        public List<RagVector> Search(float[] queryEmbedding, int topK = 5)
        {
            return _vectors
                .Select(v => new
                {
                    Vector = v,
                    Score = Helpers.VectorMath.CosineSimilarity(queryEmbedding, v.Embedding)
                })
                .OrderByDescending(x => x.Score)
                .Take(topK)
                .Select(x => x.Vector)
                .ToList();
        }
    }
}
