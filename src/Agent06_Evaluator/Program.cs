using System.ComponentModel;
using System.Text.Json;
using AgentWorkshop.Core;
using OpenAI.Chat;

var chatClient = Config.LoadFrom("../../config.json").GetChatClient();

var evaluator = new Evaluator(chatClient);

var agent = new Agent(chatClient, evaluator);

// Run our agent
var output = await agent.InvokeAsync(new AgentRequest(4, 15, "Melbourne"));

Console.WriteLine(output);