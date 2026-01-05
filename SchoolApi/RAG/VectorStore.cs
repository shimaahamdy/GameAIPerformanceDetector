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
    }

}
