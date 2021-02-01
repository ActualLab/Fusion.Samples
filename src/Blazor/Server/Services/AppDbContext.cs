using Microsoft.EntityFrameworkCore;
using Samples.Blazor.Abstractions;
using Stl.Fusion.EntityFramework.Authentication;
using Stl.Fusion.EntityFramework.Operations;

namespace Samples.Blazor.Server.Services
{
    public class AppDbContext : DbContext
    {
        public DbSet<ChatMessage> ChatMessages { get; protected set; } = null!;

        // Stl.Fusion.EntityFramework tables
        public DbSet<DbOperation> Operations { get; protected set; } = null!;
        public DbSet<DbSessionInfo> Sessions { get; protected set; } = null!;
        public DbSet<DbUser> Users { get; protected set; } = null!;
        public DbSet<DbUserIdentity> UserIdentities { get; protected set; } = null!;

        public AppDbContext(DbContextOptions options) : base(options) { }
    }
}
