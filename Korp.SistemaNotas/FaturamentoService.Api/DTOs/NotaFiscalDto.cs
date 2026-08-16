using System.ComponentModel.DataAnnotations;

namespace FaturamentoService.Api.DTOs;

public class CriarNotaFiscalDto
{
    [Required]
    public int NumeroSequencial { get; set; }

    [MinLength(1, ErrorMessage = "A nota fiscal deve conter pelo menos um item.")]
    public List<CriarNotaFiscalItemDto> Itens { get; set; } = new();
}

public class CriarNotaFiscalItemDto
{
    [Required]
    public int ProdutoId { get; set; }

    [Range(0.01, double.MaxValue, ErrorMessage = "A quantidade deve ser maior que zero.")]
    public decimal Quantidade { get; set; }
}

public class BaixaEstoqueItemDto
{
    public int ProdutoId { get; set; }
    public decimal Quantidade { get; set; }
}