namespace CleanCut.Application.Interfaces;

public interface IModelManager
{
    Task EnsureModelAsync(CancellationToken cancellationToken = default);
}
