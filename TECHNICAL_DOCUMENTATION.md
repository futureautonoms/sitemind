# SiteMind - Teknik Dokümantasyon

## 1. Proje Genel Bakış

**Proje Adı:** SiteMind  
**Açıklama:** Website içeriklerini vector database'e alan ve AI destekli RAG (Retrieval-Augmented Generation) chat servisleri sunan SaaS platformu  
**Mimari:** Mikroservis mimarisi, Strict Multi-Tenancy (Shared Database)  
**Teknoloji Stack:** .NET 9, PostgreSQL (PGVector), YARP (Gateway), Vue3-compositionApi, Docker

---

## 2. Mimari Yapı

### 2.1. Genel Mimari

```
┌─────────────────┐
│   YARP Gateway  │  Port: 8080 (Entry Point)
│  (sitemind-     │
│   gateway)      │
└────────┬────────┘
         │
    ┌────┴────┬──────────┬──────────┐
    │         │          │          │
┌───▼───┐ ┌──▼────┐ ┌───▼────┐ ┌───▼────┐
│ Auth  │ │Ingest │ │  RAG   │ │Postgres│
│ :5001 │ │ :5002 │ │ :5003  │ │ :5432  │
└───────┘ └───────┘ └────────┘ └────────┘
```

### 2.2. Solution Yapısı

```
server/
├── sitemind-gateway/       # YARP Reverse Proxy (Entry Point)
├── sitemind-auth/          # Identity & Organization Management
├── sitemind-ingestion/     # Write-Heavy: Website management & Crawling
├── sitemind-rag/           # Read-Heavy: Vector Search & Chat
└── sitemind-shared/        # Core Library (Entities, Data, Middleware)
```

---

## 3. Domain Model & Naming Conventions

### 3.1. Terminoloji

- **Organization (Tenant):** Ödeme yapan müşteri hesabı. Veri izolasyon sınırı.
- **Website (Project):** Bir Organization tarafından eklenen hedef domain (örn: `acme.com`).
- **Page (Unit):** Bir Website'e ait tek bir crawl edilmiş URL.

### 3.2. Entity İlişkileri

```
Organization (Tenant)
    │
    ├── Website (1:N)
    │       │
    │       └── Page (1:N)
    │
    └── [Future: User, Subscription, etc.]
```

---

## 4. Veritabanı Yapısı

### 4.1. Database Stratejisi

- **Strateji:** Shared Database / Shared Schema
- **İzolasyon:** `OrganizationId` ile Global Query Filter
- **Database:** PostgreSQL 16 + PGVector extension

### 4.2. Entity Modelleri

#### BaseTenantEntity (Abstract Base Class)

```csharp
public abstract class BaseTenantEntity
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }  // Partition/Isolation Key
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
```

**Özellikler:**
- Tüm tenant-specific entity'ler bu sınıftan türetilir
- `OrganizationId` otomatik olarak `SaveChanges` override'ında set edilir
- Global Query Filter ile otomatik filtreleme yapılır

#### Website Entity

```csharp
public class Website : BaseTenantEntity
{
    public string Name { get; set; } = string.Empty;           // Max: 500 chars
    public string BaseUrl { get; set; } = string.Empty;        // Max: 2000 chars
    public WebsiteStatus Status { get; set; } = WebsiteStatus.Created;
    
    // Navigation property
    public virtual ICollection<Page> Pages { get; set; } = new List<Page>();
}
```

**Enum: WebsiteStatus**
- `Created = 0`: Oluşturuldu, henüz aktif değil
- `Active = 1`: Aktif, crawl işlemleri devam ediyor
- `Error = 2`: Hata durumu

#### Page Entity

```csharp
public class Page : BaseTenantEntity
{
    public Guid WebsiteId { get; set; }                        // FK to Website
    public string Url { get; set; } = string.Empty;            // Max: 2000 chars
    public string? RawContent { get; set; }                    // Text type (nullable)
    public VectorStatus VectorStatus { get; set; } = VectorStatus.Pending;
    
    // Navigation property
    public virtual Website Website { get; set; } = null!;
}
```

**Enum: VectorStatus**
- `Pending = 0`: Beklemede
- `Processing = 1`: İşleniyor
- `Completed = 2`: Tamamlandı
- `Failed = 3`: Başarısız

**Index'ler:**
- `WebsiteId` (FK için)
- `OrganizationId` (Multi-tenancy filtreleme için)

### 4.3. İlişkiler

- **Website → Page:** One-to-Many (Cascade Delete)
- Her iki entity de `OrganizationId` içerir (join'siz performans için)

---

## 5. Multi-Tenancy Altyapısı

### 5.1. Strateji: Shared Database / Shared Schema

Tüm tenant'lar aynı veritabanını ve şemayı paylaşır. İzolasyon `OrganizationId` ile sağlanır.

### 5.2. IOrganizationProvider Interface

```csharp
public interface IOrganizationProvider
{
    Guid GetOrganizationId();
}
```

**Kullanım:**
- Scoped service olarak register edilir
- Her request'te mevcut tenant ID'sini döndürür
- Header veya JWT claim'den okur

### 5.3. OrganizationProvider Implementation

```csharp
public class OrganizationProvider : IOrganizationProvider
{
    // Öncelik sırası:
    // 1. HTTP Header: "X-Organization-Id"
    // 2. JWT Claim: "organization_id"
    // 3. Exception throw
}
```

**Header Format:**
```
X-Organization-Id: {guid}
```

**JWT Claim:**
```json
{
  "organization_id": "{guid}",
  ...
}
```

### 5.4. OrganizationMiddleware

**Konum:** Her backend serviste pipeline'ın erken aşamasında

**Özellikler:**
- Whitelist route'ları (login, health check) validation'dan muaf
- Header veya JWT'den `OrganizationId` okur
- Eksikse 400 Bad Request döner
- `HttpContext.Items["OrganizationId"]` içine kaydeder

**Whitelist Routes:**
- `/api/auth/login`
- `/health`
- `/healthz`
- `/api/health`

### 5.5. Global Query Filter

**SiteMindDbContext'te:**

```csharp
modelBuilder.Entity<Website>().HasQueryFilter(e => 
    e.OrganizationId == GetCurrentOrganizationId());

modelBuilder.Entity<Page>().HasQueryFilter(e => 
    e.OrganizationId == GetCurrentOrganizationId());
```

**Özellikler:**
- Tüm query'lerde otomatik filtreleme
- Developer'ın manuel filtreleme yapmasına gerek yok
- Migration sırasında devre dışı (Guid.Empty döner)

### 5.6. Otomatik OrganizationId Atama

**SaveChanges Override:**

```csharp
public override int SaveChanges()
{
    var entries = ChangeTracker.Entries<BaseTenantEntity>()
        .Where(e => e.State == EntityState.Added);
    
    foreach (var entry in entries)
    {
        if (entry.Entity.OrganizationId == Guid.Empty)
        {
            entry.Entity.OrganizationId = _organizationProvider.GetOrganizationId();
        }
    }
    
    return base.SaveChanges();
}
```

**Özellikler:**
- Yeni entity'lerde otomatik `OrganizationId` atar
- Developer'ın manuel set etmesine gerek yok
- Migration sırasında hata vermez

---

## 6. Servis Detayları

### 6.1. sitemind-gateway (Port: 8080)

**Teknoloji:** YARP (Yet Another Reverse Proxy) 2.2.0

**Görev:**
- Tüm external request'lerin entry point'i
- Route'ları backend servislere yönlendirir
- Load balancing (gelecekte)

**Route Yapılandırması:**

```json
{
  "ReverseProxy": {
    "Routes": {
      "auth-route": {
        "Path": "/api/auth/{**catch-all}",
        "ClusterId": "auth-cluster"
      },
      "websites-route": {
        "Path": "/api/websites/{**catch-all}",
        "ClusterId": "ingestion-cluster"
      },
      "chat-route": {
        "Path": "/api/chat/{**catch-all}",
        "ClusterId": "rag-cluster"
      }
    },
    "Clusters": {
      "auth-cluster": {
        "Destinations": {
          "destination1": {
            "Address": "http://sitemind-auth:5001"
          }
        }
      },
      "ingestion-cluster": {
        "Destinations": {
          "destination1": {
            "Address": "http://sitemind-ingestion:5002"
          }
        }
      },
      "rag-cluster": {
        "Destinations": {
          "destination1": {
            "Address": "http://sitemind-rag:5003"
          }
        }
      }
    }
  }
}
```

**Docker:**
- Build context: `./server`
- Dockerfile: `sitemind-gateway/Dockerfile`
- Port: `8080:8080`

---

### 6.2. sitemind-auth (Port: 5001)

**Teknoloji:**
- ASP.NET Core 9.0
- JWT Bearer Authentication
- System.IdentityModel.Tokens.Jwt 8.2.1

**Görev:**
- Kullanıcı kimlik doğrulama
- JWT token üretimi
- `organization_id` claim'i ile token döndürme

**Endpoint:**

**POST /api/auth/login**

Request:
```json
{
  "username": "string",
  "password": "string"
}
```

Response:
```json
{
  "token": "eyJhbGci...",
  "organization_id": "guid",
  "expires_in": 3600
}
```

**JWT Yapılandırması:**

```json
{
  "Jwt": {
    "SecretKey": "YourSuperSecretKeyForJWTTokenGenerationThatShouldBeAtLeast32CharactersLong",
    "Issuer": "sitemind-auth",
    "Audience": "sitemind-api",
    "ExpirationMinutes": 60
  }
}
```

**JWT Claims:**
- `unique_name`: Username
- `organization_id`: Tenant ID (Guid string)
- `jti`: Token ID

**Not:** Şu anda mock authentication. Production'da database'den doğrulama yapılmalı.

**Docker:**
- Build context: `./server`
- Dockerfile: `sitemind-auth/Dockerfile`
- Port: `5001:5001`
- Depends on: `postgres`

---

### 6.3. sitemind-ingestion (Port: 5002)

**Teknoloji:**
- ASP.NET Core 9.0 Web API
- Entity Framework Core 9.0
- PostgreSQL (Npgsql 9.0.2)

**Görev:**
- Website entity'lerinin CRUD işlemleri
- Crawl job yönetimi (gelecekte)
- Page entity'lerinin yönetimi (gelecekte)

**Dependencies:**
- `sitemind-shared` project reference
- `OrganizationMiddleware` aktif
- `SiteMindDbContext` DI'da register

**Endpoints:**

**GET /api/websites**
- Tüm website'leri listeler (tenant-filtered)
- Global Query Filter otomatik uygulanır

**GET /api/websites/{id}**
- Tek bir website getirir (tenant-filtered)

**POST /api/websites**
- Yeni website oluşturur
- `OrganizationId` otomatik set edilir

Request:
```json
{
  "name": "Example Website",
  "baseUrl": "https://example.com"
}
```

Response:
```json
{
  "id": "guid",
  "organizationId": "guid",
  "name": "Example Website",
  "baseUrl": "https://example.com",
  "status": 0,
  "createdAt": "2025-11-29T..."
}
```

**Docker:**
- Build context: `./server`
- Dockerfile: `sitemind-ingestion/Dockerfile`
- Port: `5002:5002`
- Depends on: `postgres`

---

### 6.4. sitemind-rag (Port: 5003)

**Teknoloji:**
- ASP.NET Core 9.0 Web API
- Entity Framework Core 9.0 (read-only access)

**Görev:**
- Vector search işlemleri (gelecekte)
- Chat endpoint'leri (gelecekte)
- RAG pipeline yönetimi (gelecekte)

**Mevcut Endpoint:**

**GET /api/health**
```json
{
  "status": "healthy",
  "service": "sitemind-rag"
}
```

**Dependencies:**
- `sitemind-shared` project reference
- `OrganizationMiddleware` aktif
- `SiteMindDbContext` DI'da register (read-only)

**Docker:**
- Build context: `./server`
- Dockerfile: `sitemind-rag/Dockerfile`
- Port: `5003:5003`
- Depends on: `postgres`

---

## 7. sitemind-shared Library

### 7.1. Proje Yapısı

```
sitemind-shared/
├── Abstractions/
│   └── IOrganizationProvider.cs
├── Data/
│   └── SiteMindDbContext.cs
├── Entities/
│   ├── BaseTenantEntity.cs
│   ├── Website.cs
│   └── Page.cs
├── Enums/
│   ├── WebsiteStatus.cs
│   └── VectorStatus.cs
├── Extensions/
│   └── ServiceCollectionExtensions.cs
├── Infrastructure/
│   └── OrganizationProvider.cs
└── Middleware/
    └── OrganizationMiddleware.cs
```

### 7.2. NuGet Paketleri

```xml
<PackageReference Include="Microsoft.AspNetCore.Http" Version="2.2.0" />
<PackageReference Include="Microsoft.EntityFrameworkCore" Version="9.0.0" />
<PackageReference Include="Microsoft.EntityFrameworkCore.Design" Version="9.0.0" />
<PackageReference Include="Npgsql.EntityFrameworkCore.PostgreSQL" Version="9.0.2" />
<PackageReference Include="Npgsql.EntityFrameworkCore.PostgreSQL.NetTopologySuite" Version="9.0.2" />
```

### 7.3. ServiceCollectionExtensions

**AddSiteMindDbContext Extension:**

```csharp
public static IServiceCollection AddSiteMindDbContext(
    this IServiceCollection services,
    IConfiguration configuration,
    string connectionStringName = "DefaultConnection")
```

**Yaptığı İşlemler:**
1. PostgreSQL connection string'i okur
2. `SiteMindDbContext`'i DI'a ekler (scoped)
3. `HttpContextAccessor`'ı singleton olarak ekler
4. `IOrganizationProvider`'ı scoped olarak ekler

**Kullanım:**
```csharp
builder.Services.AddSiteMindDbContext(builder.Configuration);
```

---

## 8. Database Migration

### 8.1. Migration Stratejisi

- Migration'lar `sitemind-shared` projesinde tutulur
- Tüm servisler aynı migration'ları kullanır
- Startup project olarak `sitemind-ingestion` kullanılır

### 8.2. Migration Oluşturma

```bash
cd server/sitemind-shared
dotnet ef migrations add MigrationName \
  --startup-project ../sitemind-ingestion \
  --context SiteMindDbContext
```

### 8.3. Migration Uygulama

```bash
cd server/sitemind-ingestion
dotnet ef database update \
  --startup-project . \
  --context SiteMindDbContext
```

### 8.4. Mevcut Migration

**InitialCreate (20251129184928_InitialCreate)**
- `Websites` tablosu
- `Pages` tablosu
- Index'ler ve foreign key'ler
- Global Query Filter tanımları

---

## 9. Docker Yapılandırması

### 9.1. docker-compose.yml

**Servisler:**
1. **postgres:** PostgreSQL 16 + PGVector
2. **sitemind-auth:** Authentication service
3. **sitemind-ingestion:** Website management service
4. **sitemind-rag:** RAG service
5. **sitemind-gateway:** API Gateway

**Network:**
- Tüm servisler `sitemind-network` bridge network'ünde
- Servisler birbirlerine container name ile erişir

**Volumes:**
- `postgres_data`: PostgreSQL data persistence

**Health Checks:**
- PostgreSQL health check aktif
- Diğer servisler PostgreSQL'in healthy olmasını bekler

### 9.2. Dockerfile Yapısı

**Multi-stage build:**
1. **base:** ASP.NET runtime image
2. **build:** SDK image (restore, build)
3. **publish:** Release build
4. **final:** Runtime image + published files

**Ortak Özellikler:**
- .NET 9 SDK ve Runtime
- Optimized layer caching
- Non-root user (güvenlik)

### 9.3. Build ve Çalıştırma

**Build:**
```bash
docker-compose build
```

**Çalıştırma:**
```bash
docker-compose up -d
```

**Logs:**
```bash
docker-compose logs -f [service-name]
```

**Stop:**
```bash
docker-compose down
```

**Clean (volumes dahil):**
```bash
docker-compose down -v
```

---

## 10. API Endpoint'leri

### 10.1. Authentication

**Base URL:** `http://localhost:8080/api/auth` (via gateway)

| Method | Endpoint | Description | Auth Required |
|--------|----------|-------------|---------------|
| POST | `/api/auth/login` | JWT token al | No |

### 10.2. Website Management

**Base URL:** `http://localhost:8080/api/websites` (via gateway)

| Method | Endpoint | Description | Auth Required | Headers |
|--------|----------|-------------|---------------|---------|
| GET | `/api/websites` | Website listesi | Yes | `X-Organization-Id` veya JWT |
| GET | `/api/websites/{id}` | Tek website | Yes | `X-Organization-Id` veya JWT |
| POST | `/api/websites` | Website oluştur | Yes | `X-Organization-Id` veya JWT |

### 10.3. RAG Service

**Base URL:** `http://localhost:8080/api/chat` (via gateway)

| Method | Endpoint | Description | Auth Required |
|--------|----------|-------------|---------------|
| GET | `/api/health` | Health check | No |

---

## 11. Güvenlik

### 11.1. Multi-Tenancy İzolasyonu

- **Global Query Filter:** Tüm query'lerde otomatik `OrganizationId` filtreleme
- **SaveChanges Override:** Yeni entity'lerde otomatik `OrganizationId` atama
- **Middleware Validation:** Eksik `OrganizationId` durumunda 400 Bad Request

### 11.2. JWT Authentication

- **Algorithm:** HS256 (HMAC SHA-256)
- **Token Lifetime:** 60 dakika (configurable)
- **Required Claims:** `organization_id`
- **Validation:** Issuer, Audience, Lifetime, Signing Key

### 11.3. Header-Based Authentication

- **Header Name:** `X-Organization-Id`
- **Format:** GUID string
- **Validation:** Middleware seviyesinde

---

## 12. Konfigürasyon

### 12.1. Connection Strings

**Format:**
```
Host={host};Port={port};Database={database};Username={user};Password={password}
```

**Örnek:**
```
Host=postgres;Port=5432;Database=sitemind;Username=postgres;Password=postgres
```

**Environment Variables:**
```bash
ConnectionStrings__DefaultConnection=Host=postgres;Port=5432;Database=sitemind;Username=postgres;Password=postgres
```

### 12.2. JWT Settings

**appsettings.json:**
```json
{
  "Jwt": {
    "SecretKey": "...",
    "Issuer": "sitemind-auth",
    "Audience": "sitemind-api",
    "ExpirationMinutes": 60
  }
}
```

### 12.3. Port Yapılandırması

| Service | Port | Environment Variable |
|---------|------|---------------------|
| Gateway | 8080 | `ASPNETCORE_URLS=http://+:8080` |
| Auth | 5001 | `ASPNETCORE_URLS=http://+:5001` |
| Ingestion | 5002 | `ASPNETCORE_URLS=http://+:5002` |
| RAG | 5003 | `ASPNETCORE_URLS=http://+:5003` |
| PostgreSQL | 5432 | - |

---

## 13. Development Workflow

### 13.1. Local Development

**Gereksinimler:**
- .NET 9 SDK
- PostgreSQL 16 (veya Docker)
- Docker & Docker Compose (opsiyonel)

**Çalıştırma:**
```bash
# Terminal 1: PostgreSQL (Docker)
docker run -d -p 5432:5432 \
  -e POSTGRES_DB=sitemind \
  -e POSTGRES_USER=postgres \
  -e POSTGRES_PASSWORD=postgres \
  pgvector/pgvector:pg16

# Terminal 2: Auth Service
cd server/sitemind-auth
dotnet run

# Terminal 3: Ingestion Service
cd server/sitemind-ingestion
dotnet run

# Terminal 4: RAG Service
cd server/sitemind-rag
dotnet run

# Terminal 5: Gateway
cd server/sitemind-gateway
dotnet run
```

### 13.2. Database Migration

**İlk Setup:**
```bash
cd server/sitemind-ingestion
dotnet ef database update
```

**Yeni Migration:**
```bash
cd server/sitemind-shared
dotnet ef migrations add MigrationName --startup-project ../sitemind-ingestion
```

### 13.3. Testing

**Auth Test:**
```bash
curl -X POST http://localhost:5001/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"username":"test","password":"test"}'
```

**Website Create Test:**
```bash
ORG_ID="your-org-id"
curl -X POST http://localhost:5002/api/websites \
  -H "X-Organization-Id: $ORG_ID" \
  -H "Content-Type: application/json" \
  -d '{"name":"Test","baseUrl":"https://example.com"}'
```

---

## 14. Bilinen Sorunlar ve Notlar

### 14.1. Middleware Header Okuma

**Durum:** `OrganizationMiddleware`'de `X-Organization-Id` header'ı okuma konusunda araştırma devam ediyor.

**Geçici Çözüm:** JWT token kullanımı önerilir.

### 14.2. Mock Authentication

**Durum:** Auth servisinde şu anda mock authentication var.

**Production'da Yapılacaklar:**
- Database'den kullanıcı doğrulama
- Password hashing (bcrypt/Argon2)
- Organization-User ilişkisi

### 14.3. Migration Sırasında OrganizationProvider

**Durum:** Migration sırasında `OrganizationProvider` mevcut olmayabilir.

**Çözüm:** `GetCurrentOrganizationId()` metodunda exception handling var, `Guid.Empty` döner.

---

## 15. Gelecek Geliştirmeler

### 15.1. Öncelikli

- [ ] Firecrawl entegrasyonu (website crawling)
- [ ] OpenAI embedding entegrasyonu (vector generation)
- [ ] PGVector extension kullanımı (vector storage)
- [ ] RAG chat endpoint'leri
- [ ] Gerçek authentication (database-based)

### 15.2. Orta Vadeli

- [ ] Rate limiting
- [ ] Caching stratejisi (Redis)
- [ ] Background job processing (Hangfire/Quartz)
- [ ] API documentation (Swagger/OpenAPI)
- [ ] Logging & Monitoring (Serilog, Application Insights)

### 15.3. Uzun Vadeli

- [ ] Horizontal scaling
- [ ] Message queue (RabbitMQ/Kafka)
- [ ] Event sourcing
- [ ] CQRS pattern
- [ ] GraphQL API

---

## 16. Teknik Detaylar

### 16.1. Entity Framework Core

**Provider:** Npgsql.EntityFrameworkCore.PostgreSQL 9.0.2  
**Extensions:** NetTopologySuite (PGVector için hazırlık)  
**Migration Strategy:** Code-First  
**Query Filter:** Global Query Filter (Multi-tenancy)

### 16.2. Dependency Injection

**Lifetime:**
- `SiteMindDbContext`: Scoped
- `IOrganizationProvider`: Scoped
- `IHttpContextAccessor`: Singleton

**Registration:**
- `AddSiteMindDbContext` extension method ile merkezi yönetim

### 16.3. Middleware Pipeline

**Sıralama:**
1. HTTPS Redirection (opsiyonel)
2. OrganizationMiddleware (Multi-tenancy validation)
3. Authentication
4. Authorization
5. Controllers/Endpoints

---

## 17. Kod Örnekleri

### 17.1. Yeni Entity Ekleme

```csharp
// 1. BaseTenantEntity'den türet
public class MyEntity : BaseTenantEntity
{
    public string Name { get; set; }
}

// 2. DbContext'e ekle
public DbSet<MyEntity> MyEntities { get; set; }

// 3. OnModelCreating'de configure et
modelBuilder.Entity<MyEntity>(entity =>
{
    entity.HasKey(e => e.Id);
    entity.Property(e => e.Name).IsRequired();
});

// 4. Global Query Filter ekle
modelBuilder.Entity<MyEntity>().HasQueryFilter(e => 
    e.OrganizationId == GetCurrentOrganizationId());
```

### 17.2. Controller'da Kullanım

```csharp
[ApiController]
[Route("api/[controller]")]
public class MyController : ControllerBase
{
    private readonly SiteMindDbContext _context;

    public MyController(SiteMindDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<MyEntity>>> Get()
    {
        // Global Query Filter otomatik uygulanır
        var entities = await _context.MyEntities.ToListAsync();
        return Ok(entities);
    }

    [HttpPost]
    public async Task<ActionResult<MyEntity>> Create(CreateRequest request)
    {
        var entity = new MyEntity
        {
            Id = Guid.NewGuid(),
            Name = request.Name
            // OrganizationId otomatik set edilir
        };

        _context.MyEntities.Add(entity);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(Get), new { id = entity.Id }, entity);
    }
}
```

---

## 18. Performans Notları

### 18.1. Database

- **Index'ler:** `WebsiteId`, `OrganizationId` üzerinde index'ler var
- **Query Filter:** Runtime'da evaluate edilir (migration sırasında değil)
- **Connection Pooling:** EF Core otomatik connection pooling kullanır

### 18.2. Multi-Tenancy

- **Filtering:** Her query'de `WHERE OrganizationId = @orgId` eklenir
- **Performance:** Index sayesinde hızlı
- **Scalability:** Shared database, gelecekte sharding gerekebilir

---

## 19. Güvenlik Best Practices

1. **JWT Secret Key:** Production'da güçlü, random key kullan
2. **HTTPS:** Production'da zorunlu
3. **CORS:** Frontend domain'lerini whitelist'le
4. **Rate Limiting:** API abuse'i önlemek için
5. **Input Validation:** Tüm input'ları validate et
6. **SQL Injection:** EF Core parameterized queries kullanır (güvenli)

---

## 20. Kaynaklar ve Referanslar

- **.NET 9:** https://learn.microsoft.com/dotnet/core/whats-new/dotnet-9
- **YARP:** https://microsoft.github.io/reverse-proxy/
- **EF Core:** https://learn.microsoft.com/ef/core/
- **PGVector:** https://github.com/pgvector/pgvector
- **JWT:** https://jwt.io/

---

**Doküman Versiyonu:** 1.0  
**Son Güncelleme:** 2025-11-29  
**Hazırlayan:** SiteMind Development Team

