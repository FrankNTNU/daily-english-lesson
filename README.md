
# Daily English Lesson

An automated C# console application that generates and emails personalized daily English lessons to multiple recipients using Google's Gemini AI.

## Features

- **AI-Generated Lessons**: Uses Gemini 2.5 Flash Lite to create daily English lessons focused on phrases, collocations, and subtle usage points
- **CEFR Level Customization**: Tailors content difficulty to each recipient's Common European Framework of Reference (CEFR) level
- **Multi-Recipient Support**: Sends personalized lessons to multiple email addresses in a single run
- **Professional Email Format**: HTML emails with bold, large headings and clean layout, plus plain-text fallback
- **IPA Transcription**: Includes phonetic transcriptions for pronunciation guidance
- **Efficient SMTP**: Reuses a single SMTP connection for all emails

## Requirements

- .NET 6.0 or higher
- NuGet packages:
  - `MailKit`
  - `MimeKit`

## Environment Variables

Configure the following environment variables before running:

| Variable | Description | Example |
|----------|-------------|---------|
| `GEMINI_API_KEY` | Your Google Gemini API key | `AIza...` |
| `SMTP_HOST` | SMTP server hostname | `smtp.gmail.com` |
| `SMTP_PORT` | SMTP server port | `587` |
| `SMTP_USER` | SMTP authentication username | `user@gmail.com` |
| `SMTP_PASS` | SMTP authentication password | `your-app-password` |
| `SMTP_FROM` | Sender email address | `lessons@example.com` |
| `SMTP_TO` | Recipient email(s) (comma/semicolon/pipe-separated) | `student1@example.com,student2@example.com` |
| `CEFR_LEVEL` | CEFR level(s) (comma/semicolon/pipe-separated or single) | `B2,C1` or `C1` |

### CEFR Level Mapping

- **Single CEFR value**: Applied to all recipients (e.g., `CEFR_LEVEL=C1`)
- **Multiple CEFR values**: Mapped 1:1 with recipients (e.g., `CEFR_LEVEL=B2,C1` for two recipients)
- **Fewer CEFRs than recipients**: Last CEFR value is repeated
- **More CEFRs than recipients**: Extra values are ignored

Supported CEFR levels: `A1`, `A2`, `B1`, `B2`, `C1`, `C2`

## Installation

1. Clone the repository
2. Install dependencies:
   ```bash
   dotnet restore
   ```
3. Set environment variables (see above)
4. Build the application:
   ```bash
   dotnet build
   ```

## Usage

Run the application:

```bash
dotnet run
```

Or compile and run the executable:

```bash
dotnet publish -c Release
./bin/Release/net6.0/DailyEnglishLesson
```

### Scheduling

For daily automated lessons, schedule the application using:

**Linux/macOS (cron)**:
```bash
# Edit crontab
crontab -e

# Run daily at 8:00 AM
0 8 * * * /path/to/DailyEnglishLesson >> /var/log/english-lesson.log 2>&1
```

**Windows (Task Scheduler)**:
1. Open Task Scheduler
2. Create Basic Task
3. Set trigger to "Daily" at your preferred time
4. Set action to "Start a program" → path to `DailyEnglishLesson.exe`

## Lesson Format

Each lesson follows this structure:

```
Today's [type]: [phrase] /IPA transcription/

Meaning:
[Clear explanation]

Example:
[Natural usage example]

Common mistake:
❌ [Incorrect usage]
✅ [Correct usage]

Natural tip:
[Practical insight about usage, register, or context]
```

## Example Output

```
Today's phrase: to take something at face value /teɪk ˈsʌmθɪŋ æt feɪs ˈvæljuː/

Meaning:
To accept information or a statement as true without questioning it or looking for hidden meaning.

Example:
He said the delay was due to technical issues, and I took it at face value—I didn't realize there were budget problems.

Common mistake:
❌ I take his words at face value, so I believe everything.
✅ You can't take everything politicians say at face value.

Natural tip:
This phrase often appears in contexts where someone is being naive or where skepticism might be warranted.
```

## Gmail Setup

If using Gmail SMTP:

1. Enable 2-factor authentication on your Google account
2. Generate an App Password: [Google Account → Security → 2-Step Verification → App passwords](https://myaccount.google.com/apppasswords)
3. Use the app password as `SMTP_PASS`
4. Set `SMTP_HOST=smtp.gmail.com` and `SMTP_PORT=587`

## Troubleshooting

**"GEMINI_API_KEY is not set"**  
→ Ensure environment variable is set correctly

**"Failed to send email: Authentication failed"**  
→ Check SMTP credentials and use app-specific passwords if required

**"Invalid recipient skipped"**  
→ Verify email addresses in `SMTP_TO` are properly formatted

**Lessons seem too easy/hard**  
→ Adjust `CEFR_LEVEL` for each recipient

## License

MIT License - feel free to modify and distribute

## Contributing

Pull requests welcome! Please ensure lessons maintain high quality and natural English usage.

## Support

For issues or questions, please open a GitHub issue.
