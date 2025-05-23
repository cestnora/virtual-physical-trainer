using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using System.Text;
using TMPro;
using System.Text.RegularExpressions;
using Debug = UnityEngine.Debug;
using System;
using System.Speech.Recognition;

public class LlamaChatManager : MonoBehaviour
{
    [Header("UI References")]
    public TMP_InputField playerInputField;
    public ScrollRect scrollRect;
    public RectTransform content;
    public Button startTalkingButton;
    public Button stopTalkingButton;
    public GameObject userTextPrefab;
    public GameObject trainerTextPrefab;

    [Header("LLM Settings")]
    public string apiUrl = "http://localhost:11434/api/generate";
    public string modelName = "llama3.2:1b";
    public string systemPrompt = "You are Alex Thompson, a virtual physical trainer. Answer briefly in max. 50 words:";

    // TTS process
    private static System.Diagnostics.Process currentSpeechProcess = null;

    // ASR
    private SpeechRecognitionEngine recognizer;
    private string recognizedText = "";
    private string partialTranscript = "";
    private TMP_Text currentUserTranscript = null;

    void Start()
    {
        // Ensure trainer voice volume
        if (Volume.trainerVoiceVolume <= 0f)
        {
            Volume.trainerVoiceVolume = 1f;
            Debug.Log("Volume.trainerVoiceVolume was 0; defaulting to 1.");
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
        string text = "Hello! My name is Alex, your virtual physical trainer! You can ask me for workout plans, dietary tips, recovery advice, or just motivation!";
        AddToChat($"Trainer: {text}");
        SpeakText(text);
    }

    public void OnSendButtonClick()
    {
        string msg = playerInputField.text;
        if (string.IsNullOrEmpty(msg)) return;
        string wrapped = AddLineBreaks(msg, 45);
        AddPlayerChat($"User: {wrapped}");
        HandleUserMessage(wrapped);
        playerInputField.text = "";
    }

    // --- ASR Methods ---
    public void StartSpeechRecognition()
    {
        Debug.Log("StartSpeechRecognition: Initializing recognizer.");
        startTalkingButton.interactable = false;
        stopTalkingButton.interactable = true;

        GameObject go = Instantiate(userTextPrefab, content);
        currentUserTranscript = go.GetComponent<TMP_Text>();
        currentUserTranscript.text = "Recording...";
        Canvas.ForceUpdateCanvases();
        scrollRect.verticalNormalizedPosition = 0f;

        recognizedText = "";
        partialTranscript = "";

        try
        {
            recognizer = new SpeechRecognitionEngine();

            // 1. Load custom commands grammar
            Choices phrases = new Choices(new string[] {
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

            // 2. Load dictation grammar
            Grammar dictationGrammar = new DictationGrammar() { Name = "Dictation" };
            recognizer.LoadGrammar(dictationGrammar);

            // 3. Handlers
            recognizer.SpeechRecognized += Recognizer_SpeechRecognized;
            recognizer.SpeechHypothesized += Recognizer_SpeechHypothesized;

            recognizer.SetInputToDefaultAudioDevice();
            recognizer.RecognizeAsync(RecognizeMode.Multiple);

            Debug.Log("StartSpeechRecognition: Recognizer started with both grammars.");
        }
        catch (Exception ex)
        {
            Debug.LogError("ASR init error: " + ex.Message);
        }
    }

    public void FinishSpeechRecognition()
    {
        Debug.Log("FinishSpeechRecognition: Stopping recognizer.");
        startTalkingButton.interactable = true;
        stopTalkingButton.interactable = false;

        if (recognizer != null)
        {
            recognizer.RecognizeAsyncStop();
            recognizer.SpeechRecognized -= Recognizer_SpeechRecognized;
            recognizer.SpeechHypothesized -= Recognizer_SpeechHypothesized;
            recognizer.Dispose();
            recognizer = null;
        }

        StartCoroutine(FinishSpeechCoroutine());
    }

    private IEnumerator FinishSpeechCoroutine()
    {
        yield return new WaitForSeconds(0.5f);

        string final = !string.IsNullOrEmpty(recognizedText)
            ? recognizedText.Trim()
            : partialTranscript.Trim();

        Debug.Log("FinishSpeechRecognition: Final transcript: " + final);
        if (!string.IsNullOrEmpty(final))
        {
            string formatted = AddLineBreaks(final, 42);
            currentUserTranscript.text = "User: " + formatted;
            HandleUserMessage(final);
        }
        else
        {
            currentUserTranscript.text = "";
        }
    }

    private void Recognizer_SpeechRecognized(object sender, SpeechRecognizedEventArgs e)
    {
        string grammar = e.Result.Grammar.Name;
        float confidence = e.Result.Confidence;
        string text = e.Result.Text;

        Debug.Log($"[ASR] Recognized ({grammar}) \"{text}\" @ {confidence}");

        // Append result if confidence is acceptable
        if ((grammar == "CustomCommands" && confidence >= 0.8f) ||
            (grammar == "Dictation" && confidence >= 0.3f))
        {
            recognizedText += (recognizedText.Length > 0 ? " " : "") + text;

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

    private void Recognizer_SpeechHypothesized(object sender, SpeechHypothesizedEventArgs e)
    {
        partialTranscript = e.Result.Text;
        UnityMainThreadDispatcher.Instance().Enqueue(() =>
        {
            if (currentUserTranscript != null)
                currentUserTranscript.text = "User: " + partialTranscript;
        });
    }


    // --- Chat and Networking ---
    private void HandleUserMessage(string message)
    {
        StartCoroutine(SendToLlama(message));
    }

    private IEnumerator SendToLlama(string prompt)
    {
        Debug.Log("SendToLlama…");
        var sw = System.Diagnostics.Stopwatch.StartNew();
        string full = systemPrompt + "\n" + prompt;
        var payload = JsonUtility.ToJson(new LlamaRequest
        {
            model = modelName,
            prompt = full,
            stream = false
        });
        sw.Stop(); Debug.Log($"Build payload: {sw.ElapsedMilliseconds}ms");

        sw.Restart();
        var req = new UnityWebRequest(apiUrl, "POST");
        byte[] body = Encoding.UTF8.GetBytes(payload);
        req.uploadHandler = new UploadHandlerRaw(body);
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "text/plain");
        req.SetRequestHeader("Accept", "application/json");

        yield return req.SendWebRequest();
        sw.Stop(); Debug.Log($"Web request: {sw.ElapsedMilliseconds}ms");

        if (req.result == UnityWebRequest.Result.Success)
        {
            var json = req.downloadHandler.text;
            var resp = JsonUtility.FromJson<LlamaResponse>(json);
            string txt = Regex.Replace(resp.response, @"\*+|\[.*?\]", "");
            AddToChat($"Trainer: {txt}");
            SpeakText(txt);
        }
        else
        {
            Debug.LogError($"LLM error: {req.error}");
            AddToChat("Trainer: Sorry, something went wrong.");
            SpeakText("Sorry, something went wrong.");
        }
    }

    // --- UI Helpers ---
    private void AddToChat(string msg)
    {
        var go = Instantiate(trainerTextPrefab, content);
        go.GetComponent<GeminiTypewriter>().StartTyping(msg);
        Canvas.ForceUpdateCanvases();
        scrollRect.verticalNormalizedPosition = 0f;
    }

    private void AddPlayerChat(string msg)
    {
        var go = Instantiate(userTextPrefab, content);
        go.GetComponent<TMP_Text>().text = msg;
        Canvas.ForceUpdateCanvases();
        scrollRect.verticalNormalizedPosition = 0f;
    }

    private string AddLineBreaks(string text, int maxChars)
    {
        var sb = new StringBuilder();
        foreach (var line in text.Split('\n'))
        {
            int len = 0;
            foreach (var w in line.Split(' '))
            {
                if (len + w.Length + 1 > maxChars)
                {
                    sb.Append('\n');
                    len = 0;
                }
                sb.Append(w).Append(' ');
                len += w.Length + 1;
            }
            sb.Append('\n');
        }
        return sb.ToString().TrimEnd('\n');
    }

    // --- TTS ---
    private void SpeakText(string text)
    {
        // kill old
        if (currentSpeechProcess != null && !currentSpeechProcess.HasExited)
            currentSpeechProcess.Kill();

        string escaped = text.Replace("\"", "\\\"");
        var psi = new System.Diagnostics.ProcessStartInfo
        {
            // Update this path with the one at which Speech.exe is located
            FileName = @"FILENAME",
            Arguments = $"\"{escaped}\" -v {Volume.trainerVoiceVolume}",
            UseShellExecute = false,
            CreateNoWindow = true
        };
        currentSpeechProcess = System.Diagnostics.Process.Start(psi);
    }

    void OnApplicationQuit()
    {
        StopSpeechProcess();
        if (recognizer != null)
        {
            recognizer.RecognizeAsyncStop();
            recognizer.Dispose();
            recognizer = null;
        }
    }

    public static void StopSpeechProcess()
    {
        if (currentSpeechProcess != null && !currentSpeechProcess.HasExited)
        {
            currentSpeechProcess.Kill();
            currentSpeechProcess = null;
        }
    }

    [Serializable]
    public class LlamaRequest 
    { 
        public string model, prompt; 
        public bool stream; 
    }
    [Serializable]
    public class LlamaResponse 
    { 
        public string response; 
    }
}
