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

- [ ] Create `IMarketDataProvider` interface in Application
- [ ] Create `BoursakuwaitScraper` in Infrastructure/ExternalApis
- [ ] Implement stock list scraping (symbol, name, sector, price)
- [ ] Implement market indices scraping (main index, sector indices)
- [ ] Implement stock price history scraping (OHLCV)
- [ ] Implement order book scraping
- [ ] Implement disclosure scraping
- [ ] Create Hangfire job: `MarketDataScraperJob` (every 30s during market hours)
- [ ] Create Hangfire job: `OrderBookScraperJob` (every 60s during market hours)
- [ ] Create Hangfire job: `DisclosureScraperJob` (every 5 min)
- [ ] Add market hours check utility (Sun-Thu 9:00-12:40 KWT)
- [ ] Add error handling and retry logic for scraping failures
- [ ] Add Redis caching for scraped data
- [ ] Verify: Scraper fetches real data from boursakuwait.com.kw

### 2.2 Market Dashboard

- [ ] Create `MarketController` in Web
- [ ] Create `MarketDashboardViewModel`
- [ ] Build indices ticker (sticky top bar, horizontal scroll)
- [ ] Build market heatmap (treemap with chart.js or custom canvas)
- [ ] Build top gainers widget (5 stocks)
- [ ] Build top losers widget (5 stocks)
- [ ] Build most traded widget (5 stocks)
- [ ] Build full stock table (sortable, searchable, paginated)
- [ ] Build tab navigation (All Stocks / Sectors / Watchlist)
- [ ] Build quick search (FAB button + modal)
- [ ] Add mobile responsive layout (single column, swipeable tabs)
- [ ] Verify: Dashboard shows real/sample market data, responsive on mobile

### 2.3 Stock Detail Page

- [ ] Create `StockController` in Web
- [ ] Create `StockDetailViewModel`
- [ ] Build stock header (name, price, change, watchlist star, alert bell)
- [ ] Build Tab A: Overview (company info, key data cards)
- [ ] Build Tab B: Interactive Chart (TradingView Lightweight Charts)
- [ ] Implement chart timeframes (1D, 1W, 1M, 3M, 6M, 1Y, All)
- [ ] Implement chart indicators (SMA, EMA, RSI, MACD, Bollinger)
- [ ] Implement volume histogram below chart
- [ ] Build Tab C: Explain Movement (placeholder - "Coming soon" button)
- [ ] Build Tab D: Financial Ratios (calculate P/E, P/B, ROE, etc.)
- [ ] Build Tab E: Disclosures list (chronological, type badges, infinite scroll)
- [ ] Build Tab F: Order Book (bid/ask columns, pressure indicator)
- [ ] Add mobile responsive layout (swipeable tabs, full-width chart)
- [ ] Verify: All 6 tabs render with real/sample data

### 2.4 Real-Time Updates (SignalR)

- [ ] Create `MarketHub` SignalR hub in Web/Hubs
- [ ] Implement `StockPriceUpdate` event
- [ ] Implement `IndexUpdate` event
- [ ] Add client-side SignalR connection in market pages
- [ ] Implement client group management (sector, watchlist)
- [ ] Add Redis backplane for SignalR
- [ ] Connect scraper output to SignalR broadcasts
- [ ] Verify: Price updates appear in real-time on dashboard and stock pages

### 2.5 Watchlist & Alerts UI

- [ ] Create `WatchlistController` in Web
- [ ] Build watchlist page (list of watched stocks with prices)
- [ ] Implement add/remove stock from watchlist (star toggle)
- [ ] Build price alert creation form (stock, target price, above/below)
- [ ] Build alert management page (list, enable/disable, delete)
- [ ] Verify: User can manage watchlist and create alerts

### 2.6 Market API Endpoints

- [ ] Create `MarketApiController` in API
- [ ] Implement GET /api/v1/market/indices
- [ ] Implement GET /api/v1/market/stocks (paginated, filterable)
- [ ] Implement GET /api/v1/market/stocks/{symbol}
- [ ] Implement GET /api/v1/market/stocks/{symbol}/prices
- [ ] Implement GET /api/v1/market/stocks/{symbol}/orderbook
- [ ] Implement GET /api/v1/market/stocks/{symbol}/financials
- [ ] Implement GET /api/v1/market/stocks/{symbol}/disclosures
- [ ] Implement GET /api/v1/market/gainers
- [ ] Implement GET /api/v1/market/losers
- [ ] Implement GET /api/v1/market/most-traded
- [ ] Implement GET /api/v1/market/heatmap
- [ ] Implement GET /api/v1/market/sectors
- [ ] Add Swagger documentation for all endpoints
- [ ] Verify: All endpoints return correct JSON with pagination

### 2.7 Admin: Market Management

- [ ] Create `AdminStocksController` in Admin
- [ ] Build stock CRUD pages (list, create, edit, delete)
- [ ] Build sector CRUD pages
- [ ] Build scraping status dashboard (last run, errors, next run)
- [ ] Build manual price entry form
- [ ] Build stock CSV import
- [ ] Verify: Admin can manage stocks and monitor scraping

---

## Phase 3: News & AI (Weeks 11-14)

### 3.1 News Integration

- [ ] Create `INewsProvider` interface in Application
- [ ] Create `NewsDataClient` in Infrastructure/ExternalApis
- [ ] Implement NewsData.io API integration
- [ ] Create Hangfire job: `NewsFetcherJob` (15 min market, 60 min off)
- [ ] Implement article deduplication by ExternalId
- [ ] Implement stock matching by keyword (Arabic/English company names)
- [ ] Store articles in NewsArticle + NewsArticleStock tables
- [ ] Verify: News articles fetched and stored correctly

### 3.2 News Feed UI

- [ ] Create `NewsController` in Web
- [ ] Create `NewsFeedViewModel`
- [ ] Build news card component (source, title, time, sentiment pill, summary, stock tags)
- [ ] Build news feed page (card list, infinite scroll)
- [ ] Build filters: company search, sector dropdown, sentiment toggle, date range
- [ ] Build desktop sidebar: trending stocks, latest disclosures
- [ ] Build mobile layout: bottom sheet filters, horizontal chips
- [ ] Add "Understand Context" button placeholder on each card
- [ ] Verify: News feed renders with real/sample articles

### 3.3 AI: Movement Explainer

- [ ] Create `IAiAnalysisService` interface in Application
- [ ] Create `ClaudeAiClient` in Infrastructure/ExternalApis
- [ ] Implement `ExplainMovementAsync` method
- [ ] Build system prompt with stock data injection
- [ ] Add Redis caching (30 min per stock)
- [ ] Wire "Explain Movement" button on stock page Tab C
- [ ] Build loading skeleton for AI response
- [ ] Add educational disclaimer to all AI responses
- [ ] Verify: AI explains stock movements in Arabic

### 3.4 AI: Disclosure Summarizer

- [ ] Implement `SummarizeDisclosureAsync` method in ClaudeAiClient
- [ ] Create Hangfire job: `AiDisclosureProcessorJob` (on new disclosure)
- [ ] Store AI summary in Disclosure.AiSummary
- [ ] Display AI summary in stock disclosures tab
- [ ] Verify: New disclosures get auto-summarized

### 3.5 AI: News Context

- [ ] Implement `GenerateNewsContextAsync` method in ClaudeAiClient
- [ ] Create Hangfire job: `AiNewsProcessorJob` (on new article)
- [ ] Generate sentiment, summary, and context data
- [ ] Store in NewsArticle fields (Sentiment, Summary, ContextData)
- [ ] Wire "Understand Context" button: expand card with context
- [ ] Verify: News articles have AI-generated sentiment and context

### 3.6 Admin: News Management

- [ ] Create `AdminNewsController` in Admin
- [ ] Build news source configuration page
- [ ] Build sentiment override UI
- [ ] Build featured articles management
- [ ] Build AI processing stats dashboard
- [ ] Verify: Admin can manage news sources and override AI

---

## Phase 4: Community (Weeks 15-18)

### 4.1 User Profiles & Follow System

- [ ] Create `ProfileController` in Web
- [ ] Build public profile page (/user/{displayName})
- [ ] Build profile header (avatar, name, badge, bio, follower counts)
- [ ] Build edit profile page (DisplayName, Bio, Avatar with crop)
- [ ] Implement follow/unfollow toggle
- [ ] Update denormalized follower/following counts
- [ ] Build followers/following list pages
- [ ] Verify: Profile pages render, follow system works

### 4.2 Post System

- [ ] Create `CommunityController` in Web
- [ ] Create `PostService` in Application
- [ ] Build post creation form (text, stock tags with $ autocomplete, media upload)
- [ ] Build media upload (image max 5MB, video max 50MB)
- [ ] Implement stock tag parsing ($SYMBOL → PostStockMention)
- [ ] Build post card component (avatar, badge, content, stock pills, media, actions)
- [ ] Build community feed page (General + Following tabs)
- [ ] Implement infinite scroll with HTMX
- [ ] Build mobile FAB for new post (full-screen modal)
- [ ] Verify: Users can create posts with text, stock tags, and media

### 4.3 Post Interactions

- [ ] Implement like/unlike toggle (updates denormalized count)
- [ ] Build comment system (threaded one level via ParentCommentId)
- [ ] Build comment creation form
- [ ] Implement repost functionality
- [ ] Implement report post (flag for moderation)
- [ ] Build post actions bar (like, comment, repost, share)
- [ ] Verify: All interactions work and counts update

### 4.4 Analyst Badge System

- [ ] Display UserType badge on posts and profile
- [ ] Create badge visual components (Normal: none, Pro: ⭐ orange, Certified: ✓ purple)
- [ ] Verify: Badges display correctly on all user appearances

### 4.5 Community API Endpoints

- [ ] Create `PostsApiController` in API
- [ ] Create `UsersApiController` in API
- [ ] Implement all community endpoints from PRD section 15.4
- [ ] Verify: All endpoints work with proper authorization

### 4.6 Admin: Content Moderation

- [ ] Create `AdminContentController` in Admin
- [ ] Build moderation queue (flagged/reported posts)
- [ ] Build reported content review page
- [ ] Implement bulk approve/delete
- [ ] Build user management (search, suspend, ban, promote)
- [ ] Implement analyst badge approval workflow
- [ ] Verify: Moderators can review and act on flagged content

---

## Phase 5: Academy & Simulation (Weeks 19-22)

### 5.1 Academy: Course System

- [ ] Create `AcademyController` in Web
- [ ] Create `CourseService` in Application
- [ ] Build course catalog page (grid, filters, sort)
- [ ] Build course detail page (header, description, lesson list, enroll button)
- [ ] Build lesson viewer page (video player, content, nav)
- [ ] Implement enrollment logic
- [ ] Build progress tracking (per lesson, per course)
- [ ] Verify: Users can browse and enroll in courses

### 5.2 Academy: Quiz System

- [ ] Create `QuizService` in Application
- [ ] Build quiz UI (multiple choice, immediate feedback)
- [ ] Implement scoring and pass/fail logic (70% default)
- [ ] Mark lesson complete on quiz pass
- [ ] Check course completion on lesson complete
- [ ] Verify: Quizzes grade correctly and update progress

### 5.3 Academy: Certificates

- [ ] Create `CertificateService` in Application
- [ ] Implement PDF certificate generation (user name, course, date, cert number)
- [ ] Auto-generate certificate when all lessons + quizzes passed
- [ ] Implement ProAnalyst upgrade on specific course completion
- [ ] Build certificate download page
- [ ] Verify: Certificates generate and download correctly

### 5.4 Simulation Portfolio

- [ ] Create `PortfolioController` in Web
- [ ] Create `SimulationService` in Application
- [ ] Implement auto-create portfolio (100,000 KWD on first visit)
- [ ] Build buy flow (stock search → quantity → estimate → confirm)
- [ ] Build sell flow (holdings → quantity → estimate → confirm)
- [ ] Implement trade validation (cash balance check, holding check)
- [ ] Update SimulationHolding with average cost calculation
- [ ] Build portfolio dashboard (total value, P&L, holdings table)
- [ ] Build performance chart (portfolio value vs index over time)
- [ ] Build sector allocation pie chart
- [ ] Implement portfolio reset (with confirmation)
- [ ] Implement public/private portfolio toggle
- [ ] Verify: Full buy/sell cycle works with correct P&L

### 5.5 Academy & Portfolio API Endpoints

- [ ] Create `AcademyApiController` in API
- [ ] Create `PortfolioApiController` in API
- [ ] Implement all academy endpoints from PRD section 15.5
- [ ] Implement all portfolio endpoints from PRD section 15.5
- [ ] Verify: All endpoints return correct data

### 5.6 Admin: Academy & Analytics

- [ ] Create `AdminAcademyController` in Admin
- [ ] Build course CRUD with rich text editor
- [ ] Build lesson management (reorder, video URL)
- [ ] Build quiz builder (add questions, set correct answer)
- [ ] Build enrollment analytics (completion rate, avg score)
- [ ] Verify: Admin can create and manage courses end-to-end

---

## Phase 6: WhatsApp & Polish (Weeks 23-26)

### 6.1 WhatsApp Integration

- [ ] Create `IWhatsAppService` interface in Application
- [ ] Create `WhatsAppClient` in Infrastructure/ExternalApis
- [ ] Implement WhatsApp Business Cloud API connection
- [ ] Build phone number opt-in flow (enter number → verify code)
- [ ] Implement alert delivery via WhatsApp (price, disclosure, index, volume)
- [ ] Create WhatsApp message templates (Arabic)
- [ ] Create Hangfire job: `AlertEngineJob` (every 30s during market hours)
- [ ] Verify: Alerts delivered via WhatsApp to opted-in users

### 6.2 WhatsApp Market Assistant

- [ ] Implement webhook for incoming WhatsApp messages
- [ ] Create `AnswerMarketQuestionAsync` in ClaudeAiClient
- [ ] Build context retrieval (latest disclosures, prices, news for query)
- [ ] Wire webhook → Claude API → WhatsApp reply
- [ ] Create Hangfire job: `DailyMarketSummaryJob` (12:45 PM KWT)
- [ ] Verify: Users can ask market questions via WhatsApp and get AI answers

### 6.3 Alert System

- [ ] Build alert management UI in web (create, list, enable/disable, delete)
- [ ] Implement alert evaluation logic (price crosses target, volume anomaly, etc.)
- [ ] Implement in-app notification system (notification bell)
- [ ] Build notification dropdown (mark read, click to navigate)
- [ ] Create alert API endpoints
- [ ] Verify: Alerts trigger correctly and notify via app + WhatsApp

### 6.4 Admin: Alerts & Analytics

- [ ] Create `AdminAlertsController` in Admin
- [ ] Build WhatsApp template management
- [ ] Build delivery analytics (sent/failed/pending)
- [ ] Build system-wide analytics dashboard (user growth, engagement, popular stocks)
- [ ] Create `AdminSettingsController` (SuperAdmin only)
- [ ] Build settings page (API keys, schedules, AI config, system params)
- [ ] Verify: Admin can manage alerts and view analytics

### 6.5 Performance & SEO

- [ ] Implement Redis caching across all hot paths
- [ ] Add output caching for anonymous pages
- [ ] Implement lazy loading for images and heavy components
- [ ] Configure Cloudflare CDN
- [ ] Add Arabic SEO meta tags on all pages
- [ ] Add Open Graph tags for social sharing
- [ ] Add structured data (JSON-LD) for stock pages
- [ ] Add sitemap.xml and robots.txt
- [ ] Run performance profiling (k6 or NBomber load test)
- [ ] Verify: Page load < 2s, Lighthouse score > 80

### 6.6 CI/CD: GitHub Actions Deployment

- [ ] Create `.github/workflows/ci.yml` — build + test on every PR to main
- [ ] Create `.github/workflows/deploy-web.yml` — deploy AlMal.Web to IIS `AlMal-Web` (almal.codexkw.co) on merge to main
- [ ] Create `.github/workflows/deploy-admin.yml` — deploy AlMal.Admin to IIS `AlMal-Admin` (admin.almal.codexkw.co) on merge to main
- [ ] Create `.github/workflows/deploy-api.yml` — deploy AlMal.API to IIS `AlMal-API` (api.almal.codexkw.co) on merge to main
- [ ] Configure GitHub Secrets: SQL connection string, server credentials, IIS Web Deploy credentials
- [ ] Configure Web Deploy publish profiles for each IIS site
- [ ] Add EF Core migration step in deploy pipeline (auto-apply on deploy)
- [ ] Add health check endpoint (`/health`) to Web, Admin, and API
- [ ] Test full CI/CD pipeline: PR → build → merge → deploy → verify live
- [ ] Verify: All 3 sites deploy automatically on merge to main

### 6.7 UAT & Production

- [ ] Full end-to-end testing of all features
- [ ] Fix all bugs found during UAT
- [ ] Configure production appsettings
- [ ] Set up IIS on Windows Server
- [ ] Configure GitHub Actions CI/CD pipeline
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
| Phase 2: Market Core | 76 | 0 | 76 |
| Phase 3: News & AI | 43 | 0 | 43 |
| Phase 4: Community | 39 | 0 | 39 |
| Phase 5: Academy & Simulation | 44 | 0 | 44 |
| Phase 6: WhatsApp, CI/CD & Polish | 55 | 0 | 55 |
| Phase 7: Flutter Mobile | 44 | 0 | 44 |
| **TOTAL** | **433** | **132** | **301** |

> *Update this summary table after completing each phase section.*

---

*Last updated: 2026-03-02 — Phase 1 complete*
