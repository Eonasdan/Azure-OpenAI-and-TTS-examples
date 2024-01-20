using Azure;
using Azure.AI.OpenAI;

namespace TalkToMe.Services;

public class OpenAIService(IConfiguration configuration) : IOpenAIService
{
    private readonly OpenAIClient _client = new(
        new Uri(configuration["OpenAI:Endpoint"]!),
        new AzureKeyCredential(configuration["OpenAI:Key"]!));

    private const string DefaultSystemMessage ="You're a helpful assistant";

    public async Task<string?> ChatCompletionAsync(List<ChatRequestMessage>? requestMessages = null,
        string? prompt = "", string? systemMessage = "")
    {
        try
        {
            requestMessages ??= [];

            requestMessages.Insert(0,new ChatRequestSystemMessage(
                string.IsNullOrEmpty(systemMessage) ? DefaultSystemMessage : systemMessage)
            );

            if (!string.IsNullOrEmpty(prompt)) requestMessages.Add(new ChatRequestUserMessage(prompt));

            var responseWithoutStream = await _client.GetChatCompletionsAsync(
                new ChatCompletionsOptions(configuration["OpenAI:Deployment"], requestMessages)
                {
                    Temperature = (float)0.7,
                    MaxTokens = 800,
                    NucleusSamplingFactor = (float)0.95,
                    FrequencyPenalty = 0,
                    PresencePenalty = 0,
                });

            var response = responseWithoutStream.Value;

            return response.Choices[0].Message.Content;
        }
        catch (Exception e)
        {
            //todo deal with this and return an error to the user
            Console.WriteLine(e.Message);
        }

        return null;
    }
}

public interface IOpenAIService
{
    Task<string?> ChatCompletionAsync(List<ChatRequestMessage>? requestMessages = null, string? prompt = "", string? systemMessage = "");
}