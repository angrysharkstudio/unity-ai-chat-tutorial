/*
 * MIT License - Copyright (c) 2025 Angry Shark Studio
 * See LICENSE file for full license text
 */

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;

namespace AngrySharkStudio.AI {
    /// <summary>
    /// Smart NPC that uses LLM for dynamic dialogue
    /// </summary>
    public class SmartNpc : MonoBehaviour {

        [Header("NPC Configuration")]
        [Tooltip("What's this character's name?")]
        [SerializeField] private string npcName = "Bob the Merchant";

        [TextArea(3, 5)]
        [Tooltip("Describe their personality in a few sentences")]
        [SerializeField] private string personality =
            "A friendly merchant who loves to tell stories about his adventuring days. " +
            "Always tries to get the best deal but has a soft spot for new adventurers.";

        [Header("Memory Settings")]
        [Tooltip("How many conversations to remember")]
        [SerializeField] private int memorySize = 5;

        [Header("UI References")]
        [Tooltip("Drag your dialogue text here")]
        [SerializeField] private TextMeshProUGUI dialogueText;

        [Tooltip("Drag your dialogue panel/bubble here")]
        [SerializeField] private GameObject dialoguePanel;

        [Tooltip("Optional speaker name text")]
        [SerializeField] private TextMeshProUGUI speakerNameText;

        [Header("Interaction Settings")]
        [Tooltip("How long to show dialogue (seconds)")]
        [SerializeField] private float dialogueDisplayTime = 5f;

        [Tooltip("Show thinking animation?")]
        [SerializeField] private bool showThinkingText = true;

        [Tooltip("Can the player interact while NPC is talking?")]
        [SerializeField] private bool allowInterruptDialogue;

        // Debug settings
        [Header("Debug")]
        [SerializeField] private bool showDebugLogs;

        // Private fields
        private readonly List<string> conversationMemory = new();
        private bool currentlyTalking;
        private LlmManager llmManager;

        private void Start() {
            // Cache reference to LLM Manager
            llmManager = LlmManager.Instance;

            if (llmManager == null) {
                Debug.LogError($"[{npcName}] No LlmManager found! Make sure to add one to your scene.");
            }

            // Set the speaker name if we have the UI element
            if (speakerNameText != null) {
                speakerNameText.text = npcName;
            }

            // Hide dialogue at the start
            if (dialoguePanel != null) {
                dialoguePanel.SetActive(false);
            }
        }

        /// <summary>
        /// Call this when the player interacts with the NPC
        /// </summary>
        private void OnPlayerInteract() {
            // Don't interrupt if already talking (unless allowed)
            if (currentlyTalking && !allowInterruptDialogue) {
                if (showDebugLogs)
                    Debug.Log($"[{npcName}] Already talking, interaction ignored.");

                return;
            }

            // Start the conversation
            _ = HaveConversation();
        }

        /// <summary>
        /// Main conversation logic
        /// </summary>
        private async Task HaveConversation() {
            currentlyTalking = true;

            // Show the dialogue UI
            ShowDialogueUI();

            // Show thinking message
            if (showThinkingText && dialogueText != null) {
                dialogueText.text = $"{npcName} is thinking...";
            }

            // Build the prompt for the AI
            var aiPrompt = CreatePrompt();

            // Get a response from AI
            var response = await GetAIResponse(aiPrompt);

            // Remember what was said
            RememberConversation(response);

            // Display the response
            if (dialogueText != null) {
                dialogueText.text = response;
            }

            // Wait before hiding
            await Task.Delay((int)(dialogueDisplayTime * 1000));

            // Hide dialogue
            HideDialogueUI();

            currentlyTalking = false;
        }

        /// <summary>
        /// Get response from LLM with error handling
        /// </summary>
        private async Task<string> GetAIResponse(string prompt) {
            if (llmManager == null) {
                return GetFallbackResponse();
            }

            try {
                var response = await llmManager.GetAIResponse(prompt);

                return response;
            } catch (Exception e) {
                Debug.LogError($"[{npcName}] AI Error: {e.Message}");

                return GetFallbackResponse();
            }
        }

        /// <summary>
        /// Create the prompt that tells the AI how to behave
        /// </summary>
        private string CreatePrompt() {
            // Get player information (in a real game, get this from your player manager)
            var playerName = GetPlayerName();
            var playerLevel = GetPlayerLevel();

            // Build memory context
            var memoryContext = "";

            if (conversationMemory.Count > 0) {
                memoryContext = "Previous conversations:\n" +
                                string.Join("\n", conversationMemory) + "\n\n";
            }

            // Create the full prompt
            return $@"You are {npcName}. {personality}

{memoryContext}The player ({playerName}, Level {playerLevel}) approaches you.

Respond as {npcName} would, staying in character. Keep your response under 50 words. Be conversational and natural.";
        }

        /// <summary>
        /// Remember conversations for context
        /// </summary>
        private void RememberConversation(string whatWasSaid) {
            conversationMemory.Add($"{npcName}: {whatWasSaid}");

            // Keep memory limited
            while (conversationMemory.Count > memorySize) {
                conversationMemory.RemoveAt(0);
            }
        }

        /// <summary>
        /// Fallback responses when AI is unavailable
        /// </summary>
        private string GetFallbackResponse() {
            string[] fallbacks = {
                $"Greetings, traveler! I'm {npcName}.",
                "Welcome to my humble shop!",
                "What can I do for you today?",
                "Ah, another adventurer! How can I help?",
                "Good to see you again!"
            };

            // Return the appropriate fallback based on interaction count
            var index = Mathf.Min(conversationMemory.Count, fallbacks.Length - 1);

            return fallbacks[index];
        }

        /// <summary>
        /// Get a player name (override this in your game)
        /// </summary>
        // ReSharper disable once VirtualMemberNeverOverridden.Global
        protected virtual string GetPlayerName() {
            // In a real game, get this from your player manager
            // Example: return PlayerManager.Instance.PlayerName;
            return "Adventurer";
        }

        /// <summary>
        /// Get player level (override this in your game)
        /// </summary>
        // ReSharper disable once VirtualMemberNeverOverridden.Global
        protected virtual int GetPlayerLevel() {
            // In a real game, get this from your player manager
            // Example: return PlayerManager.Instance.Level;
            return 5;
        }

        private void ShowDialogueUI() {
            if (dialoguePanel != null) {
                dialoguePanel.SetActive(true);
            }
        }

        private void HideDialogueUI() {
            if (dialoguePanel != null) {
                dialoguePanel.SetActive(false);
            }
        }

        // Optional: Connect to Unity's collision system
        private void OnTriggerEnter(Collider other) {
            if (other.CompareTag("Player")) {
                OnPlayerInteract();
            }
        }

        // Optional: Connect to mouse clicks
        private void OnMouseDown() {
            OnPlayerInteract();
        }

        // Test in Unity Editor
        [ContextMenu("Test Conversation")]
        private void TestInEditor() {
            OnPlayerInteract();
        }

    }
}