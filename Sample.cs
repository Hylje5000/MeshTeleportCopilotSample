namespace CloudScripting.Sample
{
    using Microsoft.Extensions.Logging;
    using Microsoft.Mesh.CloudScripting;
    using System.Text;
    using System;
    using Azure;
    using Azure.AI.OpenAI;

    public class App : IHostedService, IAsyncDisposable
    {
        private readonly ILogger<App> _logger;
        private readonly ICloudApplication _app;
        private OpenAIClient _openAIClient;

        public App(ICloudApplication app, ILogger<App> logger, IConfiguration configuration)
        {
            _app = app;
            _logger = logger;
            Uri azureOpenAIResource = new(configuration.GetValue<string>("AZURE_OPENAI_API_URI"));
            AzureKeyCredential azureOpenAIApiKey = new(configuration.GetValue<string>("AZURE_OPENAI_API_KEY"));
            _openAIClient = new(azureOpenAIResource, azureOpenAIApiKey);
        }

        /// <inheritdoc/>
        public Task StartAsync(CancellationToken token)
        {
            // Get the Copilot button and it's InteractableNode
            var AIbutton = _app.Scene.FindFirstChild("AI_Activate", true) as TransformNode;
            var AIbuttonInteract = AIbutton.FindFirstChild<InteractableNode>();

            // Get the Travel Point Group and all of the TravelPointNodes from inside it
            var TPgroup = _app.Scene.FindFirstChild("Travel Point Group", true) as TransformNode;
            var breakout1 = TPgroup.FindFirstChild("teleportd_room1", true) as TransformNode;
            var TP1 = breakout1.FindFirstChild<TravelPointNode>();
            var breakout2 = TPgroup.FindFirstChild("teleportd_room2", true) as TransformNode;
            var TP2 = breakout2.FindFirstChild<TravelPointNode>();
            var breakout3 = TPgroup.FindFirstChild("teleportd_room3", true) as TransformNode;
            var TP3 = breakout3.FindFirstChild<TravelPointNode>();
            var breakout4 = TPgroup.FindFirstChild("teleportd_room4", true) as TransformNode;
            var TP4 = breakout4.FindFirstChild<TravelPointNode>();
            var obsdeck = TPgroup.FindFirstChild("teleportd_vt", true) as TransformNode;
            var TPobs = obsdeck.FindFirstChild<TravelPointNode>();
           
            // This code gets run when the button gets clicked
            AIbuttonInteract.Selected += (sender, args) =>
            {
                // Showing the user the dialog box to input text
                _app.ShowInputDialogToParticipantAsync("Where do you want to go? ", args.Participant).ContinueWith(async (response) =>
                {
                    // Saving the user prompt into a variable which gets pushed to the AI
                    string userprompt = response.Result;
                    var chatCompletionsOptions = new ChatCompletionsOptions()
                    {
                        // You need to change the DeploymentName to the one you have in Azure OpenAI
                        DeploymentName = "chatgpt",
                        Messages =
                        {
                            new ChatRequestSystemMessage("Your task is to figure out where users want to travel. Users can travel into one of four breakout rooms, or the observation deck. If you deduce a user wants to travel to breakout room 1, you output 'breakout1', if they want to go to breakout room 2, you output 'breakout2' and so on. If they want to go to the observation deck, you output 'deck'. If you cannot deduce where users want to travel, just say 'I'm not sure where you want to go'.' Your answer should only include the word, like 'breakout1', 'breakout3', or 'deck'. You should always just answer with the specified format without spaces or other formatting."),
                            new ChatRequestUserMessage(userprompt)
                        }
                    };

                    try
                    {
                        // Get the AI response and save it to content-variable
                        Response<ChatCompletions> aiResponse = _openAIClient.GetChatCompletions(chatCompletionsOptions);
                        ChatResponseMessage responseMessage = aiResponse.Value.Choices[0].Message;
                        _logger.LogInformation($"{ responseMessage.Content}");

                        // Transform response to lowercase to increase reliability
                        content = responseMessage.Content.ToLower();

                        // Switch case to go through every single one of the travel points and compare it to the response of the AI, includes debug logger lines!
                        switch (content)
                        {
                            case "breakout1":
                                _logger.LogInformation("User wants to travel to Breakout room 1");
                                _app.Scene.GetAvatarForParticipant(args.Participant).TravelTo(TP1);
                                break;
                            case "breakout2":
                                _logger.LogInformation("User wants to travel to Breakout room 2");
                                _app.Scene.GetAvatarForParticipant(args.Participant).TravelTo(TP2);
                                break;
                            case "breakout3":
                                _logger.LogInformation("User wants to travel to Breakout room 3");
                                _app.Scene.GetAvatarForParticipant(args.Participant).TravelTo(TP3);
                                break;
                            case "breakout4":
                                _logger.LogInformation("User wants to travel to Breakout room 4");
                                _app.Scene.GetAvatarForParticipant(args.Participant).TravelTo(TP4);
                                break;
                            case "deck":
                                _logger.LogInformation("User wants to travel to Observation deck");
                                _app.Scene.GetAvatarForParticipant(args.Participant).TravelTo(TPobs);
                                break;
                            default:
                                _logger.LogInformation("Unspecified " + content);
                                break;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogCritical(ex.Message);
                    };
                }, TaskScheduler.Default);
                
            };
        
            return Task.CompletedTask;
        }


        /// <inheritdoc/>
        public Task StopAsync(CancellationToken token)
        {
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public async ValueTask DisposeAsync()
        {
            await StopAsync(CancellationToken.None)
                .ConfigureAwait(false);

            GC.SuppressFinalize(this);
        }
    }
}
