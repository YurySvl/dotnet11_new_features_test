namespace Showcase.Api.Validation;

public interface IUserDirectory
{
    Task<bool> ExistsAsync(string username, CancellationToken cancellationToken);
    Task AddAsync(string username, CancellationToken cancellationToken);
}

// In-memory fake that pretends to be a slow database.
public sealed class InMemoryUserDirectory : IUserDirectory
{
    private readonly HashSet<string> _names = [with(StringComparer.OrdinalIgnoreCase), "admin", "alice"];
    private readonly Lock _lock = new();

    public async Task<bool> ExistsAsync(string username, CancellationToken cancellationToken)
    {
        await Task.Delay(20, cancellationToken); // simulated I/O
        lock (_lock)
        {
            return _names.Contains(username);
        }
    }

    public Task AddAsync(string username, CancellationToken cancellationToken)
    {
        lock (_lock)
        {
            _names.Add(username);
        }
        return Task.CompletedTask;
    }
}
