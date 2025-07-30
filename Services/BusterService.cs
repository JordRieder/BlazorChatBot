using System.Text.Json;
using System.Text;
using System.Runtime.Intrinsics.X86;
using System.Security.Cryptography;
using System;
using BlazorChatBot.Prompts;

namespace BlazorChatBot.Services
{
    public class BusterService : IChatBotService
    {
        private readonly HttpClient _httpClient;
        private readonly PromptBuilder _promptBuilder;
        private const string ApiEndpoint = "http://localhost:4891/v1/chat/completions";

        public BusterService(HttpClient httpClient, PromptBuilder promptBuilder)
        {
            _httpClient = httpClient;
            _promptBuilder = promptBuilder;
        }


        public async Task<string> AskAsync(string userMessage)
        {
            var prompt = _promptBuilder.Build(userMessage);

            var payload = new
            {
                model = "Buster",
                messages = new[]
                {
                    new { role = "system", content = prompt },
                    new { role = "user", content = userMessage }
                },

                max_tokens = 4096,
                temperature = 0.3
            };

            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(ApiEndpoint, content);
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