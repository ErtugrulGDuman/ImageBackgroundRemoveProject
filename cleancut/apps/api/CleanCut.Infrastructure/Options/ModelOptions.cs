namespace CleanCut.Infrastructure.Options;

public class ModelOptions
{
    public const string SectionName = "Model";

    public string ModelPath { get; set; } = "Models/u2netp.onnx";

    public string ModelUrl { get; set; } = "https://github.com/xuebinqin/U-2-Net/releases/download/v1/u2netp.onnx";

    public bool SkipDownload { get; set; }
}
