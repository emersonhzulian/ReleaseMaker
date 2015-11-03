using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GeradorPacote.Servicos;
using System.IO;

namespace GeradorPacote
{
    class Program
    {
        static void Main(string[] args)
        {


            var caminhoSolution = args[0];
            var caminhoDeltaArtefatos = args[1];
            var caminhoPacote = args[2];
            var opcaoDeploy = args[3];



            var servico = Servicos.ServicoGeradorPacote.RecuperaInstancia;

            List<string> artefatos = Servicos.Servicos.Infra.BuscaArquivosDiretorio(caminhoDeltaArtefatos);
            
            servico.GerarPacote(caminhoSolution, caminhoDeltaArtefatos, artefatos, caminhoPacote, opcaoDeploy);
        }
    }
}
