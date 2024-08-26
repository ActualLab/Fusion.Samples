using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Samples.Blazor.Abstractions;
using ActualLab.Fusion.Authentication.Services;
using ActualLab.Fusion.EntityFramework.Operations;

namespace Samples.Blazor.Server.Services;

public class AppDbContext(DbContextOptions options) : DbContext(options), IDataProtectionKeyContext
{
    public DbSet<ChatMessage> ChatMessages { get; protected set; } = null!;

    // ActualLab.Fusion.EntityFramework tables
    public DbSet<DbUser<long>> Users { get; protected set; } = null!;
    public DbSet<DbUserIdentity<long>> UserIdentities { get; protected set; } = null!;
    public DbSet<DbSessionInfo<long>> Sessions { get; protected set; } = null!;

    // ActualLab.Fusion.EntityFramework.Operations tables
    public DbSet<DbOperation> Operations { get; protected set; } = null!;
    public DbSet<DbEvent> Events { get; protected set; } = null!;

    // Data protection key storage
    public DbSet<DataProtectionKey> DataProtectionKeys { get; protected set; } = null!;
}
