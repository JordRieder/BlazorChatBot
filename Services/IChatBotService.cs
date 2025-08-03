namespace BlazorChatBot.Services
{
    public interface IChatBotService
    { 
        public Task<string> AskAsync(string prompt, string context);
        IAsyncEnumerable<string> StreamResponseAsync(string userMessage, string context);

    }

}
