import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormArray, FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { ProdutoService } from '../../../services/produto.service';
import { NotaFiscalService } from '../../../services/nota-fiscal.service';
import { Produto } from '../../../models/produto.model';

@Component({
  selector: 'app-nota-fiscal-form',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    RouterLink,
    MatCardModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatButtonModule,
    MatIconModule
  ],
  templateUrl: './nota-fiscal-form.component.html',
  styleUrl: './nota-fiscal-form.component.css'
})
export class NotaFiscalFormComponent implements OnInit {
  private fb = inject(FormBuilder);
  private produtoService = inject(ProdutoService);
  private notaService = inject(NotaFiscalService);
  private router = inject(Router);

  form!: FormGroup;
  produtos: Produto[] = [];

  // Getter tipado para manipulação dinâmica das linhas de itens na tabela do formulário
  get itens(): FormArray {
    return this.form.get('itens') as FormArray;
  }

  ngOnInit(): void {
    this.form = this.fb.group({
      numeroSequencial: [null, [Validators.required, Validators.min(1)]],
      itens: this.fb.array([])
    });

    this.carregarProdutos();
    this.adicionarItem();   // inicia com um item vazio
  }

  carregarProdutos(): void {
    this.produtoService.getProdutos().subscribe({
      next: (res) => this.produtos = res,
      error: (err) => console.error('Erro ao buscar produtos:', err)
    });
  }

  criarItemFormGroup(): FormGroup {
    return this.fb.group({
      produtoId: [null, Validators.required],
      quantidade: [1, [Validators.required, Validators.min(1)]]
    });
  }

  adicionarItem(): void {
    this.itens.push(this.criarItemFormGroup());
  }

  removerItem(index: number): void {
    if (this.itens.length > 1) {
      this.itens.removeAt(index);
    }
  }

  salvar(): void {
    if (this.form.invalid) return;

    this.notaService.criarNota(this.form.value).subscribe({
      next: () => this.router.navigate(['/notas-fiscais']),
      error: (err) => alert('Erro ao criar nota fiscal: ' + (err.error?.mensagem || err.message))
    });
  }
}