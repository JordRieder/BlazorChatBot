using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Npgsql;
using NpgsqlTypes;
using Pgvector;

namespace BlazorChatBot.Services
{
    public class RagDatabaseService
    {
        private readonly string _connectionString;
        private readonly string _ollamaEndpoint;

        public RagDatabaseService(IConfiguration configuration)
        {
            _connectionString = configuration["ConnectionStrings:RagDb"]
                                ?? throw new InvalidOperationException("Missing connection string: ConnectionStrings:RagDb");

            _ollamaEndpoint = configuration.GetValue<string>("Ollama:Endpoint") ?? "http://localhost:11434";
        }

        public async Task<float[]> CreateEmbeddingAsync(string text, string model = "mxbai-embed-large")
        {
            using var httpClient = new HttpClient();

            var requestBody = new
            {
                model,
                input = text
            };

            var response = await httpClient.PostAsync(
                $"{_ollamaEndpoint}/api/embed",
                new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json"));

            if (!response.IsSuccessStatusCode)
                throw new InvalidOperationException($"Ollama API failed: {response.StatusCode}");

            var responseContent = await response.Content.ReadAsStringAsync();
            Console.WriteLine("Raw response: " + responseContent);

            var embeddingResponse = JsonSerializer.Deserialize<OllamaEmbeddingResponse>(responseContent);

            if (embeddingResponse?.Embeddings == null || embeddingResponse.Embeddings.Length == 0)
                throw new InvalidOperationException("No embedding returned");

            var embeddingVector = embeddingResponse.Embeddings[0]; // first (and only) embedding

            if (embeddingVector.Length != 1024)
                throw new InvalidOperationException($"Expected 1024 dimensions, got {embeddingVector.Length}");

            return embeddingVector.Select(d => (float)d).ToArray();
        }

        public async Task<long> InsertDocumentWithVectorAsync(string documentText,
            string embeddingModel = "mxbai-embed-large")
        {
            if (string.IsNullOrWhiteSpace(documentText))
                throw new ArgumentException("Document text cannot be null or empty", nameof(documentText));

            var embeddingArray = await CreateEmbeddingAsync(documentText, embeddingModel);

            var vector = new Vector(embeddingArray);
            var vectorString = vector.ToString(); // '[0.1, 0.2, ...]'

            await using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();

            await using var cmd = new NpgsqlCommand(
                "INSERT INTO rag_documents (doc, embedding) VALUES (@doc, @embedding::vector) RETURNING id;", conn);
            cmd.Parameters.AddWithValue("doc", documentText);
            cmd.Parameters.AddWithValue("embedding", vectorString);

            var result = await cmd.ExecuteScalarAsync();
            return Convert.ToInt64(result);
        }

        public async Task<bool> DocumentExistsAsync(string documentText)
        {
            await using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();

            await using var cmd = new NpgsqlCommand(
                "SELECT EXISTS (SELECT 1 FROM rag_documents WHERE doc = @doc);", conn);
            cmd.Parameters.AddWithValue("doc", documentText);

            var result = await cmd.ExecuteScalarAsync();
            return Convert.ToBoolean(result);
        }

        public async Task<int> GetDocumentCountAsync()
        {
            await using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();

            await using var cmd = new NpgsqlCommand("SELECT COUNT(*) FROM rag_documents;", conn);
            var result = await cmd.ExecuteScalarAsync();
            return Convert.ToInt32(result);
        }

        public async Task<string?> FindClosestMatchingDocumentAsync(string userMessage,
            float similarityThreshold = 0.85f)
        {
            var embeddingArray = await CreateEmbeddingAsync(userMessage);
            var vector = new Vector(embeddingArray);

            await using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();

            await using var cmd = new NpgsqlCommand(@"
                SELECT doc
                FROM rag_documents
                WHERE embedding <=> @query_vector::vector < @threshold
                ORDER BY embedding <=> @query_vector::vector
                LIMIT 3;", conn);

            cmd.Parameters.AddWithValue("query_vector", vector.ToString()); 
            cmd.Parameters.AddWithValue("threshold", similarityThreshold);

            var result = await cmd.ExecuteScalarAsync();
            return result?.ToString();
        }
    }


    public class OllamaEmbeddingResponse
    {
        [JsonPropertyName("model")]
        public string Model { get; set; } = string.Empty;

        [JsonPropertyName("embeddings")]
        public double[][] Embeddings { get; set; } = Array.Empty<double[]>();
    }
}