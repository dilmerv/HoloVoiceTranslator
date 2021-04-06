# HoloVoiceTranslator
Demos using HoloLens 2 and Azure Cognitive Services For Voice Translations

## Few steps required after cloning this repo:

1. Download the SpeechSDK Unity Package from [here](https://github.com/Azure-Samples/cognitive-services-speech-sdk/blob/master/samples/csharp/unity/VirtualAssistantPreview/README.md#download-speech-sdk-plugin-and-sample) and import it into this project
2. In the SpeechDK Folder go to Plugins/WSA/ARM64, select all dlls, check WSAPlayer as the only options, SDK UWP, CPU ARM64, Any Scripting Backend.
3. In the SpeechDK Folder go to Plugins/WSA/ARM, select all dlls, uncheck all platforms
4. Build it and open it in Visual Studio > Build to HoloLens 2 Device as ARM64
