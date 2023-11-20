// See https://aka.ms/new-console-template for more information

using System.Diagnostics.CodeAnalysis;
using System.IO.Pipes;
using Microsoft.Extensions.Configuration;
using OpenAI.Managers;
using OpenAI.ObjectModels.RequestModels;
using OpenAI.ObjectModels;
using OpenAI;
using System.Reflection;
using System.Security.Cryptography;

var builder = new ConfigurationBuilder()
    .AddUserSecrets(Assembly.GetExecutingAssembly())
    .AddEnvironmentVariables();
var configurationRoot = builder.Build();

var key = configurationRoot.GetSection("OpenAIKey").Get<string>() ?? string.Empty;

var openAiService = new OpenAIService(new OpenAiOptions()
{
    ApiKey = key
});

string[] content = { File.ReadAllText("chapter1.txt"), File.ReadAllText("chapter2.txt"), File.ReadAllText("chapter3.txt") };

//Console.WriteLine(string.Join(", ", chapter1Titles));
Console.WriteLine("==============================================");
Console.WriteLine("   Welcome To The History Question Machine!   ");
Console.WriteLine("==============================================");
Thread.Sleep(1000);
Console.Write("\n\n");

int chapter = choseChapters();

List<String> previousQuestions = new List<String>();

while (true)
{
    Console.WriteLine("\n\nCHAPTER " + chapter);

    Console.Write("\nWhat question would you like to ask? (Type \"Chapters\" to select a different chapter, and type \"Test\" to enter test mode)\n>");
    String input = Console.ReadLine();
    Console.WriteLine("\n");

    if (input == "Chapters" || input == "chapters")
    {
        chapter = choseChapters();
    }

    //Process for test mode
    else if(input == "Test" || input == "test")
    {
        while (true)
        {

            Console.WriteLine("\nYou are in test mode.");
            Console.WriteLine ("--------------------------------------------------------------------------------------------------------");
            Console.Write("Q. ");

            var question = "";

            //Console.WriteLine("Previous Questions: " + String.Join("\n", previousQuestions));

            //Generates the question for the user.
            var questionMaker = openAiService.ChatCompletion.CreateCompletionAsStream(new ChatCompletionCreateRequest
            {
                Messages = new List<ChatMessage>
                {
                    new(StaticValues.ChatMessageRoles.User,
                        $"You are a History teacher helping a student with review for the upcoming test. You will provide the student with a singular question ONLY relating to the chapter provided. The question MUST be related to the chapter, and be something that I could reasonably answer after having read the chapter. The question can from any part of the chapter, even the end. DO NOT ASK QUEStIONS SIMILAR TO THE ONES PREVIOUSLY ASKED. This is a list of all questions you have previously asked: ```\n{String.Join("\n", previousQuestions)}\n```Here is access to the chapter: ```\n{content[chapter - 1]}\n``` "),
                    //new(StaticValues.ChatMessageRoles.User, input),
                },
                Model = Models.Gpt_3_5_Turbo_16k,
                //MaxTokens = 100
            });
            await foreach (var completion in questionMaker)
            {
                if (completion.Successful)
                {
                    String temp = (completion.Choices.First().Message.Content);
                    Console.Write(temp);
                    question += temp;


                }
                else
                {
                    if (completion.Error == null)
                    {
                        throw new Exception("Unknown Error");
                    }

                    Console.WriteLine($"{completion.Error.Code}: {completion.Error.Message}");
                }
            }

            Console.Write("\n\nWhat is your answer? (Type \"Back\" to go to the previous menu)\n\n A. ");

            String answer = Console.ReadLine();

            previousQuestions.Add(question);

            if (answer == "Back" || answer == "back")
            {
                break;
            }


            var answerChecker = openAiService.ChatCompletion.CreateCompletionAsStream(new ChatCompletionCreateRequest
            {
                Messages = new List<ChatMessage>
                {
                    new(StaticValues.ChatMessageRoles.User,
                        $"You are a History teacher helping me with review for the upcoming test. You asked me this question: ```\n{question}\n```I responded with this answer: ```\n{answer}\n``` Verify if this response is correct or not. If the answer is incorrect or only partially right, explain why. Explanations do not need to be longer than 30 words. Determine whether these answers are right or wrong using your own knowledge, as well as the provided textbook chapter. If the information is not mentioned in the textbook, refer to your own knowledge. Here is the chapter: ```\n{content[chapter - 1]}\n```"),
                    //new(StaticValues.ChatMessageRoles.User, input),
                },
                Model = Models.Gpt_3_5_Turbo_16k,
                MaxTokens = 250
            });

            await foreach (var completion in answerChecker)
            {
                if (completion.Successful)
                {
                    Console.Write(completion.Choices.First().Message.Content);
                }
                else
                {
                    if (completion.Error == null)
                    {
                        throw new Exception("Unknown Error");
                    }

                    Console.WriteLine($"{completion.Error.Code}: {completion.Error.Message}");
                }
            }
        }
    }

    //Process for letting the user ask questions.
    else
    {
        //Makes the bot process and answer the question using the context given
        var completionResult = openAiService.ChatCompletion.CreateCompletionAsStream(new ChatCompletionCreateRequest
        {
            Messages = new List<ChatMessage>
            {
                new(StaticValues.ChatMessageRoles.User, $"You are a History teacher giving answers to questions from the latest history chapter. You need to answer my questions using information from your experience as a history teacher and from the a provided version of the chapter. Here is the abridged version of the chapter: ```\n{content[chapter - 1]}\n``` "),
                //new(StaticValues.ChatMessageRoles.User, $"You will be provided with a history textbook chapter. This chapter needs to be condensed into only the most important information. Please create a document of around 10000 - 15000 characters with all of the most important details and information. You can use headings and titles to break up the document. It is expected that the newly created document will be very long. Here is the chapter: ```\n{content[chapter - 1]}\n``` "),
                new(StaticValues.ChatMessageRoles.User, "Make all answers short while still giving valuable information. The answers do not need to be longer than 50 words. Make sure you point out how the to the question relates back to the bigger idea of the picture of the given chapter, like how it effects other things mentioned in the chapter."),
                new(StaticValues.ChatMessageRoles.User, input),
            },
            Model = Models.Gpt_3_5_Turbo_16k,
            //MaxTokens = 100
        });

        await foreach (var completion in completionResult)
        {
            if (completion.Successful)
            {
                Console.Write(completion.Choices.First().Message.Content);
            }
            else
            {
                if (completion.Error == null)
                {
                    throw new Exception("Unknown Error");
                }

                Console.WriteLine($"{completion.Error.Code}: {completion.Error.Message}");
            }
        }
    }

}



    


static int choseChapters()
{

    Console.WriteLine("What Chapter Would You Like To Choose From?\n");

    Thread.Sleep(500);

    Console.WriteLine("Chapter 1: New World Beginnings");
    Thread.Sleep(10);
    Console.WriteLine("Chapter 2: The Planting of English America");
    Thread.Sleep(10);
    Console.WriteLine("Chapter 3: Settling The Northern Colonies");

    Console.Write("\nType In A Number:");

    int returnValue = int.Parse(Console.ReadLine());

    Console.Clear();

    return returnValue;
}