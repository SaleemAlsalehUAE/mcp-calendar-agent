# 🧠 MCP Calendar Agent  
AI Agent that checks REAL weather conditions before booking events in Google Calendar using **Model Context Protocol (MCP)** and **Semantic Kernel**.

🚀 What it does  
1️⃣ Detects when the user wants to schedule an outdoor event  
2️⃣ Calls a real Weather MCP server (AccuWeather-backed)  
3️⃣ Decides if the outdoor event is possible  
4️⃣ Automatically creates a real Google Calendar event  

---

## 🔧 Tech Stack

| Layer        | Technology                                  |
|-------------|----------------------------------------------|
| LLM         | Azure OpenAI (gpt-4o / gpt-4o-agent)         |
| Agent       | Microsoft Semantic Kernel                    |
| Tools       | MCP Weather Server + Custom Calendar Plugin  |
| Weather API | AccuWeather (via MCP)                        |
| Calendar    | Google Calendar API (OAuth 2.0)              |
| Runtime     | .NET 8 C# Console App                        |

---

## 🧩 How It Works

### Agent Logic

The agent follows this workflow for **outdoor events**:

1. Ask the user for **city + country**  
2. Call `weather-get_hourly` MCP tool with that location  
3. If it’s raining, snowing, or below 10°C → suggest an **online meeting**  
4. Otherwise → create the outdoor event in **Google Calendar**  
5. Always confirm details with the user before creating the event  

### Weather MCP Tool

- Uses an MCP-compatible weather server (via `npx @timlukahorstmann/mcp-weather`)  
- Returns real hourly weather forecast for the given location  
- The agent uses this forecast to decide if the event is feasible  

### Google Calendar Integration

- Uses OAuth (opens a browser window on first run)  
- Creates real events in the user’s **primary** Google Calendar  
- Uses proper IANA time zones (e.g., `Asia/Damascus`, `America/New_York`)  

---

## 📂 Project Structure

    mcp-calendar-agent/
    ├─ McpAgentApp.sln
    ├─ .gitignore
    └─ McpAgentApp/
       ├─ Program.cs               → Agent bootstrapping & chat loop
       ├─ GoogleCalendarPlugin.cs  → Google Calendar event creation
       ├─ WeatherPlugin.cs         → Example custom weather plugin (optional)
       └─ McpAgentApp.csproj

> Note: Files like `credentials.json`, `secrets.json`, and token files are **local only** and are intentionally excluded by `.gitignore`.

---

## 🔐 Security & Secrets

This repository is designed to be **safe by default**:

- No API keys or OAuth secrets are stored in the repo  
- `.gitignore` excludes:
  - `credentials.json` (Google OAuth client secret)
  - `secrets.json` / `*.secrets.json`
  - `TokenResponse*.json` and other token files
- GitHub secret scanning / push protection is enabled and has been verified

You must configure secrets **locally** before running the agent.

---

## 🛠 Prerequisites

- .NET 8 SDK installed  
- Azure OpenAI resource (gpt-4o or similar deployment)  
- AccuWeather (or compatible) API key used by the MCP Weather server  
- Google Cloud project with Calendar API enabled and `credentials.json` downloaded  

---

## ⚙️ Local Configuration

### 1️⃣ User Secrets (recommended)

Inside `McpAgentApp`, initialize and set user secrets:

    dotnet user-secrets init
    dotnet user-secrets set "AzureOpenAI:Endpoint" "https://YOUR-RESOURCE.openai.azure.com/"
    dotnet user-secrets set "AzureOpenAI:ApiKey"   "YOUR_AZURE_OPENAI_KEY"
    dotnet user-secrets set "AzureOpenAI:DeploymentName" "gpt-4o-agent"

    dotnet user-secrets set "AccuWeather:ApiKey" "YOUR_ACCUWEATHER_KEY"

These values are read in `Program.cs` via `ConfigurationBuilder().AddUserSecrets<Program>()`.

### 2️⃣ Google OAuth Credentials

- Go to Google Cloud Console  
- Enable **Google Calendar API**  
- Create OAuth client credentials (Desktop app)  
- Download `credentials.json`  
- Place it next to where the app runs (for example, in the `McpAgentApp` folder)

Do **not** commit `credentials.json`. It is ignored by `.gitignore`.

---

## ▶️ Running the Agent

From the solution root:

    dotnet restore
    dotnet run --project McpAgentApp

You should see something like:

    Custom GoogleCalendarPlugin loaded.
    Connecting to Weather MCP Server...
    Loaded 2 tools from Weather MCP Server.
    ------------------------------------------
    Agent ready. Ask me to schedule an outdoor event.

Now you can talk to the agent:

    You: Please schedule an outdoor football meeting for tomorrow at 5pm.
    Agent: Certainly. What is the city and state/country for the weather check?

    You: Damascus, Syria

At this point, the agent:

- Calls the Weather MCP tool (`weather-get_hourly`)  
- Evaluates the forecast  
- Decides if the event is feasible  
- Asks for confirmation  
- Creates a Google Calendar event in the correct time zone (`Asia/Damascus` in this example)

---

## 🧪 Example Session (Damascus Demo)

    You: Please schedule an outdoor football meeting for tomorrow at 5pm.
    Agent: Certainly. What is the city and state/country for the weather check?

    You: Damascus, Syria
    Agent: (calls weather-get_hourly via MCP)
    Agent: The weather in Damascus, Syria tomorrow at 5 PM will be around 17°C and sunny, which is suitable for an outdoor football meeting.

    Agent: I will schedule the event with the following details:
           - Title: Outdoor Football Meeting
           - Date: <tomorrow's date>
           - Time: 5 PM–6 PM
           - Location: Damascus, Syria
           - Time Zone: Asia/Damascus
           Is this correct?

    You: Yes, create it.
    Agent: (calls CreateEventAsync via GoogleCalendarPlugin)
    Agent: The outdoor football meeting has been successfully scheduled.
           You can view and manage it at: https://www.google.com/calendar/event?eid=...

Check Google Calendar and you will see the event created at the correct local time.

---

## 🔌 MCP Tools Used

**Weather MCP Tool**

- Tool name: `weather-get_hourly`  
- Purpose: Return hourly forecast for a given location so the agent can make decisions about outdoor events  

**Google Calendar Plugin**

- Method: `CreateEventAsync(summary, startTime, endTime, location, description, timeZone)`  
- Purpose: Create an event in the authenticated user’s primary Google Calendar  

Both tools are auto-invoked via Semantic Kernel’s tool-calling behavior.

---

## 🎥 Demo (LinkedIn)

A short demo video of this project will be shared on LinkedIn, showing:

- Console output with MCP tool calls  
- Agent reasoning (weather → decision → calendar)  
- Resulting event inside Google Calendar
---

## 🌱 Future Enhancements

- Add Outlook calendar support as an alternative to Google Calendar  
- Add natural-language time zone detection based on location  
- Add support for recurring events (weekly standups, training, etc.)  
- Add more MCP tools (maps, locations, reminders, notifications)

---

## 📬 Author

**Saleem Alsaleh**  
AI / MCP / .NET / Semantic Kernel  
GitHub: [SaleemAlsalehUAE](https://github.com/SaleemAlsalehUAE)  
LinkedIn: [SaleemAlsalehUAE](https://linkedin.com/in/saleem-alsaleh-uae)  

---

## ⭐ Support

If this project helped you or inspired you to build your own MCP tools:

- ⭐ Star the repository  
- 📨 Share the LinkedIn demo  
- 🛠️ Open an issue or PR with ideas or extensions  

This project is meant as a practical, real-world example of **agents using MCP tools to act in the real world**, not just answer questions.
