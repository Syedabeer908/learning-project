using Microsoft.EntityFrameworkCore;
using WebApplication1.Entities;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options) { }

    public DbSet<Risk> Risk { get; set; }
    public DbSet<Control> Control { get; set; }
    public DbSet<RiskControl> RiskControl { get; set; }
    public DbSet<User> User { get; set; }
    public DbSet<Role> Role { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<RiskControl>(entity =>
        {
            entity.HasOne(rc => rc.Risk)
                  .WithMany(r => r.RiskControl)
                  .HasForeignKey(rc => rc.RiskId)
                  .HasPrincipalKey(r => r.RiskId);

            entity.HasOne(rc => rc.Control)
                  .WithMany(c => c.RiskControl)
                  .HasForeignKey(rc => rc.ControlId)
                  .HasPrincipalKey(r => r.ControlId);
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

        modelBuilder.Entity<Role>( entity =>
        {     
            entity.HasIndex(r => r.Name)
                .IsUnique();
        });
    }
}

