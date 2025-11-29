# SiteMind

Website içeriklerini vector database'e alan ve AI destekli RAG (Retrieval-Augmented Generation) chat servisleri sunan SaaS platformu.

## 🌐 Domain Yapılandırması

- **Frontend**: `sitemind.futureautonoms.com`
- **API**: `sitemindapi.futureautonoms.com`

## 🏗️ Mimari

SiteMind, mikroservis mimarisi kullanarak geliştirilmiş, strict multi-tenancy (shared database) destekleyen bir platformdur.

### Servisler

- **sitemind-gateway**: YARP Reverse Proxy (Entry Point - Port 8080)
- **sitemind-auth**: Identity & Organization Management (Port 5001)
- **sitemind-ingestion**: Website management & Crawling (Port 5002)
- **sitemind-rag**: Vector Search & Chat (Port 5003)
- **sitemind-shared**: Core Library (Entities, Data, Middleware)

## 🛠️ Teknoloji Stack

- **Backend**: .NET 9
- **Database**: PostgreSQL (PGVector)
- **Gateway**: YARP
- **Frontend**: Vue 3 (Composition API)
- **Containerization**: Docker & Docker Compose

## 🚀 Hızlı Başlangıç

### Gereksinimler

- .NET 9 SDK
- Node.js 18+
- Docker & Docker Compose
- PostgreSQL 15+ (PGVector extension)

### Kurulum

1. Repository'yi klonlayın:
```bash
git clone https://github.com/futureautonoms/sitemind.git
cd sitemind
```

2. Docker Compose ile servisleri başlatın:
```bash
docker-compose up -d
```

3. Frontend'i çalıştırın:
```bash
cd client/sitemind-client
npm install
npm run dev
```

### Production Deployment

Production ortamında:
- Frontend domain'i: `sitemind.futureautonoms.com`
- API domain'i: `sitemindapi.futureautonoms.com`
- Frontend, API'ye doğrudan `https://sitemindapi.futureautonoms.com/api` üzerinden bağlanır
- Tüm backend servisleri CORS ayarları ile frontend domain'ini destekler

## 📚 Dokümantasyon

Detaylı teknik dokümantasyon için [TECHNICAL_DOCUMENTATION.md](./TECHNICAL_DOCUMENTATION.md) dosyasına bakın.

## 📝 Lisans

Bu proje özel bir projedir.

