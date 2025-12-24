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
# CleanCut Background Remover

Production'a yakın, gizlilik dostu arka plan silme web uygulaması. Next.js 14 (App Router) + Tailwind + shadcn/ui frontend'i FastAPI backend ile konuşturur. Arka plan silme, **rembg (U2NET)** modeli ile tamamen lokal/offline yapılır.

## Özellik Özeti
- Drag & drop yükleme, 10MB sınırı, PNG/JPG/JPEG/WEBP desteği.
- Rembg ile arka plan silme → alfa kanallı PNG döner.
- Öncesi/sonrası kaydırıcı, canlı tema değişimi (koyu/açık).
- Arka plan seçenekleri: şeffaf, beyaz, siyah, özel renk, blur. PNG veya JPG dışa aktarım.
- Gizlilik: dosyalar kalıcı tutulmaz; işlem bellek/stream üstünden yapılır.
- Basit rate limit (varsayılan: IP başına dakikada 30 istek).
- Docker Compose ile tek komutla hem backend hem frontend.

## Mimari
- **Frontend:** `cleancut/apps/web` — Next.js 14 App Router, TypeScript strict, Tailwind, shadcn tabanlı buton/card/toast bileşenleri, `next-themes` ile tema toggle.
- **Backend:** `cleancut/apps/api` — FastAPI, `rembg` (U2NET) ile arka plan silme, Pillow ile renk/blur uygulama, CORS ve rate-limit middleware.
- **API uçları:**
  - `POST /api/remove-bg` → multipart `file`, 10MB ve MIME doğrulaması, çıktı: `image/png` (alfa).
  - `POST /api/export` → `file` (PNG), `background` (`transparent|white|black|custom|blur`), `color` (hex), `output_format` (`png|jpeg`). Çıktı: `image/png` veya `image/jpeg`.
  - `GET /health` → basit sağlık kontrolü.
- **Varsayılan varsayımlar:**
  - Rembg model adı `u2net`; docker build sırasında indiriliyor (ilk build uzayabilir fakat ilk istekte gecikme olmaz).
  - Dosyalar disk persist edilmez; bellekte tutulur ve yanıtlandıktan sonra referans bırakılmaz.
  - Rate limit ve CORS değerleri env ile değiştirilebilir.

## Hızlı Başlangıç (Docker Compose)
> Gereksinimler: Docker, Docker Compose

```bash
cd cleancut
docker-compose up --build
```

- Frontend: http://localhost:3000
- Backend: http://localhost:8000

## Lokal Geliştirme
### Backend (FastAPI)
```bash
cd cleancut/apps/api
python -m venv .venv && source .venv/bin/activate
pip install -r requirements.txt
uvicorn cleancut_api.main:app --reload --host 0.0.0.0 --port 8000
```
Test & lint:
```bash
pytest
ruff check
```

### Frontend (Next.js)
```bash
cd cleancut/apps/web
pnpm install
pnpm dev
```
Test & lint:
```bash
pnpm test
pnpm lint
```

## Ortam Değişkenleri
`.env.example` içindeki değerler Compose için de kullanılır.

| Değişken | Açıklama | Varsayılan |
| --- | --- | --- |
| `CLEANCUT_ALLOWED_ORIGINS` | CORS için izin verilen origin listesi (virgülle ayrılmış) | `http://localhost:3000` |
| `CLEANCUT_RATE_LIMIT` | IP başına dakika başı istek sınırı | `30` |
| `CLEANCUT_REMBG_MODEL` | rembg model adı | `u2net` |
| `NEXT_PUBLIC_API_BASE_URL` | Frontend'in backend'e bağlanacağı URL | `http://localhost:8000` |

## Testler
- Backend: `pytest` (geçersiz dosya reddi, PNG çıktısı doğrulama, JPEG export testi).
- Frontend: `vitest` + Testing Library ile upload bileşeni smoke testi.

## Troubleshooting
- **Model indirimi:** İlk Docker build veya ilk lokal çalıştırmada `u2net` modeli indirilir; internetiniz yavaşsa süre uzayabilir. Gerekirse `CLEANCUT_REMBG_MODEL` ile farklı (küçük) model seçilebilir.
- **Performans:** Görseller bellekte işlenir; 10MB sınırı aşılırsa 413 hatası döner (UI ve API). Rate limit 429 döndürebilir.
- **CORS:** Farklı origin kullanıyorsanız `CLEANCUT_ALLOWED_ORIGINS` değerini güncelleyin.

## Kabul Kriteri Notları
- 10MB limit ve MIME kontrolü hem frontend hem backend'de mevcut.
- Öncesi/sonrası kaydırıcı, şeffaf PNG indirme ve renkli zeminle JPG/PNG dışa aktarım akışları hazır.
- Docker Compose ile tek komut (`up --build`) iki servisi ayağa kaldırır; healthcheck ve restart politikası eklenmiştir.
