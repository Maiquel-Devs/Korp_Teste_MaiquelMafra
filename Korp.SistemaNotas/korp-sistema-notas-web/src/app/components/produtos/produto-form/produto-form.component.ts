import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { ProdutoService } from '../../../services/produto.service';

@Component({
  selector: 'app-produto-form',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    RouterLink,
    MatCardModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatIconModule
  ],
  templateUrl: './produto-form.component.html',
  styleUrl: './produto-form.component.css'
})
export class ProdutoFormComponent implements OnInit {
  private fb = inject(FormBuilder);
  private produtoService = inject(ProdutoService);
  private router = inject(Router);
  private route = inject(ActivatedRoute);

  form!: FormGroup;
  idProduto?: number;
  isEdicao = false;

  ngOnInit(): void {
    this.form = this.fb.group({
      codigo: ['', [Validators.required, Validators.maxLength(50)]],
      descricao: ['', [Validators.required, Validators.maxLength(200)]],
      saldo: [0, [Validators.required, Validators.min(0)]]
    });

    const idParam = this.route.snapshot.paramMap.get('id');
    if (idParam) {
      this.idProduto = Number(idParam);
      this.isEdicao = true;
      this.produtoService.getProdutoById(this.idProduto).subscribe({
        next: (p) => this.form.patchValue(p),
        error: (err) => console.error('Erro ao carregar produto:', err)
      });
    }
  }

  salvar(): void {
    if (this.form.invalid) return;

    const produtoData = this.form.value;

    if (this.isEdicao && this.idProduto) {
      this.produtoService.atualizarProduto(this.idProduto, { id: this.idProduto, ...produtoData }).subscribe({
        next: () => this.router.navigate(['/produtos']),
        error: (err) => alert('Erro ao atualizar produto: ' + (err.error?.mensagem || err.message))
      });
    } else {
      this.produtoService.criarProduto(produtoData).subscribe({
        next: () => this.router.navigate(['/produtos']),
        error: (err) => alert('Erro ao cadastrar produto: ' + (err.error?.mensagem || err.message))
      });
    }
  }
}