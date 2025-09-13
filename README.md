# Unity AI Chat Integration Tutorial

Complete source code for the tutorial: **[How to Add AI Chat to Unity: ChatGPT, Claude & Gemini Guide](https://www.angry-shark-studio.com/blog/what-are-llms-ai-explained-game-developers)**

Turn your boring NPCs into intelligent characters that actually remember conversations and respond dynamically!

## Quick Start

1. **Clone this repository**
   ```bash
   git clone https://github.com/angrysharkstudio/unity-ai-chat-tutorial.git
   ```

2. **Set up your API key**
    - Copy `api-config-example.json` to `api-config.json`
    - Add your API key (get one from [OpenAI](https://platform.openai.com), [Claude](https://console.anthropic.com), or [Google Gemini](https://aistudio.google.com))
    - Never commit `api-config.json` to Git!

3. **Open in Unity**
    - Unity 2022.3 or later recommended
    - Open the project folder in Unity

4. **Test it out**
    - Open the Demo scene
    - Press Play
    - Click on the NPCs to talk!

## Project Structure

```
Scripts/
└── AI/
    ├── LlmManager.cs        # Handles API communication
    └── SmartNpc.cs          # Makes NPCs intelligent (with built-in UI)

api-config-example.json      # Example configuration file
README.md                    # This file
```

## How to Use

### Basic Setup

1. **Add Llm Manager to your scene**
    - Drag `Llm Manager.prefab` into your scene
    - There should only be one in your entire game

2. **Create a Smart NPC**
    - Add `Smart NPC.prefab` to your scene
    - Or add `SmartNpc.cs` to any GameObject
    - Configure personality in the Inspector

3. **Set up the UI**
    - Add `Dialogue Canvas.prefab` to your scene
    - Link the UI elements to your NPC

4. **Configure your API**
    - Choose your provider (OpenAI, Claude, or Gemini)
    - The system loads settings from `api-config.json`

### Code Example

```csharp
// Make any GameObject a smart NPC
public class MyNPC : SmartNpc
{
    protected override string GetPlayerName()
    {
        // Return actual player name from your game
        return PlayerManager.Instance.PlayerName;
    }
    
    protected override int GetPlayerLevel()
    {
        // Return actual player level
        return PlayerManager.Instance.Level;
    }
}
```


## Configuration

Edit `api-config.json` to configure your AI provider:

```json
{
  "activeProvider": "openai",  // Switch between "openai", "claude", or "gemini"
  "globalSettings": {
    "maxTokens": 150,
    "temperature": 0.7
  },
  "providers": {
    "openai": {
      "apiKey": "your-openai-api-key-here",
      "apiUrl": "https://api.openai.com/v1/chat/completions",
      "model": "gpt-3.5-turbo"
    },
    "claude": {
      "apiKey": "your-claude-api-key-here",
      "apiUrl": "https://api.anthropic.com/v1/messages",
      "model": "claude-3-haiku-20240307",
      "apiVersion": "2023-06-01"
    },
    "gemini": {
      "apiKey": "your-gemini-api-key-here",
      "apiUrl": "https://generativelanguage.googleapis.com/v1/models/{model}:generateContent",
      "model": "gemini-1.5-flash"
    }
  }
}
```

### Configuration Options

**Global Settings:**
- `activeProvider`: Which AI provider to use ("openai", "claude", or "gemini")
- `maxTokens`: Maximum response length (higher = longer responses)
- `temperature`: Creativity level (0 = predictable, 1 = creative)

**Provider Settings:**
- Each provider has its own `apiKey`, `apiUrl`, and `model` settings
- Claude requires apiVersion (for API compatibility)

### Switching Providers

To switch between AI providers:

1. Change `"activeProvider"` to your desired provider ("openai", "claude", or "gemini")
2. Make sure you have the API key set for that provider
3. Restart your Unity game

Example: To switch from OpenAI to Gemini:
```json
"activeProvider": "gemini",  // Changed from "openai"
```

## Common Issues

### "No API key!" error
- Make sure `api-config.json` exists in your project root
- Check that your API key is correct

### "401 Unauthorized"
- Your API key is invalid
- You may need to add credits to your account

### NPCs not responding
- Check the Console window for errors
- Ensure Llm Manager is in your scene
- Verify internet connection

### Response is cut off
- Increase `maxTokens` in `api-config.json`
- Default is 150, try 300 for longer responses

## Learn More

- **Full Tutorial**: [How to Add AI Chat to Unity](https://www.angry-shark-studio.com/blog/what-are-llms-ai-explained-game-developers)
- **Video Tutorial**: [YouTube - Coming Soon]
- **Blog**: [Angry Shark Studio Blog](https://www.angry-shark-studio.com/blog)
- **Contact**: hello@angry-shark-studio.com

## Contributing

Found a bug? Have an improvement? Pull requests are welcome!

1. Fork the repository
2. Create your feature branch
3. Commit your changes
4. Push to the branch
5. Open a Pull Request

## License

This project is licensed under the MIT License - see the LICENSE file for details.

## ⚠️ Important Security Note

**NEVER commit your `api-config.json` file!** It contains your API key. The `.gitignore` is set up to prevent this, but always double-check before committing.

---

Made by [Angry Shark Studio](https://www.angry-shark-studio.com)