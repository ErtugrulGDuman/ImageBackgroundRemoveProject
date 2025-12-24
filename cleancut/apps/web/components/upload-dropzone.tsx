"use client";

import { useCallback, useState } from "react";
import { Upload } from "lucide-react";

import { Button } from "@/components/ui/button";
import { cn } from "@/lib/utils";

interface UploadDropzoneProps {
  onFileSelected: (file: File) => void;
  disabled?: boolean;
}

export function UploadDropzone({ onFileSelected, disabled }: UploadDropzoneProps) {
  const [isDragging, setIsDragging] = useState(false);

  const handleFiles = useCallback(
    (files: FileList | null) => {
      if (!files?.length) return;
      const [file] = files;
      onFileSelected(file);
    },
    [onFileSelected]
  );

  return (
    <div
      className={cn(
        "group relative flex min-h-[200px] flex-col items-center justify-center rounded-2xl border-2 border-dashed border-muted-foreground/30 bg-secondary/40 px-6 py-8 text-center transition hover:border-primary/60",
        isDragging && "border-primary bg-primary/5",
        disabled && "cursor-not-allowed opacity-70"
      )}
      onDragOver={(e) => {
        e.preventDefault();
        if (!disabled) setIsDragging(true);
      }}
      onDragLeave={(e) => {
        e.preventDefault();
        setIsDragging(false);
      }}
      onDrop={(e) => {
        e.preventDefault();
        if (disabled) return;
        setIsDragging(false);
        handleFiles(e.dataTransfer.files);
      }}
    >
      <Upload className="h-10 w-10 text-primary" />
      <p className="mt-4 text-lg font-semibold">Görselini yükle</p>
      <p className="mt-2 text-sm text-muted-foreground">
        PNG, JPG veya WEBP • Maksimum 10MB
      </p>
      <div className="mt-6 flex flex-col items-center gap-2 md:flex-row md:gap-3">
        <label className="inline-flex cursor-pointer items-center">
          <input
            type="file"
            accept="image/png,image/jpeg,image/jpg,image/webp"
            className="hidden"
            disabled={disabled}
            onChange={(event) => handleFiles(event.target.files)}
          />
          <Button type="button" disabled={disabled}>
            Dosya seç
          </Button>
        </label>
        <span className="text-sm text-muted-foreground">veya sürükle-bırak yap</span>
      </div>
    </div>
  );
}
