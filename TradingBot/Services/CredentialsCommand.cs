using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using TradingBot.Configuration;
using TradingBot.Database;
using TradingBot.Database.Entities;
using ILogger = Serilog.ILogger;

namespace TradingBot.Services;

public sealed class CredentialsCommand
{
    private readonly IOptions<SeedCredentialsConfiguration> _config;
    private readonly IDbContextFactory<AppDbContext> _dbContextFactory;
    private readonly ILogger _logger;

    public CredentialsCommand(IDbContextFactory<AppDbContext> dbContextFactory, ILogger logger,
        IOptions<SeedCredentialsConfiguration> config)
    {
        _dbContextFactory = dbContextFactory;
        _logger = logger.ForContext<CredentialsCommand>();
        _config = config;
    }

    public async Task<bool> AreCredentialsValidAsync(string username, string password,
        CancellationToken token = default)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync(token);
        var entity = await context.Credentials.FirstOrDefaultAsync(c => c.Username == username, token);
        if (entity is null) return false;

        var hasher = new PasswordHasher<string>();
        return hasher.VerifyHashedPassword(entity.Username, entity.HashedPassword, password) !=
               PasswordVerificationResult.Failed;
    }

    public async Task ChangePasswordAsync(string username, string newPassword, CancellationToken token = default)
    {
        _logger.Debug("Changing password");

        await using var context = await _dbContextFactory.CreateDbContextAsync(token);
        var entity = await context.Credentials.FirstOrDefaultAsync(c => c.Username == username, token);
        if (entity is null) throw new InvalidOperationException($"User with username '{username}' does not exist");

        var hasher = new PasswordHasher<string>();
        entity.HashedPassword = hasher.HashPassword(entity.Username, newPassword);
        await context.SaveChangesAsync(token);

        _logger.Information("Password was successfully changed");
    }

    public async Task CreateDefaultUserAsync(CancellationToken token = default)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync(token);
        if (await context.Credentials.AnyAsync(token))
        {
            _logger.Information("Credentials are already configured - skipping default user creation");
            return;
        }

        var hasher = new PasswordHasher<string>();
        context.Credentials.Add(new UserCredentialsEntity
        {
            Id = Guid.NewGuid(),
            Username = _config.Value.DefaultUsername,
            HashedPassword = hasher.HashPassword(_config.Value.DefaultUsername, _config.Value.DefaultPassword)
        });
        await context.SaveChangesAsync(token);

        _logger.Information("Default credentials were successfully set up");
    }
}
