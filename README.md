# CleanCut Background Remover (.NET + Next.js)
Production'a yakın, gizlilik dostu arka plan silme uygulaması. Backend: .NET 8 Web API (ONNX Runtime + U^2-Net), Frontend: Next.js 14 (App Router) + TypeScript + Tailwind. Docker KULLANILMAZ; her şey yerelde kolayca çalışır.

## Gereksinimler
- .NET 8 SDK
- Node.js 18+ (npm)
- İlk çalıştırmada internet bağlantısı (u2netp modelinin otomatik indirimi için)

## Hızlı Başlangıç
### Backend çalıştırma
```bash
cd cleancut/apps/api/CleanCut.Api
dotnet restore
dotnet run
```
Varsayılan URL: http://localhost:5000 (Swagger: http://localhost:5000/swagger)

### Frontend çalıştırma
```bash
cd cleancut/apps/web
npm install
npm run dev
```
Varsayılan URL: http://localhost:3000

> Frontend, backend adresini `NEXT_PUBLIC_API_BASE_URL` ile alır. Örnek `.env.example` dosyasını `.env.local` olarak kopyalayın.

## Mimari
- **Backend** (`apps/api`):
  - Katmanlar: Api (controllers), Application (sözleşmeler), Infrastructure (ONNX + ImageSharp işleme).
  - Model yönetimi: `ModelManager` açılışta u2netp modelini indirir (yol: `apps/api/Models/u2netp.onnx`). Env ile özelleştirilebilir: `CLEANCUT_MODEL_PATH`, `Model:ModelUrl`, `Model:SkipDownload`.
  - İşleme: ImageSharp ile okuma → 320x320 tensör → ONNX Runtime (u2netp) → maske normalizasyonu → PNG (alfa) veya JPG (bgColor + kalite).
  - Güvenlik/limitler: 10MB dosya sınırı, MIME kontrolü, IP bazlı dakikada 20 istek rate limit, CORS (AllowedOrigins).
  - Logging: Serilog + request logging.
- **Frontend** (`apps/web`):
  - Drag & drop upload, 10MB sınırı, PNG/JPG/JPEG/WEBP.
  - İşleme akışı: dosya → `/api/background/remove?output=png` → before/after slider → indirme seçenekleri (PNG, JPG + renk + kalite).
  - Tema toggle, responsive grid, toast ile hata/bilgilendirme.

## Ortam Değişkenleri
### Frontend (`apps/web/.env.local`)
```
NEXT_PUBLIC_API_BASE_URL=http://localhost:5000
```

### Backend (appsettings veya env)
- `Api:AllowedOrigins` veya `CLEANCUT_ALLOWED_ORIGINS` (virgülle ayrılmış), varsayılan `http://localhost:3000`
- `Api:MaxFileSizeBytes` (varsayılan 10485760)
- `RateLimit:RequestsPerMinute` (varsayılan 20)
- `Model:ModelPath` veya `CLEANCUT_MODEL_PATH` (varsayılan `Models/u2netp.onnx`)
- `Model:ModelUrl` (varsayılan u2netp indirimi)
- `Model:SkipDownload` (true ise otomatik indirme atlanır)

## Model İndirme
- Otomatik: Backend açılırken model yoksa indirir.
- Manuel: `scripts/download-model.sh` veya `scripts/download-model.ps1` çalıştırın (model repo'ya eklenmez).

## API Kullanımı
- **POST** `/api/background/remove`
  - multipart/form-data: `file`
  - Query: `output=png|jpg` (default png), `bgColor=#RRGGBB` (jpg için), `quality=0-100` (jpg, default 92)
  - Yanıt: işlenmiş görsel (binary) doğru `content-type` ile
- **GET** `/health`

### Örnek curl
```bash
curl -X POST "http://localhost:5000/api/background/remove?output=png" \
  -F "file=@/path/to/photo.jpg" \
  --output result.png

curl -X POST "http://localhost:5000/api/background/remove?output=jpg&bgColor=%23ffcc00&quality=90" \
  -F "file=@/path/to/photo.jpg" \
  --output result.jpg
```

## Testler
- Backend: `cd cleancut/apps/api/tests/CleanCut.Api.Tests && dotnet test`
- Frontend: `cd cleancut/apps/web && npm test` (vitest smoke testi: upload dropzone)

## Troubleshooting
- **Model indirilemiyor**: Çevrimdışı çalışmak için modeli manuel indirip `apps/api/Models` altına koyun veya `Model:SkipDownload=true` ayarlayıp dosyayı kendiniz sağlayın.
- **CORS hatası**: Frontend adresini `Api:AllowedOrigins` listesine ekleyin (örn. env `CLEANCUT_ALLOWED_ORIGINS=http://localhost:3000`).
- **429 Rate limit**: IP başına dakika başı 20 istek; gerekirse `RateLimit:RequestsPerMinute` değerini yükseltin.
- **10MB sınırı**: Daha büyük dosyalar 413 döner; görseli küçültüp tekrar deneyin.

## Decisions
- Docker kullanılmadı; VS Code ile doğrudan çalışacak şekilde yapılandırıldı.
- u2netp modeli (küçük, hızlı) tercih edildi; başka model kullanmak için `ModelUrl` + `ModelPath` env'lerini değiştirin.
- Testlerde gerçek ONNX indirimi gerektirmemek için `FakeBackgroundRemovalService` ile DI override edildi.
- Frontend'de kalite/rengin backend'e query paramlarıyla iletilmesi, tek endpoint üzerinden hem PNG hem JPG çıktı verir.
