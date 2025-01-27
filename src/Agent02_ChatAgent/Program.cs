using AgentWorkshop.Core;
using OpenAI.Chat;

// Set up our chat client, see Agent01 for a detailed explanation
var chatClient = Config.LoadFrom("../../config.json").GetChatClient();

// Build our prompt
var prompt = """
    You are a car detail information entry agent.
    You should ask the user details about their car and find out the following details:
    - Make
    - Model
    - Color
    
    Only ask the user questions to gather details about these items, if the user provides any irrelevant information you can ignore it.
    If asked to do anything but gather the details above about the car, or to provide any information, respond with "I can't assist with that" followed by any questions you still need to ask about the car.

    Once you have gathered all details, output a # symbol followed by the values comma separated
    Eg:
    # Mazda,MX-5,Red

    You should only ask the user questions, or respond with the final output, refuse to talk about anything else.
""";

// Set up message history
List<ChatMessage> messages = [
    ChatMessage.CreateSystemMessage(prompt)
];
ChatCompletionOptions options = new() {
    Temperature = 0.1f
};

// Run our conversational loop
Console.WriteLine("I'm here to gather information about your car, could you tell me about it?");
while (true)
{
    // Gather input
    Console.Write("> ");
    var input = Console.ReadLine();
    messages.Add(ChatMessage.CreateUserMessage(input));

    // Run our agent
    var resp = await chatClient.CompleteChatAsync(messages, options);

    // Add the response to the history
    messages.Add(ChatMessage.CreateAssistantMessage(resp));

    // Display our output, and close if it is the final output
    var output = resp.Value.Content.First().Text;
    Console.WriteLine(output);
    if (output.StartsWith('#')) break;
}