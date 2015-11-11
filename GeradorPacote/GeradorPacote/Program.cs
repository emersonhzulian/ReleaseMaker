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
            var arquivoConfiguracao = args[4];
            var caminhoInicioDeltaSolution = args[5];


            caminhoDeltaArtefatos = @"D:\GeradorPacote\TesteMorpheus\delta\src\";
            caminhoSolution = @"D:\Projetos\MetLife\Morpheus\35.0\src\src\Morpheus\Morpheus.sln";

            var servico = Servicos.ServicoGeradorPacote.RecuperaInstancia;

            var artefatos = Servicos.Servicos.Infra.BuscaArquivosDiretorio(caminhoDeltaArtefatos);

            servico.GerarPacote(caminhoSolution, caminhoDeltaArtefatos, artefatos, caminhoPacote, opcaoDeploy, arquivoConfiguracao, caminhoInicioDeltaSolution);
        }
    }
}
