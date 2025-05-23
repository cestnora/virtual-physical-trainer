using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneChanger : MonoBehaviour
{
    public void LoadScene(string sceneName)
    {
        // Stop any ongoing speech processes from both chat managers
        GeminiChatManager.StopSpeechProcess();
        LlamaChatManager.StopSpeechProcess();
        DialogueBox.StopSpeechProcess();
        SceneManager.LoadScene(sceneName);
    }
}