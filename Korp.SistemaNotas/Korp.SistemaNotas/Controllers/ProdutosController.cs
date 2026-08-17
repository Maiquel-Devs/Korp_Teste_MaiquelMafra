using EstoqueService.Api.Data;
using EstoqueService.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EstoqueService.Api.Controllers;

/// <summary>
/// Controlador responsável pelo gerenciamento de produtos e movimentação de estoque.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class ProdutosController : ControllerBase
{
    private readonly EstoqueDbContext _context;

    public ProdutosController(EstoqueDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Obtém a listagem completa de produtos cadastrados.
    /// </summary>
    /// <returns>Lista com todos os produtos e seus respectivos saldos.</returns>
    /// <response code="200">Lista de produtos retornada com sucesso.</response>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<Produto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<Produto>>> GetProdutos()
    {
        return await _context.Produtos.AsNoTracking().ToListAsync();
    }

    /// <summary>
    /// Busca um produto específico pelo seu identificador (ID).
    /// </summary>
    /// <param name="id">Identificador único do produto.</param>
    /// <returns>Dados detalhados do produto consultado.</returns>
    /// <response code="200">Produto encontrado com sucesso.</response>
    /// <response code="404">Produto não encontrado para o ID informado.</response>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(Produto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Produto>> GetProduto(int id)
    {
        var produto = await _context.Produtos.FindAsync(id);
        if (produto == null)
            return NotFound(new { mensagem = $"Produto com ID {id} não encontrado." });

        return Ok(produto);
    }

    /// <summary>
    /// Cadastra um novo produto no estoque.
    /// </summary>
    /// <param name="produto">Objeto contendo os dados do novo produto (Código, Descrição, Saldo).</param>
    /// <returns>O produto cadastrado com seu ID gerado.</returns>
    /// <response code="201">Produto cadastrado com sucesso.</response>
    /// <response code="400">Saldo inicial negativo ou código de produto já existente.</response>
    [HttpPost]
    [ProducesResponseType(typeof(Produto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Produto>> CreateProduto([FromBody] Produto produto)
    {
        if (produto.Saldo < 0)
            return BadRequest(new { mensagem = "O saldo inicial não pode ser negativo." });

        var codigoExistente = await _context.Produtos.AnyAsync(p => p.Codigo == produto.Codigo);
        if (codigoExistente)
            return BadRequest(new { mensagem = $"Já existe um produto cadastrado com o código '{produto.Codigo}'." });

        _context.Produtos.Add(produto);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetProduto), new { id = produto.Id }, produto);
    }

    /// <summary>
    /// Atualiza os dados de um produto existente.
    /// </summary>
    /// <param name="id">Identificador único do produto na URL.</param>
    /// <param name="produtoAtualizado">Objeto contendo as novas informações do produto.</param>
    /// <response code="204">Produto atualizado com sucesso.</response>
    /// <response code="400">Divergência de IDs, saldo negativo ou código já em uso por outro produto.</response>
    /// <response code="404">Produto não encontrado.</response>
    [HttpPut("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateProduto(int id, [FromBody] Produto produtoAtualizado)
    {
        if (id != produtoAtualizado.Id)
            return BadRequest(new { mensagem = "O ID da URL não confere com o ID do corpo da requisição." });

        var produto = await _context.Produtos.FindAsync(id);
        if (produto == null)
            return NotFound(new { mensagem = $"Produto com ID {id} não encontrado." });

        var codigoExistente = await _context.Produtos.AnyAsync(p => p.Codigo == produtoAtualizado.Codigo && p.Id != id);
        if (codigoExistente)
            return BadRequest(new { mensagem = $"Já existe outro produto com o código '{produtoAtualizado.Codigo}'." });

        if (produtoAtualizado.Saldo < 0)
            return BadRequest(new { mensagem = "O saldo não pode ser negativo." });

        produto.Codigo = produtoAtualizado.Codigo;
        produto.Descricao = produtoAtualizado.Descricao;
        produto.Saldo = produtoAtualizado.Saldo;

        await _context.SaveChangesAsync();

        return NoContent();
    }

    /// <summary>
    /// Exclui um produto do estoque pelo seu ID.
    /// </summary>
    /// <param name="id">Identificador único do produto.</param>
    /// <response code="204">Produto removido com sucesso.</response>
    /// <response code="404">Produto não encontrado.</response>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteProduto(int id)
    {
        var produto = await _context.Produtos.FindAsync(id);
        if (produto == null)
            return NotFound(new { mensagem = $"Produto com ID {id} não encontrado." });

        _context.Produtos.Remove(produto);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    /// <summary>
    /// Realiza a baixa em lote de estoque para os itens informados (utilizado no fechamento de Nota Fiscal).
    /// </summary>
    /// <param name="itens">Lista contendo os IDs dos produtos e as quantidades a debitar.</param>
    /// <response code="200">Baixa executada com sucesso.</response>
    /// <response code="400">Lista vazia ou saldo insuficiente para um ou mais itens.</response>
    /// <response code="404">Um dos produtos informados não foi encontrado.</response>
    [HttpPost("baixar-estoque")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> BaixarEstoque([FromBody] List<BaixaEstoqueItemDto> itens)
    {
        if (itens == null || !itens.Any())
            return BadRequest(new { mensagem = "A lista de itens para baixa não pode ser vazia." });

        // Validação prévia de saldo
        foreach (var item in itens)
        {
            var produto = await _context.Produtos.FindAsync(item.ProdutoId);
            if (produto == null)
                return NotFound(new { mensagem = $"Produto com ID {item.ProdutoId} não foi encontrado no estoque." });

            if (produto.Saldo < item.Quantidade)
            {
                return BadRequest(new
                {
                    mensagem = $"Saldo insuficiente para o produto '{produto.Descricao}' (Código: {produto.Codigo}). Saldo atual: {produto.Saldo}, Quantidade solicitada: {item.Quantidade}."
                });
            }
        }

        // Execução da baixa
        foreach (var item in itens)
        {
            var produto = await _context.Produtos.FindAsync(item.ProdutoId);
            if (produto != null)
            {
                produto.Saldo -= item.Quantidade;
            }
        }

        await _context.SaveChangesAsync();
        return Ok(new { mensagem = "Baixa de estoque realizada com sucesso." });
    }
}

/// <summary>
/// Objeto de transferência de dados para requisição de baixa de estoque.
/// </summary>
public class BaixaEstoqueItemDto
{
    /// <summary>
    /// Identificador único do produto no estoque.
    /// </summary>
    /// <example>1</example>
    public int ProdutoId { get; set; }

    /// <summary>
    /// Quantidade a ser debitada do saldo em estoque.
    /// </summary>
    /// <example>2.5</example>
    public decimal Quantidade { get; set; }
}