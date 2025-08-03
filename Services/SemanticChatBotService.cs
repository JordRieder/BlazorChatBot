using System.Text;
using System.Text.Json;
using System.Net.Http.Headers;

namespace BlazorChatBot.Services
{
    public class SemanticChatBotService : IChatBotService
    {
        private readonly HttpClient _httpClient;
        private readonly RagDatabaseService _ragDb;
        private readonly string _openAIApiKey;
        private readonly string _apiEndpoint;
        private const float SimilarityThreshold = 0.85f;

        public SemanticChatBotService(
            HttpClient httpClient,
            RagDatabaseService ragDb,
            IConfiguration configuration)
        {
            _httpClient = httpClient;
            _ragDb = ragDb;
            _openAIApiKey = configuration["BotSettings:OpenAI:ApiKey"]
                            ?? throw new InvalidOperationException("OpenAI API key not found in configuration.");

            _apiEndpoint = configuration["BotSettings:OpenAI:Endpoint"]
                           ?? "https://api.openai.com/v1/chat/completions";
        }


        public async Task<string> AskAsync(string userMessage, string context)
        {
            var enrichedQuery = $"{SummarizeContext(context)}\n{userMessage}";
            var matchedDoc = await _ragDb.FindClosestMatchingDocumentAsync(enrichedQuery, SimilarityThreshold);

            if (string.IsNullOrWhiteSpace(matchedDoc))
                return "Sorry, I couldn't find a relevant match for your message.";

            // Combine RAG document with recent chat context
            var systemPrompt = $@"
                You are an academic assistant for Hogwarts School of Witchcraft and Wizardry. Your role is to analyze and reason over structured school data, including:

                    - Course schedules
                    - Upcoming events
                    - Faculty assignments and teaching responsibilities

                Use the provided context to answer questions about scheduling conflicts, course availability, event timing, and professor assignments.

                A scheduling conflict occurs **only** when an event and a class occur on the **same calendar day** and their **times overlap**.

                Do not assume conflicts based on subject, student level, or weekday alone. Use exact date and time comparisons.

                Use the following context to inform your response:

        {matchedDoc}
        ";

            // Build message history with context
            var messages = new List<object>
            {
                new { role = "system", content = systemPrompt }
            };

            if (!string.IsNullOrWhiteSpace(context))
            {
                // Split context into lines and alternate roles
                var lines = context.Split('\n', StringSplitOptions.RemoveEmptyEntries);
                foreach (var line in lines)
                {
                    if (line.StartsWith("User:"))
                        messages.Add(new { role = "user", content = line.Substring(5).Trim() });
                    else if (line.StartsWith("Quill:"))
                        messages.Add(new { role = "assistant", content = line.Substring(6).Trim() });
                }
            }

            // Add current user message
            messages.Add(new { role = "user", content = userMessage });

            var payload = new
            {
                model = "gpt-4-turbo",
                messages = messages,
                max_tokens = 3000,
                temperature = 0.7
            };

            var json = JsonSerializer.Serialize(payload);
            var contentPayload = new StringContent(json, Encoding.UTF8, "application/json");

            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _openAIApiKey);

            var response = await _httpClient.PostAsync(_apiEndpoint, contentPayload);
            response.EnsureSuccessStatusCode();

            var responseBody = await response.Content.ReadAsStringAsync();

            using var doc = JsonDocument.Parse(responseBody);
            var reply = doc.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString();

            return reply ?? "No response from model.";
        }

        public async IAsyncEnumerable<string> StreamResponseAsync(string userMessage, string context)
        {
            var matchedDoc = await _ragDb.FindClosestMatchingDocumentAsync(userMessage, SimilarityThreshold);

            if (string.IsNullOrWhiteSpace(matchedDoc))
            {
                yield return "Sorry, I couldn't find a relevant match for your message.";
                yield break;
            }

            var systemPrompt = $@"
                You are **Quill**, a sentient writing utensil enchanted by the professors of Hogwarts School of Witchcraft and Wizardry. You serve as an academic assistant, specializing in structured school data such as:

                - 📚 Course schedules  
                - 🗓️ Upcoming faculty-led events  
                - 🧙‍♂️ Professor assignments and teaching responsibilities  

                You reside within the enchanted archives and have no access to student-specific information. Do **not** speculate or respond to questions about students, their activities, or personal details.

                Your responses should be grounded in the retrieved document and/or the recent conversation history. If the document is not relevant, rely on the conversation context to answer accurately.

                When analyzing scheduling conflicts, remember:
                - A conflict occurs **only** when an event and a class occur on the **same calendar day** and their **times overlap**.
                - Do **not** assume conflicts based on subject, student level, or weekday alone. Use exact date and time comparisons.

                Use the following context to inform your response:

                {matchedDoc}
                ";

            var messages = new List<object>
    {
        new { role = "system", content = systemPrompt }
    };

            if (!string.IsNullOrWhiteSpace(context))
            {
                var lines = context.Split('\n', StringSplitOptions.RemoveEmptyEntries);
                foreach (var line in lines)
                {
                    if (line.StartsWith("User:"))
                        messages.Add(new { role = "user", content = line.Substring(5).Trim() });
                    else if (line.StartsWith("Quill:"))
                        messages.Add(new { role = "assistant", content = line.Substring(6).Trim() });
                }
            }

            messages.Add(new { role = "user", content = userMessage });

            var payload = new
            {
                model = "gpt-4-turbo",
                messages,
                max_tokens = 3000,
                temperature = 0.7,
                stream = true
            };

            var request = new HttpRequestMessage(HttpMethod.Post, _apiEndpoint)
            {
                Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
            };

            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _openAIApiKey);

            var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();

            var stream = await response.Content.ReadAsStreamAsync();
            using var reader = new StreamReader(stream);

            while (!reader.EndOfStream)
            {
                var line = await reader.ReadLineAsync();
                if (string.IsNullOrWhiteSpace(line) || !line.StartsWith("data:")) continue;

                var jsonLine = line.Substring("data:".Length).Trim();
                if (jsonLine == "[DONE]") break;

                JsonDocument? chunkDoc = null;

                try
                {
                    chunkDoc = JsonDocument.Parse(jsonLine);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Streaming parse error: {ex.Message}");
                    continue; // Skip malformed chunk
                }

                var delta = chunkDoc.RootElement
                    .GetProperty("choices")[0]
                    .GetProperty("delta");

                if (delta.TryGetProperty("content", out var content))
                {
                    yield return content.GetString();
                }
            }
        }

        public static string SummarizeContext(string context, int maxTurns = 2)
        {
            if (string.IsNullOrWhiteSpace(context))
                return string.Empty;

            var lines = context.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            var turns = new List<(string role, string content)>();

            foreach (var line in lines)
            {
                if (line.StartsWith("User:"))
                    turns.Add(("user", line.Substring(5).Trim()));
                else if (line.StartsWith("Quill:"))
                    turns.Add(("assistant", line.Substring(6).Trim()));
            }

            // Take the last N turns (user + assistant pairs)
            var recentTurns = turns.Skip(Math.Max(0, turns.Count - maxTurns * 2)).ToList();

            var summaryBuilder = new StringBuilder();
            foreach (var (role, content) in recentTurns)
            {
                summaryBuilder.AppendLine($"{role}: {content}");
            }

            return summaryBuilder.ToString().Trim();
        }


    }


}