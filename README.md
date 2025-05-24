# A Unity Project: Virtual Physical Trainer

This Unity project features an AI Fitness Assistant ("Alex") capable of handling **voice input** from the user, **natural language generation**, and **text-to-speech output**. It supports both **online (Gemini 1.5 Flash)** and **offline (LLaMA 3.2 1b via Ollama)** operation.

## Requirements

- **Unity Version**: 2022.3 (or newer)
- **.NET Framework**: Compatible with System.Speech (requires Windows)
- **External Tools**:
  - `Speech.exe` (Windows TTS wrapper)
  - Gemini API Key (for online chat)
  - Ollama (for local LLaMA inference)

## Project Structure

### Assets/

#### GeminiChatAI/Scripts/

- `GeminiChatManager.cs`  
  Manages the Gemini online chat system, including:
  - Windows ASR integration (`System.Speech.Recognition`)
  - Gemini API calls
  - Live transcript display and confidence filtering
  - TTS trigger via `Speech.exe`

- `GeminiTypewriter.cs`  
  Animates the trainer's responses with a typewriter effect.

#### Mixamo/  
Pre-rigged animations from [Mixamo](https://www.mixamo.com/#/) for avatar actions.

### Core Scripts

- `LlamaChatManager.cs`  
  Handles offline chat using LLaMA 3.2 via REST calls to Ollama. Implements similar ASR logic as GeminiChatManager.

- `AnimationPlayer.cs`  
  Controls the avatar animations (idle, jumping jacks, etc.).

- `DemoBGM.cs`  
  Controls looping or background music playback.

- `DialogueBox.cs`  
  Manages the UI container displaying chat history.

- `MainMenu.cs`  
  Loads the main menu and navigates between scenes.

- `SceneChanger.cs`  
  Handles scene transitions.

- `Volume.cs`  
  Centralized voice volume control across scenes (TTS volume).

## Voice Pipeline

- **Speech Recognition**: `System.Speech.Recognition.SpeechRecognitionEngine`  
  Supports both `CustomCommands` (strict phrases) and `Dictation` (open input) with confidence filtering.

- **LLM Inference**:
  - Gemini 1.5 Flash (online via REST)
  - LLaMA 3.2 1b (offline via local Ollama server)

- **Text-to-Speech**:  
  External `Speech.exe` (see below)

## Compiling `Speech.exe` (Windows TTS)

`Speech.exe` is a .NET Console Application that uses the `System.Speech.Synthesis` namespace to perform text-to-speech (TTS) using Windows' built-in voices.

Here are the steps to generate Speech.exe:

1. Create a .NET Console App:

- Open Visual Studio (or use dotnet new console in CLI).
- Add the code from `Program.cs` into the Main() method of the console app.

2. Add Reference to System.Speech:

- In Visual Studio: Right-click project > Add Reference > Assemblies > Framework > Check System.Speech.
- For CLI:
`dotnet add package System.Speech`

3. Build the Project:

- In Visual Studio: Build > Build Solution (Ctrl+Shift+B).
- From CLI:
`dotnet build -c Release`

## Running the Project

1. Open the project in Unity.
2. Load the main scene.
3. Assign the UI references in the Unity Inspector.
4. Click **Play** to test.

## Limitations

- Requires Windows OS (ASR and TTS)
- No mobile/web deployment



