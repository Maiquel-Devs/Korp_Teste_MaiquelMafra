using EstoqueService.Api.Data;
using EstoqueService.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EstoqueService.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProdutosController : ControllerBase
{
    private readonly EstoqueDbContext _context;

    public ProdutosController(EstoqueDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Produto>>> GetProdutos()
    {
        return await _context.Produtos.AsNoTracking().ToListAsync();
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<Produto>> GetProduto(int id)
    {
        var produto = await _context.Produtos.FindAsync(id);
        if (produto == null)
            return NotFound(new { mensagem = $"Produto com ID {id} não encontrado." });

        return Ok(produto);
    }

    [HttpPost]
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

    [HttpPut("{id:int}")]
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

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteProduto(int id)
    {
        var produto = await _context.Produtos.FindAsync(id);
        if (produto == null)
            return NotFound(new { mensagem = $"Produto com ID {id} não encontrado." });

        _context.Produtos.Remove(produto);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpPost("baixar-estoque")]
    public async Task<IActionResult> BaixarEstoque([FromBody] List<BaixaEstoqueItemDto> itens)
    {
        if (itens == null || !itens.Any())
            return BadRequest(new { mensagem = "A lista de itens para baixa não pode ser vazia." });

        // Validação de saldo prévia
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

public class BaixaEstoqueItemDto
{
    public int ProdutoId { get; set; }
    public decimal Quantidade { get; set; }
}