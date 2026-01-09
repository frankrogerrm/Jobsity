# 💬 .NET Chat 

**Real-time chat application with stock quotes integration using .NET, Blazor, SignalR, and RabbitMQ**

---

## 👤 Author

**Frank Ramos**  
📧 Contact: [GitHub Profile](https://github.com/frankrogerrm)  
🔗 Repository: [https://github.com/frankrogerrm/Jobsity](https://github.com/frankrogerrm/Jobsity)

---

## 📋 Table of Contents

- [Description](#description)
- [Technologies](#technologies)
- [Architecture](#architecture)
- [Prerequisites](#prerequisites)
- [Installation](#installation)
- [Running the Application](#running-the-application)
- [Testing](#testing)
- [Project Structure](#project-structure)
- [Usage Guide](#usage-guide)
- [Screenshots](#screenshots)
- [Troubleshooting](#troubleshooting)

---

## 📖 Description

This project is a **browser-based real-time chat application** built with **.NET 10** and **Blazor Server**. It allows multiple users to communicate in chatrooms and retrieve **stock quotes** from an external API using commands.

The application demonstrates:
- Real-time communication with **SignalR**
- Microservices architecture with a **decoupled bot**
- Message broker integration with **RabbitMQ**
- User authentication with **ASP.NET Identity**
- Comprehensive unit testing with **xUnit**

---

## 🛠️ Technologies

### Backend
- **.NET 10** - Latest framework version
- **C# 12** - Modern C# features
- **Blazor Server** - Interactive web UI framework
- **SignalR** - Real-time web functionality
- **ASP.NET Identity** - Authentication and authorization
- **Entity Framework Core** - ORM for database access
- **RabbitMQ** - Message broker
- **SQL Server LocalDB** - Database

### Libraries
- **CsvHelper** - CSV parsing
- **RabbitMQ.Client** - RabbitMQ integration
- **xUnit** - Unit testing framework
- **Moq** - Mocking framework for tests

### External APIs
- **Stooq API** - Real-time stock quotes

---

## 🏗️ Architecture
```
┌─────────────────────────────────────────────────────────────┐
│                        User Browser                         │
│                    (Blazor + SignalR)                       │
└────────────────────────┬────────────────────────────────────┘
                         │
                         ▼
┌─────────────────────────────────────────────────────────────┐
│                   ChatApp.Web (ASP.NET)                     │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐      │
│  │   ChatHub    │  │ ChatService  │  │   Identity   │      │
│  │  (SignalR)   │  │              │  │    (Auth)    │      │
│  └──────┬───────┘  └──────────────┘  └──────────────┘      │
│         │                                                    │
│         │ Publishes /stock command                          │
│         ▼                                                    │
│  ┌──────────────────────────────────────────────────┐       │
│  │            RabbitMQ Message Broker               │       │
│  │         (stock_commands / stock_responses)       │       │
│  └──────────────┬───────────────────────────────────┘       │
└─────────────────┼───────────────────────────────────────────┘
                  │
                  │ Consumes commands
                  ▼
┌─────────────────────────────────────────────────────────────┐
│              ChatApp.StockBot (Worker Service)              │
│  ┌──────────────────────────────────────────────────┐       │
│  │          StockQuoteService                       │       │
│  │  1. Calls Stooq API                              │       │
│  │  2. Parses CSV response (CsvHelper)              │       │
│  │  3. Publishes result to RabbitMQ                 │       │
│  └──────────────────────────────────────────────────┘       │
└─────────────────────────────────────────────────────────────┘
```

---

## 📦 Prerequisites

Before running the application, ensure you have:

1. **Visual Studio 2022** (17.8 or later)
   - Download: [visualstudio.com](https://visualstudio.microsoft.com/)

2. **.NET 10 SDK**
   - Included with Visual Studio 2022 (latest version)

3. **SQL Server LocalDB**
   - Included with Visual Studio installation
   - Or install SQL Server Express

4. **RabbitMQ**
   - **Option A (Docker - Recommended):**
```bash
     docker run -d --name rabbitmq -p 5672:5672 -p 15672:15672 rabbitmq:4-management
```
   - **Option B (Windows Installation):**
     1. Install Erlang: [erlang.org/downloads](https://www.erlang.org/downloads)
     2. Install RabbitMQ: [rabbitmq.com/download](https://www.rabbitmq.com/download.html)
     3. Enable Management Plugin:
```cmd
        rabbitmq-plugins enable rabbitmq_management
```

---

## 🚀 Installation

### Step 1: Clone the Repository
```bash
git clone https://github.com/frankrogerrm/Jobsity.git
cd Jobsity
```

### Step 2: Restore NuGet Packages

Open the solution in Visual Studio and:
- Right-click on the solution → **Restore NuGet Packages**

Or via command line:
```bash
dotnet restore
```

### Step 3: Setup Database

Open **Package Manager Console** in Visual Studio:
- **Tools** → **NuGet Package Manager** → **Package Manager Console**

Run:
```powershell
Update-Database
```

This will create the database and seed initial data (including the "General" chatroom).

### Step 4: Verify RabbitMQ

Ensure RabbitMQ is running:
- Open browser: [http://localhost:15672](http://localhost:15672)
- Login: `guest` / `guest`
- You should see the RabbitMQ Management dashboard

---

## ▶️ Running the Application

### Configure Multiple Startup Projects

1. Right-click on the **solution** (Jobsity) in Solution Explorer
2. **Properties** → **Startup Project**
3. Select **Multiple startup projects**
4. Set both projects to **Start**:
   - ✅ `ChatApp.Web` → **Start**
   - ✅ `ChatApp.StockBot` → **Start**
5. Click **OK**

### Run the Application

Press **F5** or click **Start** (▶️)

You should see:
- ✅ **Browser window** with the chat application
- ✅ **Console window** with the StockBot logs

Default URL: `https://localhost:7140`

---

## 👥 Usage Guide

### 1. Register Users

**User 1:**
1. Click **"Register"** in the top menu
2. Email: `user1@test.com`
3. Password: `Test123!`
4. Click **"Register"**

**User 2 (in Incognito/Private window):**
1. Open browser in Incognito mode (`Ctrl + Shift + N`)
2. Navigate to the same URL
3. Register with: `user2@test.com` / `Test123!`

### 2. Access Chat

1. Click **"Chat"** in the navigation menu
2. You'll be taken to the **"General"** chatroom

### 3. Send Messages

- Type a message and press **Enter** or click **Send**
- Messages appear in real-time for all users

### 4. Use Stock Commands

Type the following command:
```
/stock=AAPL.US
```

Expected result:
- ✅ Temporary info message: "Processing stock quote for AAPL.US..."
- ✅ After 2-5 seconds, bot responds: "AAPL.US quote is $193.42 per share"

**Other stock codes to try:**
- `/stock=MSFT.US` - Microsoft
- `/stock=GOOG.US` - Google
- `/stock=AMZN.US` - Amazon

### 5. Navigate Chatrooms

Use the sidebar on the left to switch between chatrooms.

---

## 🧪 Testing

### Run Unit Tests

**Option 1: Visual Studio**
1. **View** → **Test Explorer**
2. Click **Run All Tests** (▶️▶️)

**Option 2: Command Line**
```bash
cd ChatApp.Tests
dotnet test
```

### Test Coverage

- ✅ **9 Unit Tests** covering:
  - Chat message operations
  - Stock command validation
  - Stock code extraction
  - API response parsing (valid/invalid/errors)
  - Bot message handling

**Expected Result:**
```
Passed!  - Failed:     0, Passed:     9, Skipped:     0, Total:     9
```

---

## 📁 Project Structure
```
Jobsity/
├── ChatApp.Web/                    # Main web application
│   ├── Components/
│   │   ├── Account/               # Identity pages
│   │   ├── Layout/                # Layout components (NavMenu)
│   │   └── Pages/                 # Blazor pages (Chat, Home)
│   ├── Data/                      # DbContext, ApplicationUser
│   ├── Hubs/                      # SignalR ChatHub
│   ├── Services/                  # ChatService, RabbitMQService
│   └── Program.cs                 # Application configuration
│
├── ChatApp.StockBot/              # Background worker service
│   ├── Services/                  # StockQuoteService, RabbitMQService
│   ├── Worker.cs                  # Main worker loop
│   └── Program.cs                 # Bot configuration
│
├── ChatApp.Shared/                # Shared models
│   └── Models/                    # ChatMessage, ChatRoom, StockCommand
│
├── ChatApp.Tests/                 # Unit tests
│   ├── ChatServiceTests.cs
│   └── StockQuoteServiceTests.cs
│
└── README.md                      # This file
```

---

## 🖼️ Screenshots

### Chat Interface
![Chat Interface](docs/chat-screenshot.png)
*Real-time chat with multiple users and stock bot responses*

### Stock Command Example
```
user1@test.com: Hello everyone!
user2@test.com: Hi! Let's check Apple stock
user2@test.com: /stock=AAPL.US
StockBot: AAPL.US quote is $193.42 per share
```

---

## 🔧 Troubleshooting

### RabbitMQ Connection Issues

**Problem:** Bot cannot connect to RabbitMQ

**Solution:**
1. Verify RabbitMQ is running:
```bash
   # Windows
   net start RabbitMQ
   
   # Docker
   docker ps | grep rabbitmq
```
2. Check connection string in `appsettings.json`

### Database Issues

**Problem:** "Cannot open database"

**Solution:**
```powershell
# In Package Manager Console
Update-Database
```

### Stock Bot Not Responding

**Problem:** No response to `/stock=CODE` commands

**Solution:**
1. Verify **both projects** are running (Web + Bot)
2. Check Bot console for errors
3. Verify RabbitMQ queues exist:
   - Go to: http://localhost:15672/#/queues
   - Should see: `stock_commands` and `stock_responses`

### Blazor Events Not Working

**Problem:** Buttons don't respond to clicks

**Solution:**
Ensure `@rendermode InteractiveServer` is at the top of the Blazor component

---

## 🎯 Features Breakdown

| Feature | Status | Technology |
|---------|--------|------------|
| User Authentication | ✅ Complete | ASP.NET Identity |
| Real-time Chat | ✅ Complete | SignalR |
| Multiple Chatrooms | ✅ Complete | Entity Framework |
| Stock Commands | ✅ Complete | Regex + RabbitMQ |
| CSV Parsing | ✅ Complete | CsvHelper |
| Message Broker | ✅ Complete | RabbitMQ |
| Unit Tests | ✅ Complete | xUnit + Moq |
| Responsive UI | ✅ Complete | Bootstrap 5 |
| Error Handling | ✅ Complete | Try-Catch + Logging |

---

## 📝 Configuration

### appsettings.json (ChatApp.Web)
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=ChatAppDb;..."
  },
  "RabbitMQ": {
    "HostName": "localhost",
    "Port": 5672,
    "UserName": "guest",
    "Password": "guest",
    "StockCommandQueue": "stock_commands",
    "StockResponseQueue": "stock_responses"
  }
}
```

### appsettings.json (ChatApp.StockBot)
```json
{
  "RabbitMQ": {
    "HostName": "localhost",
    "Port": 5672,
    "UserName": "guest",
    "Password": "guest",
    "StockCommandQueue": "stock_commands",
    "StockResponseQueue": "stock_responses"
  },
  "StooqApi": {
    "BaseUrl": "https://stooq.com/q/l/",
    "QueryFormat": "?s={0}&f=sd2t2ohlcv&h&e=csv"
  }
}
```

---

## 🤝 Contributing

This is a challenge project and is not open for contributions. However, feel free to fork and modify for your own learning purposes.

---

## 📄 License

This project was created as part of a technical challenge and is for demonstration purposes only.

---

## 📞 Contact

**Frank Ramos**
- GitHub: [@frankrogerrm](https://github.com/frankrogerrm)
- Repository: [Jobsity](https://github.com/frankrogerrm/Jobsity)

---

## 🙏 Acknowledgments

- **Jobsity** for providing this interesting technical challenge
- **Stooq API** for stock data
- **RabbitMQ** team for the excellent message broker
- **.NET Community** for comprehensive documentation

---

**Built with ❤️ using .NET 10, Blazor, SignalR, and RabbitMQ**
