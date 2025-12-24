import { fireEvent, render, screen } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";

import { UploadDropzone } from "@/components/upload-dropzone";

describe("UploadDropzone", () => {
  it("calls callback when a file is selected", () => {
    const onFileSelected = vi.fn();
    const { container } = render(<UploadDropzone onFileSelected={onFileSelected} />);

    const input = container.querySelector('input[type="file"]') as HTMLInputElement;
    const file = new File(["hello"], "test.png", { type: "image/png" });

    fireEvent.change(input, { target: { files: [file] } });

    expect(onFileSelected).toHaveBeenCalledWith(file);
    expect(screen.getByText(/PNG, JPG/i)).toBeInTheDocument();
  });
});
