using System.Diagnostics;
using System.Text;
using BlazorChatBot.Services;

public class GemmaService : IChatBotService
{
    public async Task<string> AskAsync(string prompt, string context)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "ollama",
                Arguments = "run gemma3",
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                StandardOutputEncoding = Encoding.UTF8
            };

            using var process = new Process { StartInfo = psi };
            process.Start();

            await process.StandardInput.WriteLineAsync(prompt);
            process.StandardInput.Close();

            string output = await process.StandardOutput.ReadToEndAsync();
            return output.Trim();
        }
        catch (Exception ex)
        {
            return
                $"⚠️ Unable to get response from Gemma. Please ensure Ollama and the 'gemma3' model are installed correctly.\nError: {ex.Message}";
        }
    }

    public IAsyncEnumerable<string> StreamResponseAsync(string userMessage, string context)
    {
        throw new NotImplementedException();
    }
}