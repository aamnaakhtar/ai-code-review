# AI Code Review Assistant

A full-stack AI-powered code review tool where developers paste code snippets and get structured feedback — bugs, performance issues, style suggestions, and security vulnerabilities — powered by Google Gemini.

Built with React, TypeScript, C# .NET 8, and PostgreSQL.

---

## Features

- **AI-powered reviews** — real feedback from Google Gemini categorized by type and severity
- **Async job queue** — submissions return a jobId in ~50ms, background worker handles LLM processing
- **Token-aware chunking** — large files are split into overlapping chunks, reviewed in parallel, results merged
- **Result caching** — SHA256-based cache skips redundant LLM calls, reducing average latency by 40%
- **JWT authentication** — register, login, protected routes, review history per user
- **Rate limiting** — 5 reviews per minute per IP to prevent abuse
- **Review history** — past reviews stored in PostgreSQL, accessible per user
- **VS Code-style editor** — Monaco editor with syntax highlighting for JavaScript, TypeScript, Python, C#, and Java

---

## Tech stack

| Layer | Technology |
|---|---|
| Frontend | React 18, TypeScript, Vite, Tailwind CSS, Monaco Editor |
| Backend | C# .NET 8, ASP.NET Core Web API |
| Database | PostgreSQL 16, EF Core 8 (Code First) |
| AI | Google Gemini API (gemini-2.5-flash) |
| Auth | JWT Bearer tokens, BCrypt password hashing |
| Queue | System.Threading.Channels (in-memory) |
| Cache | ConcurrentDictionary with SHA256 keys |
| Rate limiting | AspNetCoreRateLimit |

---

## Project structure

```
ai-code-review/
├── src/                          # React frontend
│   ├── api/
│   │   └── reviewApi.ts          # API calls — submit, poll, history
│   ├── components/
│   │   ├── AuthForm.tsx           # Login and register form
│   │   ├── CodeEditor.tsx         # Monaco editor wrapper
│   │   ├── HistoryPanel.tsx       # Past reviews sidebar
│   │   ├── LanguageSelector.tsx   # Language dropdown
│   │   └── ReviewPanel.tsx        # Issue cards display
│   ├── context/
│   │   └── AuthContext.tsx        # JWT auth state and axios headers
│   ├── hooks/
│   │   └── useCodeReview.ts       # Review logic, polling, rate limit countdown
│   ├── types/
│   │   └── review.ts              # TypeScript interfaces
│   └── App.tsx
│
└── CodeReview.API/               # C# .NET backend
    ├── Configuration/
    │   ├── JwtOptions.cs
    │   └── LLMOptions.cs
    ├── Controllers/
    │   ├── AuthController.cs      # Register and login endpoints
    │   └── ReviewController.cs    # Submit, poll, history, cache stats
    ├── Data/
    │   └── AppDbContext.cs        # EF Core DbContext and relationships
    ├── DTOs/                      # Request and response shapes
    ├── Middleware/
    │   └── ErrorHandlingMiddleware.cs
    ├── Models/
    │   ├── Entities/              # User, ReviewJob, ReviewResult, ReviewIssue
    │   └── LLMResponse.cs
    ├── Services/
    │   ├── LLMService.cs          # Gemini API calls with retry logic
    │   ├── PromptBuilder.cs       # Structured JSON prompt engineering
    │   ├── ReviewCacheService.cs  # SHA256 cache with TTL
    │   ├── ReviewQueue.cs         # Channel<T> producer-consumer queue
    │   ├── ReviewWorker.cs        # IHostedService background worker
    │   └── TokenService.cs        # JWT generation
    └── Program.cs
```

---

## Getting started

### Prerequisites

- Node.js 18+
- .NET 8 SDK
- PostgreSQL 16
- Google Gemini API key — get one free at [aistudio.google.com](https://aistudio.google.com)

### 1. Clone the repo

```bash
git clone https://github.com/your-username/ai-code-review.git
cd ai-code-review
```

### 2. Frontend setup

```bash
npm install
npm run dev
```

Frontend runs at `http://localhost:5173`.

### 3. Database setup

Create the database in PostgreSQL:

```sql
CREATE DATABASE codereview;
```

### 4. Backend setup

```bash
cd CodeReview.API
```

Store secrets using .NET User Secrets (never commit these):

```bash
dotnet user-secrets init
dotnet user-secrets set "LLM:ApiKey" "YOUR_GEMINI_API_KEY"
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Port=5432;Database=codereview;Username=postgres;Password=YOUR_PASSWORD"
dotnet user-secrets set "Jwt:SecretKey" "your-super-secret-key-minimum-32-characters"
```

Run migrations to create tables:

```bash
dotnet ef database update
```

Start the backend:

```bash
dotnet run
```

Backend runs at `http://localhost:5249`. Swagger UI at `http://localhost:5249/swagger`.

### 5. Configure appsettings.json

The `appsettings.json` file should contain no real secrets. Use this as a template:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": ""
  },
  "LLM": {
    "Provider": "gemini",
    "ApiKey": "",
    "Model": "gemini-2.5-flash"
  },
  "Jwt": {
    "SecretKey": "",
    "Issuer": "CodeReview.API",
    "Audience": "CodeReview.Client",
    "ExpiryHours": 24
  },
  "IpRateLimiting": {
    "EnableEndpointRateLimiting": true,
    "StackBlockedRequests": false,
    "HttpStatusCode": 429,
    "GeneralRules": [
      { "Endpoint": "POST:/api/review", "Period": "1m", "Limit": 5 },
      { "Endpoint": "GET:/api/review/*", "Period": "1m", "Limit": 60 }
    ]
  }
}
```

---

## How it works

### Request flow

```
User clicks Review
      ↓
React POST /api/review
      ↓
Controller saves job to DB → returns jobId in ~50ms
      ↓
Channel<ReviewJob> queue
      ↓
Background worker (IHostedService)
      ↓
Cache check (SHA256 hash) → HIT: return cached result
      ↓ MISS
Gemini API call (with retry + exponential backoff)
      ↓
Result saved to PostgreSQL
      ↓
React polls GET /api/review/{jobId} every 2s
      ↓
Result displayed in review panel
```

### Token-aware chunking

Large files exceeding 8,000 tokens are split before sending to the LLM:

- Token estimation at 1 token per 4 characters (industry standard approximation)
- Sliding window builds chunks line by line until hitting the token budget
- 20-line overlap between consecutive chunks preserves context at boundaries
- All chunks reviewed in parallel using `Task.WhenAll`
- Results merged and deduplicated by issue type and line number
- Line numbers adjusted from chunk-relative to file-relative

### Caching

```
cache_key = SHA256(language + ":" + code.Trim())
```

On cache hit, the entire Gemini API call is skipped. Cache entries expire after 24 hours. This reduced average review latency by ~40% in testing with realistic code overlap between submissions.

---

## API endpoints

| Method | Endpoint | Auth | Description |
|---|---|---|---|
| POST | `/api/auth/register` | None | Create account, returns JWT |
| POST | `/api/auth/login` | None | Login, returns JWT |
| POST | `/api/review` | Required | Submit code for review, returns jobId |
| GET | `/api/review/{jobId}` | Required | Poll job status and result |
| GET | `/api/review/history` | Required | Last 10 reviews for current user |
| GET | `/api/review/cache/stats` | Required | Cache hit/miss statistics |
| GET | `/api/review/health` | None | Health check |

---

## Database schema

```
Users
  Id (UUID PK), Email (unique), Username, PasswordHash, CreatedAt

ReviewJobs
  Id (UUID PK), UserId (FK → Users), Code, Language, Status, CreatedAt

ReviewResults
  Id (UUID PK), JobId (FK → ReviewJobs), Summary, TotalIssues, ReviewedAt

ReviewIssues
  Id (UUID PK), ResultId (FK → ReviewResults), Type, Severity, Line, Message, Suggestion
```

---

## Engineering decisions

**Why async queue over direct LLM call?**
LLM calls take 5-30 seconds. Blocking a thread per request exhausts the thread pool under load. The queue decouples submission from processing — the API responds in 50ms regardless of LLM latency.

**Why Channel<T> over ConcurrentQueue?**
Channel supports async waiting without CPU spinning. When the queue is empty, the worker awaits efficiently. It also supports backpressure via bounded capacity — 100 job limit prevents memory exhaustion under load.

**Why in-memory cache over Redis?**
Simpler with zero infrastructure for a single-server deployment. The tradeoff is cache resets on restart and no shared state across instances. Redis would be the next step for horizontal scaling.

**Why UUID primary keys over integers?**
UUIDs are globally unique across distributed systems, non-guessable (security benefit), and safe to merge across databases. The minor performance cost of larger index size is acceptable at this scale.

**Why IServiceScopeFactory in the background worker?**
BackgroundService is a singleton. DbContext and ILLMService are scoped. Injecting scoped services into a singleton causes lifetime mismatch — stale tracked entities and held connections. IServiceScopeFactory creates a fresh scope per job, giving a properly-disposed DbContext each time.

---

## Known limitations and future improvements

- **In-memory cache** resets on restart — replace with Redis for persistence and multi-instance support
- **Single worker** processes jobs sequentially — add concurrency or horizontal scaling with RabbitMQ
- **Polling** every 2 seconds — replace with WebSockets for real-time push updates
- **sessionStorage** for JWT — use httpOnly cookies in production to prevent XSS token theft
- **IP-based rate limiting** — switch to user-ID-based limiting for fairer per-account limits

---

## Security

- Passwords hashed with BCrypt (adaptive cost factor, automatic salting)
- JWTs signed with HMAC-SHA256, 24-hour expiry
- API keys stored in .NET User Secrets, never committed to Git
- Generic auth error messages prevent email enumeration
- Rate limiting prevents brute-force and API cost abuse
- `appsettings.json` excluded from version control

---

## License

MIT
