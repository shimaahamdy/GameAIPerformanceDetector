namespace GameAi.Api.RAG.Helpers
{
    public static class VectorMath
    {
        public static float CosineSimilarity(float[] a, float[] b)
        {
            float dot = 0f;
            float magA = 0f;
            float magB = 0f;

            for (int i = 0; i < a.Length; i++)
            {
                dot += a[i] * b[i];
                magA += a[i] * a[i];
                magB += b[i] * b[i];
            }

            return dot / ((float)Math.Sqrt(magA) * (float)Math.Sqrt(magB));
        }
    }

}
