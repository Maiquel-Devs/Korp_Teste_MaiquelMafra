export interface NotaFiscalItem {
  id?: number;
  notaFiscalId?: number;
  produtoId: number;
  quantidade: number;
  produtoCodigo?: string;
  produtoDescricao?: string;
}

export interface NotaFiscal {
  id?: number;
  numeroSequencial: number;
  status: number; // 1 = Aberta, 2 = Fechada
  dataCriacao: string;
  itens: NotaFiscalItem[];
}

export interface CriarNotaFiscalDto {
  numeroSequencial: number;
  itens: {
    produtoId: number;
    quantidade: number;
  }[];
}