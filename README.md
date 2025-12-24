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
