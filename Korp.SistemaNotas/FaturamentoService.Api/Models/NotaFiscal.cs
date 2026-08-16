namespace FaturamentoService.Api.Models;

public class NotaFiscal
{
    public int Id { get; set; }
    public int NumeroSequencial { get; set; }
    public StatusNota Status { get; set; } = StatusNota.Aberta;
    public DateTime DataCriacao { get; set; } = DateTime.UtcNow;

    public List<NotaFiscalItem> Itens { get; set; } = new();
}