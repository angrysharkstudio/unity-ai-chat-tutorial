/*
 * MIT License - Copyright (c) 2025 Angry Shark Studio
 * See LICENSE file for full license text
 */
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

// ReSharper disable NotAccessedField.Local

namespace AngrySharkStudio.AI {
    /// <summary>
    /// Simplified LLM Manager for Unity - handles API communication with OpenAI, Claude, or Gemini
    /// </summary>
    public class LlmManager : MonoBehaviour {

        // Singleton pattern - access from anywhere with LlmManager.Instance
        public static LlmManager Instance { get; private set; }

        [Header("Debug")]
        [SerializeField] private bool showDebugLogs = true;

        // Configuration from the JSON file - no hardcoded API keys!
        private string apiKey = "";
        private AIProvider provider = AIProvider.OpenAI;
        private string model = "gpt-3.5-turbo";

        private APIConfiguration config;
        private ProviderConfig currentProviderConfig;

        private enum AIProvider {

            OpenAI,
            Claude,
            Gemini

        }

        private void Awake() {
            // Set up singleton
            if (Instance == null) {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                LoadConfiguration();
            } else {
                Destroy(gameObject);
            }
        }

        // Load configuration from an external file
        private void LoadConfiguration() {
            try {
                var path = Path.Combine(Application.dataPath, "../api-config.json");

                if (File.Exists(path)) {
                    var json = File.ReadAllText(path);
                    config = JsonUtility.FromJson<APIConfiguration>(json);

                    // Load provider-specific configuration
                    LoadProviderConfig();

                    if (showDebugLogs)
                        Debug.Log("Configuration loaded from api-config.json");
                } else {
                    Debug.LogWarning("WARNING: No api-config.json found. Using Inspector values.");
                    config = new APIConfiguration(); // Use defaults
                    currentProviderConfig = new ProviderConfig(); // Use defaults
                }
            } catch (Exception e) {
                Debug.LogError($"ERROR: Error loading configuration: {e.Message}");
                config = new APIConfiguration(); // Use defaults
                currentProviderConfig = new ProviderConfig(); // Use defaults
            }
        }

        private void LoadProviderConfig() {
            // Determine which provider to use
            var activeProvider = config.activeProvider;

            if (!string.IsNullOrEmpty(activeProvider)) {
                // Update the provider enum based on string
                provider = activeProvider.ToLower() switch {
                    "openai" => AIProvider.OpenAI,
                    "claude" => AIProvider.Claude,
                    "gemini" => AIProvider.Gemini,
                    _ => AIProvider.OpenAI
                };
            }

            // Get the provider-specific configuration
            currentProviderConfig = provider switch {
                AIProvider.OpenAI => config.providers.openai,
                AIProvider.Claude => config.providers.claude,
                AIProvider.Gemini => config.providers.gemini,
                _ => new ProviderConfig()
            };

            // Override inspector values with config
            if (currentProviderConfig != null) {
                if (!string.IsNullOrEmpty(currentProviderConfig.apiKey))
                    apiKey = currentProviderConfig.apiKey;

                if (!string.IsNullOrEmpty(currentProviderConfig.model))
                    model = currentProviderConfig.model;
            }
        }

        /// <summary>
        /// Send a message to the AI and get a response
        /// </summary>
        public async Task<string> GetAIResponse(string message) {
            // Check for API key
            if (string.IsNullOrEmpty(apiKey)) {
                Debug.LogError("ERROR: No API key set! Add it in the Inspector or create api-config.json");

                return "Error: No API key";
            }

            try {
                // Get the appropriate URL
                var url = GetAPIEndpoint();
                var jsonBody = CreateRequestBody(message);

                using var request = new UnityWebRequest(url, "POST");

                // Set up the request
                var bodyRaw = Encoding.UTF8.GetBytes(jsonBody);
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.downloadHandler = new DownloadHandlerBuffer();

                // Headers
                request.SetRequestHeader("Content-Type", "application/json");
                SetAuthHeaders(request);

                // Send request
                var operation = request.SendWebRequest();

                // Wait for response
                while (!operation.isDone)
                    await Task.Yield();

                // Handle response
                if (request.result == UnityWebRequest.Result.Success) {
                    var response = ParseResponse(request.downloadHandler.text);

                    if (showDebugLogs)
                        Debug.Log($"AI Response: {response}");

                    return response;
                } else {
                    Debug.LogError($"ERROR: API Error: {request.error}");
                    Debug.LogError($"Response: {request.downloadHandler.text}");

                    return "Sorry, I couldn't connect to the AI.";
                }
            } catch (Exception e) {
                Debug.LogError($"ERROR: Exception: {e.Message}");

                return "Sorry, something went wrong.";
            }
        }

        private string GetAPIEndpoint() {
            // Get URL from config
            var url = currentProviderConfig?.apiUrl ?? "";

            if (string.IsNullOrEmpty(url)) {
                Debug.LogError("ERROR: No API URL configured for provider!");

                return "";
            }

            // Handle Gemini URL template
            if (provider == AIProvider.Gemini && url.Contains("{model}")) {
                url = url.Replace("{model}", model);
            }

            return url;
        }

        private void SetAuthHeaders(UnityWebRequest request) {
            switch (provider) {
                case AIProvider.OpenAI:
                    request.SetRequestHeader("Authorization", $"Bearer {apiKey}");

                    break;
                case AIProvider.Claude:
                    request.SetRequestHeader("x-api-key", apiKey);

                    if (!string.IsNullOrEmpty(currentProviderConfig?.apiVersion)) {
                        request.SetRequestHeader("anthropic-version", currentProviderConfig.apiVersion);
                    }

                    break;
                case AIProvider.Gemini:
                    request.SetRequestHeader("x-goog-api-key", apiKey);

                    break;
            }
        }

        // Create the JSON for the API request
        private string CreateRequestBody(string message) {
            switch (provider) {
                case AIProvider.OpenAI:
                    return JsonUtility.ToJson(new OpenAIRequest {
                        model = model,
                        messages = new List<OpenAIMessage> {
                            new() { role = "user", content = message }
                        },
                        temperature = config.globalSettings.temperature,
                        max_tokens = config.globalSettings.maxTokens
                    });

                case AIProvider.Claude:
                    return JsonUtility.ToJson(new ClaudeRequest {
                        model = model,
                        messages = new List<ClaudeMessage> {
                            new() { role = "user", content = message }
                        },
                        max_tokens = config.globalSettings.maxTokens
                    });

                case AIProvider.Gemini:
                    return JsonUtility.ToJson(new GeminiRequest {
                        contents = new List<GeminiContent> {
                            new() {
                                parts = new List<GeminiPart> {
                                    new() { text = message }
                                }
                            }
                        },
                        generationConfig = new GeminiGenerationConfig {
                            maxOutputTokens = config.globalSettings.maxTokens,
                            temperature = config.globalSettings.temperature
                        }
                    });

                default:
                    return "";
            }
        }

        // Extract the AI's response from the JSON
        private string ParseResponse(string json) {
            try {
                switch (provider) {
                    case AIProvider.OpenAI:
                        var openAIResponse = JsonUtility.FromJson<OpenAIResponse>(json);

                        if (openAIResponse?.choices != null && openAIResponse.choices.Count > 0)
                            return openAIResponse.choices[0].message.content;

                        break;

                    case AIProvider.Claude:
                        var claudeResponse = JsonUtility.FromJson<ClaudeResponse>(json);

                        if (claudeResponse?.content != null && claudeResponse.content.Count > 0)
                            return claudeResponse.content[0].text;

                        break;

                    case AIProvider.Gemini:
                        var geminiResponse = JsonUtility.FromJson<GeminiResponse>(json);

                        if (geminiResponse?.candidates != null && geminiResponse.candidates.Count > 0)
                            return geminiResponse.candidates[0].content.parts[0].text;

                        break;
                }

                Debug.LogError("Failed to parse response - unexpected format");

                return "Error parsing response.";
            } catch (Exception e) {
                Debug.LogError($"Failed to parse response: {e.Message}");
                Debug.LogError($"Raw response: {json}");

                return "Error parsing response.";
            }
        }


        #region Data Classes

        [Serializable]
        public class APIConfiguration {

            public string activeProvider = "openai";
            public GlobalSettings globalSettings = new();
            public ProvidersConfig providers = new();

        }

        [Serializable]
        public class GlobalSettings {

            public int maxTokens = 150;
            public float temperature = 0.7f;

        }


        [Serializable]
        public class ProvidersConfig {

            public ProviderConfig openai = new();
            public ProviderConfig claude = new();
            public ProviderConfig gemini = new();

        }

        [Serializable]
        public class ProviderConfig {

            public string apiKey = "";
            public string apiUrl = "";
            public string model = "";
            public string apiVersion = ""; // Required for Claude API

        }

        [Serializable]
        private class OpenAIRequest {

            public string model;
            public List<OpenAIMessage> messages;
            public float temperature;
            // ReSharper disable once InconsistentNaming
            public int max_tokens;

        }

        [Serializable]
        private class OpenAIMessage {

            public string role;
            public string content;

        }

        [Serializable]
        private class ClaudeRequest {

            public string model;
            public List<ClaudeMessage> messages;
            // ReSharper disable once InconsistentNaming
            public int max_tokens;

        }

        [Serializable]
        private class ClaudeMessage {

            public string role;
            public string content;

        }

        [Serializable]
        private class GeminiRequest {

            public List<GeminiContent> contents;
            public GeminiGenerationConfig generationConfig;

        }

        [Serializable]
        private class GeminiGenerationConfig {

            public int maxOutputTokens;
            public float temperature;

        }

        [Serializable]
        private class GeminiContent {

            public List<GeminiPart> parts;

        }

        [Serializable]
        private class GeminiPart {

            public string text;

        }

        // Response models for parsing API responses
        [Serializable]
        private class OpenAIResponse {

            public List<OpenAIChoice> choices;

        }

        [Serializable]
        private class OpenAIChoice {

            public OpenAIMessage message;

        }

        [Serializable]
        private class ClaudeResponse {

            public List<ClaudeContent> content;

        }

        [Serializable]
        private class ClaudeContent {

            public string text;

        }

        [Serializable]
        private class GeminiResponse {

            public List<GeminiCandidate> candidates;

        }

        [Serializable]
        private class GeminiCandidate {

            public GeminiContent content;

        }

        #endregion

    }
}