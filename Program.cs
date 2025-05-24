using System.Speech.Synthesis;
using System.Globalization;
using System.Collections.Generic;

namespace SpeechDemo
{
    class Program
    {
        static void Main(string[] args)
        {
            // Exit if no text is provided
            if (args.Length == 0)
            {
                return;
            }

            // Default volume: full volume (1.0 = 100%)
            float volume = 1.0f;

            // List to hold the text parts.
            List<string> textArgs = new List<string>();

            // Parse arguments: look for "-v" flag and use the following value as volume
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == "-v" && i < args.Length - 1)
                {
                    if (float.TryParse(args[i + 1], NumberStyles.Float, CultureInfo.InvariantCulture, out float vol))
                    {
                        volume = Clamp01(vol);
                    }
                    // Skip the volume argument
                    i++;
                }
                else
                {
                    textArgs.Add(args[i]);
                }
            }

            // Combine remaining arguments into the text to speak
            string textToSpeak = string.Join(" ", textArgs);

            using (SpeechSynthesizer synthesizer = new SpeechSynthesizer())
            {
                synthesizer.Volume = (int)(volume * 100);
                synthesizer.SelectVoice("Microsoft David Desktop");

                // Build SSML with prosody adjustments for a more enthusiastic tone
                string ssml = $@"
                <speak version=""1.0"" xml:lang=""en-US"">
                  <voice name=""Microsoft David Desktop"">
                    <prosody rate=""+20%"" pitch=""+20%"">
                      {System.Security.SecurityElement.Escape(textToSpeak)}
                    </prosody>
                  </voice>
                </speak>";

                synthesizer.SpeakSsml(ssml);
            }
        }

        // Helper method to clamp a float value between 0.0 and 1.0
        private static float Clamp01(float value)
        {
            if (value < 0f)
                return 0f;
            if (value > 1f)
                return 1f;
            return value;
        }
    }
}