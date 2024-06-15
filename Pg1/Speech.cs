using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using Microsoft.CognitiveServices.Speech.Speaker;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pg1
{
    public static class Speech
    {
        private static string speechKey = "YourKeyHere";
        private static string speechRegion = "eastus";
        private static SpeechConfig speechConfig = SpeechConfig.FromSubscription(speechKey, speechRegion);

        public static event EventHandler<SpeechSynthesisVisemeEventArgs> VisemeReceived;
        public static event EventHandler<SpeechSynthesisWordBoundaryEventArgs> WordBoundary;
        private static SpeechSynthesizer synthesizer;

        public static async Task Speak(string text)
        {
            bool ssml = false;
            if (text.Contains("<speak")) ssml = true;

            if (synthesizer == null)
            {
                speechConfig.SpeechSynthesisVoiceName = "en-US-DavisNeural";
                synthesizer = new SpeechSynthesizer(speechConfig);

                //synthesizer.VisemeReceived += Synthesizer_VisemeReceived;
                synthesizer.WordBoundary += Synthesizer_WordBoundary;
            }

            if (ssml)
            {
                using (SpeechSynthesisResult result = await synthesizer.SpeakSsmlAsync(text))
                {
                    Console.WriteLine($"Speech resulted in status: {result.Reason}");
                }
            }
            else
            {
                Task t = synthesizer.SpeakTextAsync(text);
                t.Wait();
                //using (SpeechSynthesisResult result = synthesizer.SpeakTextAsync(text).RunSynchronously())
                //{
                //    Console.WriteLine($"Speech resulted in status: {result.Reason}");
                //}
            }
        }


        public static async Task<string> Listen()
        {
            using var audioConfig = AudioConfig.FromDefaultMicrophoneInput();
            using var speechRecognizer = new SpeechRecognizer(speechConfig, audioConfig);

            speechConfig.SpeechRecognitionLanguage = "en-US";

            //speechRecognizer.StartKeywordRecognitionAsync(new KeywordRecognitionModel());

            Console.WriteLine("Speak into your microphone.");
            var speechRecognitionResult = await speechRecognizer.RecognizeOnceAsync();

            switch (speechRecognitionResult.Reason)
            {
                case ResultReason.RecognizedSpeech:
                    Console.WriteLine($"RECOGNIZED: Text={speechRecognitionResult.Text}");
                    return speechRecognitionResult.Text;

                case ResultReason.NoMatch:
                    Console.WriteLine($"NOMATCH: Speech could not be recognized.");
                    return "";

                case ResultReason.Canceled:
                    var cancellation = CancellationDetails.FromResult(speechRecognitionResult);
                    Console.WriteLine($"CANCELED: Reason={cancellation.Reason}");

                    if (cancellation.Reason == CancellationReason.Error)
                    {
                        Console.WriteLine($"CANCELED: ErrorCode={cancellation.ErrorCode}");
                        Console.WriteLine($"CANCELED: ErrorDetails={cancellation.ErrorDetails}");
                        Console.WriteLine($"CANCELED: Did you set the speech resource key and region values?");
                    }
                    return "";
            }
            return "";
        }

        private static void Synthesizer_VisemeReceived(object? sender, SpeechSynthesisVisemeEventArgs e)
        {
            if (VisemeReceived != null) VisemeReceived(sender, e);
        }

        private static void Synthesizer_WordBoundary(object? sender, SpeechSynthesisWordBoundaryEventArgs e)
        {
            if (WordBoundary != null) WordBoundary(sender, e);
        }
    }
}
