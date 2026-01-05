public class VectorEntry
{
    public string Id { get; set; } = null!;
    public string Text { get; set; } = null!;
    public float[] Vector { get; set; } = null!;
}

public class InMemoryVectorStore
{
    private readonly List<VectorEntry> _store = new();

    public void Add(VectorEntry entry) => _store.Add(entry);

    public List<VectorEntry> Search(float[] queryVector, int topK = 3)
    {
        return _store
            .Select(e => new { Entry = e, Score = CosineSimilarity(e.Vector, queryVector) })
            .OrderByDescending(x => x.Score)
            .Take(topK)
            .Select(x => x.Entry)
            .ToList();
    }

    private static float CosineSimilarity(float[] a, float[] b)
    {
        float dot = 0, magA = 0, magB = 0;
        for (int i = 0; i < a.Length; i++)
        {
            dot += a[i] * b[i];
            magA += a[i] * a[i];
            magB += b[i] * b[i];
        }
        return dot / ((float)Math.Sqrt(magA) * (float)Math.Sqrt(magB) + 1e-8f);
    }
}
