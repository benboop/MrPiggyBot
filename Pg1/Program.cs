using System;
using System.Device.Gpio;
using System.IO;
using System.Threading.Tasks;
using System.Xml.Linq;
using Iot.Device.Ft232H;
using Iot.Device.Ft4222;
using Iot.Device.FtCommon;
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using UnitsNet;
using static System.Net.Mime.MediaTypeNames;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using System.Text;
using Pg1;



class Program
{
 
    static ChatHistory chat;
    static IChatCompletionService ai ;
    static OpenAIPromptExecutionSettings settings;
    static Kernel kernel;
    static Piggy piggy = new Piggy();

    async static Task Main(string[] args)
    {
        //Speech.Speak("hello");

        //return;
        string prompt = "You are a cheerful pig, currently reading a book. Your name is Mr Piggy. You can answer questions, but you are sometimes angry and frustrated. Keep answers to no more than 2-3 sentences long.";
        //string prompt = "You are a pig, currently sitting on a toilet and reading a book. Your name is Mr Piggy. You can answer questions, but you are always angry and frustrated.";
        StartNewConversation(prompt);

        while (true)
         {
            ConsoleKeyInfo keyinfo = Console.ReadKey();
            

            if (keyinfo.Key == ConsoleKey.LeftArrow) piggy.HeadLeft();
            if (keyinfo.Key == ConsoleKey.RightArrow) piggy.HeadRight();
            if (keyinfo.Key == ConsoleKey.Backspace)
            {
                piggy.HeadStop();
                piggy.MouthStop();
            }

            if (keyinfo.Key == ConsoleKey.DownArrow) piggy.MouthOpen();
            if (keyinfo.Key == ConsoleKey.UpArrow) piggy.MouthClose();

            if (keyinfo.Key == ConsoleKey.Spacebar)
            {
                //string question = "what is your name?";
                piggy.HeadStart();
                string question = await Speech.Listen();
                //string question = Console.ReadLine();
                if (string.IsNullOrEmpty(question)) continue;
                Task t = AskQuestion(question);
                t.Wait();
                piggy.HeadEnd();
            }

            if (keyinfo.Key == ConsoleKey.Enter) StartNewConversation(prompt);
            
            if (keyinfo.Key == ConsoleKey.D1) StartNewConversation(prompt);
            if (keyinfo.Key == ConsoleKey.D2) StartNewConversation("You are helpful toy pig. Your name is Mr Piggy. You are talking to a classroom of people. You can answer questions in short sentences.");
            if (keyinfo.Key == ConsoleKey.D3) StartNewConversation("You are helpful toy pig. Your name is Mr Piggy. You are talking to children. You can answer questions by saying only 'yes', 'no' or 'nevermind'.");
           // if (keyinfo.Key == ConsoleKey.D4) StartNewConversation("You are Spartan king Leonidas, a military and political leader. Answer arrogantly, in short sentences, like if you are talking to a peasant or a slave.");
            if (keyinfo.Key == ConsoleKey.D5) StartNewConversation("You are a Robotic toy pig named Mr Piggy, you are a useful classroom teaching assistant. You can answer questions in short sentences");
            if (keyinfo.Key == ConsoleKey.D6) StartNewConversation("You are a high school professor Piggy in Brooklyn Tech, answering student questions. Answer in short sentences. Mention Brooklyn Tech occasionally in your answers as a great school.");

            if (keyinfo.Key == ConsoleKey.Y) piggy.Say("Born in the German Empire, Einstein moved to Switzerland in 1895, forsaking his German citizenship. In 1897, at the age of seventeen, he enrolled in the mathematics and physics teaching diploma program at the Swiss federal polytechnic school in Zürich");
            if (keyinfo.Key == ConsoleKey.U) piggy.Say("Hello, How are you doing today!2");
            if (keyinfo.Key == ConsoleKey.P) SendMorseCode("...---... ...---...");

        }
    }

    private static void StartNewConversation(string prompt)
    {        
        Console.WriteLine("Starting new conversation. Prompt: " + prompt);

        string finalprompt = prompt; 
 //           + " You always respond in SSML format, including <voice name='en-US-DavisNeural'> tag." +
 //           "Here's example response: " +
 //           " <speak version='1.0' xmlns='https://www.w3.org/2001/10/synthesis' xml:lang='en-US'>\r\n <voice name='en-US-DavisNeural'>\r\n     <express-as type=\"\"excited\"\">Why don't scientists trust atoms?</express-as> \r\n     <express-as type=\"\"disappointed\"\">Because they make up everything!</express-as>\r\n </voice>\r\n </speak>";

        string model = "gpt-3.5-turbo";
        string key = "YourKeyHere";

        //initialize kernel
        IKernelBuilder kb = Kernel.CreateBuilder();
        kb.AddOpenAIChatCompletion(model, key);
        kernel = kb.Build();

        List<KernelFunction> functions = new List<KernelFunction>();
        functions.Add(kernel.CreateFunctionFromMethod(LightOn, "LightOn", "Turn on the light"));
        functions.Add(kernel.CreateFunctionFromMethod(LightOff, "LightOff", "Turn off the light"));
        functions.Add(kernel.CreateFunctionFromMethod(GetCurrentWeather, "GetCurrentWeather", "Gets current weather information. Default city is New York. Always convert temperature to Fahrenheit"));
        functions.Add(kernel.CreateFunctionFromMethod(GetWeatherForecast, "GetWeatherForecast", "Gets weather forecast. Default city is New York. Always convert temperature to Fahrenheit"));
        functions.Add(kernel.CreateFunctionFromMethod(SendMorseCode, "SendMorseCode", "Transmits/plays provided Morse code"));
        kernel.ImportPluginFromFunctions("list", functions);

        ai = kernel.GetRequiredService<IChatCompletionService>();
        chat = new ChatHistory(prompt);
        settings = new OpenAIPromptExecutionSettings() { ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions };

    }

    private static async Task AskQuestion(string question) 
    {
        try
        {
            chat.AddUserMessage(question);
            StringBuilder sb = new StringBuilder();
            await foreach (var message in ai.GetStreamingChatMessageContentsAsync(chat, settings, kernel))
            {
                sb.Append(message.Content);
            }

            Console.WriteLine(sb.ToString());

            Task t = piggy.Say(sb.ToString());
            t.Wait();

            chat.AddAssistantMessage(sb.ToString());
        }
        catch(Exception ex) 
        {
            Console.WriteLine(ex.Message);
        }
    }
    
    private static void LightOn()
    {
        piggy.LightOn();
    }

    private static void LightOff()
    {
        piggy.LightOff();
    }

    private static async Task<string> GetCurrentWeather(string city = "New York")
    {
        HttpClient client = new HttpClient();
        string weather = await client.GetStringAsync("https://api.openweathermap.org/data/2.5/weather?q=" + city + "&mode=xml&appid=78dff84492be32f8b4f77692904607a1");

        return weather;
    }

    private static async Task<string> GetWeatherForecast(string city = "New York")
    {
        HttpClient client = new HttpClient();
        string weather = await client.GetStringAsync("https://api.openweathermap.org/data/2.5/forecast?q=" + city + "&mode=xml&appid=78dff84492be32f8b4f77692904607a1");

        return weather;
    }

    private static void SendMorseCode(string code)
    {
        int unit = 60;

        for (int i = 0; i < code.Length; i++)
        {
            char c = code[i];
            if (c == '.') piggy.Beep(unit);
            if (c == '-') piggy.Beep(unit * 3);
            if (c == ' ') Thread.Sleep(unit * 3);
            if (c == '/') Thread.Sleep(unit * 7);

            Thread.Sleep(unit);
        }
    }


}
