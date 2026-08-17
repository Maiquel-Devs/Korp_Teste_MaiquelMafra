using FaturamentoService.Api.Data;
using FaturamentoService.Api.DTOs;
using FaturamentoService.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FaturamentoService.Api.Controllers;

/// <summary>
/// Controlador responsável pela emissão, consulta e fechamento de Notas Fiscais com baixa de estoque integrada.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class NotasFiscaisController : ControllerBase
{
    private readonly FaturamentoDbContext _context;
    private readonly HttpClient _httpClient;

    public NotasFiscaisController(FaturamentoDbContext context, IHttpClientFactory httpClientFactory)
    {
        _context = context;
        _httpClient = httpClientFactory.CreateClient("EstoqueService");
    }

    /// <summary>
    /// Lista todas as notas fiscais cadastradas e seus respectivos itens.
    /// </summary>
    /// <returns>Lista completa de notas fiscais com detalhamento de itens.</returns>
    /// <response code="200">Lista retornada com sucesso.</response>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<NotaFiscal>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<NotaFiscal>>> GetNotas()
    {
        return await _context.NotasFiscais
            .Include(n => n.Itens)
            .AsNoTracking()
            .ToListAsync();
    }

    /// <summary>
    /// Obtém os detalhes de uma nota fiscal específica através do ID.
    /// </summary>
    /// <param name="id">Identificador único da Nota Fiscal.</param>
    /// <returns>Dados completos da nota e seus itens vinculados.</returns>
    /// <response code="200">Nota fiscal encontrada com sucesso.</response>
    /// <response code="404">Nota fiscal não encontrada para o ID informado.</response>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(NotaFiscal), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<NotaFiscal>> GetNota(int id)
    {
        var nota = await _context.NotasFiscais
            .Include(n => n.Itens)
            .FirstOrDefaultAsync(n => n.Id == id);

        if (nota == null)
            return NotFound(new { mensagem = $"Nota fiscal com ID {id} não encontrada." });

        return Ok(nota);
    }

    /// <summary>
    /// Cria e emite uma nova nota fiscal no status "Aberta".
    /// </summary>
    /// <param name="dto">Dados de criação da nota fiscal e itens associados.</param>
    /// <returns>A nota fiscal recém-criada com o ID gerado.</returns>
    /// <response code="201">Nota fiscal criada com sucesso.</response>
    /// <response code="400">Dados inválidos ou número sequencial já existente.</response>
    [HttpPost]
    [ProducesResponseType(typeof(NotaFiscal), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
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

    /// <summary>
    /// Realiza o fechamento da nota fiscal e efetua a baixa automática dos produtos no microsserviço de Estoque.
    /// </summary>
    /// <param name="id">Identificador único da Nota Fiscal a ser fechada.</param>
    /// <response code="200">Nota fechada com sucesso e estoque atualizado.</response>
    /// <response code="400">Nota já fechada, sem itens ou saldo insuficiente no Estoque.</response>
    /// <response code="404">Nota fiscal ou produto não encontrado.</response>
    /// <response code="503">Falha na comunicação com o microsserviço de Estoque.</response>
    [HttpPost("{id:int}/fechar")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
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