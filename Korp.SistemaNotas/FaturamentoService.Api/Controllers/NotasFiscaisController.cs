using FaturamentoService.Api.Data;
using FaturamentoService.Api.DTOs;
using FaturamentoService.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FaturamentoService.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class NotasFiscaisController : ControllerBase
{
    private readonly FaturamentoDbContext _context;
    private readonly HttpClient _httpClient;

    public NotasFiscaisController(FaturamentoDbContext context, IHttpClientFactory httpClientFactory)
    {
        _context = context;
        _httpClient = httpClientFactory.CreateClient("EstoqueService");
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<NotaFiscal>>> GetNotas()
    {
        return await _context.NotasFiscais
            .Include(n => n.Itens)
            .AsNoTracking()
            .ToListAsync();
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<NotaFiscal>> GetNota(int id)
    {
        var nota = await _context.NotasFiscais
            .Include(n => n.Itens)
            .FirstOrDefaultAsync(n => n.Id == id);

        if (nota == null)
            return NotFound(new { mensagem = $"Nota fiscal com ID {id} não encontrada." });

        return Ok(nota);
    }

    [HttpPost]
    public async Task<ActionResult<NotaFiscal>> CreateNota([FromBody] CriarNotaFiscalDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var numeroExistente = await _context.NotasFiscais.AnyAsync(n => n.NumeroSequencial == dto.NumeroSequencial);
        if (numeroExistente)
            return BadRequest(new { mensagem = $"Já existe uma nota fiscal cadastrada com o número sequencial {dto.NumeroSequencial}." });

        var novaNota = new NotaFiscal
        {
            NumeroSequencial = dto.NumeroSequencial,
            Status = StatusNota.Aberta,
            DataCriacao = DateTime.UtcNow,
            Itens = dto.Itens.Select(i => new NotaFiscalItem
            {
                ProdutoId = i.ProdutoId,
                Quantidade = i.Quantidade
            }).ToList()
        };

        _context.NotasFiscais.Add(novaNota);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetNota), new { id = novaNota.Id }, novaNota);
    }

    [HttpPost("{id:int}/fechar")]
    public async Task<IActionResult> FecharNota(int id)
    {
        var nota = await _context.NotasFiscais
            .Include(n => n.Itens)
            .FirstOrDefaultAsync(n => n.Id == id);

        if (nota == null)
            return NotFound(new { mensagem = $"Nota fiscal com ID {id} não encontrada." });

        if (nota.Status == StatusNota.Fechada)
            return BadRequest(new { mensagem = "Esta nota fiscal já se encontra fechada." });

        if (!nota.Itens.Any())
            return BadRequest(new { mensagem = "A nota fiscal não possui itens para fechamento." });

        // Monta o payload para baixar o estoque no EstoqueService
        var itensParaBaixa = nota.Itens.Select(i => new BaixaEstoqueItemDto
        {
            ProdutoId = i.ProdutoId,
            Quantidade = i.Quantidade
        }).ToList();

        try
        {
            var response = await _httpClient.PostAsJsonAsync("api/produtos/baixar-estoque", itensParaBaixa);

            if (!response.IsSuccessStatusCode)
            {
                var erroDetalhe = await response.Content.ReadAsStringAsync();
                return StatusCode((int)response.StatusCode, new
                {
                    mensagem = "Não foi possível fechar a nota fiscal devido a um erro no estoque.",
                    detalhes = erroDetalhe
                });
            }
        }
        catch (HttpRequestException ex)
        {
            return StatusCode(503, new
            {
                mensagem = "O serviço de Estoque está indisponível no momento. Tente novamente mais tarde.",
                erro = ex.Message
            });
        }

        nota.Status = StatusNota.Fechada;
        await _context.SaveChangesAsync();

        return Ok(new { mensagem = $"Nota fiscal {nota.NumeroSequencial} fechada e estoque atualizado com sucesso!" });
    }
}