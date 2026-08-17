import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { MatTableModule } from '@angular/material/table';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatCardModule } from '@angular/material/card';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { NotaFiscalService } from '../../../services/nota-fiscal.service';
import { NotaFiscal } from '../../../models/nota-fiscal.model';

@Component({
  selector: 'app-nota-fiscal-list',
  standalone: true,
  imports: [
    CommonModule, 
    RouterLink, 
    MatTableModule, 
    MatButtonModule, 
    MatIconModule, 
    MatCardModule,
    MatProgressSpinnerModule
  ],
  templateUrl: './nota-fiscal-list.component.html',
  styleUrl: './nota-fiscal-list.component.css'
})
export class NotaFiscalListComponent implements OnInit {
  private notaService = inject(NotaFiscalService);

  notas = signal<NotaFiscal[]>([]);
  processandoId = signal<number | null>(null);
  colunasExibidas: string[] = ['id', 'numeroSequencial', 'dataCriacao', 'status', 'totalItens', 'acoes'];

  ngOnInit(): void {
    this.carregarNotas();
  }

  carregarNotas(): void {
    this.notaService.getNotas().subscribe({
      next: (dados) => this.notas.set(dados),
      error: () => {
        alert('Erro ao buscar a lista de notas fiscais.');
      }
    });
  }

  fecharNotaFiscal(id: number): void {
    if (confirm(`Deseja realmente fechar a Nota Fiscal ID ${id}? O estoque será atualizado.`)) {
      this.processandoId.set(id);

      this.notaService.fecharNota(id).subscribe({
        next: (res) => {
          this.processandoId.set(null);
          alert(res.mensagem || 'Nota fechada com sucesso!');
          this.carregarNotas();
        },
        error: (err) => {
          this.processandoId.set(null);
          let mensagemExibicao = 'Erro desconhecido ao fechar a nota.';

          if (err.error?.detalhes) {
            try {
              const detalhesObj = JSON.parse(err.error.detalhes);
              mensagemExibicao = detalhesObj.mensagem || err.error.detalhes;
            } catch {
              mensagemExibicao = err.error.detalhes;
            }
          } else if (err.error?.mensagem) {
            mensagemExibicao = err.error.mensagem;
          } else if (typeof err.error === 'string') {
            mensagemExibicao = err.error;
          } else if (err.message) {
            mensagemExibicao = err.message;
          }

          alert(`Atenção: ${mensagemExibicao}`);
        }
      });
    }
  }
}