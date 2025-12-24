// See https://aka.ms/new-console-template for more information
using MailKit.Net.Smtp;
using MailKit.Security;

using MimeKit;

using System.Text;

Console.WriteLine("Starting DailyEnglishLesson.exe...");

string apiUrl = "https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash-lite:generateContent";

// Required environment variables: GEMINI_API_KEY, SMTP_HOST, SMTP_PORT, SMTP_USER, SMTP_PASS, SMTP_FROM, SMTP_TO
string apiKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY")
    ?? throw new InvalidOperationException("GEMINI_API_KEY is not set");
string smtpHost = Environment.GetEnvironmentVariable("SMTP_HOST")
    ?? throw new InvalidOperationException("SMTP_HOST is not set");
int smtpPort = int.TryParse(Environment.GetEnvironmentVariable("SMTP_PORT"), out var p) ? p : 587;
string smtpUser = Environment.GetEnvironmentVariable("SMTP_USER")
    ?? throw new InvalidOperationException("SMTP_USER is not set");
string smtpPass = Environment.GetEnvironmentVariable("SMTP_PASS")
    ?? throw new InvalidOperationException("SMTP_PASS is not set");
string smtpFrom = Environment.GetEnvironmentVariable("SMTP_FROM")
    ?? throw new InvalidOperationException("SMTP_FROM is not set");
string smtpTo = Environment.GetEnvironmentVariable("SMTP_TO")
    ?? throw new InvalidOperationException("SMTP_TO is not set");

// Enhanced prompt with IPA requirement and stricter formatting
string prompt = @"Role & goal
You are an expert English instructor specializing in natural, advanced-level English for adult learners. Your task is to generate one short daily English lesson suitable for a quick email that can be read in under one minute.

Audience
The learner is an advanced non-native speaker aiming for native-like fluency and high-stakes exams (e.g. IELTS Band 9). Avoid beginner explanations.

Lesson constraints
- Focus on one item only: a phrase, collocation, or subtle usage point
- Prioritize natural spoken and written English, not textbook language
- Avoid rare or obscure expressions
- Avoid slang unless it is widely used and neutral in tone

Required structure (strict)
Produce the lesson using exactly this format:

Today's [word/phrase/usage]: [the actual phrase] /IPA transcription/

Meaning:
[One clear sentence explaining the meaning]

Example:
[One natural example sentence]

Common mistake:
❌ [Incorrect usage example]
✅ [Correct usage example]

Natural tip:
[One practical insight about usage, register, or context]

Style rules
- Concise and confident
- No emojis
- No meta commentary
- No greetings or sign-offs
- No markdown symbols other than ❌ and ✅
- Plain text only
- IPA transcription must be in slashes immediately after the phrase

Quality bar
Every example sentence must sound like something a well-educated native speaker would actually say in professional or daily life.";

var requestPayload = new
{
    contents = new[]
    {
        new
        {
            parts = new[]
            {
                new { text = prompt }
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

var response = await httpClient.SendAsync(request);
response.EnsureSuccessStatusCode();

var responseContent = await response.Content.ReadAsStringAsync();
using var document = System.Text.Json.JsonDocument.Parse(responseContent);
var generatedText = document
    .RootElement
    .GetProperty("candidates")[0]
    .GetProperty("content")
    .GetProperty("parts")[0]
    .GetProperty("text")
    .GetString();

Console.WriteLine("Generated Daily English Lesson:");
Console.WriteLine(generatedText);

// Build professional HTML email with bold, large title
var emailBody = new StringBuilder();
emailBody.AppendLine("<html><body style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;'>");
emailBody.AppendLine("<div style='border-bottom: 2px solid #333; padding-bottom: 10px; margin-bottom: 20px;'>");
emailBody.AppendLine("<h2 style='margin: 0; color: #333;'>DAILY ENGLISH LESSON</h2>");
emailBody.AppendLine($"<p style='margin: 5px 0 0 0; color: #666;'>{DateTime.UtcNow:dddd, MMMM d, yyyy}</p>");
emailBody.AppendLine("</div>");

// Extract the phrase from generatedText (assumes format: "Today's [type]: [phrase] /IPA/")
var lines = generatedText?.Split('\n') ?? Array.Empty<string>();
var firstLine = lines.FirstOrDefault() ?? "";
var restOfContent = string.Join("\n", lines.Skip(1));

emailBody.AppendLine($"<div style='font-size: 22px; font-weight: bold; margin-bottom: 15px; color: #000;'>{System.Security.SecurityElement.Escape(firstLine)}</div>");
emailBody.AppendLine($"<pre style='font-family: Arial, sans-serif; white-space: pre-wrap; line-height: 1.6;'>{System.Security.SecurityElement.Escape(restOfContent)}</pre>");

emailBody.AppendLine("<div style='border-top: 2px solid #333; margin-top: 20px; padding-top: 10px;'></div>");
emailBody.AppendLine("</body></html>");

// Build email message
var message = new MimeMessage();
message.From.Add(MailboxAddress.Parse(smtpFrom));
message.To.Add(MailboxAddress.Parse(smtpTo));
message.Subject = $"Daily English Lesson — {DateTime.UtcNow:MMM d, yyyy}";
message.Body = new TextPart("html") { Text = emailBody.ToString() };  // Changed to "html"

// Send email via SMTP
try
{
    using var smtp = new SmtpClient();
    await smtp.ConnectAsync(smtpHost, smtpPort, SecureSocketOptions.StartTls);
    await smtp.AuthenticateAsync(smtpUser, smtpPass);
    await smtp.SendAsync(message);
    await smtp.DisconnectAsync(true);
    Console.WriteLine("✓ Email sent successfully.");
}
catch (Exception ex)
{
    Console.Error.WriteLine($"✗ Failed to send email: {ex.Message}");
    throw;
}

Console.WriteLine("DailyEnglishLesson.exe finished.");