using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;

namespace TalkToMe.Services;

public interface ISpeechSynthesisService
{
    Task<byte[]?> TextToSpeech(string prompt);
    Task<byte[]?> TextToSpeechSSML(string prompt);
    Task<SpeechSynthesisServiceService.SpeechResult<string?>> SpeechRecognizeAsync(AudioConfig audioConfig);
}

public class SpeechSynthesisServiceService : ISpeechSynthesisService, IDisposable
{
    private readonly SpeechSynthesizer _synthesizer;
    private readonly SpeechConfig? _config;

    public SpeechSynthesisServiceService(IConfiguration configuration)
    {
        _config = SpeechConfig
            .FromSubscription(configuration["SpeechAI:Key"]!, configuration["SpeechAI:Region"]!);
        _config.SpeechSynthesisVoiceName = "en-US-JennyNeural";
        _config.SpeechRecognitionLanguage = "en-US";
        _config.SetSpeechSynthesisOutputFormat(SpeechSynthesisOutputFormat.Audio16Khz32KBitRateMonoMp3);
        _synthesizer = new SpeechSynthesizer(_config);
    }

    public async Task<byte[]?> TextToSpeech(string prompt)
    {
        using var result = await _synthesizer.SpeakTextAsync(prompt);
        switch (result.Reason)
        {
            case ResultReason.SynthesizingAudioCompleted:
                return result.AudioData;
            case ResultReason.Canceled:
            {
                var cancellation = SpeechSynthesisCancellationDetails.FromResult(result);
                Console.WriteLine($"CANCELED: Reason={cancellation.Reason}");

                if (cancellation.Reason == CancellationReason.Error)
                {
                    Console.WriteLine($"CANCELED: ErrorCode={cancellation.ErrorCode}");
                    Console.WriteLine($"CANCELED: ErrorDetails=[{cancellation.ErrorDetails}]");
                }

                break;
            }
        }

        return null;
    }

    public async Task<byte[]?> TextToSpeechSSML(string prompt)
    {
        using var result = await _synthesizer.SpeakSsmlAsync(prompt);
        switch (result.Reason)
        {
            case ResultReason.SynthesizingAudioCompleted:
                return result.AudioData;
            case ResultReason.Canceled:
            {
                var cancellation = SpeechSynthesisCancellationDetails.FromResult(result);
                Console.WriteLine($"CANCELED: Reason={cancellation.Reason}");

                if (cancellation.Reason == CancellationReason.Error)
                {
                    Console.WriteLine($"CANCELED: ErrorCode={cancellation.ErrorCode}");
                    Console.WriteLine($"CANCELED: ErrorDetails=[{cancellation.ErrorDetails}]");
                }

                break;
            }
        }

        return null;
    }

    public async Task<SpeechResult<string?>> SpeechRecognizeAsync(AudioConfig audioConfig)
    {
        using var speechRecognize = new SpeechRecognizer(_config, audioConfig);
        var result = await speechRecognize.RecognizeOnceAsync();
        string? text = null;
        switch (result.Reason)
        {
            case ResultReason.RecognizedSpeech:
                text = result.Text;
                break;
            case ResultReason.NoMatch:
                Console.WriteLine("No Match");
                break;
            case ResultReason.Canceled:
            {
                var cancellation = CancellationDetails.FromResult(result);
                Console.WriteLine($"CANCELED: Reason={cancellation.Reason}");

                if (cancellation.Reason == CancellationReason.Error)
                {
                    Console.WriteLine($"CANCELED: ErrorCode={cancellation.ErrorCode}");
                    Console.WriteLine($"CANCELED: ErrorDetails=[{cancellation.ErrorDetails}]");
                }

                break;
            }
        }

        return new SpeechResult<string?>(result.Reason, text);
    }

    void IDisposable.Dispose()
    {
        _synthesizer.StopSpeakingAsync();
        _synthesizer.Dispose();
        GC.SuppressFinalize(this);
    }
    
    public record SpeechResult<T>(ResultReason ResultReason, T Data);
}