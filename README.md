# ⚙️ HireSync AI — ASP.NET Core Web API Backend

**Live API Health Check:** [https://hiresync-api.runasp.net](https://hiresync-api.runasp.net)  
**Live Frontend PWA:** [https://hiresync-ui.vercel.app](https://hiresync-ui.vercel.app)  
**Frontend Repository (React PWA):** [https://github.com/mayur18506/hiresync-ui](https://github.com/mayur18506/hiresync-ui)

Enterprise-grade **ASP.NET Core Web API** backend powering **HireSync AI**. Built with **C#**, **Entity Framework Core**, and **Microsoft SQL Server**, this service handles PDF resume text extraction, AI-powered ATS keyword analysis, technical voice interview evaluation, JWT authentication, Gmail SMTP OTP recovery, and persistent user telemetry.

---

## 🏗️ Core Engineering Highlights

* **🔐 Secure Authentication & OTP Recovery:** Implements stateless **JWT** authentication, **BCrypt** salted password hashing, OAuth social login handling, and automated 6-digit OTP email delivery via **Gmail SMTP** for password resets.
* **📄 Automated PDF Resume Parsing (`PdfPig`):** Extracts clean text from uploaded multi-page `.pdf` resumes via `multipart/form-data` streams without external Python dependencies.
* **🤖 Hybrid Gemini AI + Resilient C# Fallback Engine:** Integrates **Google Gemini API** for contextual JD-to-resume matching, dynamic technical interview orchestration, answer grading, and resume summary polishing—backed by a deterministic C# fallback engine to guarantee zero downtime.
* **🗄️ Code-First SQL Persistence & Auto-Migrations:** Uses **Entity Framework Core** with **SQL Server** (`Users`, `AtsScanLogs`, `InterviewLogs`) and automatic startup schema migration in cloud environments.

---

## 🛠️ Tech Stack

* **Framework:** C#, ASP.NET Core Web API
* **ORM & Database:** Entity Framework Core (Code-First), Microsoft SQL Server, LINQ
* **Security:** JWT Bearer Tokens, `BCrypt.Net-Next`, HTTPS, CORS Policies
* **Email Service:** SMTP (`SmtpClient`) with Gmail App Password Integration
* **Libraries & AI:** `UglyToad.PdfPig` (PDF Extraction), Google Gemini REST API, Swagger / OpenAPI
* **Cloud Hosting:** MonsterASP.NET (IIS + Cloud MSSQL Server)

---

## 📡 Key API Endpoints

| Module | Method | Endpoint | Description |
| :--- | :--- | :--- | :--- |
| **Auth** | `POST` | `/api/Auth/register` | Register a new user with BCrypt password hashing |
| **Auth** | `POST` | `/api/Auth/login` | Authenticate user and issue JWT token |
| **Auth** | `POST` | `/api/Auth/social-login` | Authenticate/provision OAuth user account |
| **Auth** | `POST` | `/api/Auth/forgot-password` | Generate & send 6-digit OTP via Gmail SMTP |
| **Auth** | `POST` | `/api/Auth/reset-password` | Verify OTP and update hashed password |
| **ATS Analyzer** | `POST` | `/api/AtsAnalyzer/upload-resume` | Extract plain text from uploaded `.pdf` file |
| **ATS Analyzer** | `POST` | `/api/AtsAnalyzer/analyze` | Score resume against JD & log result in SQL Server |
| **Voice Interview** | `POST` | `/api/Interview/start` | Start a 5-turn role-based technical mock interview |
| **Voice Interview** | `POST` | `/api/Interview/answer` | Evaluate candidate answer, return score & next question |
| **Resume Builder** | `POST` | `/api/ResumeBuilder/polish-summary` | Rewrite & optimize professional summary using AI |
| **History** | `GET` | `/api/History` | Fetch persisted ATS scan logs & interview scorecards |

---

## ⚡ Getting Started Locally

### Prerequisites
* .NET SDK (.NET 8.0 or later)
* Microsoft SQL Server (LocalDB or SQL Server Express)

### 1. Clone the Repository

```bash
git clone [https://github.com/mayur18506/HireSync-API.git](https://github.com/mayur18506/HireSync-API.git)
cd HireSync-API
```

### 2. Configure Secrets (`appsettings.Development.json`)

Create an `appsettings.Development.json` file (ignored by Git) with your local credentials:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\MSSQLLocalDB;Database=HireSyncDb;Trusted_Connection=True;TrustServerCertificate=True;"
  },
  "Jwt": {
    "Key": "YOUR_SUPER_SECRET_JWT_KEY_32_CHARS_MINIMUM"
  },
  "EmailSettings": {
    "SenderEmail": "your-email@gmail.com",
    "AppPassword": "YOUR_16_DIGIT_GMAIL_APP_PASSWORD"
  },
  "Gemini": {
    "ApiKey": "YOUR_GEMINI_API_KEY"
  }
}
```

### 3. Apply Migrations & Run

```bash
dotnet restore
dotnet ef database update
dotnet run --launch-profile https
```

The API will start at `https://localhost:7097` (Swagger UI available at `https://localhost:7097/swagger`).

---

## 👨‍💻 Author

**Mayur Piprewar** — *.NET Developer*  
* **GitHub:** [@mayur18506](https://github.com/mayur18506)
* **LinkedIn:** [mayur-piprewar-310251291](https://www.linkedin.com/in/mayur-piprewar-310251291)
