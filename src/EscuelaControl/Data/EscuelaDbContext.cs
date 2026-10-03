using EscuelaControl.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace EscuelaControl.Data;

public class EscuelaDbContext(DbContextOptions<EscuelaDbContext> options) : IdentityDbContext<IdentityUser>(options)
{
    public DbSet<Escuela> Escuelas => Set<Escuela>();
    public DbSet<Familia> Familias => Set<Familia>();
    public DbSet<Alumno> Alumnos => Set<Alumno>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.Entity<Escuela>().HasIndex(e => e.Clave).IsUnique();
        builder.Entity<Alumno>().HasIndex(a => a.Matricula).IsUnique();
        builder.Entity<Alumno>().HasIndex(a => new { a.EscuelaId, a.Activo });
        builder.Entity<Alumno>().HasOne(a => a.Familia).WithMany(f => f.Alumnos)
            .HasForeignKey(a => a.FamiliaId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<Alumno>().HasOne(a => a.Escuela).WithMany(e => e.Alumnos)
            .HasForeignKey(a => a.EscuelaId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<Escuela>().Property(e => e.Nivel).HasConversion<string>().HasMaxLength(20);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        foreach (var entry in ChangeTracker.Entries<Registro>())
        {
            if (entry.State is EntityState.Modified or EntityState.Added)
            {
                entry.Entity.Version = Guid.NewGuid();
                entry.Entity.ActualizadoUtc = DateTime.UtcNow;
            }
        }
        return base.SaveChangesAsync(cancellationToken);
    }
}
