using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using System.Text;
using TMPro;
using System.Text.RegularExpressions;
using System.Diagnostics; // For Stopwatch and Process (used by TTS)
using Debug = UnityEngine.Debug; // Explicitly use UnityEngine.Debug
using System;
using System.Speech.Recognition;  // Add reference to System.Speech.dll

public class GeminiChatManager : MonoBehaviour
{
    public TMP_InputField playerInputField;
    // To obtain an API key, visit https://ai.google.dev/gemini-api/docs/api-key
    public string apiKey = "APIKEY";
    private string apiUrl = "https://generativelanguage.googleapis.com/v1beta/models/gemini-1.5-flash:generateContent?key";
    public ScrollRect scrollRect;
    public RectTransform content;

    // Prefabs for different message types
    public GameObject userTextPrefab;    // White text for user messages
    public GameObject trainerTextPrefab; // Light yellow text for LLM messages

    // System prompt
    public string systemPrompt = "You are Alex Thompson, a virtual physical trainer. Answer briefly:";

    // UI buttons for speech input control
    public Button startTalkingButton;
    public Button stopTalkingButton;

    // For TTS (trainer messages)
    private static Process currentSpeechProcess = null;

    // Windows SpeechRecognitionEngine
    private SpeechRecognitionEngine recognizer;
    // Accumulated recognized text
    private string recognizedText = "";

    // Current user transcript text element
    private TMP_Text currentUserTranscript = null;

    private string partialTranscript = "";


    void Start()
    {
        // Ensure that trainerVoiceVolume has a valid (non-zero) default
        if (Volume.trainerVoiceVolume <= 0)
        {
            Volume.trainerVoiceVolume = 1f;  // Set a default voice volume if not initialized
            Debug.Log("Volume.trainerVoiceVolume was 0; setting default volume to 1.");
        }

        StartChat();
        if (startTalkingButton != null && stopTalkingButton != null)
        {
            startTalkingButton.interactable = true;
            stopTalkingButton.interactable = false;
        }
    }

    public void StartChat()
    {
        string text = "Hello! My name is Alex, your virtual physical trainer! You can ask me for workouts plans, dietary tips, recovery advice, or just motivation!";
        AddToChat($"Trainer: {text}");
        SpeakText(text);
    }

    public void OnSendButtonClick()
    {
        string playerMessage = playerInputField.text;
        if (string.IsNullOrEmpty(playerMessage)) return;
        string message = AddLineBreaks(playerMessage, 42);
        AddPlayerChat($"User: {message}");
        HandleUserMessage(message);
        playerInputField.text = "";
    }

    // --- Speech Recognition Methods using Windows Built-In Speech Recognition ---
    // Called by "Start Talking" button
    public void StartSpeechRecognition()
    {
        Debug.Log("StartSpeechRecognition: Button pressed. Initializing recognizer.");

        if (startTalkingButton != null && stopTalkingButton != null)
        {
            startTalkingButton.interactable = false;
            stopTalkingButton.interactable = true;
        }

        GameObject newUserText = Instantiate(userTextPrefab, content);
        currentUserTranscript = newUserText.GetComponent<TMP_Text>();
        currentUserTranscript.text = "Recording...";
        Canvas.ForceUpdateCanvases();
        scrollRect.verticalNormalizedPosition = 0;

        recognizedText = "";
        partialTranscript = "";

        try
        {
            recognizer = new SpeechRecognitionEngine();

            // 1. Custom phrases
            Choices phrases = new Choices();
            phrases.Add(new string[] {
            "Hi Alex",
            "Can you introduce yourself",
            "What is a good warm up",
            "Give me a workout",
            "Goodbye Alex",
            "What’s a core exercise",
            "Give me a full body routine",
            "What can you do"
        });

            GrammarBuilder gb = new GrammarBuilder();
            gb.Append(phrases);
            Grammar customGrammar = new Grammar(gb) { Name = "CustomCommands" };
            recognizer.LoadGrammar(customGrammar);

            // 2. Dictation grammar for open speech
            Grammar dictationGrammar = new DictationGrammar() { Name = "Dictation" };
            recognizer.LoadGrammar(dictationGrammar);

            // 3. Event handlers
            recognizer.SpeechRecognized += Recognizer_SpeechRecognized;
            recognizer.SpeechHypothesized += Recognizer_SpeechHypothesized;

            recognizer.SetInputToDefaultAudioDevice();
            recognizer.RecognizeAsync(RecognizeMode.Multiple);

            Debug.Log("StartSpeechRecognition: Recognizer started with custom + dictation grammars.");
        }
        catch (Exception ex)
        {
            Debug.LogError("StartSpeechRecognition: Error initializing recognizer: " + ex.Message);
        }
    }

    // Called by "Stop Talking" button
    public void FinishSpeechRecognition()
    {
        Debug.Log("FinishSpeechRecognition: Button pressed. Stopping recognizer.");

        if (startTalkingButton != null && stopTalkingButton != null)
        {
            startTalkingButton.interactable = true;
            stopTalkingButton.interactable = false;
        }

        if (recognizer != null)
        {
            try
            {
                recognizer.RecognizeAsyncStop();
                recognizer.SpeechRecognized -= Recognizer_SpeechRecognized;
                recognizer.SpeechHypothesized -= Recognizer_SpeechHypothesized;
                recognizer.Dispose();
                recognizer = null;
                Debug.Log("FinishSpeechRecognition: Recognizer stopped successfully.");
            }
            catch (Exception ex)
            {
                Debug.LogError("FinishSpeechRecognition: Error stopping recognizer: " + ex.Message);
            }
        }

        //string finalTranscript = recognizedText.Trim();

        string finalTranscript = !string.IsNullOrEmpty(recognizedText)
        ? recognizedText.Trim()
            : partialTranscript.Trim();

        Debug.Log("FinishSpeechRecognition: Final transcript: " + finalTranscript);
        if (!string.IsNullOrEmpty(finalTranscript))
        {
            // Update the live transcript bubble to include the "User:" prefix
            currentUserTranscript.text = "User: " + finalTranscript;
            // Use this same transcript for processing (without adding a duplicate chat line)
            HandleUserMessage(finalTranscript);
        }
        else
        {
            currentUserTranscript.text = "";
        }
    }

    // Final result
    // Event handler for finalized recognition results
    private void Recognizer_SpeechRecognized(object sender, SpeechRecognizedEventArgs e)
    {
        string grammar = e.Result.Grammar.Name;
        float confidence = e.Result.Confidence;
        string text = e.Result.Text;

        Debug.Log($"[ASR] Recognized ({grammar}) \"{text}\" @ {confidence}");

        // Filter based on grammar and confidence
        if ((grammar == "CustomCommands" && confidence >= 0.8f) ||
            (grammar == "Dictation" && confidence >= 0.3f))
        {
            // Append this result to the cumulative transcript
            if (!string.IsNullOrEmpty(recognizedText))
                recognizedText += " " + text;
            else
                recognizedText = text;

            // Format for display
            string formatted = AddLineBreaks(recognizedText, 45);

            UnityMainThreadDispatcher.Instance().Enqueue(() =>
            {
                if (currentUserTranscript != null)
                    currentUserTranscript.text = "User: " + formatted;
            });
        }
        else
        {
            Debug.LogWarning("[ASR] Ignored: grammar or confidence too low.");
        }
    }

    // Partial result (optional UI feedback)
    private void Recognizer_SpeechHypothesized(object sender, SpeechHypothesizedEventArgs e)
    {
        partialTranscript = e.Result.Text;
        UnityMainThreadDispatcher.Instance().Enqueue(() => {
            if (currentUserTranscript != null)
                currentUserTranscript.text = "User: " + partialTranscript;
        });
    }

    private string AddLineBreaks(string text, int maxCharactersPerLine)
    {
        string[] segments = text.Split('\n');
        StringBuilder result = new StringBuilder();
        foreach (string segment in segments)
        {
            result.Append(WrapSegment(segment, maxCharactersPerLine));
            result.Append("\n");
        }
        return result.ToString().TrimEnd('\n');
    }

    private string WrapSegment(string segment, int maxCharactersPerLine)
    {
        if (string.IsNullOrEmpty(segment))
            return segment;
        StringBuilder wrapped = new StringBuilder();
        int currentLineLength = 0;
        string[] words = segment.Split(' ');
        foreach (string word in words)
        {
            if (currentLineLength == 0)
            {
                wrapped.Append(word);
                currentLineLength = word.Length;
            }
            else if (currentLineLength + 1 + word.Length > maxCharactersPerLine)
            {
                wrapped.Append("\n" + word);
                currentLineLength = word.Length;
            }
            else
            {
                wrapped.Append(" " + word);
                currentLineLength += 1 + word.Length;
            }
        }
        return wrapped.ToString();
    }

    private void HandleUserMessage(string message)
    {
        StartCoroutine(SendToGemini(message));
    }

    private void AddToChat(string message)
    {
        GameObject newText = Instantiate(trainerTextPrefab, content);
        newText.GetComponent<GeminiTypewriter>().StartTyping(message);
        Canvas.ForceUpdateCanvases();
        scrollRect.verticalNormalizedPosition = 0;
    }

    private void AddPlayerChat(string message)
    {
        GameObject newText = Instantiate(userTextPrefab, content);
        newText.GetComponent<TMP_Text>().text = message;
        Canvas.ForceUpdateCanvases();
        scrollRect.verticalNormalizedPosition = 0;
    }

    private IEnumerator SendToGemini(string prompt)
    {
        Debug.Log("SendToGemini started");
        Stopwatch stopwatch = Stopwatch.StartNew();
        string fullPrompt = systemPrompt + "\n" + prompt;
        Debug.Log("Full prompt built.");
        string jsonPayload = "{\"contents\":[{\"parts\":[{\"text\":\"" + fullPrompt + "\"}]}]}";
        Debug.Log("JSON payload built: " + jsonPayload);
        stopwatch.Stop();
        Debug.Log("Time to build payload: " + stopwatch.ElapsedMilliseconds + "ms");
        stopwatch.Restart();
        UnityWebRequest request = new UnityWebRequest(apiUrl + "=" + apiKey, "POST");
        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonPayload);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("Accept", "application/json");
        Debug.Log("Sending request...");
        yield return request.SendWebRequest();
        stopwatch.Stop();
        Debug.Log("Time for web request: " + stopwatch.ElapsedMilliseconds + "ms");
        stopwatch.Restart();
        if (request.result == UnityWebRequest.Result.Success)
        {
            string response = request.downloadHandler.text;
            Debug.Log("Received response: " + response);
            GeminiResponse responseFinal = JsonUtility.FromJson<GeminiResponse>(response);
            string text = responseFinal.candidates[0].content.parts[0].text;
            text = Regex.Replace(text, @"\*+", "");
            text = Regex.Replace(text, @"\[.*?\]", "");
            string trainerMessage = $"Trainer: {text}";
            AddToChat(trainerMessage);
            SpeakText(text);
        }
        else
        {
            Debug.LogError("Error: " + request.error);
            Debug.LogError("Response: " + request.downloadHandler.text);
            string text = "Sorry, something went wrong.";
            AddToChat($"Trainer: {text}");
            SpeakText(text);
        }
        stopwatch.Stop();
        Debug.Log("Time for processing response: " + stopwatch.ElapsedMilliseconds + "ms");
    }

    private void SpeakText(string text)
    {
        string escapedText = text.Replace("\"", "\\\"");
        // Update this path with the one at which Speech.exe is located
        string speechDemoPath = @"SPEECHDEMOPATH";
        Debug.Log("SpeakText: Preparing to speak text: " + text);
        ProcessStartInfo startInfo = new ProcessStartInfo();
        startInfo.FileName = speechDemoPath;
        // Update this path with the folder in which Speech.exe is located
        startInfo.WorkingDirectory = @"WORKINGDIRECTORY";
        startInfo.Arguments = "\"" + escapedText + "\" -v " + Volume.trainerVoiceVolume;
        startInfo.UseShellExecute = false;
        startInfo.CreateNoWindow = true;
        try
        {
            currentSpeechProcess = Process.Start(startInfo);
            Debug.Log("SpeakText: Speech process started successfully.");
        }
        catch (Exception ex)
        {
            Debug.LogError("SpeakText: Failed to launch speech process: " + ex.Message);
        }
    }

    void OnApplicationQuit()
    {
        if (currentSpeechProcess != null && !currentSpeechProcess.HasExited)
        {
            try
            {
                currentSpeechProcess.Kill();
                currentSpeechProcess = null;
            }
            catch (Exception ex)
            {
                Debug.LogError("OnApplicationQuit: Failed to kill speech process on exit: " + ex.Message);
            }
        }
        if (recognizer != null)
        {
            try
            {
                recognizer.RecognizeAsyncStop();
                recognizer.Dispose();
                recognizer = null;
            }
            catch (Exception ex)
            {
                Debug.LogError("OnApplicationQuit: Failed to stop recognizer: " + ex.Message);
            }
        }
    }

    // Public static method to stop the speech process (if needed externally)
    public static void StopSpeechProcess()
    {
        if (currentSpeechProcess != null && !currentSpeechProcess.HasExited)
        {
            try
            {
                currentSpeechProcess.Kill();
                currentSpeechProcess = null;
                Debug.Log("StopSpeechProcess: Speech process successfully stopped.");
            }
            catch (Exception ex)
            {
                Debug.LogError("StopSpeechProcess: Failed to kill speech process: " + ex.Message);
            }
        }
    }

    [Serializable]
    public class GeminiResponse
    {
        public Candidate[] candidates;
        public UsageMetadata usageMetadata;
    }

    [Serializable]
    public class Candidate
    {
        public Content content;
        public string finishReson;
        public float avgLogprobs;
    }

    [Serializable]
    public class Content
    {
        public Part[] parts;
        public string role;
    }

    [Serializable]
    public class Part
    {
        public string text;
    }

    [Serializable]
    public class UsageMetadata
    {
        public int promptTokenCount;
        public int candidatesTokenCount;
        public int totalTokenCount;
    }
}