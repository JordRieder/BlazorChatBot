# Blazor ChatBot with Gemma

A lightweight, floating chatbot component for Blazor applications powered by Google's Gemma model through Ollama. Features a clean popup interface that can be embedded in any Blazor page.

## Features

- **Floating Chat Interface** - Unobtrusive popup that stays anchored to the bottom-right corner
- **Gemma AI Integration** - Powered by Google's Gemma 3 model via Ollama
- **Real-time Responses** - Instant AI responses with typing indicators
- **Responsive Design** - Works seamlessly on desktop and mobile devices
- **Easy Integration** - Drop-in component for any Blazor application
- **Local AI Processing** - No external API dependencies, runs entirely on your machine

## Prerequisites

Before running this project, ensure you have the following installed:

- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) or later
- [Visual Studio 2022](https://visualstudio.microsoft.com/) or [Visual Studio Code](https://code.visualstudio.com/)
- [Ollama](https://ollama.ai/) - For running the Gemma model locally

## Setting Up Ollama and Gemma

### 1. Install Ollama

**Windows:**
- Download Ollama from [https://ollama.ai/](https://ollama.ai/)
- Run the installer and follow the setup instructions

**macOS:**
```bash
brew install ollama
```

**Linux:**
```bash
curl -fsSL https://ollama.ai/install.sh | sh
```

### 2. Install the Gemma 3 Model

Once Ollama is installed, pull the Gemma 3 model:

```bash
ollama pull gemma3
```

This will download the Gemma 3 model (approximately 2-4GB depending on the variant). The download may take several minutes depending on your internet connection.

### 3. Verify Installation

Test that Gemma is working correctly:

```bash
ollama run gemma3
```

You should see a prompt where you can interact with the model. Type a test message and verify you get a response. Press `Ctrl+D` or type `/bye` to exit.

## Getting Started

### 1. Clone the Repository

```bash
git clone https://github.com/JordRieder/BlazorChatBot.git
cd blazor-chatbot
```

### 2. Restore Dependencies

```bash
dotnet restore
```

### 3. Run the Application

```bash
dotnet run
```

Navigate to `https://localhost:7001` or `http://localhost:5000` to access the application.

### 4. Test the Chatbot

1. Look for the chat icon in the bottom-right corner of the page
2. Click the chat icon to open the popup
3. Type a message and click "Send"
4. The bot will respond using the Gemma model

## Project Structure

```
BlazorChatBot/
├── Components/
│   └── ChatPopup.razor      # Main chat component
├── Services/
│   ├── IChatBotService.cs   # Chat service interface
│   └── GemmaService.cs      # Gemma AI service implementation
├── Pages/
│   └── Home.razor           # Demo page
├── wwwroot/
│   ├── css/site.css         # Styling
│   └── imgs/chat.png        # Chat icon
└── Program.cs               # Application entry point
```

## Usage

### Adding the Chat Component to Your Pages

To add the chatbot to any Blazor page, simply include the component:

```razor
@using BlazorChatBot.Components
<ChatPopup />
```

### Customizing the Chat Interface

The chat popup includes:
- **Toggle Button**: Click the chat icon to open/close the popup
- **Chat Header**: Displays "ChatBot" and can be clicked to minimize
- **Message History**: Shows conversation between user and bot
- **Typing Indicator**: Shows "Bot is typing..." while processing
- **Input Field**: Type messages and click "Send" or press Enter

### Available Models

You can easily switch to different Ollama models by modifying the `GemmaService.cs` file:

```csharp
Arguments = "run llama2"  // For Llama 2
Arguments = "run codellama"  // For Code Llama
Arguments = "run mistral"  // For Mistral
```

See the full list of available models at [https://ollama.ai/library](https://ollama.ai/library)

## Configuration

### Service Registration

The chat service is registered in `Program.cs`:

```csharp
builder.Services.AddScoped<IChatBotService, GemmaService>();
```

### Styling

The chat interface styling is defined in `wwwroot/css/site.css`. Key classes include:
- `.chat-container` - Main container for the floating chat
- `.chat-toggle-button` - The chat icon button
- `.chat-popup` - The popup window
- `.chat-message` - Individual message styling

## Troubleshooting

### Common Issues

**"Ollama command not found"**
- Ensure Ollama is properly installed and added to your system PATH
- Restart your terminal/command prompt after installation
- On Windows, you may need to restart your IDE

**"Model not found" or "gemma3 not available"**
- Run `ollama pull gemma3` to download the model
- Verify the model is available with `ollama list`
- Check that you have sufficient disk space (2-4GB required)

**Chat not responding**
- Ensure Ollama service is running in the background
- Check that the Gemma model is properly installed
- Look at the browser console for any JavaScript errors
- Verify the GemmaService is properly registered in Program.cs

**Performance Issues**
- Gemma requires significant computational resources
- Consider using a smaller model like `gemma:2b` for faster responses
- Ensure your system meets the minimum requirements for running local AI models

### Checking Ollama Status

To verify Ollama is running correctly:

```bash
# List installed models
ollama list

# Check if Ollama service is running
ollama ps

# Test a model directly
ollama run gemma3 "Hello, how are you?"
```

## Development

### Adding New AI Services

To add support for different AI providers:

1. Create a new service class implementing `IChatBotService`
2. Register the service in `Program.cs`
3. Update the service injection in components as needed

Example for OpenAI integration:
```csharp
public class OpenAIService : IChatBotService
{
    public async Task<string> AskAsync(string prompt)
    {
        // OpenAI API implementation
    }
}
```

### Customizing the UI

The chat component can be easily customized:
- Modify `ChatPopup.razor` for layout changes
- Update `site.css` for styling modifications
- Replace `chat.png` with your own chat icon

## System Requirements

**Minimum Requirements:**
- 8GB RAM (16GB recommended)
- 4GB free disk space
- Modern CPU with decent processing power

**Recommended for Best Performance:**
- 16GB+ RAM
- SSD storage
- GPU acceleration (if supported by Ollama)

## Security Considerations

- **Local Processing**: All AI processing happens locally, ensuring data privacy
- **No External APIs**: No data is sent to external services
- **Input Sanitization**: User inputs are processed safely through Ollama

## Contributing

We welcome contributions! To contribute:

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Support

For support and questions:

- Create an [issue](https://github.com/yourusername/blazor-chatbot/issues) on GitHub
- Check [Ollama documentation](https://ollama.ai/docs) for model-related issues
- Review [Blazor documentation](https://docs.microsoft.com/en-us/aspnet/core/blazor/) for framework questions

## Acknowledgments

- [Blazor](https://blazor.net/) - The web framework powering this application
- [Ollama](https://ollama.ai/) - Local AI model runtime
- [Google Gemma](https://blog.google/technology/developers/gemma-open-models/) - The AI model providing chat responses

---

**Built with ❤️ using Blazor and powered by local AI**
