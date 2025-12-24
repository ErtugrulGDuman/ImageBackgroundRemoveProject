namespace CleanCut.Application.Models;

public sealed record ProcessedImageResult(byte[] Data, string ContentType, string FileName);
