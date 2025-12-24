"use client";

import Image from "next/image";
import { useMemo, useState } from "react";

interface ImageCompareProps {
  beforeSrc: string;
  afterSrc: string;
}

export function ImageCompare({ beforeSrc, afterSrc }: ImageCompareProps) {
  const [position, setPosition] = useState(50);

  const clipStyle = useMemo(() => ({
    clipPath: `inset(0 ${100 - position}% 0 0)`,
  }), [position]);

  return (
    <div className="relative w-full overflow-hidden rounded-xl border bg-card">
      <div className="relative aspect-[4/3] w-full">
        <Image src={beforeSrc} alt="Öncesi" fill className="object-contain" sizes="(min-width: 768px) 50vw, 100vw" />
        <div className="absolute inset-0" style={clipStyle}>
          <Image src={afterSrc} alt="Sonrası" fill className="object-contain" sizes="(min-width: 768px) 50vw, 100vw" />
        </div>
      </div>
      <div className="relative isolate -mt-6 flex items-center px-6 pb-4">
        <input
          type="range"
          min={0}
          max={100}
          value={position}
          onChange={(e) => setPosition(Number(e.target.value))}
          className="w-full accent-primary"
          aria-label="Öncesi/sonrası kaydırıcı"
        />
      </div>
    </div>
  );
}
