# Al-Mal Channel (قناة المال) — Product Requirements Document

**Kuwait Market Intelligence Platform | منصة ذكاء سوق الكويت**

Detailed Technical Specification for Claude Code Implementation

Version 1.0 | March 2026

---

## Table of Contents

1. [Product Overview & Goals](#1-product-overview--goals)
2. [Solution Architecture & Project Structure](#2-solution-architecture--project-structure)
3. [Technology Stack & Dependencies](#3-technology-stack--dependencies)
4. [Database Schema (Complete EF Core Models)](#4-database-schema-complete-ef-core-models)
5. [Module 1: Authentication & User Management](#5-module-1-authentication--user-management)
6. [Module 2: Market Dashboard](#6-module-2-market-dashboard)
7. [Module 3: Stock Detail Page](#7-module-3-stock-detail-page)
8. [Module 4: News Feed](#8-module-4-news-feed)
9. [Module 5: Community](#9-module-5-community)
10. [Module 6: Academy](#10-module-6-academy)
11. [Module 7: Simulation Portfolio](#11-module-7-simulation-portfolio)
12. [Module 8: WhatsApp Integration](#12-module-8-whatsapp-integration)
13. [Module 9: AI Layer (Claude Anthropic)](#13-module-9-ai-layer-claude-anthropic)
14. [Module 10: Admin Panel](#14-module-10-admin-panel)
15. [API Endpoints Specification](#15-api-endpoints-specification)
16. [UI/UX Design System & Components](#16-uiux-design-system--components)
17. [Background Services & Jobs](#17-background-services--jobs)
18. [Security Implementation](#18-security-implementation)
19. [Deployment Configuration](#19-deployment-configuration)
20. [Development Phases & Task Breakdown](#20-development-phases--task-breakdown)

---

## 1. Product Overview & Goals

### 1.1 What is Al-Mal Channel?

Al-Mal Channel (قناة المال) is a digital financial platform for the Kuwait Stock Exchange (Boursa Kuwait). It transforms raw market data into clear, actionable intelligence for investors.

### 1.2 Core Product Goals

- **Transform:** Turn a basic stock price app into a market understanding platform
- **Educate:** AI explains market movements — never gives buy/sell advice
- **Connect:** Community of investors with verified analyst badges
- **Practice:** Simulation portfolios with virtual capital
- **Alert:** Multi-channel notifications via app + WhatsApp

### 1.3 Language & Localization

- **Language:** Arabic only (العربية) — entire UI, content, and all user-facing text
- **Direction:** RTL (Right-to-Left) first throughout the entire application
- **Calendar:** Gregorian dates, but formatted in Arabic numerals (optional)
- **Currency:** Kuwaiti Dinar (KWD) — 3 decimal places (e.g., 0.345 KWD)
- **Market hours:** Sunday–Thursday, 9:00 AM – 12:40 PM Kuwait Time (AST, UTC+3)

### 1.4 Target Platforms

| Platform | Technology | Phase | Notes |
|----------|-----------|-------|-------|
| **Web (Public)** | ASP.NET Core 9 MVC | **Phase 1** | Mobile-first responsive |
| **Admin Panel** | ASP.NET Core 9 MVC | **Phase 1** | Desktop-first, /admin area |
| **REST API** | ASP.NET Core 9 Web API | **Phase 1** | Shared by web + mobile |
| **Mobile App** | Flutter 3.x | **Phase 2** | After web is tested |

---

## 2. Solution Architecture & Project Structure

### 2.1 Clean Architecture (.NET 9)

The solution follows Clean Architecture with strict dependency rules: inner layers never depend on outer layers.

```
AlMal/
├── src/
│   ├── AlMal.Domain/                  # Entities, Enums, Interfaces
│   │   ├── Entities/
│   │   ├── Enums/
│   │   ├── ValueObjects/
│   │   └── Interfaces/
│   ├── AlMal.Application/             # Use Cases, DTOs, Services
│   │   ├── DTOs/
│   │   ├── Services/
│   │   ├── Interfaces/
│   │   ├── Mapping/
│   │   └── Validators/
│   ├── AlMal.Infrastructure/          # EF Core, APIs, Caching
│   │   ├── Data/
│   │   │   ├── AlMalDbContext.cs
│   │   │   ├── Configurations/        # EF Core Fluent API
│   │   │   └── Migrations/
│   │   ├── Repositories/
│   │   ├── ExternalApis/
│   │   │   ├── BoursakuwaitScraper.cs
│   │   │   ├── NewsDataClient.cs
│   │   │   ├── WhatsAppClient.cs
│   │   │   └── ClaudeAiClient.cs
│   │   ├── Caching/
│   │   └── Identity/
│   ├── AlMal.Web/                     # Public MVC Website
│   │   ├── Controllers/
│   │   ├── Views/
│   │   ├── ViewModels/
│   │   ├── wwwroot/
│   │   │   ├── css/
│   │   │   ├── js/
│   │   │   └── images/
│   │   ├── Hubs/                      # SignalR
│   │   └── Program.cs
│   ├── AlMal.Admin/                   # Admin Panel MVC
│   │   ├── Controllers/
│   │   ├── Views/
│   │   └── ViewModels/
│   ├── AlMal.API/                     # REST API for Flutter
│   │   ├── Controllers/
│   │   ├── Filters/
│   │   └── Middleware/
│   └── AlMal.BackgroundServices/      # Hangfire Jobs
│       ├── Jobs/
│       └── Workers/
├── tests/
│   ├── AlMal.UnitTests/
│   ├── AlMal.IntegrationTests/
│   └── AlMal.E2ETests/
└── AlMal.sln
```

### 2.2 Dependency Flow

```
Domain ← Application ← Infrastructure ← Web/Admin/API/BackgroundServices
```

- **Domain** has ZERO dependencies
- **Application** depends only on Domain
- **Infrastructure** implements interfaces defined in Application
- **Web/Admin/API** depend on Application (via DI) and Infrastructure (for registration only in Program.cs)

---

## 3. Technology Stack & Dependencies

### 3.1 NuGet Packages (Backend)

| Package | Version | Purpose |
|---------|---------|---------|
| `Microsoft.AspNetCore.Identity.EntityFrameworkCore` | 9.x | Authentication & roles |
| `Microsoft.EntityFrameworkCore.SqlServer` | 9.x | SQL Server ORM |
| `Microsoft.EntityFrameworkCore.Tools` | 9.x | EF Core migrations CLI |
| `Microsoft.AspNetCore.Authentication.JwtBearer` | 9.x | JWT auth for API |
| `Microsoft.AspNetCore.SignalR` | 9.x | Real-time WebSocket push |
| `Hangfire.AspNetCore` | 1.8.x | Background job scheduler |
| `Hangfire.SqlServer` | 1.8.x | Hangfire SQL persistence |
| `StackExchange.Redis` | 2.7.x | Redis cache client |
| `Microsoft.Extensions.Caching.StackExchangeRedis` | 9.x | Redis DI integration |
| `HtmlAgilityPack` | 1.11.x | HTML parsing for scraping |
| `Anthropic.SDK` | latest | Claude AI API client |
| `FluentValidation.AspNetCore` | 11.x | Input validation |
| `AutoMapper` | 13.x | DTO mapping |
| `Serilog.AspNetCore` | 8.x | Structured logging |
| `NSwag.AspNetCore` | 14.x | Swagger/OpenAPI docs |

### 3.2 Frontend Libraries (npm / CDN)

| Library | Version | Purpose |
|---------|---------|---------|
| `bootstrap` (RTL build) | 5.3.x | Responsive RTL grid & components |
| `lightweight-charts` | 4.x (TradingView) | Interactive stock charts |
| `alpinejs` | 3.x | Reactive UI without SPA |
| `htmx.org` | 1.9.x | AJAX partial view loading |
| `chart.js` | 4.x | Heatmap & pie charts |
| `sweetalert2` | 11.x | Arabic modal dialogs |
| `flatpickr` | 4.x | Date picker with Arabic locale |
| `cropperjs` | 1.6.x | Avatar image cropping |
| `@microsoft/signalr` | 8.x | Real-time client |

### 3.3 External Service Accounts Required

- **Boursa Kuwait** — boursakuwait.com.kw (scraping initially, API license later)
- **NewsData.io** — API key for Arabic/Kuwait news (newsdata.io)
- **Anthropic** — Claude API key for AI features (anthropic.com)
- **Meta WhatsApp Business** — WhatsApp Cloud API access
- **Cloudflare** — CDN & DNS

---

## 4. Database Schema (Complete EF Core Models)

All string columns use `NVARCHAR` for Arabic support. SQL Server collation: `Arabic_CI_AS`. All entities include `CreatedAt` (DateTime) and `UpdatedAt` (DateTime?) audit fields.

### 4.1 Market Data Entities

#### Stock

| Column | Type | Required | Notes |
|--------|------|----------|-------|
| **Id** | int | PK | Auto-increment |
| **Symbol** | nvarchar(20) | Yes | Unique, indexed (e.g., NBK, ZAIN) |
| **NameAr** | nvarchar(200) | Yes | Arabic company name |
| **NameEn** | nvarchar(200) | No | English company name |
| **SectorId** | int | FK | FK → Sector.Id |
| **ISIN** | nvarchar(20) | No | International Securities ID |
| **ListingDate** | date | No | IPO date |
| **IsActive** | bit | Yes | Default: true |
| **MarketCap** | decimal(18,3) | No | In KWD |
| **SharesOutstanding** | bigint | No | Total shares |
| **LogoUrl** | nvarchar(500) | No | Company logo |
| **DescriptionAr** | nvarchar(max) | No | Arabic company description |
| **LastPrice** | decimal(18,3) | No | Cached latest price |
| **DayChange** | decimal(18,3) | No | Cached daily change |
| **DayChangePercent** | decimal(8,4) | No | Cached daily change % |

**Indexes:** `IX_Stock_Symbol` (unique), `IX_Stock_SectorId`, `IX_Stock_IsActive`

#### Sector

| Column | Type | Required | Notes |
|--------|------|----------|-------|
| **Id** | int | PK | Auto-increment |
| **NameAr** | nvarchar(200) | Yes | Arabic sector name |
| **NameEn** | nvarchar(200) | No | English sector name |
| **SortOrder** | int | Yes | Display order |
| **IndexValue** | decimal(18,3) | No | Current sector index |
| **ChangePercent** | decimal(8,4) | No | Daily change % |

#### StockPrice

| Column | Type | Required | Notes |
|--------|------|----------|-------|
| **Id** | long | PK | Auto-increment |
| **StockId** | int | FK | FK → Stock.Id, indexed |
| **Date** | date | Yes | Trading date |
| **Open** | decimal(18,3) | Yes | Opening price |
| **High** | decimal(18,3) | Yes | Day high |
| **Low** | decimal(18,3) | Yes | Day low |
| **Close** | decimal(18,3) | Yes | Closing price |
| **Volume** | bigint | Yes | Shares traded |
| **Value** | decimal(18,3) | No | Total value traded (KWD) |
| **Trades** | int | No | Number of trades |

**Indexes:** `IX_StockPrice_StockId_Date` (unique composite), `IX_StockPrice_Date`

#### MarketIndex

| Column | Type | Required | Notes |
|--------|------|----------|-------|
| **Id** | int | PK | Auto-increment |
| **NameAr** | nvarchar(200) | Yes | Arabic name |
| **Type** | int (enum) | Yes | Main=0, Sector=1, Premier=2 |
| **Value** | decimal(18,3) | Yes | Current value |
| **Change** | decimal(18,3) | No | Point change |
| **ChangePercent** | decimal(8,4) | No | Percent change |
| **Date** | datetime2 | Yes | Timestamp |

#### OrderBook

| Column | Type | Required | Notes |
|--------|------|----------|-------|
| **Id** | long | PK | Auto-increment |
| **StockId** | int | FK | FK → Stock.Id |
| **Level** | int | Yes | 1-10 (depth level) |
| **BidPrice** | decimal(18,3) | No | Buy price |
| **BidQuantity** | bigint | No | Buy quantity |
| **AskPrice** | decimal(18,3) | No | Sell price |
| **AskQuantity** | bigint | No | Sell quantity |
| **Timestamp** | datetime2 | Yes | Snapshot time |

#### FinancialStatement

| Column | Type | Required | Notes |
|--------|------|----------|-------|
| **Id** | int | PK | Auto-increment |
| **StockId** | int | FK | FK → Stock.Id |
| **Year** | int | Yes | Fiscal year |
| **Quarter** | int | Yes | 1-4 (0=annual) |
| **Revenue** | decimal(18,3) | No | Total revenue KWD |
| **NetIncome** | decimal(18,3) | No | Net income KWD |
| **TotalAssets** | decimal(18,3) | No | Total assets |
| **TotalEquity** | decimal(18,3) | No | Shareholders equity |
| **TotalDebt** | decimal(18,3) | No | Total debt |
| **EPS** | decimal(18,6) | No | Earnings per share |
| **DPS** | decimal(18,6) | No | Dividends per share |
| **BookValue** | decimal(18,3) | No | Book value per share |

**Indexes:** `IX_FinancialStatement_StockId_Year_Quarter` (unique composite)

#### Disclosure

| Column | Type | Required | Notes |
|--------|------|----------|-------|
| **Id** | int | PK | Auto-increment |
| **StockId** | int | FK | FK → Stock.Id |
| **TitleAr** | nvarchar(500) | Yes | Arabic title |
| **ContentAr** | nvarchar(max) | No | Full Arabic content |
| **Type** | int (enum) | Yes | Financial=0, Board=1, General=2, AGM=3, Dividend=4 |
| **PublishedDate** | datetime2 | Yes | Publication datetime |
| **SourceUrl** | nvarchar(1000) | No | Link to Boursa Kuwait |
| **AiSummary** | nvarchar(max) | No | Claude-generated summary |
| **IsProcessed** | bit | Yes | Default: false |

### 4.2 User & Community Entities

#### ApplicationUser (extends IdentityUser)

| Column | Type | Required | Notes |
|--------|------|----------|-------|
| **DisplayName** | nvarchar(100) | Yes | Public display name |
| **Bio** | nvarchar(500) | No | User bio / description |
| **AvatarUrl** | nvarchar(500) | No | Profile picture URL |
| **UserType** | int (enum) | Yes | Normal=0, ProAnalyst=1, CertifiedAnalyst=2 |
| **IsVerified** | bit | Yes | Default: false |
| **FollowersCount** | int | Yes | Denormalized, default: 0 |
| **FollowingCount** | int | Yes | Denormalized, default: 0 |
| **PostCount** | int | Yes | Denormalized, default: 0 |
| **WhatsAppNumber** | nvarchar(20) | No | Encrypted, for alerts |
| **WhatsAppOptIn** | bit | Yes | Default: false |
| **IsActive** | bit | Yes | Default: true (soft delete) |

#### Post

| Column | Type | Required | Notes |
|--------|------|----------|-------|
| **Id** | long | PK | Auto-increment |
| **UserId** | string | FK | FK → ApplicationUser.Id |
| **Content** | nvarchar(2000) | Yes | Post text content |
| **ImageUrl** | nvarchar(500) | No | Attached image |
| **VideoUrl** | nvarchar(500) | No | Attached video |
| **LikeCount** | int | Yes | Denormalized, default: 0 |
| **CommentCount** | int | Yes | Denormalized, default: 0 |
| **RepostCount** | int | Yes | Denormalized, default: 0 |
| **IsDeleted** | bit | Yes | Soft delete, default: false |
| **IsFlagged** | bit | Yes | Flagged for moderation |

#### PostStockMention (Join Table)

`PostId` (long, FK) + `StockId` (int, FK) — composite PK. Links stocks tagged in posts via `$SYMBOL` syntax.

#### Comment

| Column | Type | Required | Notes |
|--------|------|----------|-------|
| **Id** | long | PK | Auto-increment |
| **PostId** | long | FK | FK → Post.Id |
| **UserId** | string | FK | FK → ApplicationUser.Id |
| **Content** | nvarchar(1000) | Yes | Comment text |
| **ParentCommentId** | long? | FK | Nullable, for threaded replies |
| **IsDeleted** | bit | Yes | Soft delete |

#### PostLike

`UserId` (string, FK) + `PostId` (long, FK) — composite PK. `CreatedAt` (datetime2).

#### UserFollow

`FollowerId` (string, FK) + `FollowingId` (string, FK) — composite PK. `CreatedAt` (datetime2). Constraint: `FollowerId != FollowingId`.

#### Watchlist

| Column | Type | Required | Notes |
|--------|------|----------|-------|
| **Id** | int | PK | Auto-increment |
| **UserId** | string | FK | FK → ApplicationUser.Id |
| **StockId** | int | FK | FK → Stock.Id |
| **AlertPrice** | decimal(18,3)? | No | Price alert target |
| **AlertType** | int (enum)? | No | Above=0, Below=1 |
| **AlertEnabled** | bit | Yes | Default: false |

### 4.3 Simulation & Education Entities

#### SimulationPortfolio

| Column | Type | Required | Notes |
|--------|------|----------|-------|
| **Id** | int | PK | Auto-increment |
| **UserId** | string | FK | FK → ApplicationUser.Id (unique) |
| **InitialCapital** | decimal(18,3) | Yes | Default: 100,000 KWD |
| **CashBalance** | decimal(18,3) | Yes | Available cash |
| **IsPublic** | bit | Yes | Show on profile, default: false |

#### SimulationTrade

| Column | Type | Required | Notes |
|--------|------|----------|-------|
| **Id** | long | PK | Auto-increment |
| **PortfolioId** | int | FK | FK → SimulationPortfolio.Id |
| **StockId** | int | FK | FK → Stock.Id |
| **Type** | int (enum) | Yes | Buy=0, Sell=1 |
| **Quantity** | int | Yes | Number of shares |
| **Price** | decimal(18,3) | Yes | Execution price |
| **TotalValue** | decimal(18,3) | Yes | Quantity * Price |
| **ExecutedAt** | datetime2 | Yes | Trade timestamp |

#### SimulationHolding

`PortfolioId` (int, FK) + `StockId` (int, FK) — composite PK. `Quantity` (int), `AverageCost` (decimal(18,3)). Represents current positions.

#### Course

| Column | Type | Required | Notes |
|--------|------|----------|-------|
| **Id** | int | PK | Auto-increment |
| **TitleAr** | nvarchar(300) | Yes | Arabic course title |
| **DescriptionAr** | nvarchar(2000) | No | Arabic description |
| **ThumbnailUrl** | nvarchar(500) | No | Course image |
| **IsFree** | bit | Yes | Default: true |
| **Price** | decimal(18,3) | No | Price in KWD (if paid) |
| **SortOrder** | int | Yes | Display order |
| **IsPublished** | bit | Yes | Default: false |
| **LessonCount** | int | Yes | Denormalized count |
| **EnrollmentCount** | int | Yes | Denormalized count |

#### Lesson, Quiz, QuizQuestion, Enrollment, Certificate

- **Lesson:** CourseId (FK), TitleAr, ContentAr (nvarchar max), VideoUrl, SortOrder, DurationMinutes
- **Quiz:** LessonId (FK), PassingScore (int, default 70)
- **QuizQuestion:** QuizId (FK), QuestionAr (nvarchar 1000), Options (JSON nvarchar max), CorrectIndex (int)
- **Enrollment:** UserId (FK) + CourseId (FK) composite PK, Progress (int 0-100), EnrolledAt
- **Certificate:** Id, UserId (FK), CourseId (FK), CertificateNumber (nvarchar 50, unique), IssuedAt, PdfUrl

### 4.4 News & Alert Entities

#### NewsArticle

| Column | Type | Required | Notes |
|--------|------|----------|-------|
| **Id** | long | PK | Auto-increment |
| **TitleAr** | nvarchar(500) | Yes | Arabic headline |
| **Source** | nvarchar(200) | Yes | Source name |
| **SourceUrl** | nvarchar(1000) | No | Original article link |
| **Sentiment** | int (enum) | Yes | Positive=0, Negative=1, Neutral=2 |
| **Summary** | nvarchar(1000) | No | AI-generated summary |
| **ContextData** | nvarchar(max) | No | JSON: related history |
| **PublishedAt** | datetime2 | Yes | Publication time |
| **ExternalId** | nvarchar(100) | No | NewsData.io article ID |
| **ImageUrl** | nvarchar(500) | No | Article thumbnail |
| **IsProcessed** | bit | Yes | AI processed, default: false |

**NewsArticleStock** (join table): `NewsArticleId` (long, FK) + `StockId` (int, FK) — M2M relationship.

#### Alert

| Column | Type | Required | Notes |
|--------|------|----------|-------|
| **Id** | int | PK | Auto-increment |
| **UserId** | string | FK | FK → ApplicationUser.Id |
| **Type** | int (enum) | Yes | Price=0, Disclosure=1, Volume=2, Index=3 |
| **StockId** | int? | FK | Nullable (Index alerts are global) |
| **Condition** | nvarchar(50) | Yes | Above, Below, AnyNew, ChangeAbove |
| **TargetValue** | decimal(18,3)? | No | Target price/percentage |
| **Channel** | int (enum) | Yes | App=0, WhatsApp=1, Both=2 |
| **IsActive** | bit | Yes | Default: true |
| **LastTriggered** | datetime2? | No | Last trigger time |

#### AlertHistory

`AlertId` (int, FK), `TriggeredAt` (datetime2), `Message` (nvarchar 1000), `DeliveryStatus` (int enum: Pending=0, Sent=1, Failed=2), `Channel` (int enum).

#### Notification (In-App)

`Id` (long), `UserId` (string, FK), `Title` (nvarchar 200), `Body` (nvarchar 500), `Type` (int enum), `ReferenceId` (nvarchar 100), `IsRead` (bit), `CreatedAt` (datetime2). Used for in-app notification bell.

---

## 5. Module 1: Authentication & User Management

### 5.1 Registration

- **Fields:** DisplayName, Email, PhoneNumber, Password
- **Validation:** Email unique, phone unique, password 8+ chars with mixed case + digit
- **Flow:** Register → Email verification (optional for MVP) → Login
- **Default role:** User (Normal)

### 5.2 Login

- **MVC:** Cookie-based authentication with ASP.NET Identity
- **API:** JWT token with refresh token flow
- **JWT config:** Access token: 60 min, Refresh token: 30 days

### 5.3 User Roles

| Role | Permissions | Assignment |
|------|------------|------------|
| **User** | View market, news, community. Create posts. Simulation portfolio. | Self-register |
| **ProAnalyst** | All User + Pro badge on posts. Access to advanced academy. | Complete academy courses |
| **CertifiedAnalyst** | All Pro + Verified badge. Featured in suggested analysts. | Manual admin approval |
| **Moderator** | Review flagged content. Approve/reject posts. | Admin assignment |
| **Admin** | Manage users, stocks, news, academy, alerts. | Super Admin only |
| **SuperAdmin** | Full system access. API keys. Settings. Delete anything. | System seed |

### 5.4 Profile Features

- Edit profile: DisplayName, Bio, Avatar (with crop)
- Public profile page: `/user/{displayName}`
- Show/hide simulation portfolio on profile
- Follow/unfollow other users
- Account settings: change password, WhatsApp opt-in, notification preferences

---

## 6. Module 2: Market Dashboard

### 6.1 Route

`/` (homepage) or `/market`

### 6.2 Components & Layout

#### Desktop Layout

- **Top bar:** Sticky. Main index ticker (live value, change, change%). Horizontal scroll of sector indices.
- **Tab navigation:** All Stocks | Sectors | My Watchlist
- **Main area (60%):** Market heatmap (treemap of all stocks, color = change%, size = volume)
- **Sidebar (40%):** Top Gainers (5), Top Losers (5), Most Traded by Volume (5)
- **Below:** Full stock table (sortable, searchable, paginated — 20 per page)
- **FAB:** Quick search floating button

#### Mobile Layout

- **Sticky top:** Horizontally scrollable index ticker
- **Full-width tabs:** Swipeable between All Stocks / Sectors / Watchlist
- **Content:** Single column. Heatmap → Top Movers (swipeable cards) → Stock table
- **Bottom nav:** 5 tabs: Market | News | Community | Academy | Profile
- **Pull-to-refresh:** Refresh all market data

### 6.3 Real-Time Data (SignalR)

SignalR hub: `/hubs/market`

- **Events pushed:** `StockPriceUpdate(symbol, price, change, changePercent, volume)`
- **Events pushed:** `IndexUpdate(indexName, value, change, changePercent)`
- **Client groups:** Join by sector or watchlist stocks for filtered updates
- **Frequency:** Every 15-30 seconds during market hours

### 6.4 Heatmap Specification

- Treemap layout using chart.js treemap plugin or custom canvas
- Each cell = one stock. Size = market cap or volume. Color gradient: deep red (-5%+) to deep green (+5%+)
- Click cell → navigate to `/stock/{symbol}`
- Sector grouping with borders
- Touch-friendly: tap to see tooltip, double-tap to navigate

---

## 7. Module 3: Stock Detail Page

### 7.1 Route

`/stock/{symbol}` (e.g., `/stock/NBK`)

### 7.2 Header (Always Visible)

- Stock name (Arabic), symbol, sector badge
- Current price (large font), change value, change % with color (green/red)
- Watchlist star button (toggle add/remove)
- Alert bell button (set price alert)

### 7.3 Tab A: Overview

- **Company info:** Description, sector, listing date
- **Key data cards:** Market Cap, Shares Outstanding, 52-Week High, 52-Week Low, Average Volume (30d), Day Range
- **Layout:** 2-column card grid on desktop, single column on mobile

### 7.4 Tab B: Interactive Chart

- **Library:** TradingView Lightweight Charts v4
- **Timeframes:** 1D, 1W, 1M, 3M, 6M, 1Y, All (button toolbar)
- **Chart types:** Candlestick (default), Line
- **Volume:** Volume histogram below main chart
- **Indicators:** SMA (7, 14, 50), EMA (12, 26), RSI (14), MACD (12, 26, 9), Bollinger Bands (20, 2)
- **Indicator selector:** Dropdown/bottom sheet to toggle indicators on/off
- **Crosshair:** Shows date, OHLCV on hover/touch
- **Mobile:** Full viewport width. Landscape support. Pinch-to-zoom.
- **Data format:** API returns OHLCV array: `[{time, open, high, low, close, volume}]`

### 7.5 Tab C: Explain Movement (اشرح الحركة)

- **Trigger:** Big orange button: "اشرح الحركة"
- **Loading:** Skeleton loader while Claude processes
- **AI prompt sends:** Last 5 days OHLCV, recent disclosures, sector performance, volume anomalies
- **AI returns (Arabic):** What happened today? Any related disclosure? Sector context? Volume analysis?
- **Disclaimer:** Always show: "هذا تحليل تعليمي وليس نصيحة استثمارية"
- **Cache:** Cache response for 30 minutes per stock

### 7.6 Tab D: Financial Ratios

Auto-calculated from FinancialStatement data:

| Ratio | Formula | Display |
|-------|---------|---------|
| **P/E** | Market Price / EPS | Number with arrow |
| **P/B** | Market Price / Book Value Per Share | Number with arrow |
| **ROE** | (Net Income / Total Equity) * 100 | Percentage |
| **Profit Margin** | (Net Income / Revenue) * 100 | Percentage |
| **Dividend Yield** | (DPS / Market Price) * 100 | Percentage |
| **Debt/Equity** | Total Debt / Total Equity | Ratio |
| **EPS** | From FinancialStatement | KWD value |
| **DPS** | From FinancialStatement | KWD value |

Each ratio shows: value, Arabic explanation tooltip, comparison to sector average (colored arrow).

### 7.7 Tab E: Disclosures

- Chronological list of all Disclosure records for this stock
- Each card: Date, Type badge (color-coded), Title, AI Summary (expandable)
- Filter by type (Financial, Board, General, AGM, Dividend)
- Infinite scroll with 10 items per page

### 7.8 Tab F: Order Book

- Display modes: Full (10 levels) or Compact (5 levels)
- Two columns: Bids (green, right-aligned) | Asks (red, left-aligned)
- Each row: Price, Quantity, visual bar showing relative size
- Buy/Sell pressure indicator: percentage bar showing bid vs ask volume
- Liquidity imbalance score: `(totalBidQty - totalAskQty) / (totalBidQty + totalAskQty)`
- Real-time updates via SignalR during market hours

---

## 8. Module 4: News Feed

### 8.1 Route

`/news`

### 8.2 Data Source

NewsData.io API: `GET https://newsdata.io/api/1/news?country=kw&language=ar&category=business`

Polling: Every 15 min during market hours, every 60 min off-hours. Background service via Hangfire.

### 8.3 Processing Pipeline

1. Fetch new articles from NewsData.io
2. Deduplicate by `ExternalId`
3. Match to stocks by keyword matching (company names in Arabic/English)
4. Send to Claude API for: sentiment analysis (Positive/Negative/Neutral), Arabic summary generation, context data (related historical events)
5. Store in `NewsArticle` + `NewsArticleStock` tables

### 8.4 UI Layout

#### Desktop

- **Left sidebar (20%):** Filters — company search, sector dropdown, sentiment toggle, date range
- **Main feed (55%):** Card-based news items. Each card: source icon, title, time ago, sentiment pill (green/red/gray), summary text, stock tags, "Understand Context" button
- **Right sidebar (25%):** Trending stocks, Latest disclosures

#### Mobile

- Full-width card feed, single column
- Filters: Slide-up bottom sheet, activated by filter icon
- Trending: Horizontal scrollable chips at top
- Infinite scroll with skeleton placeholders

### 8.5 "Understand Context" Feature

When user clicks "Understand Context" ("افهم السياق") on a news card:

- Expand card to show: related past news for this company, historical price reaction to similar events, sector comparison context
- Data comes from pre-computed `ContextData` JSON field (populated by Claude during processing)
- If not yet computed, trigger async computation and show loading state

---

## 9. Module 5: Community

### 9.1 Route

`/community`

### 9.2 Feed Types

- **General Feed:** All public posts, sorted by CreatedAt DESC. Default tab.
- **Following Feed:** Posts only from users the current user follows.
- **Tab switching:** Swipeable on mobile, toggle buttons on desktop.

### 9.3 Post Creation

- **Content:** nvarchar(2000), supports Arabic text
- **Stock tags:** Type `$` to trigger autocomplete. Stores in PostStockMention. Renders as clickable pill linking to `/stock/{symbol}`
- **Media:** One image OR one video per post. Max 5MB image, 50MB video. Upload to server file storage.
- **Mobile:** New post button (FAB) opens full-screen modal with soft keyboard

### 9.4 Post Interactions

- **Like:** Toggle like/unlike. Updates denormalized LikeCount.
- **Comment:** Threaded (one level deep via ParentCommentId). 1000 char limit.
- **Repost:** Creates new post referencing original (via RepostOfId field). Shows original embedded.
- **Report:** Flag post for moderation. Stores in ReportedContent table.

### 9.5 User Badges

| Badge | Visual | Color | Earned By |
|-------|--------|-------|-----------|
| Normal User | No badge | - | Self-register |
| Pro Analyst | ⭐ icon + "محلل محترف" label | Orange | Complete courses |
| Certified Analyst | ✓ icon + "محلل معتمد" label | Purple | Admin approval |

### 9.6 User Profile Page

Route: `/user/{displayName}`

- Header: Avatar, DisplayName, Badge, Bio, Followers count, Following count
- Tab: Posts | Simulation Portfolio (if public)
- Follow/Unfollow button

---

## 10. Module 6: Academy

### 10.1 Route

`/academy`, `/academy/course/{id}`, `/academy/lesson/{id}`

### 10.2 Course Catalog

- Grid of course cards (thumbnail, title, description, lesson count, free/paid badge)
- Filter: Free only, Paid only, All
- Sort: Newest, Most enrolled, Alphabetical

### 10.3 Course Detail

- Course header with thumbnail, title, description, enrollment button
- Lesson list (accordion on mobile)
- Progress bar (if enrolled)

### 10.4 Lesson Viewer

- Video player (if VideoUrl provided) — use HTML5 video
- Lesson content rendered from ContentAr (supports basic HTML formatting)
- Next/Previous lesson navigation
- Quiz at end of lesson (if Quiz exists)

### 10.5 Quiz Engine

- Multiple choice questions
- Immediate feedback: correct/incorrect with explanation
- Passing score: 70% default
- On pass: mark lesson complete, check if course completed for certificate

### 10.6 Certificates

- Auto-generated when all lessons + quizzes passed in a course
- PDF with: User name, Course title, Date, Certificate number, Al-Mal Channel branding
- Downloadable and shareable
- Completing specific courses upgrades UserType to ProAnalyst

---

## 11. Module 7: Simulation Portfolio

### 11.1 Route

`/portfolio`, `/portfolio/trade`

### 11.2 Initial Setup

- On first visit, auto-create SimulationPortfolio with 100,000 KWD virtual capital
- Option to reset portfolio (requires confirmation)

### 11.3 Trading

- **Buy flow:** Select stock (search autocomplete) → Enter quantity → Shows estimated cost (quantity * lastPrice) → Confirm → Deduct from CashBalance, create SimulationTrade, update SimulationHolding
- **Sell flow:** Select from current holdings → Enter quantity (max = holding qty) → Shows estimated proceeds → Confirm → Add to CashBalance, create SimulationTrade, update SimulationHolding
- **Price:** Uses last known market price (`Stock.LastPrice`)
- **Validation:** Cannot sell more than held. Cannot spend more than CashBalance.

### 11.4 Portfolio Dashboard

- Total portfolio value = CashBalance + sum(holding.Quantity * stock.LastPrice)
- P&L = TotalValue - InitialCapital (absolute and %)
- Holdings table: Stock, Qty, Avg Cost, Current Price, P&L, Weight %
- Performance chart: Portfolio value over time vs Main Index
- Risk distribution: Pie chart by sector allocation

### 11.5 Public Portfolio

- Toggle `IsPublic` in settings
- Shows on user profile as a read-only view: holdings, performance, P&L

---

## 12. Module 8: WhatsApp Integration

### 12.1 Purpose

WhatsApp is **NOT** for registration or login. It is purely for alerts and a market Q&A assistant.

### 12.2 Opt-In Flow

1. User enters phone number in app settings
2. App sends verification code via WhatsApp API
3. User confirms code → `WhatsAppOptIn = true`

### 12.3 Alert Types Delivered via WhatsApp

| Alert Type | Message Template | Trigger |
|-----------|-----------------|---------|
| **Price Alert** | "سهم {symbol} وصل لسعر {price} KWD" | Price crosses target |
| **New Disclosure** | "إفصاح جديد لـ {company}: {title}" | New disclosure scraped |
| **Index Movement** | "المؤشر العام {direction} {percent}%" | > 2% index change |
| **Volume Anomaly** | "حجم تداول غير طبيعي على {symbol}" | > 3x avg volume |

### 12.4 Market Assistant Bot

Users can send WhatsApp messages to ask questions. The bot uses Claude API to answer:

- "آخر إفصاح لبنك الكويت الوطني?" → Returns latest NBK disclosure summary
- "ملخص أخبار اليوم" → Returns today's top news summary
- "ماذا يعني P/E?" → Returns Arabic explanation of P/E ratio

---

## 13. Module 9: AI Layer (Claude Anthropic)

### 13.1 Integration Architecture

- **SDK:** `Anthropic.SDK` NuGet package (or direct REST to `https://api.anthropic.com/v1/messages`)
- **Model:** `claude-sonnet-4-20250514` (balance of speed and quality)
- **Abstraction:** `IAlSmartAnalysisService` interface in Application layer, `ClaudeAiClient` implementation in Infrastructure
- **Config:** API key in appsettings (encrypted), model name configurable

### 13.2 Strict AI Rules

**CRITICAL:** The AI layer must **NEVER:**

- Give buy or sell recommendations
- Predict future prices
- Suggest specific investment actions
- Claim certainty about market direction

It **ALWAYS:**

- Provides educational explanations
- Links data to events for context
- Uses past tense ("this is what happened") not future ("this will happen")
- Includes disclaimer: "هذا تحليل تعليمي وليس نصيحة استثمارية"

### 13.3 AI Features & Prompts

#### Feature 1: Movement Explainer

**Triggered by:** "Explain Movement" button on stock page

**Input data sent to Claude:** Stock OHLCV (5 days), Recent disclosures, Sector performance, Volume vs 30-day average

**System prompt (Arabic):**

```
You are a financial education assistant for the Kuwait market. Analyze the provided stock data and explain what happened today. Link price movements to disclosures, sector trends, or volume changes. Never recommend buying or selling. Always respond in Arabic.
```

#### Feature 2: Disclosure Summarizer

**Triggered by:** Background job when new disclosure is scraped

**Input:** Full disclosure text (ContentAr)

**System prompt:**

```
Summarize this Kuwait Stock Exchange disclosure in 2-3 sentences of simple Arabic. Focus on what it means for existing shareholders. Do not give investment advice.
```

#### Feature 3: News Context Engine

**Triggered by:** "Understand Context" button on news cards, or background processing

**Input:** News article text, Company name, Historical news for same company

**System prompt:**

```
Given this news article about a Kuwaiti company, provide educational context: What is the background? How has this company reacted to similar news historically? What should investors understand? Respond in Arabic. No investment advice.
```

#### Feature 4: WhatsApp Market Assistant

**Triggered by:** WhatsApp webhook receiving user message

**Input:** User question + relevant context (latest disclosures, prices, news)

**System prompt:**

```
You are مساعد قناة المال (Al-Mal Channel Assistant), a helpful market education bot for the Kuwait Stock Exchange. Answer the user's question in Arabic using only the provided market data. Never give buy/sell advice. Keep answers concise for WhatsApp (max 500 chars).
```

---

## 14. Module 10: Admin Panel

### 14.1 Route

`/admin` (separate MVC area, cookie auth, admin roles only)

### 14.2 Admin Sections

| Section | Features | Min Role |
|---------|----------|----------|
| **Dashboard** | Active users (24h), Total users, Posts today, Market status (open/closed), Alert volume, Revenue from paid courses | Admin |
| **Users** | Search/filter users, View profile, Suspend/Ban, Manage UserType (promote to ProAnalyst/CertifiedAnalyst), Reset password, View user posts/activity | Admin |
| **Stocks** | CRUD stocks/sectors, Manual price entry, Import CSV, Scraping status dashboard (last run, errors, next scheduled), Toggle stock IsActive | Admin |
| **Content** | Moderation queue (flagged posts), Reported content review, Bulk delete/approve, Ban repeat offenders | Moderator |
| **News** | Source configuration, Override AI sentiment, Mark articles as featured, Exclude irrelevant articles, View processing stats | Admin |
| **Academy** | Course CRUD with rich text editor, Lesson management (reorder, video URL), Quiz builder (add questions, set correct answer), Certificate template config, Enrollment analytics (completion rate, avg score) | Admin |
| **Alerts** | WhatsApp message template management, Delivery analytics (sent/failed/pending), System-wide alert configuration (thresholds), View user alert subscriptions | Admin |
| **Analytics** | User growth chart (daily/weekly/monthly), Most viewed stocks, Most active community members, Course completion rates, Revenue reports | Admin |
| **Settings** | API keys (NewsData.io, Anthropic, WhatsApp), Scraping schedule (cron expressions), AI model configuration, System parameters (virtual capital amount, alert limits), Maintenance mode toggle | SuperAdmin |

---

## 15. API Endpoints Specification

Base URL: `/api/v1`. All endpoints return JSON. All require JWT except where noted. Pagination: `page` (default 1), `pageSize` (default 20, max 100). Arabic error messages in response body.

### 15.1 Auth Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| **POST** | `/api/v1/auth/register` | Register new user. Body: `{displayName, email, phone, password}` |
| **POST** | `/api/v1/auth/login` | Login. Body: `{email, password}`. Returns: `{accessToken, refreshToken}` |
| **POST** | `/api/v1/auth/refresh` | Refresh JWT. Body: `{refreshToken}` |
| **POST** | `/api/v1/auth/logout` | Revoke refresh token |
| **GET** | `/api/v1/auth/me` | Get current user profile |

### 15.2 Market Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| **GET** | `/api/v1/market/indices` | All market indices with current values [anonymous] |
| **GET** | `/api/v1/market/stocks` | Paginated stock list. Query: sector, search, sort, page |
| **GET** | `/api/v1/market/stocks/{symbol}` | Single stock detail with latest price |
| **GET** | `/api/v1/market/stocks/{symbol}/prices` | OHLCV history. Query: from, to, interval |
| **GET** | `/api/v1/market/stocks/{symbol}/orderbook` | Current order book (5 or 10 levels) |
| **GET** | `/api/v1/market/stocks/{symbol}/financials` | Financial statements + calculated ratios |
| **GET** | `/api/v1/market/stocks/{symbol}/disclosures` | Paginated disclosures list |
| **GET** | `/api/v1/market/gainers` | Top 10 gainers today [anonymous] |
| **GET** | `/api/v1/market/losers` | Top 10 losers today [anonymous] |
| **GET** | `/api/v1/market/most-traded` | Top 10 by volume today [anonymous] |
| **GET** | `/api/v1/market/heatmap` | All stocks with change% and volume for heatmap [anonymous] |
| **GET** | `/api/v1/market/sectors` | All sectors with indices [anonymous] |

### 15.3 AI Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| **POST** | `/api/v1/ai/explain-movement/{symbol}` | Trigger movement explanation for stock |
| **POST** | `/api/v1/ai/news-context/{newsId}` | Generate context for news article |
| **GET** | `/api/v1/ai/explain-movement/{symbol}` | Get cached explanation (if exists) |

### 15.4 Community Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| **GET** | `/api/v1/posts` | Feed. Query: `feed=general|following`, page, pageSize |
| **POST** | `/api/v1/posts` | Create post. Body: `{content, stockMentions[], image?, video?}` |
| **DELETE** | `/api/v1/posts/{id}` | Soft delete own post |
| **POST** | `/api/v1/posts/{id}/like` | Like/unlike toggle |
| **GET** | `/api/v1/posts/{id}/comments` | Get comments for post |
| **POST** | `/api/v1/posts/{id}/comments` | Add comment. Body: `{content, parentCommentId?}` |
| **POST** | `/api/v1/posts/{id}/report` | Report post. Body: `{reason}` |
| **GET** | `/api/v1/users/{displayName}` | Get public profile |
| **POST** | `/api/v1/users/{id}/follow` | Follow/unfollow toggle |
| **GET** | `/api/v1/users/{id}/followers` | Paginated followers list |
| **GET** | `/api/v1/users/{id}/following` | Paginated following list |

### 15.5 Watchlist, News, Academy, Simulation, Alert Endpoints

Following same pattern — CRUD operations with pagination, filtering, and proper authorization:

- `GET/POST/DELETE /api/v1/watchlist` — Manage watchlist items
- `GET /api/v1/news` — Paginated news. Query: stock, sector, sentiment, page
- `GET /api/v1/academy/courses` — Course catalog
- `POST /api/v1/academy/courses/{id}/enroll` — Enroll in course
- `GET /api/v1/academy/lessons/{id}` — Lesson content
- `POST /api/v1/academy/quizzes/{id}/submit` — Submit quiz answers
- `GET /api/v1/portfolio` — Get simulation portfolio
- `POST /api/v1/portfolio/trade` — Execute simulation trade
- `GET /api/v1/portfolio/performance` — Portfolio performance data
- `GET/POST/DELETE /api/v1/alerts` — Manage alerts

---

## 16. UI/UX Design System & Components

### 16.1 Brand Colors

| Name | HEX | CSS Variable | Usage | Context |
|------|-----|-------------|-------|---------|
| **Deep Purple** | `#4F0E5E` | `--clr-primary` | Headers, nav, CTAs | Brand primary |
| **Vivid Orange** | `#FC4A04` | `--clr-accent` | Buttons, alerts, highlights | Brand accent |
| **Green** | `#22C55E` | `--clr-positive` | Price up, positive | Market positive |
| **Red** | `#EF4444` | `--clr-negative` | Price down, negative | Market negative |
| **Dark BG** | `#1A1A2E` | `--clr-dark` | Charts, market views | Dark mode base |
| **Light BG** | `#F5F5F5` | `--clr-light` | Content sections | Light mode base |

### 16.2 Typography

- **Primary font:** IBM Plex Sans Arabic (Google Fonts)
- **Fallback:** Noto Sans Arabic
- **Numbers/monospace:** IBM Plex Mono
- **Base size:** 16px (1rem)
- **Scale:** H1: 2rem, H2: 1.5rem, H3: 1.25rem, Body: 1rem, Small: 0.875rem

### 16.3 Shared Components

Build these as Razor partial views / view components for reuse:

- **StockCard:** Symbol, name, price, change%, mini spark chart. Used in: market lists, watchlist, search results.
- **SentimentBadge:** Colored pill (إيجابي/سلبي/محايد). Used in: news cards.
- **UserBadge:** Avatar + display name + analyst badge. Used in: posts, comments, profiles.
- **PriceDisplay:** Price with color-coded change. Used everywhere prices appear.
- **BottomNavBar:** Mobile bottom navigation (5 tabs). Hidden on desktop.
- **BottomSheet:** Slide-up panel for filters, options. Mobile only.
- **LoadingSkeleton:** Animated placeholder for async content.
- **EmptyState:** Arabic message + illustration for empty lists.
- **ConfirmDialog:** SweetAlert2-based Arabic confirmation modal.
- **InfiniteScroll:** HTMX-powered scroll loader for feeds and lists.

### 16.4 Dark/Light Mode Strategy

- **Market pages:** Dark mode default (`/`, `/stock/*`, `/market`) — reduces eye strain during trading
- **Content pages:** Light mode default (`/community`, `/academy`, `/news`)
- **User toggle:** Theme switcher in header. Preference saved in localStorage + user settings
- **Implementation:** CSS custom properties. `data-theme="dark"` on `<html>`. Bootstrap dark mode utilities.

---

## 17. Background Services & Jobs

All background jobs managed via Hangfire with SQL Server persistence.

| Job Name | Schedule | Purpose | Notes |
|----------|----------|---------|-------|
| **MarketDataScraper** | Every 30s (market hours) | Scrape stock prices, indices from Boursa Kuwait | Sun-Thu 9:00-12:40 KWT |
| **OrderBookScraper** | Every 60s (market hours) | Scrape order book data | Depends on data availability |
| **DisclosureScraper** | Every 5 min | Check for new disclosures | 24/7 operation |
| **NewsFetcher** | Every 15 min (market), 60 min (off) | Fetch from NewsData.io API | Rate limit: per plan |
| **AiDisclosureProcessor** | On new disclosure | Generate AI summary via Claude | Queue-based, max 5 concurrent |
| **AiNewsProcessor** | On new article | Sentiment + summary + context | Queue-based |
| **AlertEngine** | Every 30s (market hours) | Check all active alerts vs current data | Batch process, send via app + WhatsApp |
| **DailyMarketSummary** | 12:45 PM KWT daily | Generate daily summary for WhatsApp | After market close |
| **DataCleanup** | Daily at 2 AM KWT | Archive old order book data, clean temp files | Keep 30 days order book |

---

## 18. Security Implementation

### 18.1 Authentication

- ASP.NET Identity with ApplicationUser extending IdentityUser
- MVC: Cookie authentication (HttpOnly, Secure, SameSite=Strict)
- API: JWT Bearer (Access: 60 min, Refresh: 30 days, stored in DB)
- Password policy: 8+ chars, uppercase, lowercase, digit

### 18.2 Authorization

- Role-based: `[Authorize(Roles = "Admin,SuperAdmin")]` on admin controllers
- Policy-based: custom policies for content ownership checks
- API: JWT claims contain userId and roles

### 18.3 Rate Limiting

- Authenticated users: 100 requests/minute
- Anonymous: 30 requests/minute
- AI endpoints: 10 requests/minute per user
- Implementation: ASP.NET Core 9 built-in rate limiting middleware

### 18.4 Input Security

- All Arabic text inputs: sanitized for XSS (HtmlSanitizer library)
- EF Core parameterized queries prevent SQL injection
- CSRF: anti-forgery tokens on all MVC forms
- Content Security Policy headers
- CORS: configured for allowed origins: `https://almal.codexkw.co`, `https://almal-admin.codexkw.co`, `https://almal-api.codexkw.co` + Flutter app origins

### 18.5 Data Protection

- HTTPS enforced (HSTS header)
- SQL Server TDE (Transparent Data Encryption) at rest
- WhatsApp phone numbers encrypted at application level (AES-256)
- API keys stored in appsettings with encrypted sections or environment variables
- Audit log: all admin actions logged with UserId, Action, Timestamp, Details

---

## 19. Deployment Configuration

### 19.1 Windows Server Setup

| Component | Configuration |
|-----------|--------------|
| **OS** | Windows Server 2022 |
| **Web Server** | IIS 10 + ASP.NET Core 9 Hosting Bundle (out-of-process) |
| **Database** | SQL Server 2022 at `83.229.86.221,1433` — Database: `AlMal` |
| **Cache** | Redis via Memurai (Windows-native) or WSL2 |
| **File Storage** | Local disk `D:\AlMalUploads` (or S3-compatible) |
| **CDN** | Cloudflare (DNS, caching, DDoS protection) |
| **SSL** | Cloudflare SSL or Let's Encrypt via win-acme |
| **CI/CD** | GitHub Actions → Web Deploy to IIS |

### 19.2 IIS Sites

| IIS Site | Domain | App Pool |
|----------|--------|----------|
| `AlMal-Web` | `almal.codexkw.co` | `AlMal-Web-Pool` (No Managed Code) |
| `AlMal-Admin` | `almal-admin.codexkw.co` | `AlMal-Admin-Pool` (No Managed Code) |
| `AlMal-API` | `almal-api.codexkw.co` | `AlMal-API-Pool` (No Managed Code) |

### 19.3 IIS Configuration

- Separate IIS Application Pool per environment (AlMal-Dev, AlMal-Staging, AlMal-Prod)
- Application pool: No Managed Code (ASP.NET Core runs out-of-process)
- `web.config`: generated by publish, points to `AlMal.Web.exe`
- Environment variable `ASPNETCORE_ENVIRONMENT` set per app pool

### 19.4 CI/CD Pipeline (GitHub Actions)

#### Pipeline 1: CI — On every Pull Request to `main`

**File:** `.github/workflows/ci.yml`
**Trigger:** `pull_request` to `main` branch

```
Steps:
1. Checkout code
2. Setup .NET 9 SDK
3. dotnet restore
4. dotnet build --no-restore
5. dotnet test --no-build
6. Report results as PR check
```

#### Pipeline 2: Deploy Web — On merge to `main`

**File:** `.github/workflows/deploy-web.yml`
**Trigger:** `push` to `main` (only when `src/AlMal.Web/**` or `src/AlMal.Domain/**` or `src/AlMal.Application/**` or `src/AlMal.Infrastructure/**` changed)

```
Steps:
1. Checkout code
2. Setup .NET 9 SDK
3. dotnet publish src/AlMal.Web -c Release -o ./publish-web
4. Apply EF Core migrations to production database
5. Web Deploy to IIS site AlMal-Web (almal.codexkw.co)
6. Hit health check: GET https://almal.codexkw.co/health
```

#### Pipeline 3: Deploy Admin — On merge to `main`

**File:** `.github/workflows/deploy-admin.yml`
**Trigger:** `push` to `main` (only when `src/AlMal.Admin/**` or shared projects changed)

```
Steps:
1-3. Same as web
4. dotnet publish src/AlMal.Admin -c Release -o ./publish-admin
5. Web Deploy to IIS site AlMal-Admin (almal-admin.codexkw.co)
6. Hit health check: GET https://almal-admin.codexkw.co/health
```

#### Pipeline 4: Deploy API — On merge to `main`

**File:** `.github/workflows/deploy-api.yml`
**Trigger:** `push` to `main` (only when `src/AlMal.API/**` or shared projects changed)

```
Steps:
1-3. Same as web
4. dotnet publish src/AlMal.API -c Release -o ./publish-api
5. Web Deploy to IIS site AlMal-API (almal-api.codexkw.co)
6. Hit health check: GET https://almal-api.codexkw.co/health
```

#### Required GitHub Secrets

| Secret | Value | Used By |
|--------|-------|---------|
| `SQL_CONNECTION_STRING` | Production SQL Server connection string | All deploy workflows |
| `IIS_SERVER_URL` | Web Deploy server URL | All deploy workflows |
| `IIS_WEB_SITE_NAME` | `AlMal-Web` | deploy-web |
| `IIS_ADMIN_SITE_NAME` | `AlMal-Admin` | deploy-admin |
| `IIS_API_SITE_NAME` | `AlMal-API` | deploy-api |
| `WEB_DEPLOY_USER` | Web Deploy username | All deploy workflows |
| `WEB_DEPLOY_PASSWORD` | Web Deploy password | All deploy workflows |

#### Health Check Endpoint

All 3 projects must expose `GET /health` returning:

```json
{
  "status": "healthy",
  "version": "1.0.0",
  "database": "connected",
  "redis": "connected",
  "timestamp": "2026-03-01T12:00:00Z"
}
```

### 19.5 Configuration Files

- `appsettings.json` — shared config
- `appsettings.Development.json` — local dev
- `appsettings.Staging.json` — staging
- `appsettings.Production.json` — production (API keys, connection strings)
- **NEVER** commit production appsettings to git. Use environment variables or encrypted config.

---

## 20. Development Phases & Task Breakdown

Total estimated: **26 weeks** (web + admin) + **14 weeks** (Flutter). Each phase has concrete deliverables that can be tested independently.

### Phase 1: Foundation (Weeks 1-4)

**Goal: Running application skeleton with auth and base UI.**

1. Create .NET 9 solution with all 7 projects (Domain, Application, Infrastructure, Web, Admin, API, BackgroundServices)
2. Set up SQL Server database with EF Core. Create ALL entity classes and DbContext. Run initial migration.
3. Implement ASP.NET Identity with ApplicationUser. Registration, Login, JWT for API.
4. Build master layout: RTL Bootstrap 5, dark/light mode toggle, responsive nav, bottom nav bar for mobile.
5. Admin panel skeleton: login, dashboard shell, sidebar navigation.
6. Set up Redis connection, Hangfire dashboard, Serilog logging.
7. Seed database: sectors, sample stocks, admin user.

### Phase 2: Market Core (Weeks 5-10)

**Goal: Fully functional market data display with real-time updates.**

1. Build BoursakuwaitScraper: scrape stock prices, indices, sectors, disclosures from boursakuwait.com.kw
2. Implement `IMarketDataProvider` interface (swap scraper for API later)
3. Market Dashboard page: indices ticker, gainers/losers, most traded, heatmap
4. Stock Detail page: all 6 tabs (Overview, Chart, Movement placeholder, Ratios, Disclosures, Order Book)
5. TradingView Lightweight Charts integration with OHLCV data and indicators
6. SignalR hub for real-time price and index updates
7. Watchlist CRUD + price alert creation UI
8. All market REST API endpoints
9. Admin: Stock/Sector CRUD, scraping status monitor

### Phase 3: News & AI (Weeks 11-14)

**Goal: News feed with AI-powered analysis features.**

1. NewsData.io integration: background fetcher, deduplication, stock matching
2. News feed UI: card-based feed with filters and sentiment badges
3. AI movement explainer: "Explain Movement" feature on stock pages
4. AI disclosure summarizer: auto-generated summaries for new disclosures
5. AI news context engine: "Understand Context" button functionality
6. Admin: News management, source configuration, sentiment override

### Phase 4: Community (Weeks 15-18)

**Goal: Full social features with posts, follows, and moderation.**

1. User profiles & follow system: public profiles, follower/following
2. Post creation with media upload: text, image, video posts with stock tags
3. Feed system (General + Following): two-feed architecture with real-time updates
4. Comments, likes, reposts: full interaction system
5. Analyst badge system: badge types, verification flow
6. Admin: User management & moderation, content moderation queue, badge approval

### Phase 5: Academy & Simulation (Weeks 19-22)

**Goal: Educational content system and paper trading.**

1. Course & lesson management: course catalog, lesson viewer
2. Quiz system: quiz engine with grading
3. Certificate generation: PDF certificates with verification
4. Simulation portfolio engine: virtual trading at market prices
5. Performance reporting: P&L, risk, benchmark comparison
6. Admin: Course CRUD, quiz builder, analytics dashboard

### Phase 6: WhatsApp & Polish (Weeks 23-26)

**Goal: WhatsApp alerts, performance optimization, go-live.**

1. WhatsApp Business API integration: alert delivery system
2. WhatsApp market assistant: Q&A bot for market queries
3. SEO optimization: Arabic SEO, meta tags, structured data
4. Performance optimization: caching, lazy loading, CDN setup
5. UAT & bug fixes: user acceptance testing
6. Production deployment: go-live web + admin

### Phase 7: Flutter Mobile App (Weeks 27-40)

**Goal: Native mobile app reusing web API and UI patterns.**

- Uses same REST API endpoints built in Phases 1-6
- Mirrors mobile web layout: bottom nav, card layouts, swipe gestures
- State management: Provider or Riverpod
- Push notifications: Firebase Cloud Messaging
- WhatsApp deep linking for alert tap-through

---

*End of PRD | Al-Mal Channel | قناة المال*
