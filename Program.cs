// See https://aka.ms/new-console-template for more information
using System.Text;

Console.WriteLine("Sarting DailyEnglishLesson.exe...");
string apiUrl = "https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash-lite:generateContent";
// ExternalApi:GeminiApiKey
string apiKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY")
    ?? throw new InvalidOperationException("GEMINI_API_KEY is not set");
string prompt = @"Role & goal You are an expert English instructor specializing in natural, advanced-level English for adult learners. Your task is to generate one short daily English lesson suitable for a quick email that can be read in under one minute. Audience The learner is an advanced non-native speaker aiming for native-like fluency and high-stakes exams (e.g. IELTS Band 9). Avoid beginner explanations. Lesson constraints Focus on one item only: a phrase, collocation, or subtle usage point Prioritize natural spoken and written English, not textbook language Avoid rare or obscure expressions Avoid slang unless it is widely used and neutral in tone Required structure (strict) Produce the lesson using exactly this format: Today’s [word/phrase/usage]: Meaning: Example: Common mistake: ❌ ✅ Natural tip: Style rules Concise and confident No emojis No meta commentary No greetings or sign-offs No markdown symbols other than ❌ and ✅ Plain text only Quality bar Every example sentence must sound like something a well-educated native speaker would actually say in professional or daily life.";
var requestBody = new
{
    prompt = prompt,
    maxTokens = 100
};
var requestPayload = new
{
    contents = new[]
    {
                    new
                    {
                        parts = new[]
                        {
                            new
                            {
                                text = prompt
                            }
                        }
                    }
                }
};
using var httpClient = new HttpClient();
var jsonPayload = System.Text.Json.JsonSerializer.Serialize(requestPayload);
var request = new HttpRequestMessage(HttpMethod.Post, $"{apiUrl}?key={apiKey}")
{
    Content = new StringContent(jsonPayload, Encoding.UTF8, "application/json")
};
var response = httpClient.SendAsync(request).Result; 
response.EnsureSuccessStatusCode();
var responseContent = response.Content.ReadAsStringAsync().Result;
using var document = System.Text.Json.JsonDocument.Parse(responseContent);
// example ooutput: "{\n  \"candidates\": [\n    {\n      \"content\": {\n        \"parts\": [\n          {\n            \"text\": \"Today’s phrase: borne out by\\nMeaning: confirmed or supported by something (evidence, facts, research, etc.)\\nExample: The company’s recent financial struggles were borne out by the quarterly report.\\nCommon mistake: ❌ This information was borne out of our findings. ✅ This information was borne out by our findings.\\nNatural tip: Use \\\"borne out by\\\" to show how existing evidence supports a conclusion or idea.\"\n          }\n        ],\n        \"role\": \"model\"\n      },\n      \"finishReason\": \"STOP\",\n      \"index\": 0\n    }\n  ],\n  \"usageMetadata\": {\n    \"promptTokenCount\": 212,\n    \"candidatesTokenCount\": 89,\n    \"totalTokenCount\": 301,\n    \"promptTokensDetails\": [\n      {\n        \"modality\": \"TEXT\",\n        \"tokenCount\": 212\n      }\n    ]\n  },\n  \"modelVersion\": \"gemini-2.5-flash-lite\",\n  \"responseId\": \"aFRLab34GrOwvr0PjMGb0Qk\"\n}\n"
var generatedText = document
    .RootElement
    .GetProperty("candidates")[0]
    .GetProperty("content")
    .GetProperty("parts")[0]
    .GetProperty("text")
    .GetString();
Console.WriteLine("Generated Daily English Lesson:");
Console.WriteLine(generatedText);
Console.WriteLine("DailyEnglishLesson.exe finished.");
