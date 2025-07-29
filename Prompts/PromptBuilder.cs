using System.Text.Json;

namespace BlazorChatBot.Prompts
{
    
    public class PromptBuilder
    {
        private readonly string _templatesPath;
        private readonly List<PromptRule> _rules;
        private readonly string _basePrompt;

        public PromptBuilder(string promptsDirectory)
        {
            _templatesPath = Path.Combine(promptsDirectory, "PromptTemplates");
            var rulesPath = Path.Combine(promptsDirectory, "PromptRules.json");
            _rules = JsonSerializer.Deserialize<List<PromptRule>>(File.ReadAllText(rulesPath)) ?? new();
            _basePrompt = File.ReadAllText(Path.Combine(_templatesPath, "BasePrompt.txt"));
        }

        public string Build(string userMessage)
        {
            string prompt = _basePrompt;

            foreach (var rule in _rules)
            {
                if (rule.Keywords.Any(k => userMessage.Contains(k, StringComparison.OrdinalIgnoreCase)))
                {
                    var text = File.ReadAllText(Path.Combine(_templatesPath, rule.File));
                    if (rule.Type.Equals("override", StringComparison.OrdinalIgnoreCase))
                        return text;
                    prompt += " " + text;
                }
            }

            return prompt;
        }
    }

    public class PromptRule
    {
        public string[] Keywords { get; set; } = Array.Empty<string>();
        public string File { get; set; } = string.Empty;
        public string Type { get; set; } = "append"; // or "override"
    }



}
