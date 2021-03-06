using BoletoNet.EDI.Banco;
using BoletoNet.Excecoes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.UI;

[assembly: WebResource("BoletoNet.Imagens.136.jpg", "image/jpg")]
namespace BoletoNet
{
    /// <Author>
    /// Ivan Teles (ivan@idevweb.com.br)- Uniced
    /// </Author>
    internal class Banco_Unicred : AbstractBanco, IBanco
    {
        private HeaderRetorno header;

        /// <author>
        /// Classe responsavel em criar os campos do Banco Unicred.
        /// </author>
        internal Banco_Unicred()
        {
            this.Codigo = 136;
            this.Digito = "8";
            this.Nome = "Banco Unicred";
        }

        public override void ValidaBoleto(Boleto boleto)
        {
            //Formata o tamanho do nÃºmero da agÃªncia
            if (boleto.Cedente.ContaBancaria.Agencia.Length < 4)
                boleto.Cedente.ContaBancaria.Agencia = Utils.FormatCode(boleto.Cedente.ContaBancaria.Agencia, 4);

            //Formata o tamanho do nÃºmero da conta corrente
            if (boleto.Cedente.ContaBancaria.Conta.Length < 5)
                boleto.Cedente.ContaBancaria.Conta = Utils.FormatCode(boleto.Cedente.ContaBancaria.Conta, 5);

            //Atribui o nome do banco ao local de pagamento
            if (boleto.LocalPagamento == "AtÃ© o vencimento, preferencialmente no ")
                boleto.LocalPagamento += Nome;
            else 
                boleto.LocalPagamento = "Pagável em qualquer banco mesmo após o vencimento";

            //Verifica se data do processamento Ã© valida
            if (boleto.DataProcessamento == DateTime.MinValue) // diegomodolo (diego.ribeiro@nectarnet.com.br)
                boleto.DataProcessamento = DateTime.Now;

            //Verifica se data do documento Ã© valida
            if (boleto.DataDocumento == DateTime.MinValue) // diegomodolo (diego.ribeiro@nectarnet.com.br)
                boleto.DataDocumento = DateTime.Now;

            string infoFormatoCodigoCedente = "formato AAAAPPCCCCC, onde: AAAA = NÃºmero da agÃªncia, PP = Posto do beneficiÃ¡rio, CCCCC = CÃ³digo do beneficiÃ¡rio";

            var codigoCedente = Utils.FormatCode(boleto.Cedente.Codigo, 11);

            if (string.IsNullOrEmpty(codigoCedente))
                throw new BoletoNetException("CÃ³digo do cedente deve ser informado, " + infoFormatoCodigoCedente);

            var conta = boleto.Cedente.ContaBancaria.Conta;
            if (boleto.Cedente.ContaBancaria != null &&
                (!codigoCedente.StartsWith(boleto.Cedente.ContaBancaria.Agencia) ||
                 !(codigoCedente.EndsWith(conta) || codigoCedente.EndsWith(conta.Substring(0, conta.Length - 1)))))
                //throw new BoletoNetException("CÃ³digo do cedente deve estar no " + infoFormatoCodigoCedente);
                boleto.Cedente.Codigo = string.Format("{0}{1}{2}", boleto.Cedente.ContaBancaria.Agencia, boleto.Cedente.ContaBancaria.OperacaConta, boleto.Cedente.Codigo);


            //Verifica se o nosso nÃºmero Ã© vÃ¡lido
            var Length_NN = boleto.NossoNumero.Length;
            if (Length_NN > 11) throw new NotImplementedException("Nosso nÃºmero invÃ¡lido");

            FormataCodigoBarra(boleto);
            //if (boleto.CodigoBarra.Codigo.Length != 44)
            //    throw new BoletoNetException("CÃ³digo de barras Ã© invÃ¡lido");

            FormataLinhaDigitavel(boleto);
            FormataNossoNumero(boleto);
        }


        public override void FormataNossoNumero(Boleto boleto)
        {
            string nossoNumero = boleto.NossoNumero;

            if (nossoNumero == null || nossoNumero.Length != 10)
            {
                throw new Exception("Erro ao tentar formatar nosso nÃºmero, verifique o tamanho do campo: " + nossoNumero.Length);
            }

            try
            {
                boleto.NossoNumero = string.Format("{0}-{1}", nossoNumero, Mod11UniCred(nossoNumero));
            }
            catch (Exception ex)
            {
                throw new Exception("Erro ao formatar nosso nÃºmero", ex);
            }
        }

        protected static int Mod11UniCred(string seq)
        {
            /* Variáveis
             * -------------
             * d - Dígito
             * s - Soma
             * p - Peso
             * b - Base
             * r - Resto
             */
            int[] mult = new[] {3, 2, 9, 8, 7, 6, 5, 4, 3, 2};

            int d, s = 0, p = 2, b = 9, i = 0;

            foreach (char c in seq)
            {
                var mul = mult[i];
                s = s + (int.Parse(c.ToString()) * mul);
                i++;
            }

            d = 11 - (s % 11);
            if (d  == 0 || d== 10)
                d = 0;
            return d;
        }

        public override void FormataNumeroDocumento(Boleto boleto)
        {
            throw new BoletoNetException("NÃ£o implantado");
        }
        public override void FormataLinhaDigitavel(Boleto boleto)
        {
            //041M2.1AAAd1  CCCCC.CCNNNd2  NNNNN.041XXd3  V FFFF9999999999

            string campo1 = "1369" + boleto.CodigoBarra.Codigo.Substring(19, 5);
            int d1 = Mod10Unicred(campo1);
            campo1 = FormataCampoLD(campo1) + d1.ToString();

            string campo2 = boleto.CodigoBarra.Codigo.Substring(24, 10);
            int d2 = Mod10Unicred(campo2);
            campo2 = FormataCampoLD(campo2) + d2.ToString();

            string campo3 = boleto.CodigoBarra.Codigo.Substring(34, 10);
            int d3 = Mod10Unicred(campo3);
            campo3 = FormataCampoLD(campo3) + d3.ToString();

            string campo4 = boleto.CodigoBarra.Codigo.Substring(4, 1);

            string campo5 = boleto.CodigoBarra.Codigo.Substring(5, 14);

            boleto.CodigoBarra.LinhaDigitavel = campo1 + "  " + campo2 + "  " + campo3 + "  " + campo4 + "  " + campo5;
        }
        private string FormataCampoLD(string campo)
        {
            return string.Format("{0}.{1}", campo.Substring(0, 5), campo.Substring(5));
        }

        public override void FormataCodigoBarra(Boleto boleto)
        {
            string valorBoleto = boleto.ValorBoleto.ToString("f").Replace(",", "").Replace(".", "");
            valorBoleto = Utils.FormatCode(valorBoleto, 10);

            string cmp_livre = Utils.FormatCode(boleto.Cedente.ContaBancaria.Agencia, 4) +
                                                Utils.FormatCode(boleto.Cedente.ContaBancaria.Conta, 10) +
                                                Utils.FormatCode(boleto.NossoNumero, 9);

            string dv_cmpLivre = digUnicred(cmp_livre).ToString();

            var codigoTemp = GerarCodigoDeBarras(boleto, valorBoleto, cmp_livre, dv_cmpLivre);

            boleto.CodigoBarra.CampoLivre = cmp_livre;
            boleto.CodigoBarra.FatorVencimento = FatorVencimento(boleto);
            boleto.CodigoBarra.Moeda = 9;
            boleto.CodigoBarra.ValorDocumento = valorBoleto;

            int _dacBoleto = digUnicred(codigoTemp);

            if (_dacBoleto == 0 || _dacBoleto > 9)
                _dacBoleto = 1;

            boleto.CodigoBarra.Codigo = GerarCodigoDeBarras(boleto, valorBoleto, cmp_livre, dv_cmpLivre, _dacBoleto);
        }

        private string GerarCodigoDeBarras(Boleto boleto, string valorBoleto, string cmp_livre, string dv_cmpLivre, int? dv_geral = null)
        {
            return string.Format("{0}{1}{2}{3}{4}{5}{6}",
                Utils.FormatCode(Codigo.ToString(), 3),
                boleto.Moeda,
                dv_geral.HasValue ? dv_geral.Value.ToString() : string.Empty,
                FatorVencimento(boleto),
                valorBoleto,
                cmp_livre,
                dv_cmpLivre);
        }

        #region MÃ©todos de GeraÃ§Ã£o do Arquivo de Remessa
        public override string GerarDetalheRemessa(Boleto boleto, int numeroRegistro, TipoArquivo tipoArquivo)
        {
            throw new BoletoNetException("NÃ£o implantado");
        }
        public override string GerarHeaderRemessa(string numeroConvenio, Cedente cedente, TipoArquivo tipoArquivo, int numeroArquivoRemessa, Boleto boletos)
        {
            throw new BoletoNetException("NÃ£o implantado");
        }
        public string GerarDetalheRemessaCNAB240(Boleto boleto, int numeroRegistro, TipoArquivo tipoArquivo)
        {
            throw new BoletoNetException("NÃ£o implantado");
        }

        public override string GerarHeaderRemessa(Cedente cedente, TipoArquivo tipoArquivo, int numeroArquivoRemessa)
        {
            throw new BoletoNetException("NÃ£o implantado");
        }

        public override string GerarHeaderRemessa(string numeroConvenio, Cedente cedente, TipoArquivo tipoArquivo, int numeroArquivoRemessa)
        {
            throw new BoletoNetException("NÃ£o implantado");
        }

        private string GerarHeaderLoteRemessaCNAB240(Cedente cedente, int numeroArquivoRemessa)
        {
            try
            {
                return GerarHeaderRemessaCNAB240(cedente);
            }
            catch (Exception e)
            {
                throw new Exception("Erro ao gerar HEADER DO LOTE do arquivo de remessa.", e);
            }
        }

        public override string GerarHeaderLoteRemessa(string numeroConvenio, Cedente cedente, int numeroArquivoRemessa, TipoArquivo tipoArquivo)
        {
            throw new BoletoNetException("NÃ£o implantado");
        }

        public string GerarHeaderRemessaCNAB240(Cedente cedente)
        {
            throw new BoletoNetException("NÃ£o implantado");
        }

        public override string GerarTrailerRemessa(int numeroRegistro, TipoArquivo tipoArquivo, Cedente cedente, decimal vltitulostotal)
        {
            throw new BoletoNetException("NÃ£o implantado");
        }

        public string GerarTrailerRemessa240(int numeroRegistro)
        {
            throw new BoletoNetException("NÃ£o implantado");
        }

        #endregion

        public int Mod10Unicred(string seq)
        {
            /* VariÃ¡veis
             * -------------
             * d - DÃ­gito
             * s - Soma
             * p - Peso
             * b - Base
             * r - Resto
             */

            int d, s = 0, p = 2, b = 2, r;

            for (int i = seq.Length - 1; i >= 0; i--)
            {

                r = (Convert.ToInt32(seq.Substring(i, 1)) * p);
                if (r > 9)
                    r = SomaDezena(r);
                s = s + r;
                if (p < b)
                    p++;
                else
                    p--;
            }

            d = Multiplo10(s);
            return d;
        }

        public int SomaDezena(int dezena)
        {
            string d = dezena.ToString();
            int d1 = Convert.ToInt32(d.Substring(0, 1));
            int d2 = Convert.ToInt32(d.Substring(1));
            return d1 + d2;
        }

        public int digUnicred(string seq)
        {
            /* VariÃ¡veis
             * -------------
             * d - DÃ­gito
             * s - Soma
             * p - Peso
             * b - Base
             * r - Resto
             */

            int d, s = 0, p = 2, b = 9;

            for (int i = seq.Length - 1; i >= 0; i--)
            {
                s = s + (Convert.ToInt32(seq.Substring(i, 1)) * p);
                if (p < b)
                    p = p + 1;
                else
                    p = 2;
            }

            d = 11 - (s % 11);
            if (d > 9)
                d = 0;
            return d;
        }

        public string DigNossoNumero(Boleto boleto, bool arquivoRemessa = false)
        {
            //Adicionado por diego.dariolli pois ao gerar remessa o dÃ­gito saÃ­a errado pois faltava agÃªncia e posto no cÃ³digo do cedente
            string codigoCedente = ""; //cÃ³digo do beneficiÃ¡rio aaaappccccc
            if (arquivoRemessa)
            {
                if (string.IsNullOrEmpty(boleto.Cedente.ContaBancaria.OperacaConta))
                    throw new Exception("O cÃ³digo do posto beneficiÃ¡rio nÃ£o foi informado.");

                codigoCedente = string.Concat(boleto.Cedente.ContaBancaria.Agencia, boleto.Cedente.ContaBancaria.OperacaConta, boleto.Cedente.Codigo);
            }
            else
                codigoCedente = boleto.Cedente.Codigo;

            string nossoNumero = boleto.NossoNumero; //ano atual (yy), indicador de geraÃ§Ã£o do nosso nÃºmero (b) e o nÃºmero seqÃ¼encial do beneficiÃ¡rio (nnnnn);

            string seq = string.Concat(codigoCedente, nossoNumero); // = aaaappcccccyybnnnnn
            /* VariÃ¡veis
             * -------------
             * d - DÃ­gito
             * s - Soma
             * p - Peso
             * b - Base
             * r - Resto
             */

            int d, s = 0, p = 2, b = 9;
            //Atribui os pesos de {2..9}
            for (int i = seq.Length - 1; i >= 0; i--)
            {
                s = s + (Convert.ToInt32(seq.Substring(i, 1)) * p);
                if (p < b)
                    p = p + 1;
                else
                    p = 2;
            }
            d = 11 - (s % 11);//Calcula o MÃ³dulo 11;
            if (d > 9)
                d = 0;
            return d.ToString();
        }


        /// <summary>
        /// Efetua as ValidaÃ§Ãµes dentro da classe Boleto, para garantir a geraÃ§Ã£o da remessa
        /// </summary>
        public override bool ValidarRemessa(TipoArquivo tipoArquivo, string numeroConvenio, IBanco banco, Cedente cedente, Boletos boletos, int numeroArquivoRemessa, out string mensagem)
        {
            throw new BoletoNetException("NÃ£o implantado");
        }
    }
}
