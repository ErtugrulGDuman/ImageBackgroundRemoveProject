"use client";

import { useEffect, useMemo, useState } from "react";
import { Loader2, Sparkles, Wand2 } from "lucide-react";

import { UploadDropzone } from "@/components/upload-dropzone";
import { ImageCompare } from "@/components/image-compare";
import { ThemeToggle } from "@/components/theme-toggle";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { useToast } from "@/components/ui/use-toast";
import { cn } from "@/lib/utils";

const MAX_FILE_BYTES = 10 * 1024 * 1024;
const API_BASE_URL = process.env.NEXT_PUBLIC_API_BASE_URL ?? "http://localhost:5000";

type BackgroundOption = "transparent" | "white" | "black" | "custom";

const backgroundOptions: { key: BackgroundOption; label: string; description: string; color?: string }[] = [
  { key: "transparent", label: "Şeffaf", description: "PNG ile alfa kanallı indirin" },
  { key: "white", label: "Beyaz", description: "JPG için temiz beyaz zemin", color: "#ffffff" },
  { key: "black", label: "Siyah", description: "JPG için koyu zemin", color: "#000000" },
  { key: "custom", label: "Özel", description: "Marka rengi seç", color: "#5b21b6" },
];

const SAMPLE_BEFORE = "/sample-before.svg";
const SAMPLE_AFTER = "/sample-after.svg";

export default function HomePage() {
  const { toast, dismiss } = useToast();
  const [selectedFile, setSelectedFile] = useState<File | null>(null);
  const [originalUrl, setOriginalUrl] = useState<string | null>(null);
  const [resultBlob, setResultBlob] = useState<Blob | null>(null);
  const [resultUrl, setResultUrl] = useState<string | null>(null);
  const [isProcessing, setIsProcessing] = useState(false);
  const [isExporting, setIsExporting] = useState(false);
  const [background, setBackground] = useState<BackgroundOption>("transparent");
  const [customColor, setCustomColor] = useState("#5b21b6");
  const [quality, setQuality] = useState(92);

  useEffect(() => {
    return () => {
      if (originalUrl) URL.revokeObjectURL(originalUrl);
      if (resultUrl) URL.revokeObjectURL(resultUrl);
    };
  }, [originalUrl, resultUrl]);

  const previewBefore = originalUrl ?? SAMPLE_BEFORE;
  const previewAfter = resultUrl ?? SAMPLE_AFTER;

  const callToast = (title: string, description?: string) => {
    dismiss();
    toast({ title, description });
  };

  const validateFile = (file: File) => {
    if (!file.type.startsWith("image/")) {
      callToast("Geçersiz dosya", "Lütfen bir görsel yükleyin.");
      return false;
    }
    if (file.size > MAX_FILE_BYTES) {
      callToast("Dosya çok büyük", "Maksimum 10MB yükleyebilirsiniz.");
      return false;
    }
    return true;
  };

  const handleFileSelected = (file: File) => {
    if (!validateFile(file)) return;
    setSelectedFile(file);
    const original = URL.createObjectURL(file);
    setOriginalUrl(original);
    processRemoval(file);
  };

  const processRemoval = async (file: File) => {
    setIsProcessing(true);
    setResultBlob(null);
    setResultUrl(null);
    try {
      const formData = new FormData();
      formData.append("file", file);
      const response = await fetch(`${API_BASE_URL}/api/background/remove?output=png`, {
        method: "POST",
        body: formData,
      });
      if (!response.ok) {
        const error = await response.json().catch(() => ({ error: "İşlem başarısız." }));
        throw new Error(error.error || "İşlem sırasında hata oluştu.");
      }
      const blob = await response.blob();
      setResultBlob(blob);
      const url = URL.createObjectURL(blob);
      setResultUrl(url);
      callToast("Hazır!", "Arka plan başarıyla temizlendi.");
    } catch (error) {
      callToast("İşlem başarısız", (error as Error).message);
    } finally {
      setIsProcessing(false);
    }
  };

  const downloadBlob = (blob: Blob, filename: string) => {
    const link = document.createElement("a");
    link.href = URL.createObjectURL(blob);
    link.download = filename;
    link.click();
    URL.revokeObjectURL(link.href);
  };

  const handleDownloadPng = () => {
    if (!resultBlob) {
      callToast("Henüz çıktı yok", "Lütfen bir görsel işleyin.");
      return;
    }
    downloadBlob(resultBlob, "cleancut.png");
  };

  const getSelectedColor = () => {
    const preset = backgroundOptions.find((opt) => opt.key === background)?.color;
    return background === "custom" ? customColor : preset ?? "#ffffff";
  };

  const handleExport = async (format: "jpeg" | "png") => {
    if (!resultBlob) {
      callToast("Henüz çıktı yok", "Lütfen önce arka planı temizleyin.");
      return;
    }
    setIsExporting(true);
    try {
      const formData = new FormData();
      formData.append("file", resultBlob, "output.png");
      const params = new URLSearchParams();
      params.set("output", format === "jpeg" ? "jpg" : "png");
      if (format === "jpeg") {
        params.set("bgColor", getSelectedColor());
        params.set("quality", quality.toString());
      }

      const response = await fetch(`${API_BASE_URL}/api/background/remove?${params.toString()}`, {
        method: "POST",
        body: formData,
      });
      if (!response.ok) {
        const error = await response.json().catch(() => ({ error: "Dışa aktarım başarısız." }));
        throw new Error(error.error || "Dışa aktarım sırasında hata oluştu.");
      }
      const blob = await response.blob();
      const extension = format === "png" ? "png" : "jpg";
      downloadBlob(blob, `cleancut.${extension}`);
      callToast("İndirme hazır", "Arka plan ayarları uygulandı.");
    } catch (error) {
      callToast("İndirme başarısız", (error as Error).message);
    } finally {
      setIsExporting(false);
    }
  };

  const processingMessage = useMemo(() => {
    if (isProcessing) return "Arka plan temizleniyor...";
    if (isExporting) return "Arka plan uygulanıyor...";
    return "";
  }, [isProcessing, isExporting]);

  return (
    <main className="mx-auto max-w-6xl px-4 pb-16 pt-10 md:px-8">
      <header className="mb-10 flex items-center justify-between gap-4">
        <div>
          <p className="text-xs uppercase tracking-[0.25em] text-primary">CleanCut</p>
          <h1 className="text-3xl font-bold md:text-4xl">Background Remover</h1>
          <p className="mt-2 max-w-2xl text-muted-foreground">
            Görsellerinizin arka planını saniyeler içinde kaldırın, şeffaf PNG indirin veya markanıza uygun zeminlerle hızla dışa aktarın.
          </p>
        </div>
        <ThemeToggle />
      </header>

      <div className="grid gap-6 lg:grid-cols-2">
        <div className="flex flex-col gap-4">
          <Card>
            <CardHeader>
              <CardTitle className="flex items-center gap-2">
                <Wand2 className="h-5 w-5 text-primary" />
                Yükle ve temizle
              </CardTitle>
              <CardDescription>
                Sürükle-bırak veya dosya seç. Maksimum 10MB, PNG/JPG/JPEG/WEBP.
              </CardDescription>
            </CardHeader>
            <CardContent>
              <UploadDropzone onFileSelected={handleFileSelected} disabled={isProcessing} />
              <div className="mt-4 flex items-center justify-between text-xs text-muted-foreground">
                <span>Gizlilik: Görseller sunucuda kalıcı saklanmaz.</span>
                {processingMessage && (
                  <span className="flex items-center gap-2 text-primary">
                    <Loader2 className="h-4 w-4 animate-spin" />
                    {processingMessage}
                  </span>
                )}
              </div>
            </CardContent>
          </Card>

          <Card>
            <CardHeader>
              <CardTitle>Arka plan seçenekleri</CardTitle>
              <CardDescription>İndirirken uygulanacak zemini seç.</CardDescription>
            </CardHeader>
            <CardContent className="space-y-4">
              <div className="grid gap-3 md:grid-cols-2">
                {backgroundOptions.map((option) => (
                  <button
                    key={option.key}
                    className={cn(
                      "flex items-start gap-3 rounded-lg border p-3 text-left transition hover:border-primary",
                      background === option.key && "border-primary bg-primary/5"
                    )}
                    onClick={() => setBackground(option.key)}
                    aria-pressed={background === option.key}
                  >
                    <div className="mt-0.5 h-3 w-3 rounded-full border border-primary bg-primary/80" />
                    <div>
                      <p className="text-sm font-semibold">{option.label}</p>
                      <p className="text-xs text-muted-foreground">{option.description}</p>
                    </div>
                  </button>
                ))}
              </div>
              {background === "custom" && (
                <div className="flex items-center gap-3">
                  <Label htmlFor="customColor" className="text-sm">
                    Renk
                  </Label>
                  <Input
                    id="customColor"
                    type="color"
                    value={customColor}
                    onChange={(e) => setCustomColor(e.target.value)}
                    className="h-10 w-20 p-1"
                  />
                  <Input
                    type="text"
                    value={customColor}
                    onChange={(e) => setCustomColor(e.target.value)}
                    className="max-w-[140px]"
                  />
                </div>
              )}
              <div>
                <Label htmlFor="quality" className="text-sm">
                  JPG kalite ({quality})
                </Label>
                <input
                  id="quality"
                  type="range"
                  min={50}
                  max={100}
                  value={quality}
                  onChange={(e) => setQuality(Number(e.target.value))}
                  className="mt-2 w-full"
                />
              </div>
              <div className="flex flex-wrap gap-3">
                <Button onClick={handleDownloadPng} disabled={isProcessing || !resultBlob}>
                  Şeffaf PNG indir
                </Button>
                <Button
                  variant="secondary"
                  onClick={() => handleExport("jpeg")}
                  disabled={isProcessing || isExporting || !resultBlob}
                >
                  {isExporting ? "Hazırlanıyor..." : "Seçili zeminle JPG"}
                </Button>
                <Button
                  variant="outline"
                  onClick={() => handleExport("png")}
                  disabled={isProcessing || isExporting || !resultBlob}
                >
                  Seçili zeminle PNG
                </Button>
              </div>
            </CardContent>
          </Card>
        </div>

        <div className="flex flex-col gap-4">
          <Card>
            <CardHeader>
              <CardTitle className="flex items-center gap-2">
                <Sparkles className="h-5 w-5 text-primary" /> Önizleme & karşılaştırma
              </CardTitle>
              <CardDescription>Öncesi/Sonrası kaydırıcısıyla sonucu kontrol et.</CardDescription>
            </CardHeader>
            <CardContent>
              <ImageCompare beforeSrc={previewBefore} afterSrc={previewAfter} />
            </CardContent>
          </Card>

          <Card>
            <CardHeader>
              <CardTitle>İpuçları</CardTitle>
              <CardDescription>En iyi sonuçlar için.</CardDescription>
            </CardHeader>
            <CardContent className="grid gap-3 text-sm text-muted-foreground">
              <div className="rounded-lg bg-secondary/60 p-3">
                Yüksek kontrastlı görsellerde model daha iyi çalışır. 3000x3000 piksel altı görseller önerilir.
              </div>
              <div className="rounded-lg bg-secondary/60 p-3">
                İlk kullanımda model indirilir; birkaç saniye sürebilir. Sonraki işler hızlanır.
              </div>
              <div className="rounded-lg bg-secondary/60 p-3">
                Mobilde tek kolon, masaüstünde çift kolon düzeni otomatik uygulanır.
              </div>
            </CardContent>
          </Card>
        </div>
      </div>
    </main>
  );
}
