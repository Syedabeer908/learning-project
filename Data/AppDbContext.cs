using Microsoft.EntityFrameworkCore;
using WebApplication1.Entities;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options) { }

    public DbSet<Risk> Risk { get; set; }
    public DbSet<Control> Control { get; set; }
    public DbSet<RiskControl> RiskControl { get; set; }
    public DbSet<User> User { get; set; }
    public DbSet<Role> Role { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasIndex(r => r.Name)
                .IsUnique();
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasOne(r => r.Role)
                  .WithMany(u => u.Users)
                  .HasForeignKey(ur => ur.RoleId)
                  .HasPrincipalKey(r => r.RoleId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(u => u.Email)
                  .IsUnique();
        });

        modelBuilder.Entity<Risk>(entity =>
        {
            entity.HasOne(u => u.User)
                  .WithMany(r => r.Risks)
                  .HasForeignKey(u => u.UserId)
                  .HasPrincipalKey(u => u.UserId)
                  .OnDelete(DeleteBehavior.NoAction);
        });

        modelBuilder.Entity<Control>(entity =>
        {
            entity.HasOne(u => u.User)
                  .WithMany(c => c.Controls)
                  .HasForeignKey(u => u.UserId)
                  .HasPrincipalKey(u => u.UserId)
                  .OnDelete(DeleteBehavior.NoAction);
        });

        modelBuilder.Entity<RiskControl>(entity =>
        {
            entity.HasOne(u => u.User)
                  .WithMany(r => r.RiskControls)
                  .HasForeignKey(u => u.UserId)
                  .HasPrincipalKey(u => u.UserId)
                  .OnDelete(DeleteBehavior.NoAction);

            entity.HasOne(r => r.Risk)
                  .WithMany(rc => rc.RiskControls)
                  .HasForeignKey(r => r.RiskId)
                  .HasPrincipalKey(r => r.RiskId)
                  .OnDelete(DeleteBehavior.NoAction);

            entity.HasOne(c => c.Control)
                  .WithMany(rc => rc.RiskControls)
                  .HasForeignKey(c => c.ControlId)
                  .HasPrincipalKey(c => c.ControlId)
                  .OnDelete(DeleteBehavior.NoAction);
        });
    }
}

