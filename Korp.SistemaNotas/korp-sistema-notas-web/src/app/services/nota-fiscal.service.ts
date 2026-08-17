import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { NotaFiscal, CriarNotaFiscalDto } from '../models/nota-fiscal.model';
import { environment } from '../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class NotaFiscalService {
  private http = inject(HttpClient);
  private apiUrl = `${environment.faturamentoApiUrl}/notasfiscais`;

  getNotas(): Observable<NotaFiscal[]> {
    return this.http.get<NotaFiscal[]>(this.apiUrl);
  }

  getNotaById(id: number): Observable<NotaFiscal> {
    return this.http.get<NotaFiscal>(`${this.apiUrl}/${id}`);
  }

  criarNota(dto: CriarNotaFiscalDto): Observable<NotaFiscal> {
    return this.http.post<NotaFiscal>(this.apiUrl, dto);
  }

  fecharNota(id: number): Observable<{ mensagem: string }> {
    return this.http.post<{ mensagem: string }>(`${this.apiUrl}/${id}/fechar`, {});
  }
}