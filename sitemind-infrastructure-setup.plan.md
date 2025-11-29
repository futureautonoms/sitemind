<!-- 86f5eae0-8577-4ff4-a0f0-238cea64eb3f e2cbe359-c26e-4668-812d-0157f8f9ea64 -->
# SiteMind Teknik Altyapı ve Mimari Planı

## 1. Solution ve Proje Yapısı

- `.sln` dosyası oluşturulacak
- 5 proje eklenecek:
- `sitemind-gateway` (ASP.NET Core Web App, .NET 9)
- `sitemind-auth` (ASP.NET Core Web API, .NET 9)
- `sitemind-ingestion` (ASP.NET Core Web API, .NET 9)
- `sitemind-rag` (ASP.NET Core Web API, .NET 9)
- `sitemind-shared` (Class Library, .NET 9)

## 2. sitemind-shared - Core Library

### A. NuGet Paketleri

- `Microsoft.EntityFrameworkCore` (9.0)
- `Microsoft.EntityFrameworkCore.PostgreSQL` (9.0)
- `Npgsql.EntityFrameworkCore.PostgreSQL.NetTopologySuite` (PGVector desteği için)

### B. Entities

- `BaseTenantEntity` abstract class ([sitemind-shared/Entities/BaseTenantEntity.cs](sitemind-shared/Entities/BaseTenantEntity.cs))
- `Id` (Guid)
- `OrganizationId` (Guid)
- `CreatedAt` (DateTime)
- `Website` entity ([sitemind-shared/Entities/Website.cs](sitemind-shared/Entities/Website.cs))
- Inherits `BaseTenantEntity`
- `Name` (string)
- `BaseUrl` (string)
- `Status` (WebsiteStatus enum: Created, Active, Error)
- `Page` entity ([sitemind-shared/Entities/Page.cs](sitemind-shared/Entities/Page.cs))
- Inherits `BaseTenantEntity`
- `WebsiteId` (Guid, FK)
- `Url` (string)
- `RawContent` (string?)
- `VectorStatus` (VectorStatus enum: Pending, Processing, Completed, Failed)

### C. Enums

- `WebsiteStatus` ([sitemind-shared/Enums/WebsiteStatus.cs](sitemind-shared/Enums/WebsiteStatus.cs))
- `VectorStatus` ([sitemind-shared/Enums/VectorStatus.cs](sitemind-shared/Enums/VectorStatus.cs))

### D. Multi-Tenancy Infrastructure

- `IOrganizationProvider` interface ([sitemind-shared/Abstractions/IOrganizationProvider.cs](sitemind-shared/Abstractions/IOrganizationProvider.cs))
- `GetOrganizationId(): Guid`
- `OrganizationProvider` implementation ([sitemind-shared/Infrastructure/OrganizationProvider.cs](sitemind-shared/Infrastructure/OrganizationProvider.cs))
- HTTP Header `X-Organization-Id`'den okur
- JWT claim'den okuma desteği (gelecek için hazır)
- `OrganizationMiddleware` ([sitemind-shared/Middleware/OrganizationMiddleware.cs](sitemind-shared/Middleware/OrganizationMiddleware.cs))
- Her request'te `OrganizationId`'yi extract eder
- Scoped `IOrganizationProvider`'a set eder
- Whitelist route'ları (login, health check) hariç validation yapar

### E. Database Context

- `SiteMindDbContext` ([sitemind-shared/Data/SiteMindDbContext.cs](sitemind-shared/Data/SiteMindDbContext.cs))
- `IOrganizationProvider` inject edilir
- `OnModelCreating` override edilir
- Global Query Filter: `OrganizationId == currentOrgId`
- `DbSet<Website>` ve `DbSet<Page>` tanımları
- `Website` -> `Page` one-to-many relationship

### F. Extension Methods

- `AddSiteMindDbContext` extension ([sitemind-shared/Extensions/ServiceCollectionExtensions.cs](sitemind-shared/Extensions/ServiceCollectionExtensions.cs))
- DI container'a DbContext ve OrganizationProvider ekler

### G. Migrations

- İlk migration `sitemind-shared` projesinde oluşturulacak
- Migration dosyaları `sitemind-shared/Migrations/` klasöründe

## 3. sitemind-gateway - YARP Reverse Proxy

### A. NuGet Paketleri

- `Yarp.ReverseProxy` (2.2)

### B. Yapılandırma

- `appsettings.json` içinde YARP route'ları:
- `/api/auth/**` → `http://sitemind-auth:5001`
- `/api/websites/**` → `http://sitemind-ingestion:5002`
- `/api/chat/**` → `http://sitemind-rag:5003`
- `Program.cs`'de YARP middleware eklenir
- Port: 8080

## 4. sitemind-auth - Authentication Service

### A. NuGet Paketleri

- `Microsoft.AspNetCore.Authentication.JwtBearer` (9.0)
- `System.IdentityModel.Tokens.Jwt` (8.0)

### B. Implementation

- `Program.cs`'de JWT authentication yapılandırması
- Mock login endpoint (`POST /api/auth/login`)
- Basit username/password kontrolü
- JWT token oluşturur (`organization_id` claim ile)
- `appsettings.json`'da JWT secret key
- Port: 5001

## 5. sitemind-ingestion - Website Management Service

### A. NuGet Paketleri

- `sitemind-shared` project reference

### B. Implementation

- `Program.cs`'de:
- `AddSiteMindDbContext` extension kullanılır
- `OrganizationMiddleware` register edilir
- Connection string yapılandırması
- `WebsitesController` ([sitemind-ingestion/Controllers/WebsitesController.cs](sitemind-ingestion/Controllers/WebsitesController.cs))
- `POST /api/websites` - Website oluşturma
- `GET /api/websites` - Website listesi (tenant-filtered)
- `GET /api/websites/{id}` - Tek website getirme
- Port: 5002

## 6. sitemind-rag - RAG Service (Skeleton)

### A. NuGet Paketleri

- `sitemind-shared` project reference

### B. Implementation

- `Program.cs`'de:
- `AddSiteMindDbContext` extension kullanılır
- `OrganizationMiddleware` register edilir
- Basit health check endpoint
- Port: 5003

## 7. Docker Compose

### A. docker-compose.yml

- `postgres` service:
- Image: `pgvector/pgvector:pg16`
- Port: 5432
- Environment: POSTGRES_DB, POSTGRES_USER, POSTGRES_PASSWORD
- Volume: postgres_data
- `sitemind-gateway` service:
- Build context: `./server/sitemind-gateway`
- Port: 8080:8080
- Depends on: auth, ingestion, rag
- `sitemind-auth` service:
- Build context: `./server/sitemind-auth`
- Port: 5001:5001
- Depends on: postgres
- `sitemind-ingestion` service:
- Build context: `./server/sitemind-ingestion`
- Port: 5002:5002
- Depends on: postgres
- `sitemind-rag` service:
- Build context: `./server/sitemind-rag`
- Port: 5003:5003
- Depends on: postgres

### B. Dockerfile'lar

- Her servis için basit Dockerfile (multi-stage build)
- .NET 9 SDK ve Runtime kullanılacak

## 8. Yapılandırma Dosyaları

- Her servis için `appsettings.json` ve `appsettings.Development.json`
- Connection string'ler environment variable'lardan okunacak
- Gateway için YARP route konfigürasyonu

## 9. .gitignore

- .NET standart .gitignore dosyası eklenecek

## Notlar

- Multi-tenancy isolation: Global Query Filter ile otomatik
- Database: Tüm servisler aynı PostgreSQL instance'ını kullanır (shared database)
- Migration'lar: `sitemind-shared` projesinden yönetilir
- JWT: `organization_id` claim zorunlu
- Middleware: Her backend serviste `OrganizationMiddleware` çalışır

### To-dos

- [x] Solution dosyası (.sln) ve 5 proje yapısını oluştur (.NET 9)
- [x] sitemind-shared: BaseTenantEntity, Website, Page entities ve enum'ları oluştur
- [x] sitemind-shared: IOrganizationProvider, OrganizationProvider, OrganizationMiddleware implementasyonu
- [ ] sitemind-shared: SiteMindDbContext, Global Query Filter, ve extension methods
- [ ] sitemind-shared: İlk EF Core migration oluştur
- [ ] sitemind-gateway: YARP konfigürasyonu ve appsettings.json route tanımları
- [ ] sitemind-auth: JWT authentication setup ve mock login endpoint
- [ ] sitemind-ingestion: WebsitesController CRUD operations, DI ve middleware setup
- [ ] sitemind-rag: Basit skeleton yapısı, DI ve middleware setup
- [ ] docker-compose.yml ve her servis için Dockerfile oluştur