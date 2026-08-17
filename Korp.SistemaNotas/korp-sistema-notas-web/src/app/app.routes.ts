import { Routes } from '@angular/router';
import { ProdutoListComponent } from './components/produtos/produto-list/produto-list.component';
import { ProdutoFormComponent } from './components/produtos/produto-form/produto-form.component';
import { NotaFiscalListComponent } from './components/notas-fiscais/nota-fiscal-list/nota-fiscal-list.component';
import { NotaFiscalFormComponent } from './components/notas-fiscais/nota-fiscal-form/nota-fiscal-form.component';

export const routes: Routes = [
  { path: '', redirectTo: 'produtos', pathMatch: 'full' },
  { path: 'produtos', component: ProdutoListComponent },
  { path: 'produtos/novo', component: ProdutoFormComponent },
  { path: 'produtos/editar/:id', component: ProdutoFormComponent },
  { path: 'notas-fiscais', component: NotaFiscalListComponent },
  { path: 'notas-fiscais/nova', component: NotaFiscalFormComponent }
];