// See https://aka.ms/new-console-template for more information
using MailKit.Net.Smtp;
using MailKit.Security;

using MimeKit;

using System;
using System.Linq;
using System.Net;
using System.Text;

Console.WriteLine("Starting DailyEnglishLesson.exe...");

string apiUrl = "https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash-lite:generateContent";

// Required environment variables:
// GEMINI_API_KEY, SMTP_HOST, SMTP_PORT, SMTP_USER, SMTP_PASS, SMTP_FROM,
// SMTP_TO (comma/semicolon/pipe-separated), CEFR_LEVEL (comma/semicolon/pipe-separated or single value)
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

// Parse recipients
string smtpToRaw = Environment.GetEnvironmentVariable("SMTP_TO")
    ?? throw new InvalidOperationException("SMTP_TO is not set");
string[] smtpToList = smtpToRaw
    .Split(new[] { ',', ';', '|' }, StringSplitOptions.RemoveEmptyEntries)
    .Select(s => s.Trim())
    .Where(s => !string.IsNullOrEmpty(s))
    .ToArray();

if (smtpToList.Length == 0)
    throw new InvalidOperationException("SMTP_TO must contain at least one recipient (comma/semicolon/pipe-separated).");

// Parse CEFR levels (map to recipients)
string cefrRaw = Environment.GetEnvironmentVariable("CEFR_LEVEL") ?? "C1";
string[] cefrList = cefrRaw
    .Split(new[] { ',', ';', '|' }, StringSplitOptions.RemoveEmptyEntries)
    .Select(s => s.Trim().ToUpperInvariant())
    .Where(s => !string.IsNullOrEmpty(s))
    .ToArray();

// If no valid CEFR entries, default to C1
if (cefrList.Length == 0)
    cefrList = new[] { "C1" };

// If a single CEFR provided, use it for all recipients.
// If multiple CEFRs provided but counts differ, pad with last value.
if (cefrList.Length == 1 && smtpToList.Length > 1)
{
    cefrList = Enumerable.Repeat(cefrList[0], smtpToList.Length).ToArray();
}
else if (cefrList.Length < smtpToList.Length)
{
    var padded = cefrList.ToList();
    while (padded.Count < smtpToList.Length)
        padded.Add(cefrList.Last());
    cefrList = padded.ToArray();
}
else if (cefrList.Length > smtpToList.Length)
{
    // More CEFRs than recipients: ignore extras and warn
    Console.WriteLine("Warning: more CEFR_LEVEL entries than recipients; extra CEFR values will be ignored.");
    cefrList = cefrList.Take(smtpToList.Length).ToArray();
}

Console.WriteLine($"Recipients: {smtpToList.Length}, CEFR mappings: {cefrList.Length}");

// Shared HttpClient and SmtpClient
using var httpClient = new HttpClient();
using var smtp = new SmtpClient();

try
{
    // Connect and authenticate SMTP once
    await smtp.ConnectAsync(smtpHost, smtpPort, SecureSocketOptions.StartTls);
    await smtp.AuthenticateAsync(smtpUser, smtpPass);

    for (int i = 0; i < smtpToList.Length; i++)
    {
        var recipient = smtpToList[i];
        var cefr = cefrList[i];

        Console.WriteLine($"Generating lesson for {recipient} (CEFR: {cefr})...");
        var dayOfWeek = DateTime.UtcNow.DayOfWeek.ToString();
        // Build prompt per recipient/CEFR
        string prompt = $@"Role & goal
You are an expert English instructor. Your task is to generate one short daily English lesson suitable for a quick email that can be read in under one minute.

Target CEFR level: {cefr}
This CEFR level defines the learner’s English ability.
Adjust vocabulary choice, sentence complexity, explanation depth, tone, and example difficulty to match this level precisely.
Do NOT assume the learner is advanced unless the CEFR level explicitly indicates it (C1 or C2).

Audience
The learner is an adult non-native English speaker at the specified CEFR level.
Use language that is clear, natural, and appropriate for this level.
Avoid both over-simplification and unnecessary complexity.

Lesson category rotation (STRICT)
Today is: {dayOfWeek}

You MUST follow this mapping exactly:

- Monday → Phrase  
  A fixed multi-word expression with a stable meaning appropriate to the CEFR level

- Tuesday → Collocation  
  A natural word partnership commonly used by native speakers and learnable at this level

- Wednesday → Usage  
  A grammatical, pragmatic, or usage distinction that is appropriate for this CEFR level

- Thursday → Register & tone  
  How word choice or structure changes depending on formality, context, or relationship, scaled to this level

- Friday → Precision & nuance  
  Fine distinctions suitable for the learner’s level
  (At lower CEFR levels, keep contrasts simple; at higher levels, allow subtle nuance.)

- Saturday → Contrast & refinement  
  Compare TWO different items introduced earlier this week.
  Focus on differences in meaning, tone, or usage.
  Do NOT introduce any new words, phrases, or structures.

- Sunday → Applied usage  
  Reuse ONE item introduced earlier this week.
  Show how it works naturally in a slightly longer, realistic context.
  Do NOT introduce any new words, phrases, or structures.

Rules:
- Follow today’s assigned category only
- Do NOT mix categories
- Do NOT reuse or paraphrase items that belong to other days’ categories
- On weekends, reuse ONLY items from earlier this week
- Ensure all content is appropriate to the specified CEFR level

Lesson constraints
- Focus on one clear teaching objective
- Prioritize natural spoken and written English
- Avoid rare, obscure, or gimmicky expressions
- Avoid slang unless it is widely used and appropriate for the CEFR level
- Avoid expressions that would be confusing or unnatural for this level

Required structure (STRICT)
Produce the lesson using exactly this format:

Today's [category]: [the actual word/phrase/usage] /IPA transcription/

Meaning:
[One clear sentence explaining the meaning, scaled to the CEFR level]

Example:
[One natural example sentence appropriate to the CEFR level]

Common mistake:
❌ [A typical mistake made by learners at this CEFR level]
✅ [A corrected version appropriate to this CEFR level]

Natural tip:
[One practical insight about usage, register, or context suitable for this level]

Style rules
- Clear, concise, and level-appropriate
- No emojis
- No meta commentary
- No greetings or sign-offs
- No markdown symbols other than ❌ and ✅
- Plain text only
- IPA transcription must be in slashes immediately after the item

Quality bar
All explanations and examples must sound natural while remaining fully accessible to a learner at the specified CEFR level.
";

        var requestPayload = new
        {
            contents = new[]
            {
                new { parts = new[] { new { text = prompt } } }
            }
        };

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
            .GetString() ?? "(no content)";

        Console.WriteLine($"Generated content length: {generatedText.Length}");

        // Build professional HTML email with bold, large title
        var lines = (generatedText ?? string.Empty).Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
        var firstLine = lines.FirstOrDefault(l => !string.IsNullOrWhiteSpace(l)) ?? "Daily Lesson";
        var restOfContent = string.Join("\n", lines.SkipWhile(l => string.IsNullOrWhiteSpace(l)).Skip(1));

        string EncodeHtml(string s) => WebUtility.HtmlEncode(s ?? string.Empty);

        var emailBodyBuilder = new StringBuilder();
        emailBodyBuilder.AppendLine("<html><head><meta charset='utf-8'/></head><body style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;'>");
        emailBodyBuilder.AppendLine("<div style='border-bottom: 2px solid #333; padding-bottom: 10px; margin-bottom: 20px;'>");
        emailBodyBuilder.AppendLine("<h2 style='margin: 0; color: #333;'>DAILY ENGLISH LESSON</h2>");
        emailBodyBuilder.AppendLine($"<p style='margin: 5px 0 0 0; color: #666;'>{EncodeHtml(DateTime.UtcNow.ToString("dddd, MMMM d, yyyy"))}</p>");
        emailBodyBuilder.AppendLine("</div>");
        emailBodyBuilder.AppendLine($"<div style='font-size: 22px; font-weight: bold; margin-bottom: 15px; color: #000;'>{EncodeHtml(firstLine)}</div>");
        emailBodyBuilder.AppendLine($"<pre style='font-family: Arial, sans-serif; white-space: pre-wrap; line-height: 1.6;'>{EncodeHtml(restOfContent)}</pre>");
        emailBodyBuilder.AppendLine("<div style='border-top: 2px solid #333; margin-top: 20px; padding-top: 10px;'></div>");
        emailBodyBuilder.AppendLine("</body></html>");
        string emailHtml = emailBodyBuilder.ToString();

        var plainBuilder = new StringBuilder();
        plainBuilder.AppendLine("DAILY ENGLISH LESSON");
        plainBuilder.AppendLine($"{DateTime.UtcNow:dddd, MMMM d, yyyy}");
        plainBuilder.AppendLine();
        plainBuilder.AppendLine(generatedText.Trim());
        plainBuilder.AppendLine();
        plainBuilder.AppendLine($"Target CEFR level: {cefr}");
        plainBuilder.AppendLine();
        plainBuilder.AppendLine("This message was generated automatically for educational purposes.");
        string plainBody = plainBuilder.ToString();

        // Build message for this recipient
        var message = new MimeMessage();
        message.From.Add(MailboxAddress.Parse(smtpFrom));
        try
        {
            message.To.Add(MailboxAddress.Parse(recipient));
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Warning: invalid recipient '{recipient}' skipped: {ex.Message}");
            continue;
        }
        message.Subject = $"Daily English Lesson — {DateTime.UtcNow:MMM d, yyyy}";
        var bodyBuilder = new BodyBuilder { TextBody = plainBody, HtmlBody = emailHtml };
        message.Body = bodyBuilder.ToMessageBody();

        // Send message
        try
        {
            await smtp.SendAsync(message);
            Console.WriteLine($"✓ Email sent to {recipient}");
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"✗ Failed to send to {recipient}: {ex.Message}");
        }
    }
}
finally
{
    if (smtp.IsConnected)
        await smtp.DisconnectAsync(true);
}

Console.WriteLine("DailyEnglishLesson.exe finished.");