namespace BlazorChatBot.Services
{
    public interface IChatBotService
    { 
        Task<string> AskAsync(string userMessage);
    }

}
