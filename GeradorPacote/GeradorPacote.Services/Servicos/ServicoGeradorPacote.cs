using GeradorPacote.Entidades;
using GeradorPacote.Servicos.Servicos;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;

namespace GeradorPacote.Servicos
{
    public class ServicoGeradorPacote
    {
        #region Singleton

        private ServicoGeradorPacote()
        {

        }

        private static ServicoGeradorPacote instancia;

        public static ServicoGeradorPacote RecuperaInstancia
        {
            get
            {
                if (instancia == null)
                    instancia = new ServicoGeradorPacote();
                return instancia;
            }
            set
            {
                instancia = value;
            }
        }

        #endregion

        private List<string> RecuperaProjetosSolution(string diretorioSolution)
        {
            string solution = File.ReadAllText(diretorioSolution);

            int posInicialDadosProjeto = 0;
            int posFinalDadosProjeto = 0;

            List<string> projetosSolution = new List<string>();

            do
            {
                posInicialDadosProjeto = solution.IndexOf("Project(", posInicialDadosProjeto);

                if (posInicialDadosProjeto != -1)
                {
                    posFinalDadosProjeto = solution.IndexOf("EndProject", posInicialDadosProjeto);

                    string dadosProjeto = solution.Substring(posInicialDadosProjeto, posFinalDadosProjeto - posInicialDadosProjeto);

                    if (posInicialDadosProjeto > -1 && posFinalDadosProjeto > -1)
                    {
                        posInicialDadosProjeto++;

                        //Pega apenas as linhas que representam um csproj, ignorando as de Folder
                        if (dadosProjeto.Split('"')[5].EndsWith(".csproj"))
                            projetosSolution.Add(dadosProjeto.Split('"')[5]);
                    }
                }

            } while (posInicialDadosProjeto > -1 && posFinalDadosProjeto > -1);

            return projetosSolution;
        }

        private Projeto RecuperaProjeto(string diretorioProjeto, string opcaoDeploy, string arquivoConfiguracoes)
        {
            string csproj = string.Empty;
            XmlDocument xmlCsproj = null;
            Projeto retorno = null;
            XmlNodeList auxNodes;
            XmlNode auxNode;

            csproj = File.ReadAllText(diretorioProjeto);
            csproj = Infra.RemoveTagXmlTexto(csproj, " xmlns=\"");

            xmlCsproj = new XmlDocument();
            xmlCsproj.LoadXml(csproj);

            retorno = new Projeto();

            auxNode = xmlCsproj.SelectSingleNode("/Project/PropertyGroup/AssemblyName");

            retorno.ArtefatoGerado = auxNode.InnerText;

            auxNode = xmlCsproj.SelectSingleNode("/Project/PropertyGroup/ProjectGuid");

            retorno.GUIDProjeto = auxNode.InnerText;

            auxNode = xmlCsproj.SelectSingleNode("/Project/PropertyGroup/OutputType");

            switch (auxNode.InnerText)
            {
                case "Exe":
                    retorno.TipoProjeto = Constantes.TipoProjeto.exe;
                    break;
                case "Library":
                    retorno.TipoProjeto = Constantes.TipoProjeto.dll;
                    break;
            }

            auxNodes = xmlCsproj.SelectNodes("/Project/ItemGroup/Compile");

            if (auxNodes.Count > 0)
            {
                foreach (XmlNode node in auxNodes)
                {
                    retorno.ArtefatosCompiladosProjeto.Add(Infra.EncontraDiretorio(diretorioProjeto, node.Attributes["Include"].Value));
                }
            }

            auxNodes = xmlCsproj.SelectNodes("/Project/ItemGroup/Reference/HintPath");

            if (auxNodes.Count > 0)
            {
                foreach (XmlNode node in auxNodes)
                {
                    retorno.ArtefatosNaoCompiladosImportadosProjeto.Add(Infra.EncontraDiretorio(diretorioProjeto, node.InnerText));
                }
            }

            auxNodes = xmlCsproj.SelectNodes("/Project/ItemGroup/Content");

            if (auxNodes.Count > 0)
            {
                foreach (XmlNode node in auxNodes)
                {
                    retorno.ArtefatosNaoCompiladosProjeto.Add(Infra.EncontraDiretorio(diretorioProjeto, node.Attributes["Include"].Value));
                }
            }

            auxNodes = xmlCsproj.SelectNodes("/Project/ItemGroup/None");

            if (auxNodes.Count > 0)
            {
                foreach (XmlNode node in auxNodes)
                {
                    if (node.HasChildNodes
                        && node.ChildNodes.Cast<XmlNode>().Where(x => x.Name == "SubType" && x.InnerText == "Designer").Any())
                    {
                        retorno.ArtefatosNaoCompiladosProjeto.Add(Infra.EncontraDiretorio(diretorioProjeto, node.Attributes["Include"].Value));
                    }
                }
            }

            auxNodes = xmlCsproj.SelectNodes("/Project/ItemGroup/ProjectReference/Project");

            if (auxNodes.Count > 0)
            {
                foreach (XmlNode node in auxNodes)
                {
                    retorno.ReferenciasProjeto.Add(node.InnerText.ToUpper());
                }
            }

            auxNodes = xmlCsproj.SelectNodes("/Project/PropertyGroup");

            var noTipoDeploy =
                auxNodes.Cast<XmlNode>()
                    .Where(x =>
                        x.ChildNodes.Cast<XmlNode>()
                        .Any(a => a.Name == "OutputPath"))
                    .Where(x =>
                        x.Attributes["Condition"].Value.Contains(opcaoDeploy))
                    .ToList()
                    .FirstOrDefault();

            if (noTipoDeploy != null)
            {
                XmlNode caminhoTipoDeploy = noTipoDeploy.ChildNodes.Cast<XmlNode>().Where(x => x.Name == "OutputPath").FirstOrDefault();

                retorno.ArtefatoGerado = Infra.EncontraDiretorio(diretorioProjeto, caminhoTipoDeploy.InnerText) + retorno.ArtefatoGerado + "." + retorno.TipoProjeto;
            }

            retorno.DiretorioProjeto = Path.GetDirectoryName(diretorioProjeto);

            retorno.ConfiguracaoDeploy = RecuperaConfiguracoesDeploy(retorno.DiretorioProjeto, retorno.DiretorioProjeto + Path.DirectorySeparatorChar + arquivoConfiguracoes);

            return retorno;
        }

        public void GerarPacote(string solution, string diretorioDeltaArtefatos, List<string> artefatosDelta, string diretorioPacote, string opcaoDeploy, string arquivoConfiguracao, string caminhoInicioDeltaSolution)
        {
            List<string> projetosSolution = this.RecuperaProjetosSolution(solution);

            string diretorioSolution = Path.GetDirectoryName(solution);

            List<Projeto> lstProjeto = new List<Projeto>();

            foreach (var caminhoProjeto in projetosSolution)
            {
                string caminhoRealProjeto = string.Empty;

                if (!Path.IsPathRooted(caminhoProjeto))
                {
                    caminhoRealProjeto = Infra.EncontraDiretorio(diretorioSolution + Path.DirectorySeparatorChar, caminhoProjeto);
                }

                lstProjeto.Add(this.RecuperaProjeto(caminhoRealProjeto, opcaoDeploy, arquivoConfiguracao));
            }

            var artefatosDeltaCaminhoCorrigido = artefatosDelta.Select(b => b.Replace(diretorioDeltaArtefatos, string.Empty)).ToList();

            //identifica projetos que precisar ter suas DLLs enviadas novamente
            var projetosAfetados =
                lstProjeto
                    .Where(x =>
                        x.ArtefatosCompiladosProjeto
                        .Select(y => y.Replace(caminhoInicioDeltaSolution, string.Empty))
                        .Where(a =>
                            artefatosDeltaCaminhoCorrigido.Contains(a))
                        .Any()
                        ||
                        x.ArtefatosNaoCompiladosImportadosProjeto
                        .Select(y => y.Replace(caminhoInicioDeltaSolution, string.Empty))
                        .Where(a =>
                            artefatosDeltaCaminhoCorrigido.Contains(a))
                        .Any()
                        ||
                        x.ArtefatosNaoCompiladosProjeto
                        .Select(y => y.Replace(caminhoInicioDeltaSolution, string.Empty))
                        .Where(a =>
                            artefatosDeltaCaminhoCorrigido.Contains(a))
                        .Any()
                    ).ToList();

            Dictionary<Projeto, List<ObjetoDeploy>> dicArtefatosNecessarios = new Dictionary<Projeto, List<ObjetoDeploy>>();

            RecuperaArtefatos(projetosAfetados, lstProjeto, artefatosDeltaCaminhoCorrigido, caminhoInicioDeltaSolution, ref dicArtefatosNecessarios);

            if (Directory.Exists(diretorioPacote))
            {
                Directory.Delete(diretorioPacote, true);
            }

            foreach (var artefatosNecessarios in dicArtefatosNecessarios)
            {
                for (int i = 0; i < artefatosNecessarios.Key.ConfiguracaoDeploy.Servidor.Count; i++)
                {
                    var diretorioPacoteProjeto = diretorioPacote + artefatosNecessarios.Key.ConfiguracaoDeploy.Servidor[i];

                    foreach (var artefato in artefatosNecessarios.Value)
                    {
                        if (artefato.Binario)
                        {
                            var diretorioDestino = diretorioPacoteProjeto + artefatosNecessarios.Key.ConfiguracaoDeploy.DiretorioBinario[i] + Path.GetFileName(artefato.Arquivo);

                            if (!Directory.Exists(Path.GetDirectoryName(diretorioDestino)))
                            {
                                Directory.CreateDirectory(Path.GetDirectoryName(diretorioDestino));
                            }

                            File.Copy(artefato.Arquivo, diretorioDestino, true);
                        }
                        else
                        {
                            var arquivo = string.Empty;

                            arquivo = artefato.Arquivo.Remove(artefato.Arquivo.IndexOf(artefatosNecessarios.Key.DiretorioProjeto, 0), artefatosNecessarios.Key.DiretorioProjeto.Length);

                            if (arquivo.StartsWith(@"\"))
                            {
                                arquivo = arquivo.Remove(0, 1);
                            }

                            var diretorioDestino = diretorioPacoteProjeto + artefatosNecessarios.Key.ConfiguracaoDeploy.Diretorio[i] + arquivo;

                            if (!Directory.Exists(Path.GetDirectoryName(diretorioDestino)))
                            {
                                Directory.CreateDirectory(Path.GetDirectoryName(diretorioDestino));
                            }

                            File.Copy(artefato.Arquivo, diretorioDestino, true);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Recupera os arquivos que deverão fazer parte do pacote
        /// </summary>
        /// <param name="projetosDeploy">Lista com os projetos da solution que precisam de deploy</param>
        /// <param name="projetosSolution">Lista com todos os projetos da solution</param>
        /// <param name="artefatosDelta">Lista com os artefato delta com seus caminhos iniciando em apartir da primeira pasta dentro delta</param>
        /// <param name="caminhoInicioDeltaSolution">Diretorio da Solution em que o delta se encontra</param>
        /// <param name="projetoArtefatos">Dicionario contendo como chave o projeto e como valor os objetos de deploy desse projeto</param>
        private void RecuperaArtefatos(List<Projeto> projetosDeploy, List<Projeto> projetosSolution, List<string> artefatosDelta, string caminhoInicioDeltaSolution, ref Dictionary<Projeto, List<ObjetoDeploy>> projetoArtefatos)
        {
            foreach (var projeto in projetosSolution.Where(x => x.ConfiguracaoDeploy.RealizaDeploy))
            {
                List<ObjetoDeploy> artefatosNecessarios = new List<ObjetoDeploy>();

                projetoArtefatos.Add(projeto, new List<ObjetoDeploy>());

                RecuperaArtefatosRecursivo(projetosSolution, projeto, artefatosDelta, caminhoInicioDeltaSolution, projetosDeploy, ref artefatosNecessarios);

                var artefatosNaoCompiladosAlterados = projeto.ArtefatosNaoCompiladosProjeto
                   .Where(a =>
                       artefatosDelta.Contains(a.Replace(caminhoInicioDeltaSolution, string.Empty)))
                   .Select(x => new ObjetoDeploy() { Arquivo = x, Binario = false });

                foreach (var item in artefatosNaoCompiladosAlterados.ToList())
                {
                    if (!projetoArtefatos[projeto].Where(x => x.Arquivo == item.Arquivo).Any())
                    {
                        projetoArtefatos[projeto].Add(item);
                    }
                }

                foreach (var item in artefatosNecessarios)
                {
                    if (!projetoArtefatos[projeto].Where(x => x.Arquivo == item.Arquivo).Any())
                    {
                        projetoArtefatos[projeto].Add(item);
                    }
                }
            }
        }

        /// <summary>
        /// Metedo que recursivamente recupera os artefatos que precisam constar no deploy
        /// </summary>
        /// <param name="projetosSolution">Lista com todos os projetos da solution</param>
        /// <param name="projetoAfetado">Projeto sendo analisado</param>
        /// <param name="artefatosDelta">Lista com os artefato delta com seus caminhos iniciando em apartir da primeira pasta dentro delta</param>
        /// <param name="caminhoInicioDeltaSolution">Diretorio da Solution em que o delta se encontra</param>
        /// <param name="projetosAfetados">Lista com os projetos afetados pelo delta</param>
        /// <param name="artefatosNecessarios">Lista de artefatos que precisam constar no deploy</param>
        private void RecuperaArtefatosRecursivo(List<Projeto> projetosSolution, Projeto projetoAfetado, List<string> artefatosDelta, string caminhoInicioDeltaSolution, List<Projeto> projetosAfetados, ref List<ObjetoDeploy> artefatosNecessarios)
        {
            if (projetosAfetados.Contains(projetoAfetado))
            {
                artefatosNecessarios.Add(new ObjetoDeploy() { Arquivo = projetoAfetado.ArtefatoGerado, Binario = true });

                artefatosNecessarios.AddRange(projetoAfetado.ArtefatosNaoCompiladosImportadosProjeto
                        .Where(a =>
                            artefatosDelta
                        .Contains(a.Replace(caminhoInicioDeltaSolution, string.Empty)))
                        .Select(x => new ObjetoDeploy() { Arquivo = x, Binario = true }));
            }

            var projetosFilhos = projetosSolution.Where(x => projetoAfetado.ReferenciasProjeto.Contains(x.GUIDProjeto));

            foreach (var projetoFilho in projetosFilhos)
            {
                RecuperaArtefatosRecursivo(projetosSolution, projetoFilho, artefatosDelta, caminhoInicioDeltaSolution, projetosAfetados, ref artefatosNecessarios);
            }
        }

        private ConfiguracoesDeploy RecuperaConfiguracoesDeploy(string diretorioProjeto, string diretorioConfiguracao)
        {
            var retorno = new ConfiguracoesDeploy();

            if (File.Exists(diretorioConfiguracao))
            {
                var arquivo = File.ReadAllLines(diretorioConfiguracao).ToList();

                arquivo.RemoveAll(x => x.Trim() == string.Empty || x.TrimStart().StartsWith("#"));

                retorno.Servidor = RecuperaConfiguracaoDeLinhas(arquivo, "servidor");

                retorno.Diretorio = RecuperaConfiguracaoDeLinhas(arquivo, "diretorio");

                retorno.DiretorioBinario = RecuperaConfiguracaoDeLinhas(arquivo, "diretorioBinario");

                retorno.RealizaDeploy = true;
            }

            return retorno;
        }

        private List<string> RecuperaConfiguracaoDeLinhas(List<string> linhas, string configuracao)
        {
            List<string> retorno = new List<string>();

            List<string> aux = linhas.Where(x => x.StartsWith(configuracao + "=")).ToList();

            foreach (var a in aux)
            {
                var valor = a.Split('=').ToList().Last().Trim();

                retorno.Add(valor);
            }

            return retorno;
        }
    }
}
