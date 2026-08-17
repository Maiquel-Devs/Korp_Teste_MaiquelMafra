import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { MatTableModule } from '@angular/material/table';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatCardModule } from '@angular/material/card';
import { ProdutoService } from '../../../services/produto.service';
import { Produto } from '../../../models/produto.model';

@Component({
  selector: 'app-produto-list',
  standalone: true,
  imports: [
    CommonModule,
    RouterLink,
    MatTableModule,
    MatButtonModule,
    MatIconModule,
    MatCardModule
  ],
  templateUrl: './produto-list.component.html',
  styleUrl: './produto-list.component.css'
})
export class ProdutoListComponent implements OnInit {
  private produtoService = inject(ProdutoService);

  produtos = signal<Produto[]>([]);
  colunasExibidas: string[] = ['id', 'codigo', 'descricao', 'saldo', 'acoes'];

  ngOnInit(): void {
    this.carregarProdutos();
  }

  carregarProdutos(): void {
    this.produtoService.getProdutos().subscribe({
      next: (dados) => this.produtos.set(dados),
      error: () => alert('Erro ao buscar lista de produtos.')
    });
  }

  excluir(id: number): void {
    if (confirm(`Deseja realmente excluir o produto ID ${id}?`)) {
      this.produtoService.excluirProduto(id).subscribe({
        next: () => {
          this.carregarProdutos();
        },
        error: (err) => {
          alert('Erro ao excluir produto: ' + (err.error?.mensagem || err.message || 'Erro inesperado.'));
        }
      });
    }
  }
}