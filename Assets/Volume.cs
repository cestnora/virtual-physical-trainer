using UnityEngine;
using UnityEngine.UI;

public class Volume : MonoBehaviour
{
    [SerializeField] private Slider musicSlider; // UI slider for BGM volume
    [SerializeField] private Slider voiceSlider;   // UI slider for trainer voice volume
    [SerializeField] private AudioSource bgmSource;  // BGM AudioSource in the Menu scene

    // These static variables are not required for saving;
    // instead, we always use PlayerPrefs as our storage
    public static float globalMusicVolume;
    public static float trainerVoiceVolume;

    void Awake()
    {
        // Load the saved music volume; if not present, use the slider's current value
        globalMusicVolume = PlayerPrefs.GetFloat("musicVolume", musicSlider != null ? musicSlider.value : 1f);
        // Immediately update the AudioSource and slider with this value
        if (bgmSource != null)
            bgmSource.volume = globalMusicVolume;
        if (musicSlider != null)
            musicSlider.value = globalMusicVolume;

        // Do the same for trainer voice volume
        trainerVoiceVolume = PlayerPrefs.GetFloat("trainerVoiceVolume", voiceSlider != null ? voiceSlider.value : 1f);
        if (voiceSlider != null)
            voiceSlider.value = trainerVoiceVolume;
    }

    void OnEnable()
    {
        // Each time the Menu scene (or volume settings) becomes active,
        // load the saved volume values and update the slider and AudioSource
        globalMusicVolume = PlayerPrefs.GetFloat("musicVolume", globalMusicVolume);
        if (bgmSource != null)
            bgmSource.volume = globalMusicVolume;
        if (musicSlider != null)
            musicSlider.value = globalMusicVolume;

        trainerVoiceVolume = PlayerPrefs.GetFloat("trainerVoiceVolume", trainerVoiceVolume);
        if (voiceSlider != null)
            voiceSlider.value = trainerVoiceVolume;
    }

    // Called when the user adjusts the music slider
    public void ChangeMusicVolume()
    {
        if (musicSlider != null)
        {
            globalMusicVolume = musicSlider.value;
            if (bgmSource != null)
                bgmSource.volume = globalMusicVolume;
            PlayerPrefs.SetFloat("musicVolume", globalMusicVolume);
            PlayerPrefs.Save();
        }
    }

    // Called when the user adjusts the voice slider
    public void ChangeVoiceVolume()
    {
        if (voiceSlider != null)
        {
            trainerVoiceVolume = voiceSlider.value;
            PlayerPrefs.SetFloat("trainerVoiceVolume", trainerVoiceVolume);
            PlayerPrefs.Save();
        }
    }

    // Test method for trainer voice volume
    public void TestVoiceVolume()
    {
        string testText = "Testing trainer voice volume.";
        string escapedText = testText.Replace("\"", "\\\"");
        // Update this path with the one at which Speech.exe is located
        string speechDemoPath = @"SPEECHDEMOPATH";

        System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo();
        startInfo.FileName = speechDemoPath;
        startInfo.Arguments = "\"" + escapedText + "\" -v " + trainerVoiceVolume;
        startInfo.UseShellExecute = false;
        startInfo.CreateNoWindow = true;

        try
        {
            System.Diagnostics.Process.Start(startInfo);
        }
        catch (System.Exception ex)
        {
            Debug.LogError("Failed to launch speech process: " + ex.Message);
        }
    }
}
