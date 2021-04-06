using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Translation;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SpeechRecognition : MonoBehaviour
{
    public TextMeshProUGUI RecognizedText;
    public TextMeshProUGUI ErrorText;

    // Dropdown lists used to select translation languages, if enabled
    public Toggle TranslationEnabled;
    public TMP_Dropdown Languages1;
    public TMP_Dropdown Languages2;
    public TMP_Dropdown Languages3;

    private string recognizedString = "";
    private string errorString = "";
    private System.Object threadLocker = new System.Object();

    public string SpeechServiceAPIKey = string.Empty;
    public string SpeechServiceRegion = "westus";

    private SpeechRecognizer recognizer;
    private TranslationRecognizer translator;
    
    string fromLanguage = "es-ES";

    private bool micPermissionGranted = false;


    private void Start()
    {
        micPermissionGranted = true;
    }

    public void StartContinuous()
    {
        errorString = "";
        if (micPermissionGranted)
        {
            if (TranslationEnabled.isOn)
            {
                StartContinuousTranslation();
            }
            else
            {
                StartContinuousRecognition();
            }
        }
        else
        {
            recognizedString = "This app cannot function without access to the microphone.";
            errorString = "ERROR: Microphone access denied.";
            Debug.LogFormat(errorString);
        }
    }

    void CreateSpeechRecognizer()
    {
        if (SpeechServiceAPIKey.Length == 0 || SpeechServiceAPIKey == "YourSubscriptionKey")
        {
            recognizedString = "You forgot to obtain Cognitive Services Speech credentials and inserting them in this app." + Environment.NewLine +
                               "See the README file and/or the instructions in the Awake() function for more info before proceeding.";
            errorString = "ERROR: Missing service credentials";
            Debug.LogFormat(errorString);
            return;
        }
        Debug.LogFormat("Creating Speech Recognizer.");
        recognizedString = "Initializing speech recognition, please wait...";

        if (recognizer == null)
        {
            SpeechConfig config = SpeechConfig.FromSubscription(SpeechServiceAPIKey, SpeechServiceRegion);
            config.SpeechRecognitionLanguage = fromLanguage;
            recognizer = new SpeechRecognizer(config);

            if (recognizer != null)
            {
                // Subscribes to speech events.
                recognizer.Recognizing += RecognizingHandler;
                recognizer.Recognized += RecognizedHandler;
                recognizer.SpeechStartDetected += SpeechStartDetectedHandler;
                recognizer.SpeechEndDetected += SpeechEndDetectedHandler;
                recognizer.Canceled += CanceledHandler;
                recognizer.SessionStarted += SessionStartedHandler;
                recognizer.SessionStopped += SessionStoppedHandler;
            }
        }
        Debug.LogFormat("CreateSpeechRecognizer exit");
    }

    private async void StartContinuousRecognition()
    {
        Debug.LogFormat("Starting Continuous Speech Recognition.");
        CreateSpeechRecognizer();

        if (recognizer != null)
        {
            Debug.LogFormat("Starting Speech Recognizer.");
            await recognizer.StartContinuousRecognitionAsync().ConfigureAwait(false);

            recognizedString = "Speech Recognizer is now running.";
            Debug.LogFormat("Speech Recognizer is now running.");
        }
        Debug.LogFormat("Start Continuous Speech Recognition exit");
    }

#region Speech Recognition event handlers
    private void SessionStartedHandler(object sender, SessionEventArgs e)
    {
        Debug.LogFormat($"\n    Session started event. Event: {e.ToString()}.");
    }

    private void SessionStoppedHandler(object sender, SessionEventArgs e)
    {
        Debug.LogFormat($"\n    Session event. Event: {e.ToString()}.");
        Debug.LogFormat($"Session Stop detected. Stop the recognition.");
    }

    private void SpeechStartDetectedHandler(object sender, RecognitionEventArgs e)
    {
        Debug.LogFormat($"SpeechStartDetected received: offset: {e.Offset}.");
    }

    private void SpeechEndDetectedHandler(object sender, RecognitionEventArgs e)
    {
        Debug.LogFormat($"SpeechEndDetected received: offset: {e.Offset}.");
        Debug.LogFormat($"Speech end detected.");
    }

    // "Recognizing" events are fired every time we receive interim results during recognition (i.e. hypotheses)
    private void RecognizingHandler(object sender, SpeechRecognitionEventArgs e)
    {
        if (e.Result.Reason == ResultReason.RecognizingSpeech)
        {
            Debug.LogFormat($"HYPOTHESIS: Text={e.Result.Text}");
            lock (threadLocker)
            {
                recognizedString = $"HYPOTHESIS: {Environment.NewLine}{e.Result.Text}";
            }
        }
    }

    // "Recognized" events are fired when the utterance end was detected by the server
    private void RecognizedHandler(object sender, SpeechRecognitionEventArgs e)
    {
        if (e.Result.Reason == ResultReason.RecognizedSpeech)
        {
            Debug.LogFormat($"RECOGNIZED: Text={e.Result.Text}");
            lock (threadLocker)
            {
                recognizedString = $"RESULT: {Environment.NewLine}{e.Result.Text}";
            }
        }
        else if (e.Result.Reason == ResultReason.NoMatch)
        {
            Debug.LogFormat($"NOMATCH: Speech could not be recognized.");
        }
    }

    // "Canceled" events are fired if the server encounters some kind of error.
    // This is often caused by invalid subscription credentials.
    private void CanceledHandler(object sender, SpeechRecognitionCanceledEventArgs e)
    {
        Debug.LogFormat($"CANCELED: Reason={e.Reason}");

        errorString = e.ToString();
        if (e.Reason == CancellationReason.Error)
        {
            Debug.LogFormat($"CANCELED: ErrorDetails={e.ErrorDetails}");
            Debug.LogFormat($"CANCELED: Did you update the subscription info?");
        }
    }
    #endregion

    void CreateTranslationRecognizer()
    {
        Debug.LogFormat("Creating Translation Recognizer.");
        recognizedString = "Initializing speech recognition with translation, please wait...";

        if (translator == null)
        {
            SpeechTranslationConfig config = SpeechTranslationConfig.FromSubscription(SpeechServiceAPIKey, SpeechServiceRegion);
            config.SpeechRecognitionLanguage = fromLanguage;
            if (Languages1.captionText.text.Length > 0)
                config.AddTargetLanguage(ExtractLanguageCode(Languages1.captionText.text));
            if (Languages2.captionText.text.Length > 0)
                config.AddTargetLanguage(ExtractLanguageCode(Languages2.captionText.text));
            if (Languages3.captionText.text.Length > 0)
                config.AddTargetLanguage(ExtractLanguageCode(Languages3.captionText.text));
            translator = new TranslationRecognizer(config);

            if (translator != null)
            {
                translator.Recognizing += RecognizingTranslationHandler;
                translator.Recognized += RecognizedTranslationHandler;
                translator.SpeechStartDetected += SpeechStartDetectedHandler;
                translator.SpeechEndDetected += SpeechEndDetectedHandler;
                translator.Canceled += CanceledTranslationHandler;
                translator.SessionStarted += SessionStartedHandler;
                translator.SessionStopped += SessionStoppedHandler;
            }
        }
        Debug.LogFormat("CreateTranslationRecognizer exit");
    }

    /// <summary>
    /// Extract the language code from the enum used to populate the droplists.
    /// Assumes that an underscore "_" is used as a separator in the enum name.
    /// </summary>
    /// <param name="languageListLabel"></param>
    /// <returns></returns>
    string ExtractLanguageCode(string languageListLabel)
    {
        return languageListLabel.Substring(0, languageListLabel.IndexOf("_"));
    }

    /// <summary>
    /// Initiate continuous speech recognition from the default microphone, including live translation.
    /// </summary>
    private async void StartContinuousTranslation()
    {
        Debug.LogFormat("Starting Continuous Translation Recognition.");
        CreateTranslationRecognizer();

        if (translator != null)
        {
            Debug.LogFormat("Starting Speech Translator.");
            await translator.StartContinuousRecognitionAsync().ConfigureAwait(false);

            recognizedString = "Speech Translator is now running.";
            Debug.LogFormat("Speech Translator is now running.");
        }
        Debug.LogFormat("Start Continuous Speech Translation exit");
    }

#region Speech Translation event handlers
    // "Recognizing" events are fired every time we receive interim results during recognition (i.e. hypotheses)
    private void RecognizingTranslationHandler(object sender, TranslationRecognitionEventArgs e)
    {
        if (e.Result.Reason == ResultReason.TranslatingSpeech)
        {
            Debug.LogFormat($"RECOGNIZED HYPOTHESIS: Text={e.Result.Text}");
            lock (threadLocker)
            {
                recognizedString = $"RECOGNIZED HYPOTHESIS ({fromLanguage}): {Environment.NewLine}{e.Result.Text}";
                recognizedString += $"{Environment.NewLine}TRANSLATED HYPOTHESESE:";
                foreach (var element in e.Result.Translations)
                {
                    recognizedString += $"{Environment.NewLine}[{element.Key}]: {element.Value}";
                }
            }
        }
    }

    // "Recognized" events are fired when the utterance end was detected by the server
    private void RecognizedTranslationHandler(object sender, TranslationRecognitionEventArgs e)
    {
        if (e.Result.Reason == ResultReason.TranslatedSpeech)
        {
            Debug.LogFormat($"RECOGNIZED: Text={e.Result.Text}");
            lock (threadLocker)
            {
                recognizedString = $"RECOGNIZED RESULT ({fromLanguage}): {Environment.NewLine}{e.Result.Text}";
                recognizedString += $"{Environment.NewLine}TRANSLATED RESULTS:";
                foreach (var element in e.Result.Translations)
                {
                    recognizedString += $"{Environment.NewLine}[{element.Key}]: {element.Value}";
                }
            }
        }
        else if (e.Result.Reason == ResultReason.RecognizedSpeech)
        {
            Debug.LogFormat($"RECOGNIZED: Text={e.Result.Text}");
            lock (threadLocker)
            {
                recognizedString = $"NON-TRANSLATED RESULT: {Environment.NewLine}{e.Result.Text}";
            }
        }
        else if (e.Result.Reason == ResultReason.NoMatch)
        {
            Debug.LogFormat($"NOMATCH: Speech could not be recognized or translated.");
        }
    }

    // "Canceled" events are fired if the server encounters some kind of error.
    // This is often caused by invalid subscription credentials.
    private void CanceledTranslationHandler(object sender, TranslationRecognitionCanceledEventArgs e)
    {
        Debug.LogFormat($"CANCELED: Reason={e.Reason}");

        errorString = e.ToString();
        if (e.Reason == CancellationReason.Error)
        {
            Debug.LogFormat($"CANCELED: ErrorDetails={e.ErrorDetails}");
            Debug.LogFormat($"CANCELED: Did you update the subscription info?");
        }
    }
#endregion

    /// <summary>
    /// Main update loop: Runs every frame
    /// </summary>
    void Update()
    {
        // Used to update results on screen during updates
        lock (threadLocker)
        {
            RecognizedText.text = recognizedString;
            ErrorText.text = errorString;
        }
    }

    void OnDisable()
    {
        StopRecognition();
    }

    public async void StopRecognition()
    {
        if (recognizer != null)
        {
            await recognizer.StopContinuousRecognitionAsync().ConfigureAwait(false);
            recognizer.Recognizing -= RecognizingHandler;
            recognizer.Recognized -= RecognizedHandler;
            recognizer.SpeechStartDetected -= SpeechStartDetectedHandler;
            recognizer.SpeechEndDetected -= SpeechEndDetectedHandler;
            recognizer.Canceled -= CanceledHandler;
            recognizer.SessionStarted -= SessionStartedHandler;
            recognizer.SessionStopped -= SessionStoppedHandler;
            recognizer.Dispose();
            recognizer = null;
            recognizedString = "Speech Recognizer is now stopped.";
            Debug.LogFormat("Speech Recognizer is now stopped.");
        }
        if (translator != null)
        {
            await translator.StopContinuousRecognitionAsync().ConfigureAwait(false);
            translator.Recognizing -= RecognizingTranslationHandler;
            translator.Recognized -= RecognizedTranslationHandler;
            translator.SpeechStartDetected -= SpeechStartDetectedHandler;
            translator.SpeechEndDetected -= SpeechEndDetectedHandler;
            translator.Canceled -= CanceledTranslationHandler;
            translator.SessionStarted -= SessionStartedHandler;
            translator.SessionStopped -= SessionStoppedHandler;
            translator.Dispose();
            translator = null;
            recognizedString = "Speech Translator is now stopped.";
            Debug.LogFormat("Speech Translator is now stopped.");
        }
    }
}
