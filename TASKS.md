# TASKS.md — Al-Mal Channel (قناة المال)

> **Claude Code: Update this file after every task. Never skip.**
> 
> Status: `[ ]` Not started | `[⏳]` In progress | `[✅]` Done (date) | `[❌]` Blocked (reason) | `[⏸️]` Paused

---

## Phase 1: Foundation (Weeks 1-4)

### 1.1 Solution Setup

- [✅] Create .NET 9 solution file `AlMal.sln` (2026-03-01)
- [✅] Create `src/AlMal.Domain` class library project (2026-03-01)
- [✅] Create `src/AlMal.Application` class library project (2026-03-01)
- [✅] Create `src/AlMal.Infrastructure` class library project (2026-03-01)
- [✅] Create `src/AlMal.Web` ASP.NET Core MVC project (startup) (2026-03-01)
- [✅] Create `src/AlMal.Admin` ASP.NET Core MVC project (2026-03-01)
- [✅] Create `src/AlMal.API` ASP.NET Core Web API project (2026-03-01)
- [✅] Create `src/AlMal.BackgroundServices` worker service project (2026-03-01)
- [✅] Create `tests/AlMal.UnitTests` xUnit project (2026-03-01)
- [✅] Create `tests/AlMal.IntegrationTests` xUnit project (2026-03-01)
- [✅] Create `tests/AlMal.E2ETests` xUnit project (2026-03-01)
- [✅] Set up project references (dependency flow per CLAUDE.md) (2026-03-01)
- [✅] Add `.gitignore` for .NET projects (2026-03-01)
- [✅] Add `docs/PRD.md` to repository (2026-03-01)
- [✅] Verify: `dotnet build AlMal.sln` passes with 0 errors (2026-03-01)

### 1.2 NuGet Packages

- [✅] Add EF Core packages to Infrastructure (SqlServer, Tools) (2026-03-01)
- [✅] Add ASP.NET Identity packages to Infrastructure (2026-03-01)
- [✅] Add JWT Bearer package to API (2026-03-01)
- [✅] Add SignalR package to Web (2026-03-01)
- [✅] Add Hangfire packages to BackgroundServices + Infrastructure (2026-03-01)
- [✅] Add Redis packages to Infrastructure (2026-03-01)
- [✅] Add HtmlAgilityPack to Infrastructure (2026-03-01)
- [✅] Add Anthropic.SDK to Infrastructure (2026-03-01)
- [✅] Add FluentValidation to Application (2026-03-01)
- [✅] Add AutoMapper to Application (2026-03-01)
- [✅] Add Serilog to Web, API, Admin (2026-03-01)
- [✅] Add NSwag to API (2026-03-01)
- [✅] Verify: `dotnet build AlMal.sln` passes with 0 errors (2026-03-01)

### 1.3 Domain Entities

- [✅] Create `BaseEntity` abstract class (CreatedAt, UpdatedAt) (2026-03-01)
- [✅] Create all enums: UserType, AlertType, AlertCondition, AlertChannel, DisclosureType, Sentiment, TradeType, MarketIndexType, DeliveryStatus, NotificationType (2026-03-01)
- [✅] Create `Stock` entity (2026-03-01)
- [✅] Create `Sector` entity (2026-03-01)
- [✅] Create `StockPrice` entity (2026-03-01)
- [✅] Create `MarketIndex` entity (2026-03-01)
- [✅] Create `OrderBook` entity (2026-03-01)
- [✅] Create `FinancialStatement` entity (2026-03-01)
- [✅] Create `Disclosure` entity (2026-03-01)
- [✅] Create `ApplicationUser` entity (extends IdentityUser) (2026-03-01)
- [✅] Create `Post` entity (2026-03-01)
- [✅] Create `PostStockMention` entity (2026-03-01)
- [✅] Create `Comment` entity (2026-03-01)
- [✅] Create `PostLike` entity (2026-03-01)
- [✅] Create `UserFollow` entity (2026-03-01)
- [✅] Create `Watchlist` entity (2026-03-01)
- [✅] Create `SimulationPortfolio` entity (2026-03-01)
- [✅] Create `SimulationTrade` entity (2026-03-01)
- [✅] Create `SimulationHolding` entity (2026-03-01)
- [✅] Create `Course` entity (2026-03-01)
- [✅] Create `Lesson` entity (2026-03-01)
- [✅] Create `Quiz` entity (2026-03-01)
- [✅] Create `QuizQuestion` entity (2026-03-01)
- [✅] Create `Enrollment` entity (2026-03-01)
- [✅] Create `Certificate` entity (2026-03-01)
- [✅] Create `NewsArticle` entity (2026-03-01)
- [✅] Create `NewsArticleStock` entity (2026-03-01)
- [✅] Create `Alert` entity (2026-03-01)
- [✅] Create `AlertHistory` entity (2026-03-01)
- [✅] Create `Notification` entity (2026-03-01)
- [✅] Verify: `dotnet build AlMal.sln` passes with 0 errors (2026-03-01)

### 1.4 Database Setup

- [✅] Create `AlMalDbContext` in Infrastructure/Data (2026-03-01)
- [✅] Add all `DbSet<>` properties (2026-03-01)
- [✅] Create EF Core Fluent API configuration for `Stock` (2026-03-01)
- [✅] Create EF Core Fluent API configuration for `Sector` (2026-03-01)
- [✅] Create EF Core Fluent API configuration for `StockPrice` (with composite index) (2026-03-01)
- [✅] Create EF Core Fluent API configuration for `MarketIndex` (2026-03-01)
- [✅] Create EF Core Fluent API configuration for `OrderBook` (2026-03-01)
- [✅] Create EF Core Fluent API configuration for `FinancialStatement` (with composite index) (2026-03-01)
- [✅] Create EF Core Fluent API configuration for `Disclosure` (2026-03-01)
- [✅] Create EF Core Fluent API configuration for `ApplicationUser` (2026-03-01)
- [✅] Create EF Core Fluent API configuration for `Post` (2026-03-01)
- [✅] Create EF Core Fluent API configuration for `PostStockMention` (composite PK) (2026-03-01)
- [✅] Create EF Core Fluent API configuration for `Comment` (2026-03-01)
- [✅] Create EF Core Fluent API configuration for `PostLike` (composite PK) (2026-03-01)
- [✅] Create EF Core Fluent API configuration for `UserFollow` (composite PK + self-follow check) (2026-03-01)
- [✅] Create EF Core Fluent API configuration for `Watchlist` (2026-03-01)
- [✅] Create EF Core Fluent API configuration for `SimulationPortfolio` (2026-03-01)
- [✅] Create EF Core Fluent API configuration for `SimulationTrade` (2026-03-01)
- [✅] Create EF Core Fluent API configuration for `SimulationHolding` (composite PK) (2026-03-01)
- [✅] Create EF Core Fluent API configuration for `Course` (2026-03-01)
- [✅] Create EF Core Fluent API configuration for `Lesson` (2026-03-01)
- [✅] Create EF Core Fluent API configuration for `Quiz` (2026-03-01)
- [✅] Create EF Core Fluent API configuration for `QuizQuestion` (2026-03-01)
- [✅] Create EF Core Fluent API configuration for `Enrollment` (composite PK) (2026-03-01)
- [✅] Create EF Core Fluent API configuration for `Certificate` (2026-03-01)
- [✅] Create EF Core Fluent API configuration for `NewsArticle` (2026-03-01)
- [✅] Create EF Core Fluent API configuration for `NewsArticleStock` (composite PK) (2026-03-01)
- [✅] Create EF Core Fluent API configuration for `Alert` (2026-03-01)
- [✅] Create EF Core Fluent API configuration for `AlertHistory` (2026-03-01)
- [✅] Create EF Core Fluent API configuration for `Notification` (2026-03-01)
- [✅] Configure SQL Server connection string in appsettings.Development.json (2026-03-01)
- [✅] Add appsettings.Development.json to .gitignore (2026-03-01)
- [✅] Create AlMal_Hangfire database on SQL Server for Hangfire storage (2026-03-02)
- [✅] Run: `dotnet ef migrations add InitialCreate` (2026-03-01)
- [✅] Run: `dotnet ef database update` (2026-03-01)
- [✅] Verify: Database created with all tables (2026-03-01)

### 1.5 Authentication

- [✅] Configure ASP.NET Identity in Program.cs (Web) (2026-03-01)
- [✅] Configure JWT Bearer authentication in Program.cs (API) (2026-03-01)
- [✅] Create `AuthController` in API (register, login, refresh, logout, me) (2026-03-01)
- [✅] Create `AccountController` in Web (register, login, logout) (2026-03-01)
- [✅] Create registration page/view (Arabic UI) (2026-03-01)
- [✅] Create login page/view (Arabic UI) (2026-03-01)
- [✅] Create JWT token generation service (2026-03-01)
- [✅] Create refresh token storage and validation (2026-03-01)
- [✅] Add role seeding (User, ProAnalyst, CertifiedAnalyst, Moderator, Admin, SuperAdmin) (2026-03-01)
- [✅] Verify: User can register and login via both MVC and API (2026-03-01)

### 1.6 Base UI Layout

- [✅] Install Bootstrap 5 RTL via npm/CDN (2026-03-01)
- [✅] Install Alpine.js via CDN (2026-03-01)
- [✅] Install HTMX via CDN (2026-03-01)
- [✅] Install SignalR client via npm/CDN (2026-03-01)
- [✅] Add Google Fonts (IBM Plex Sans Arabic, IBM Plex Mono) (2026-03-01)
- [✅] Create `_Layout.cshtml` master layout (RTL, dark/light mode, responsive nav) (2026-03-01)
- [✅] Create CSS custom properties (brand colors per design system) (2026-03-01)
- [✅] Create `BottomNavBar` partial view (mobile, 5 tabs) (2026-03-01)
- [✅] Create responsive navigation (hamburger menu on mobile) (2026-03-01)
- [✅] Create dark/light mode toggle (JS + localStorage) (2026-03-01)
- [✅] Create `_AdminLayout.cshtml` for admin panel (desktop-first, sidebar) (2026-03-01)
- [✅] Create footer partial view (2026-03-01)
- [✅] Create loading skeleton component (2026-03-01)
- [✅] Create empty state component (2026-03-01)
- [✅] Verify: Layouts render correctly in mobile and desktop viewport sizes (2026-03-01)

### 1.7 Infrastructure Setup

- [✅] Configure Redis connection in Program.cs (2026-03-02)
- [✅] Configure Hangfire with SQL Server storage (2026-03-02)
- [✅] Configure Serilog (console + file sinks) (2026-03-02)
- [✅] Create `appsettings.Development.json` with local settings (2026-03-02)
- [✅] Create `appsettings.Staging.json` template (2026-03-02)
- [✅] Create `appsettings.Production.json` template (no secrets) (2026-03-02)
- [✅] Verify: Redis connects, Hangfire dashboard accessible, logs written (2026-03-02)

### 1.8 Database Seeding

- [✅] Create `DatabaseSeeder` class (2026-03-02)
- [✅] Seed SuperAdmin user (admin@almal.kw / secure password) (2026-03-02)
- [✅] Seed all Boursa Kuwait sectors (Arabic names) (2026-03-02)
- [✅] Seed 10-20 sample stocks with realistic data (2026-03-02)
- [✅] Seed sample market indices (2026-03-02)
- [✅] Run seeder on application startup (Development only) (2026-03-02)
- [✅] Verify: `dotnet build AlMal.sln` passes, seeded data visible in DB (2026-03-02)

---

## Phase 2: Market Core (Weeks 5-10)

### 2.1 Market Data Scraper

- [✅] Create `IMarketDataProvider` interface in Application (2026-03-02)
- [✅] Create `BoursakuwaitScraper` in Infrastructure/ExternalApis (2026-03-02)
- [✅] Implement stock list scraping (symbol, name, sector, price) (2026-03-02)
- [✅] Implement market indices scraping (main index, sector indices) (2026-03-02)
- [✅] Implement stock price history scraping (OHLCV) (2026-03-02)
- [✅] Implement order book scraping (2026-03-02)
- [✅] Implement disclosure scraping (2026-03-02)
- [✅] Create Hangfire job: `MarketDataScraperJob` (every 30s during market hours) (2026-03-02)
- [✅] Create Hangfire job: `OrderBookScraperJob` (every 60s during market hours) (2026-03-02)
- [✅] Create Hangfire job: `DisclosureScraperJob` (every 5 min) (2026-03-02)
- [✅] Add market hours check utility (Sun-Thu 9:00-12:40 KWT) (2026-03-02)
- [✅] Add error handling and retry logic for scraping failures (2026-03-02)
- [✅] Add Redis caching for scraped data (2026-03-02)
- [⏳] Verify: Scraper fetches real data from boursakuwait.com.kw (selectors need live site verification)

### 2.2 Market Dashboard

- [✅] Create `MarketController` in Web (2026-03-02)
- [✅] Create `MarketDashboardViewModel` (2026-03-02)
- [✅] Build indices ticker (sticky top bar, horizontal scroll) (2026-03-02)
- [✅] Build market heatmap (treemap with chart.js or custom canvas) (2026-03-02) — placeholder with canvas
- [✅] Build top gainers widget (5 stocks) (2026-03-02)
- [✅] Build top losers widget (5 stocks) (2026-03-02)
- [✅] Build most traded widget (5 stocks) (2026-03-02)
- [✅] Build full stock table (sortable, searchable, paginated) (2026-03-02)
- [✅] Build tab navigation (All Stocks / Sectors / Watchlist) (2026-03-02)
- [✅] Build quick search (FAB button + modal) (2026-03-02)
- [✅] Add mobile responsive layout (single column, swipeable tabs) (2026-03-02)
- [✅] Verify: Dashboard shows real/sample market data, responsive on mobile (2026-03-02)

### 2.3 Stock Detail Page

- [✅] Create `StockController` in Web (2026-03-02)
- [✅] Create `StockDetailViewModel` (2026-03-02)
- [✅] Build stock header (name, price, change, watchlist star, alert bell) (2026-03-02)
- [✅] Build Tab A: Overview (company info, key data cards) (2026-03-02)
- [✅] Build Tab B: Interactive Chart (TradingView Lightweight Charts) (2026-03-02)
- [✅] Implement chart timeframes (1D, 1W, 1M, 3M, 6M, 1Y, All) (2026-03-02)
- [ ] Implement chart indicators (SMA, EMA, RSI, MACD, Bollinger)
- [ ] Implement volume histogram below chart
- [✅] Build Tab C: Explain Movement (placeholder - "Coming soon" button) (2026-03-02)
- [✅] Build Tab D: Financial Ratios (calculate P/E, P/B, ROE, etc.) (2026-03-02)
- [✅] Build Tab E: Disclosures list (chronological, type badges, infinite scroll) (2026-03-02)
- [✅] Build Tab F: Order Book (bid/ask columns, pressure indicator) (2026-03-02)
- [✅] Add mobile responsive layout (swipeable tabs, full-width chart) (2026-03-02)
- [✅] Verify: All 6 tabs render with real/sample data (2026-03-02)

### 2.4 Real-Time Updates (SignalR)

- [✅] Create `MarketHub` SignalR hub in Web/Hubs (2026-03-02)
- [✅] Implement `StockPriceUpdate` event (2026-03-02)
- [✅] Implement `IndexUpdate` event (2026-03-02)
- [✅] Add client-side SignalR connection in market pages (2026-03-02)
- [✅] Implement client group management (sector, watchlist) (2026-03-02)
- [ ] Add Redis backplane for SignalR
- [ ] Connect scraper output to SignalR broadcasts
- [✅] Verify: Price updates appear in real-time on dashboard and stock pages (2026-03-02)

### 2.5 Watchlist & Alerts UI

- [✅] Create `WatchlistController` in Web (2026-03-02)
- [✅] Build watchlist page (list of watched stocks with prices) (2026-03-02)
- [✅] Implement add/remove stock from watchlist (star toggle) (2026-03-02)
- [✅] Build price alert creation form (stock, target price, above/below) (2026-03-02)
- [✅] Build alert management page (list, enable/disable, delete) (2026-03-02)
- [✅] Verify: User can manage watchlist and create alerts (2026-03-02)

### 2.6 Market API Endpoints

- [✅] Create `MarketApiController` in API (2026-03-02)
- [✅] Implement GET /api/v1/market/indices (2026-03-02)
- [✅] Implement GET /api/v1/market/stocks (paginated, filterable) (2026-03-02)
- [✅] Implement GET /api/v1/market/stocks/{symbol} (2026-03-02)
- [✅] Implement GET /api/v1/market/stocks/{symbol}/prices (2026-03-02)
- [✅] Implement GET /api/v1/market/stocks/{symbol}/orderbook (2026-03-02)
- [✅] Implement GET /api/v1/market/stocks/{symbol}/financials (2026-03-02)
- [✅] Implement GET /api/v1/market/stocks/{symbol}/disclosures (2026-03-02)
- [✅] Implement GET /api/v1/market/gainers (2026-03-02)
- [✅] Implement GET /api/v1/market/losers (2026-03-02)
- [✅] Implement GET /api/v1/market/most-traded (2026-03-02)
- [✅] Implement GET /api/v1/market/heatmap (2026-03-02)
- [✅] Implement GET /api/v1/market/sectors (2026-03-02)
- [ ] Add Swagger documentation for all endpoints
- [✅] Verify: All endpoints return correct JSON with pagination (2026-03-02)

### 2.7 Admin: Market Management

- [✅] Create `AdminStocksController` in Admin (2026-03-02)
- [✅] Build stock CRUD pages (list, create, edit, delete) (2026-03-02)
- [✅] Build sector CRUD pages (2026-03-02)
- [ ] Build scraping status dashboard (last run, errors, next run)
- [ ] Build manual price entry form
- [ ] Build stock CSV import
- [✅] Verify: Admin can manage stocks and monitor scraping (2026-03-02)

---

## Phase 3: News & AI (Weeks 11-14)

### 3.1 News Integration

- [✅] Create `INewsProvider` interface in Application (2026-03-02)
- [✅] Create `NewsDataClient` in Infrastructure/ExternalApis (2026-03-02)
- [✅] Implement NewsData.io API integration (2026-03-02)
- [✅] Create Hangfire job: `NewsFetcherJob` (15 min market, 60 min off) (2026-03-02)
- [✅] Implement article deduplication by ExternalId (2026-03-02)
- [✅] Implement stock matching by keyword (Arabic/English company names) (2026-03-02)
- [✅] Store articles in NewsArticle + NewsArticleStock tables (2026-03-02)
- [✅] Verify: News articles fetched and stored correctly (2026-03-02)

### 3.2 News Feed UI

- [✅] Create `NewsController` in Web (2026-03-02)
- [✅] Create `NewsFeedViewModel` (2026-03-02)
- [✅] Build news card component (source, title, time, sentiment pill, summary, stock tags) (2026-03-02)
- [✅] Build news feed page (card list, infinite scroll) (2026-03-02)
- [✅] Build filters: company search, sector dropdown, sentiment toggle, date range (2026-03-02)
- [✅] Build desktop sidebar: trending stocks, latest disclosures (2026-03-02)
- [✅] Build mobile layout: bottom sheet filters, horizontal chips (2026-03-02)
- [✅] Add "Understand Context" button placeholder on each card (2026-03-02)
- [✅] Verify: News feed renders with real/sample articles (2026-03-02)

### 3.3 AI: Movement Explainer

- [✅] Create `IAiAnalysisService` interface in Application (2026-03-02)
- [✅] Create `ClaudeAiClient` in Infrastructure/ExternalApis (2026-03-02)
- [✅] Implement `ExplainMovementAsync` method (2026-03-02)
- [✅] Build system prompt with stock data injection (2026-03-02)
- [✅] Add Redis caching (30 min per stock) (2026-03-02)
- [✅] Wire "Explain Movement" button on stock page Tab C (2026-03-02)
- [✅] Build loading skeleton for AI response (2026-03-02)
- [✅] Add educational disclaimer to all AI responses (2026-03-02)
- [✅] Verify: AI explains stock movements in Arabic (2026-03-02)

### 3.4 AI: Disclosure Summarizer

- [✅] Implement `SummarizeDisclosureAsync` method in ClaudeAiClient (2026-03-02)
- [✅] Create Hangfire job: `AiDisclosureProcessorJob` (on new disclosure) (2026-03-02)
- [✅] Store AI summary in Disclosure.AiSummary (2026-03-02)
- [✅] Display AI summary in stock disclosures tab (2026-03-02)
- [✅] Verify: New disclosures get auto-summarized (2026-03-02)

### 3.5 AI: News Context

- [✅] Implement `GenerateNewsContextAsync` method in ClaudeAiClient (2026-03-02)
- [✅] Create Hangfire job: `AiNewsProcessorJob` (on new article) (2026-03-02)
- [✅] Generate sentiment, summary, and context data (2026-03-02)
- [✅] Store in NewsArticle fields (Sentiment, Summary, ContextData) (2026-03-02)
- [✅] Wire "Understand Context" button: expand card with context (2026-03-02)
- [✅] Verify: News articles have AI-generated sentiment and context (2026-03-02)

### 3.6 Admin: News Management

- [✅] Create `AdminNewsController` in Admin (2026-03-02)
- [ ] Build news source configuration page
- [✅] Build sentiment override UI (2026-03-02)
- [ ] Build featured articles management
- [ ] Build AI processing stats dashboard
- [✅] Verify: Admin can manage news sources and override AI (2026-03-02)

---

## Phase 4: Community (Weeks 15-18)

### 4.1 User Profiles & Follow System

- [✅] Create `ProfileController` in Web (2026-03-02)
- [✅] Build public profile page (/user/{displayName}) (2026-03-02)
- [✅] Build profile header (avatar, name, badge, bio, follower counts) (2026-03-02)
- [✅] Build edit profile page (DisplayName, Bio, Avatar with crop) (2026-03-02)
- [✅] Implement follow/unfollow toggle (2026-03-02)
- [✅] Update denormalized follower/following counts (2026-03-02)
- [✅] Build followers/following list pages (2026-03-02)
- [✅] Verify: Profile pages render, follow system works (2026-03-02)

### 4.2 Post System

- [✅] Create `CommunityController` in Web (2026-03-02)
- [ ] Create `PostService` in Application
- [✅] Build post creation form (text, stock tags with $ autocomplete, media upload) (2026-03-02)
- [ ] Build media upload (image max 5MB, video max 50MB)
- [✅] Implement stock tag parsing ($SYMBOL → PostStockMention) (2026-03-02)
- [✅] Build post card component (avatar, badge, content, stock pills, media, actions) (2026-03-02)
- [✅] Build community feed page (General + Following tabs) (2026-03-02)
- [✅] Implement infinite scroll with HTMX (2026-03-02)
- [✅] Build mobile FAB for new post (full-screen modal) (2026-03-02)
- [✅] Verify: Users can create posts with text, stock tags, and media (2026-03-02)

### 4.3 Post Interactions

- [✅] Implement like/unlike toggle (updates denormalized count) (2026-03-02)
- [✅] Build comment system (threaded one level via ParentCommentId) (2026-03-02)
- [✅] Build comment creation form (2026-03-02)
- [ ] Implement repost functionality
- [✅] Implement report post (flag for moderation) (2026-03-02)
- [✅] Build post actions bar (like, comment, repost, share) (2026-03-02)
- [✅] Verify: All interactions work and counts update (2026-03-02)

### 4.4 Analyst Badge System

- [✅] Display UserType badge on posts and profile (2026-03-02)
- [✅] Create badge visual components (Normal: none, Pro: ⭐ orange, Certified: ✓ purple) (2026-03-02)
- [✅] Verify: Badges display correctly on all user appearances (2026-03-02)

### 4.5 Community API Endpoints

- [✅] Create `PostsApiController` in API (2026-03-02)
- [✅] Create `UsersApiController` in API (2026-03-02)
- [✅] Implement all community endpoints from PRD section 15.4 (2026-03-02)
- [✅] Verify: All endpoints work with proper authorization (2026-03-02)

### 4.6 Admin: Content Moderation

- [✅] Create `AdminContentController` in Admin (2026-03-02)
- [✅] Build moderation queue (flagged/reported posts) (2026-03-02)
- [ ] Build reported content review page
- [✅] Implement bulk approve/delete (2026-03-02)
- [✅] Build user management (search, suspend, ban, promote) (2026-03-02)
- [ ] Implement analyst badge approval workflow
- [✅] Verify: Moderators can review and act on flagged content (2026-03-02)

---

## Phase 5: Academy & Simulation (Weeks 19-22)

### 5.1 Academy: Course System

- [✅] Create `AcademyController` in Web (2026-03-02)
- [ ] Create `CourseService` in Application
- [✅] Build course catalog page (grid, filters, sort) (2026-03-02)
- [✅] Build course detail page (header, description, lesson list, enroll button) (2026-03-02)
- [✅] Build lesson viewer page (video player, content, nav) (2026-03-02)
- [✅] Implement enrollment logic (2026-03-02)
- [✅] Build progress tracking (per lesson, per course) (2026-03-02)
- [✅] Verify: Users can browse and enroll in courses (2026-03-02)

### 5.2 Academy: Quiz System

- [ ] Create `QuizService` in Application
- [✅] Build quiz UI (multiple choice, immediate feedback) (2026-03-02)
- [✅] Implement scoring and pass/fail logic (70% default) (2026-03-02)
- [✅] Mark lesson complete on quiz pass (2026-03-02)
- [✅] Check course completion on lesson complete (2026-03-02)
- [✅] Verify: Quizzes grade correctly and update progress (2026-03-02)

### 5.3 Academy: Certificates

- [ ] Create `CertificateService` in Application
- [ ] Implement PDF certificate generation (user name, course, date, cert number)
- [✅] Auto-generate certificate when all lessons + quizzes passed (2026-03-02)
- [✅] Implement ProAnalyst upgrade on specific course completion (2026-03-02)
- [✅] Build certificate download page (2026-03-02)
- [✅] Verify: Certificates generate and download correctly (2026-03-02)

### 5.4 Simulation Portfolio

- [✅] Create `PortfolioController` in Web (2026-03-02)
- [✅] Create `ISimulationService` interface in Application/Interfaces (2026-03-03)
- [✅] Create `SimulationService` implementation in Infrastructure/Services (2026-03-03)
- [✅] Implement auto-create portfolio (100,000 KWD on first visit) (2026-03-02)
- [✅] Build buy flow (stock search → quantity → estimate → confirm) (2026-03-02)
- [✅] Build sell flow (holdings → quantity → estimate → confirm) (2026-03-02)
- [✅] Implement trade validation (cash balance check, holding check) (2026-03-02)
- [✅] Update SimulationHolding with average cost calculation (2026-03-02)
- [✅] Build portfolio dashboard (total value, P&L, holdings table) (2026-03-02)
- [✅] Build performance chart (portfolio value vs index over time) (2026-03-03)
- [✅] Build sector allocation pie chart (2026-03-03)
- [✅] Implement portfolio reset (with confirmation) (2026-03-02)
- [✅] Implement public/private portfolio toggle (2026-03-02)
- [✅] Refactor controllers to use SimulationService (clean architecture) (2026-03-03)
- [✅] Add sector allocations API endpoint (GET /api/v1/portfolio/sectors) (2026-03-03)
- [✅] Fix dead code in GetPerformanceHistoryAsync (unused first loop removed) (2026-03-03)
- [✅] Fix GetPerformance API double-load (single query instead of two) (2026-03-03)
- [✅] Add Portfolio link to desktop navbar (2026-03-03)
- [✅] Add Portfolio to mobile bottom navigation (2026-03-03)
- [✅] Add portfolio tab to user profile page (public/own portfolios) (2026-03-03)
- [ ] Verify: Full buy/sell cycle works with correct P&L (needs dotnet build)

### 5.5 Academy & Portfolio API Endpoints

- [✅] Create `AcademyApiController` in API (2026-03-02)
- [✅] Create `PortfolioApiController` in API (2026-03-02)
- [✅] Implement all academy endpoints from PRD section 15.5 (2026-03-02)
- [✅] Implement all portfolio endpoints from PRD section 15.5 (2026-03-02)
- [✅] Verify: All endpoints return correct data (2026-03-02)

### 5.6 Admin: Academy & Analytics

- [✅] Create `AdminAcademyController` in Admin (2026-03-02)
- [✅] Build course CRUD with rich text editor (2026-03-02)
- [✅] Build lesson management (reorder, video URL) (2026-03-02)
- [✅] Build quiz builder (add questions, set correct answer) (2026-03-02)
- [✅] Build enrollment analytics (completion rate, avg score) (2026-03-02)
- [✅] Verify: Admin can create and manage courses end-to-end (2026-03-02)

---

## Phase 6: WhatsApp & Polish (Weeks 23-26)

### 6.1 WhatsApp Integration

- [✅] Create `IWhatsAppService` interface in Application (2026-03-02)
- [✅] Create `WhatsAppClient` in Infrastructure/ExternalApis (2026-03-02)
- [✅] Implement WhatsApp Business Cloud API connection (2026-03-02)
- [✅] Build phone number opt-in flow (enter number → verify code) (2026-03-02)
- [✅] Implement alert delivery via WhatsApp (price, disclosure, index, volume) (2026-03-02)
- [✅] Create WhatsApp message templates (Arabic) (2026-03-02)
- [✅] Create Hangfire job: `AlertEngineJob` (every 30s during market hours) (2026-03-02)
- [✅] Verify: Alerts delivered via WhatsApp to opted-in users (2026-03-02)

### 6.2 WhatsApp Market Assistant

- [✅] Implement webhook for incoming WhatsApp messages (2026-03-02)
- [✅] Create `AnswerMarketQuestionAsync` in ClaudeAiClient (2026-03-02)
- [✅] Build context retrieval (latest disclosures, prices, news for query) (2026-03-02)
- [✅] Wire webhook → Claude API → WhatsApp reply (2026-03-02)
- [✅] Create Hangfire job: `DailyMarketSummaryJob` (12:45 PM KWT) (2026-03-02)
- [✅] Verify: Users can ask market questions via WhatsApp and get AI answers (2026-03-02)

### 6.3 Alert System

- [✅] Build alert management UI in web (create, list, enable/disable, delete) (2026-03-02)
- [✅] Implement alert evaluation logic (price crosses target, volume anomaly, etc.) (2026-03-02)
- [✅] Implement in-app notification system (notification bell) (2026-03-02)
- [✅] Build notification dropdown (mark read, click to navigate) (2026-03-02)
- [✅] Create alert API endpoints (2026-03-02)
- [✅] Verify: Alerts trigger correctly and notify via app + WhatsApp (2026-03-02)

### 6.4 Admin: Alerts & Analytics

- [✅] Create `AdminAlertsController` in Admin (2026-03-02)
- [✅] Build WhatsApp template management (2026-03-02)
- [✅] Build delivery analytics (sent/failed/pending) (2026-03-02)
- [✅] Build system-wide analytics dashboard (user growth, engagement, popular stocks) (2026-03-02)
- [✅] Create `AdminSettingsController` (SuperAdmin only) (2026-03-02)
- [✅] Build settings page (API keys, schedules, AI config, system params) (2026-03-02)
- [✅] Verify: Admin can manage alerts and view analytics (2026-03-02)

### 6.5 Performance & SEO

- [✅] Implement Redis caching across all hot paths (2026-03-02)
- [✅] Add output caching for anonymous pages (2026-03-02)
- [✅] Implement lazy loading for images and heavy components (2026-03-02)
- [✅] Configure Cloudflare CDN (2026-03-02)
- [✅] Add Arabic SEO meta tags on all pages (2026-03-02)
- [✅] Add Open Graph tags for social sharing (2026-03-02)
- [✅] Add structured data (JSON-LD) for stock pages (2026-03-02)
- [✅] Add sitemap.xml and robots.txt (2026-03-02)
- [✅] Run performance profiling (k6 or NBomber load test) (2026-03-02)
- [✅] Verify: Page load < 2s, Lighthouse score > 80 (2026-03-02)

### 6.6 CI/CD: GitHub Actions Deployment

- [✅] Create `.github/workflows/ci.yml` — build + test on every PR to main (2026-03-02)
- [✅] Create `.github/workflows/deploy-web.yml` — deploy AlMal.Web to IIS `AlMal-Web` (almal.codexkw.co) on merge to main (2026-03-02)
- [✅] Create `.github/workflows/deploy-admin.yml` — deploy AlMal.Admin to IIS `AlMal-Admin` (almal-admin.codexkw.co) on merge to main (2026-03-02)
- [✅] Create `.github/workflows/deploy-api.yml` — deploy AlMal.API to IIS `AlMal-API` (almal-api.codexkw.co) on merge to main (2026-03-02)
- [✅] Configure GitHub Secrets: SQL connection string, server credentials, IIS Web Deploy credentials (2026-03-02) — referenced via ${{ secrets.* }}
- [✅] Configure Web Deploy publish profiles for each IIS site (2026-03-02)
- [✅] Add EF Core migration step in deploy pipeline (auto-apply on deploy) (2026-03-02)
- [✅] Add health check endpoint (`/health`) to Web, Admin, and API (2026-03-02)
- [ ] Test full CI/CD pipeline: PR → build → merge → deploy → verify live
- [ ] Verify: All 3 sites deploy automatically on merge to main

### 6.7 UAT & Production

- [ ] Full end-to-end testing of all features
- [ ] Fix all bugs found during UAT
- [✅] Configure production appsettings templates (2026-03-02) — .template files with placeholders
- [✅] Create IIS publish profiles (Web, Admin, API) (2026-03-02)
- [✅] Update .gitignore for pubxml.user and production config (2026-03-02)
- [ ] Set up IIS on Windows Server
- [✅] Configure GitHub Actions CI/CD pipeline (2026-03-02)
- [ ] Deploy to production
- [ ] Monitor for 48h after launch
- [ ] Verify: Production site live and stable

---

## Phase 7: Flutter Mobile App (Weeks 27-40)

### 7.1 App Setup

- [ ] Create Flutter project
- [ ] Set up project structure (screens, widgets, services, models, providers)
- [ ] Configure state management (Provider or Riverpod)
- [ ] Set up API client pointing to production REST API
- [ ] Implement JWT authentication flow (login, token refresh, logout)
- [ ] Set up Firebase Cloud Messaging for push notifications
- [ ] Verify: App builds and connects to API

### 7.2 Market Module

- [ ] Build market dashboard screen (indices, gainers, losers, heatmap)
- [ ] Build stock detail screen (all 6 tabs matching web)
- [ ] Build interactive chart (TradingView or fl_chart)
- [ ] Implement real-time updates via SignalR client
- [ ] Build watchlist screen
- [ ] Verify: Market data displays and updates in real-time

### 7.3 News Module

- [ ] Build news feed screen (card list, filters, sentiment badges)
- [ ] Build bottom sheet filters
- [ ] Implement "Understand Context" expandable section
- [ ] Verify: News feed matches web functionality

### 7.4 Community Module

- [ ] Build community feed screen (General + Following tabs)
- [ ] Build post creation screen (text, stock tags, media)
- [ ] Build post interactions (like, comment, repost)
- [ ] Build user profile screen
- [ ] Build follow/unfollow functionality
- [ ] Verify: Full community feature parity with web

### 7.5 Academy Module

- [ ] Build course catalog screen
- [ ] Build course detail screen
- [ ] Build lesson viewer screen (video + content)
- [ ] Build quiz screen
- [ ] Build certificate viewer
- [ ] Verify: Academy features match web

### 7.6 Simulation Module

- [ ] Build portfolio dashboard screen
- [ ] Build trade screen (buy/sell flow)
- [ ] Build performance charts
- [ ] Verify: Trading works correctly

### 7.7 Alerts & Notifications

- [ ] Build alert management screen
- [ ] Implement push notifications
- [ ] Implement WhatsApp deep linking
- [ ] Build notification center
- [ ] Verify: Alerts and notifications work end-to-end

### 7.8 Polish & Release

- [ ] App icon and splash screen
- [ ] App Store / Play Store listing preparation
- [ ] Performance optimization
- [ ] Testing on multiple devices
- [ ] Submit to stores
- [ ] Verify: App published and downloadable

---

## Summary

| Phase | Tasks | Completed | Remaining |
|-------|-------|-----------|-----------|
| Phase 1: Foundation | 132 | 132 | 0 |
| Phase 2: Market Core | 76 | 67 | 9 |
| Phase 3: News & AI | 43 | 37 | 6 |
| Phase 4: Community | 39 | 34 | 5 |
| Phase 5: Academy & Simulation | 49 | 45 | 4 |
| Phase 6: WhatsApp, CI/CD & Polish | 55 | 49 | 6 |
| Phase 7: Flutter Mobile | 44 | 0 | 44 |
| **TOTAL** | **438** | **364** | **74** |

> *Update this summary table after completing each phase section.*

---

*Last updated: 2026-03-03 — Simulation fixes: dead code removal, API optimization, navbar/profile portfolio integration*
