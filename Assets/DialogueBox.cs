using System.Collections;
using UnityEngine;
using TMPro;
using System.Diagnostics;

public class DialogueBox : MonoBehaviour
{
    public TextMeshProUGUI textComponent;
    public string[] captions;
    public float typeSpeed = 0.05f;  // Adjust for typewriter effect speed

    // New enum for selecting exercise types
    public enum ExerciseType
    {
        WarmUp,
        PikeWalk,
        PushUp,
        BicycleCrunches,
        Plank,
        Situps,
        AirSquats,
        Pistols,
        JumpingJacks,
        Burpees
    }

    // Set these indices via Inspector to point to the start of each workout section
    public int warmUpIndex = 0;
    public int jumpingJacksIndex = 0;
    public int pikeWalkIndex = 0;
    public int airSquatsIndex = 0;
    public int bicycleCrunchesIndex = 0;
    public int pushUpIndex = 0;
    public int pistolsIndex = 0;
    public int plankIndex = 0;
    public int burpeesIndex = 0;
    public int situpsIndex = 0;

    [HideInInspector]
    public int currentIndex = 0;

    // UI Buttons (assign via Inspector)
    public GameObject nextButton;
    public GameObject againButton;

    // Field to store the current speech process (now static so it can be stopped externally)
    private static Process currentSpeechProcess = null;

    // Public volume variable (range 0.0 to 1.0) for speech, update via a UI slider
    [Range(0f, 1f)]
    public float speechVolume = 1.0f;

    void Start()
    {
        // Initialize Volume.trainerVoiceVolume for the Demo Scene if not already set.
        Volume.trainerVoiceVolume = PlayerPrefs.GetFloat("trainerVoiceVolume", 1f);
        UnityEngine.Debug.Log("DialogueBox: Loaded trainerVoiceVolume: " + Volume.trainerVoiceVolume);

        // At the beginning, ensure the dialogue field is active,
        // NextButton is enabled and AgainButton is disabled.
        gameObject.SetActive(true);
        nextButton.SetActive(true);
        againButton.SetActive(false);

        // Start by showing the first caption
        DisplayCurrentCaption();
    }

    // Call this method from AnimationPlayer.cs or a UI button to show the next caption
    public void ShowNextCaption()
    {
        if (currentIndex < captions.Length - 1)
        {
            currentIndex++;
            DisplayCurrentCaption();

            // While captions are ongoing, NextButton should be active and AgainButton inactive.
            nextButton.SetActive(true);
            againButton.SetActive(false);
        }
        else
        {
            // All captions shown—disable the dialogue field,
            // set NextButton to false and AgainButton to true.
            gameObject.SetActive(false);
            nextButton.SetActive(false);
            againButton.SetActive(true);
        }
    }

    // New combined method to select an exercise section.
    public void ShowSelectedExercises(ExerciseType exercise)
    {
        int selectedIndex = -1;
        switch (exercise)
        {
            case ExerciseType.WarmUp:
                selectedIndex = warmUpIndex;
                break;
            case ExerciseType.PikeWalk:
                selectedIndex = pikeWalkIndex;
                break;
            case ExerciseType.PushUp:
                selectedIndex = pushUpIndex;
                break;
            case ExerciseType.BicycleCrunches:
                selectedIndex = bicycleCrunchesIndex;
                break;
            case ExerciseType.Plank:
                selectedIndex = plankIndex;
                break;
            case ExerciseType.Situps:
                selectedIndex = situpsIndex;
                break;
            case ExerciseType.AirSquats:
                selectedIndex = airSquatsIndex;
                break;
            case ExerciseType.Pistols:
                selectedIndex = pistolsIndex;
                break;
            case ExerciseType.JumpingJacks:
                selectedIndex = jumpingJacksIndex;
                break;
            case ExerciseType.Burpees:
                selectedIndex = burpeesIndex;
                break;
            default:
                UnityEngine.Debug.LogError("Unknown exercise type");
                break;
        }

        if (selectedIndex >= 0 && selectedIndex < captions.Length)
        {
            currentIndex = selectedIndex;
            DisplayCurrentCaption();
            nextButton.SetActive(true);
            againButton.SetActive(false);
        }
        else
        {
            UnityEngine.Debug.LogError("Selected exercise index out of range.");
        }
    }

    public void RestartDialogue()
    {
        currentIndex = 0;
        gameObject.SetActive(true);
        StartCoroutine(DelayedRestart());
    }

    private IEnumerator DelayedRestart()
    {
        yield return null; // Wait one frame for the GameObject to become active.
        nextButton.SetActive(true);
        againButton.SetActive(false);
        DisplayCurrentCaption();
    }


    // Displays the current caption with a typewriter effect
    private void DisplayCurrentCaption()
    {
        StopAllCoroutines();
        StartCoroutine(TypeCaption(captions[currentIndex]));
        SpeakText(captions[currentIndex]);
    }

    public void SpeakText(string text)
    {
        // If there's an active speech process, kill it before starting a new one.
        if (currentSpeechProcess != null && !currentSpeechProcess.HasExited)
        {
            try
            {
                currentSpeechProcess.Kill();
                currentSpeechProcess = null;
            }
            catch (System.Exception ex)
            {
                UnityEngine.Debug.LogError("Failed to stop current speech process: " + ex.Message);
            }
        }

        string escapedText = text.Replace("\"", "\\\"");
        // Update this path with the one at which Speech.exe is located
        string speechDemoPath = @"SPEECHDEMOPATH";

        ProcessStartInfo startInfo = new ProcessStartInfo();
        startInfo.FileName = speechDemoPath;
        // Pass the text and the volume flag (-v) as arguments.
        // The volume is now taken from Music.trainerVoiceVolume.
        startInfo.Arguments = "\"" + escapedText + "\" -v " + Volume.trainerVoiceVolume;
        startInfo.UseShellExecute = false;
        startInfo.CreateNoWindow = true;

        try
        {
            currentSpeechProcess = Process.Start(startInfo);
        }
        catch (System.Exception ex)
        {
            UnityEngine.Debug.LogError("Failed to launch speech process: " + ex.Message);
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
            catch (System.Exception ex)
            {
                UnityEngine.Debug.LogError("Failed to kill speech process on exit: " + ex.Message);
            }
        }
    }

    // Public static method to stop the speech process
    public static void StopSpeechProcess()
    {
        if (currentSpeechProcess != null && !currentSpeechProcess.HasExited)
        {
            try
            {
                currentSpeechProcess.Kill();
                currentSpeechProcess = null;
                UnityEngine.Debug.Log("DialogueBox speech process successfully stopped.");
            }
            catch (System.Exception ex)
            {
                UnityEngine.Debug.LogError("Failed to kill DialogueBox speech process: " + ex.Message);
            }
        }
    }

    // Coroutine to display text character by character
    IEnumerator TypeCaption(string caption)
    {
        textComponent.text = "";
        foreach (char letter in caption)
        {
            textComponent.text += letter;
            yield return new WaitForSeconds(typeSpeed);
        }
    }
}
