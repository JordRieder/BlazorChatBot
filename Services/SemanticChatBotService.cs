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


        public async Task<string> AskAsync(string userMessage)
        {
            var matchedDoc = await _ragDb.FindClosestMatchingDocumentAsync(userMessage, SimilarityThreshold);

            if (string.IsNullOrWhiteSpace(matchedDoc))
                return "Sorry, I couldn't find a relevant match for your message.";

            var prompt = $@"
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


            var payload = new
            {
                model = "gpt-4-turbo",
                messages = new[]
                {
                    new { role = "system", content = prompt },
                    new { role = "user", content = userMessage }
                },
                max_tokens = 3000,
                temperature = 0.7
            };

            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _openAIApiKey);

            var response = await _httpClient.PostAsync(_apiEndpoint, content);
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
    }
}