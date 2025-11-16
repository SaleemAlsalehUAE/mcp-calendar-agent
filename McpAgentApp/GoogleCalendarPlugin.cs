using Google.Apis.Auth.OAuth2;
using Google.Apis.Calendar.v3;
using Google.Apis.Calendar.v3.Data;
using Google.Apis.Services;
using Microsoft.SemanticKernel;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

public class GoogleCalendarPlugin
{
    // Cached Calendar service (reuse authenticated client)
    private CalendarService? _calendarService;

    [KernelFunction]
    [Description("Creates a new event in the user's Google Calendar.")]
    public async Task<string> CreateEventAsync(
        [Description("A brief summary or title for the event.")]
        string summary,

        [Description("The start time for the event in ISO 8601 format (e.g., 2025-11-17T13:00:00).")]
        string startTime,

        [Description("The end time for the event in ISO 8601 format (e.g., 2025-11-17T15:00:00).")]
        string endTime,

        [Description("The physical location of the event.")]
        string? location = null,

        [Description("A detailed description for the event.")]
        string? description = null,

        [Description("IANA time zone ID for the event (e.g., America/New_York, Europe/Berlin). If not provided, America/New_York is used.")]
        string timeZone = "America/New_York"
    )
    {
        try
        {
            Console.WriteLine("--- TOOL CALLED: CreateEventAsync ---");
            Console.WriteLine($"Summary: {summary}");
            Console.WriteLine($"Start: {startTime} | End: {endTime} | TZ: {timeZone}");
            Console.WriteLine($"Location: {location}");
            Console.WriteLine($"Description: {description}");

            var service = await GetCalendarServiceAsync();

            // Parse ISO 8601 strings into DateTime with proper style
            var parsedStart = DateTime.Parse(
                startTime,
                null,
                DateTimeStyles.RoundtripKind
            );

            var parsedEnd = DateTime.Parse(
                endTime,
                null,
                DateTimeStyles.RoundtripKind
            );

            var newEvent = new Event
            {
                Summary = summary,
                Location = location,
                Description = description,
                Start = new EventDateTime
                {
                    DateTime = parsedStart,
                    TimeZone = timeZone
                },
                End = new EventDateTime
                {
                    DateTime = parsedEnd,
                    TimeZone = timeZone
                }
            };

            const string calendarId = "primary";
            var createdEvent = await service.Events.Insert(newEvent, calendarId).ExecuteAsync();

            Console.WriteLine("--- TOOL SUCCEEDED ---");
            Console.WriteLine($"Created event id: {createdEvent.Id}");
            Console.WriteLine($"HTML link: {createdEvent.HtmlLink}");

            return $"Event created successfully in time zone '{timeZone}'. You can view it at: {createdEvent.HtmlLink}";
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("--- TOOL FAILED ---");
            Console.WriteLine($"An error occurred in GoogleCalendarPlugin: {ex.Message}");
            Console.ResetColor();

            return $"An error occurred while creating the event: {ex.Message}. Please check the application console for more details.";
        }
    }

    /// <summary>
    /// Authenticates with Google and creates a CalendarService instance.
    /// Handles OAuth flow and caches the service for subsequent calls.
    /// </summary>
    private async Task<CalendarService> GetCalendarServiceAsync()
    {
        if (_calendarService != null)
        {
            return _calendarService;
        }

        string[] scopes = { CalendarService.Scope.Calendar };
        UserCredential credential;

        await using (var stream = new FileStream("credentials.json", FileMode.Open, FileAccess.Read))
        {
            var clientSecrets = await GoogleClientSecrets.FromStreamAsync(stream);

            credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                clientSecrets.Secrets,
                scopes,
                "user", // token id
                CancellationToken.None
            );
        }

        _calendarService = new CalendarService(new BaseClientService.Initializer
        {
            HttpClientInitializer = credential,
            ApplicationName = "MCP Calendar Agent",
        });

        return _calendarService;
    }
}
