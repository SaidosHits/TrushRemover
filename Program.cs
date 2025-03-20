using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Xml;

class Program
{
    private static readonly HttpClient client = new HttpClient();
    private static readonly string configFile = "telegram_config.json";

    static void Main(string[] args)
    {
        RunProgram().Wait();  // .NET Framework does not support async Main
    }

    static async Task RunProgram()
    {
        try
        {
            // Load existing Telegram credentials if available
            string botToken = null;
            string chatId = null;
            Console.Title = "TrushRemvoer v0.1 By @SaidosHits";
            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.WriteLine(@"
 _____  ____  _     ____  ____  _____ _      ____  _     _____ ____ 
/__ __\/  __\/ \ /\/ ___\/  __\/  __// \__/|/  _ \/ \ |\/  __//  __\
  / \  |  \/|| | |||    \|  \/||  \  | |\/||| / \|| | //|  \  |  \/|
  | |  |    /| \_/|\___ ||    /|  /_ | |  ||| \_/|| \// |  /_ |    /
  \_/  \_/\_\\____/\____/\_/\_\\____\\_/  \|\____/\__/  \____\\_/\_\
                                                                   
                             TG = @SaidosHits
                                                                    
");

            if (File.Exists(configFile))
            {
                string jsonContent = File.ReadAllText(configFile);
                var config = JsonConvert.DeserializeObject<Dictionary<string, string>>(jsonContent);
                if (config != null)
                {
                    config.TryGetValue("BotToken", out botToken);
                    config.TryGetValue("ChatId", out chatId);

                    if (!string.IsNullOrEmpty(botToken) && !string.IsNullOrEmpty(chatId))
                    {
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine($"Loaded saved Telegram credentials: BotToken={botToken.Substring(0, 5)}..., ChatId={chatId}");
                        Console.ResetColor();
                    }
                }
            }

            // Ask for Telegram credentials if not loaded or user wants to change them
            Console.WriteLine("Enter your Telegram Bot Token (press Enter to use saved or skip Telegram upload):");
            string inputToken = Console.ReadLine()?.Trim();

            if (!string.IsNullOrEmpty(inputToken))
            {
                botToken = inputToken;
                Console.WriteLine("Enter your Telegram Chat ID:");
                chatId = Console.ReadLine()?.Trim();
                if (string.IsNullOrEmpty(chatId))
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Chat ID cannot be empty. Skipping Telegram upload...");
                    Console.ResetColor();
                    botToken = null;
                }
                else
                {
                    Console.WriteLine("Would you like to save these credentials for future use? (y/n)");
                    string saveResponse = Console.ReadLine()?.Trim().ToLower();
                    if (saveResponse == "y" || saveResponse == "yes")
                    {
                        var config = new Dictionary<string, string>
                        {
                            { "BotToken", botToken },
                            { "ChatId", chatId }
                        };
                        string jsonContent = JsonConvert.SerializeObject(config, Newtonsoft.Json.Formatting.Indented);
                        File.WriteAllText(configFile, jsonContent);
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine($"Credentials saved to {configFile}");
                        Console.ResetColor();
                    }
                }
            }

            // Define patterns to filter out
            string[] unwantedPatterns = new string[]
            {
                @"r.*[dD][aA][tT][aA].*",
                @".*[jJ][oO][iI][nN].*",
                @".*1 8 7.*",
                @".*[uU][lL][uU].*",
                @".*[kK].*[eE].*[nN].*",
                @".*FULL MAIL ACCESS.*",
                @".*VALID.*",
                @".*JOIN PRIVATE CLOUD TELEGRAM CHANNEL.*",
                @".*https://t.me/.*",
                @".*DAILY UPDATE WITH NEW FILES.*",
                @".*EU / USA / CORPS / MIXED.*",
                @"^=+$",
                @".*(7.*){2,}7.*",
                @".*[hH].*[uU].*[lL].*[uU].*",
                @".*[tT][gG].*",
                @".*[cC][oO][mM][bB][oO][sS][oO][uU].*"
            };

            // Input file
            string inputFile = "combo.txt";
            if (!File.Exists(inputFile))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Error: combo.txt file not found!");
                Console.ResetColor();
                Console.ReadKey();
                return;
            }

            // Create output directory with timestamp
            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string outputDir = $"cleaned_combo_{timestamp}";
            Directory.CreateDirectory(outputDir);

            // Process the file
            List<string> cleanedLines = new List<string>();
            List<string> removedLines = new List<string>();

            string[] lines = File.ReadAllLines(inputFile);
            foreach (string line in lines)
            {
                string trimmedLine = line.Trim();
                if (string.IsNullOrEmpty(trimmedLine))
                    continue;

                bool isUnwanted = false;
                foreach (string pattern in unwantedPatterns)
                {
                    if (Regex.IsMatch(trimmedLine, pattern, RegexOptions.IgnoreCase))
                    {
                        removedLines.Add(trimmedLine);
                        isUnwanted = true;
                        break;
                    }
                }

                if (!isUnwanted)
                {
                    cleanedLines.Add(trimmedLine);
                }
            }

            // Save cleaned results
            string outputFile = Path.Combine(outputDir, $"x{cleanedLines.Count} Hotmail Fresh.txt");
            File.WriteAllLines(outputFile, cleanedLines);

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"\nKept {cleanedLines.Count} clean combos");
            Console.WriteLine($"Removed {removedLines.Count} unwanted lines");
            Console.WriteLine($"Results saved to: {outputFile}");
            Console.ResetColor();

            // Send to Telegram if credentials provided
            if (!string.IsNullOrEmpty(botToken) && !string.IsNullOrEmpty(chatId))
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"Sending {Path.GetFileName(outputFile)} to Telegram...");
                Console.ResetColor();
                Task.Run(() => SendFileToTelegram(outputFile, botToken, chatId));
            }

            Console.WriteLine("\nPress any key to exit...");
            Console.ReadKey();
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"An error occurred: {ex.Message}");
            Console.ResetColor();
            Console.ReadKey();
        }
    }

    private static async Task SendFileToTelegram(string filePath, string botToken, string chatId)
    {
        try
        {
            string url = $"https://api.telegram.org/bot{botToken}/sendDocument";

            using (var form = new MultipartFormDataContent())
            {
                form.Add(new StringContent(chatId), "chat_id");
                var fileContent = new ByteArrayContent(File.ReadAllBytes(filePath));
                form.Add(fileContent, "document", Path.GetFileName(filePath));

                HttpResponseMessage response = await client.PostAsync(url, form);
                Console.WriteLine(response.IsSuccessStatusCode ? "File sent successfully!" : "Failed to send file.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error sending file: {ex.Message}");
        }
    }
}
