using EstoqueService.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace EstoqueService.Api.Data;

public class EstoqueDbContext : DbContext
{
    public EstoqueDbContext(DbContextOptions<EstoqueDbContext> options) : base(options) { }

    public DbSet<Produto> Produtos => Set<Produto>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Produto>()
            .HasIndex(p => p.Codigo)
            .IsUnique();

        modelBuilder.Entity<Produto>()
            .Property(p => p.Saldo)
            .HasPrecision(18, 2);
    }
}