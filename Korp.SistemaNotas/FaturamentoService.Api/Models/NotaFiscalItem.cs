using System.ComponentModel.DataAnnotations;

namespace FaturamentoService.Api.Models;

public class NotaFiscalItem
{
    public int Id { get; set; }
    public int NotaFiscalId { get; set; }

    public int ProdutoId { get; set; }

    [Range(0.01, double.MaxValue)]
    public decimal Quantidade { get; set; }
}