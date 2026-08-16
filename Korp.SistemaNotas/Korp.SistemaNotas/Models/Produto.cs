using System.ComponentModel.DataAnnotations;

namespace EstoqueService.Api.Models;

public class Produto
{
    public int Id { get; set; }

    [Required]
    public string Codigo { get; set; } = string.Empty;

    [Required]
    public string Descricao { get; set; } = string.Empty;

    [Range(0, double.MaxValue)]
    public decimal Saldo { get; set; }

    
    [Timestamp]
    public byte[]? VersaoLinha { get; set; }
}