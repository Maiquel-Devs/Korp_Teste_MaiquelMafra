using FaturamentoService.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace FaturamentoService.Api.Data;

public class FaturamentoDbContext : DbContext
{
    public FaturamentoDbContext(DbContextOptions<FaturamentoDbContext> options) : base(options) { }

    public DbSet<NotaFiscal> NotasFiscais => Set<NotaFiscal>();
    public DbSet<NotaFiscalItem> NotaFiscalItens => Set<NotaFiscalItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<NotaFiscal>()
            .HasMany(n => n.Itens)
            .WithOne()
            .HasForeignKey(i => i.NotaFiscalId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<NotaFiscalItem>()
            .Property(i => i.Quantidade)
            .HasPrecision(18, 2);
    }
}