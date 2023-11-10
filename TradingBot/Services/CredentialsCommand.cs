using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TradingBot.Database;

namespace TradingBot.Services;

public sealed class CredentialsCommand
{
    private readonly IDbContextFactory<AppDbContext> _dbContextFactory;

    public CredentialsCommand(IDbContextFactory<AppDbContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    public async Task<bool> AreCredentialsValidAsync(string username, string password)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        var entity = await context.Credentials.FirstOrDefaultAsync(c => c.Username == username);
        if (entity is null) return false;

        var hasher = new PasswordHasher<string>();
        return hasher.VerifyHashedPassword(entity.Username, entity.HashedPassword, password) !=
               PasswordVerificationResult.Failed;
    }

    public async Task ChangePasswordAsync(string username, string newPassword)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        var entity = await context.Credentials.FirstOrDefaultAsync(c => c.Username == username);
        if (entity is null) throw new InvalidOperationException($"User with username '{username}' does not exist");

        var hasher = new PasswordHasher<string>();
        entity.HashedPassword = hasher.HashPassword(entity.Username, newPassword);
        await context.SaveChangesAsync();
    }
}
