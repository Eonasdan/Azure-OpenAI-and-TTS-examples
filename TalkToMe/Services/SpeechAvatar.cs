using System.Text.Json;
using System.Text;
using JetBrains.Annotations;

namespace TalkToMe.Services;

public interface ISpeechAvatarService
{
    Task<string?> SubmitSynthesis(string text);
    Task<(string? status, string? url)> GetJobStatus(string jobId);
}

public class SpeechAvatarService : ISpeechAvatarService
{
    private static readonly HttpClient Client = new();

    private readonly JsonSerializerOptions _jsonSerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public SpeechAvatarService(IConfiguration configuration)
    {
        Client.BaseAddress =
            new Uri(
                $"https://{configuration["Avatar:Region"]}.customvoice.api.speech.microsoft.com/api/texttospeech/3.1-preview1/batchsynthesis/talkingavatar/");
        Client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", configuration["Avatar:Key"]);
    }

    public async Task<string?> SubmitSynthesis(string text)
    {
        var payload = new SpeechAvatarRequest.SynthesisPayload
        {
            Inputs = [new SpeechAvatarRequest.Input(text)]
        };
        var jsonPayload = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

        var response = await Client.PostAsync("", jsonPayload);

        if (response.IsSuccessStatusCode)
        {
            Console.Write("Batch avatar synthesis job submitted successfully");
            var result = await response.Content.ReadAsStringAsync();
            var jobResponse =
                JsonSerializer.Deserialize<SpeechAvatarJobResponse.JobPostResponse>(result, _jsonSerializerOptions);
            var jobId = jobResponse!.Id!;
            Console.Write($"Job ID: {jobId}");
            return jobId;
        }

        Console.Write($"Failed to submit batch avatar synthesis job: {response.StatusCode}");
        return null;
    }
    
    public async Task<(string? status, string? url)> GetJobStatus(string jobId)
    {
        return await GetSynthesis(jobId);
    }

    private async Task<(string? status, string? url)> GetSynthesis(string? jobId)
    {
        var response = await Client.GetAsync(jobId);

        if (response.IsSuccessStatusCode)
        {
            var result = await response.Content.ReadAsStringAsync();
            var synthesisJob =
                JsonSerializer.Deserialize<SpeechAvatarJobResponse.JobResponse>(result, _jsonSerializerOptions);

            if (synthesisJob?.Status != "Succeeded") return (synthesisJob?.Status, null);
            
            var outputsResult = synthesisJob.Outputs.Result;
            Console.Write($"Batch synthesis job succeeded, download URL: {outputsResult}");
            return (synthesisJob.Status, outputsResult);

        }

        Console.Write($"Failed to get batch synthesis job: {response.StatusCode}");
        return("Failed", null) ;
    }

    public class SpeechAvatarRequest
    {
        [PublicAPI]
        public class SynthesisPayload
        {
            public string DisplayName { get; set; } = "Simple avatar synthesis";
            public string Description { get; set; } = "Simple avatar synthesis description";
            public string TextType { get; set; } = "PlainText";
            public SynthesisConfigVoice SynthesisConfig { get; set; } = new();
            public List<Input> Inputs { get; set; } = default!;
            public Properties Properties { get; set; } = new();
        }

        [PublicAPI]
        public class SynthesisConfigVoice
        {
            public string Voice { get; set; } = "en-US-JennyNeural";
        }

        [PublicAPI]
        public class Input(string text)
        {
            public string Text { get; set; } = text;
        }

        [PublicAPI]
        public class Properties
        {
            public bool Customized { get; }
            public string TalkingAvatarCharacter { get; } = "lisa";
            public string TalkingAvatarStyle { get; } = "graceful-sitting";
            public string VideoFormat { get; } = "webm";
            public string VideoCodec { get; } = "vp9";
            public string SubtitleType { get; } = "soft_embedded";
            public string BackgroundColor { get; } = "#212529";
        }
    }

    public class SpeechAvatarJobResponse
    {
        public class JobPostResponse
        {
            public string? Id { get; set; }
            public string? TextType { get; set; }
            public CustomVoices CustomVoices { get; set; } = new();
            public SynthesisConfig SynthesisConfig { get; set; } = new();
            public JobPostProperties Properties { get; set; } = new();
            public string? LastActionDateTime { get; set; }
            public string? Status { get; set; }
            public string? CreatedDateTime { get; set; }
            public string? DisplayName { get; set; }
            public string? Description { get; set; }
        }

        public class JobResponse : JobPostResponse
        {
            public Outputs Outputs { get; set; } = new();
        }

        public class SynthesisConfig
        {
            public string? Voice { get; set; }
        }

        public class CustomVoices
        {
        }

        public class JobPostProperties
        {
            public string? TimeToLive { get; set; }
            public string? OutputFormat { get; set; }
            public string? TalkingAvatarCharacter { get; set; }
            public string? TalkingAvatarStyle { get; set; }
            public int KBitrate { get; set; }
            public string? VideoFormat { get; set; }
            public string? VideoCodec { get; set; }
            public string? SubtitleType { get; set; }
            public string? BackgroundColor { get; set; }
            public bool Customized { get; set; }
        }

        public class JobResponseProperties : JobPostProperties
        {
            public int AudioSize { get; set; }
            public int DurationInTicks { get; set; }
            public int SucceededAudioCount { get; set; }
            public string? Duration { get; set; }
            public BillingDetails BillingDetails { get; set; } = new();
        }

        public class BillingDetails
        {
            public int CustomNeural { get; set; }
            public int Neural { get; set; }
        }

        public class Outputs
        {
            public string? Result { get; set; }
            public string? Summary { get; set; }
        }
    }
}