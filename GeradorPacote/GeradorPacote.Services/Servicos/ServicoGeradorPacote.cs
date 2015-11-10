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

                        if (dadosProjeto.Split('"')[5].EndsWith(".csproj"))
                            projetosSolution.Add(dadosProjeto.Split('"')[5]);
                    }
                }

            } while (posInicialDadosProjeto > -1 && posFinalDadosProjeto > -1);

            return projetosSolution;
        }

        private Projeto RecuperaProjeto(string diretorioProjeto, string opcaoDeploy, string arquivoConfiguracoes)
        {
            var csproj = File.ReadAllText(diretorioProjeto);

            csproj = Infra.RemoveTagXmlTexto(csproj, " xmlns=\"");

            var xmlCsproj = new XmlDocument();

            xmlCsproj.LoadXml(csproj);

            Projeto retorno = new Projeto();

            XmlNodeList auxNodes;
            XmlNode auxNode;

            auxNode = xmlCsproj.SelectSingleNode("/Project/PropertyGroup/AssemblyName");

            retorno.ArtefatoGerado = auxNode.InnerText;

            auxNode = xmlCsproj.SelectSingleNode("/Project/PropertyGroup/ProjectGuid");

            retorno.GUIDProjeto = auxNode.InnerText;

            auxNode = xmlCsproj.SelectSingleNode("/Project/PropertyGroup/OutputType");

            //Recupera tipo de BuildUtilizado para buscar o caminho que estao as dll
            //csproj.SelectSingleNode("/Project/PropertyGroup/Configuration").InnerXml

            switch (auxNode.InnerText)
            {
                case "Exe":
                    retorno.TipoProjeto = Constantes.TipoProjeto.EXE;
                    break;
                case "Library":
                    retorno.TipoProjeto = Constantes.TipoProjeto.DLL;
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
                var caminhoTipoDeploy = noTipoDeploy.ChildNodes.Cast<XmlNode>().Where(x => x.Name == "OutputPath").FirstOrDefault();

                retorno.ArtefatoGerado = Infra.EncontraDiretorio(diretorioProjeto, caminhoTipoDeploy.InnerText) + retorno.ArtefatoGerado + "." + retorno.TipoProjeto;
            }

            retorno.DiretorioProjeto = Path.GetDirectoryName(diretorioProjeto);

            retorno.ConfiguracaoDeploy = RecuperaConfiguracoesDeploy(retorno.DiretorioProjeto, retorno.DiretorioProjeto + Path.DirectorySeparatorChar + arquivoConfiguracoes);

            foreach (var a in retorno.ConfiguracaoDeploy.DiretoriosIgnorar)
            {
                retorno.ArtefatosNaoCompiladosImportadosProjeto.RemoveAll(x => Path.GetDirectoryName(x).Contains(a));
            }

            return retorno;
        }

        public void GerarPacote(string solution, string diretorioDeltaArtefatos, List<string> artefatosDelta, string diretorioPacote, string opcaoDeploy, string arquivoConfiguracao)
        {
            List<string> projetosSolution = this.RecuperaProjetosSolution(solution);

            string diretorioSolution = Path.GetDirectoryName(solution);

            List<Projeto> lstProjeto = new List<Projeto>();

            foreach (var caminhoProjeto in projetosSolution)
            {
                string caminhoRealProjeto = string.Empty;

                if (!Path.IsPathRooted(caminhoProjeto))
                {
                    caminhoRealProjeto = diretorioSolution + Path.DirectorySeparatorChar;
                }

                caminhoRealProjeto += caminhoProjeto;

                lstProjeto.Add(this.RecuperaProjeto(caminhoRealProjeto, opcaoDeploy, arquivoConfiguracao));
            }


            var artefatosDeltaCaminhoCorrigido = artefatosDelta.Select(b => b.Replace(diretorioDeltaArtefatos, string.Empty));

            //identifica projetos que precisar ter suas DLLs enviadas novamente
            var projetosAfetados =
                lstProjeto
                    .Where(x =>
                        x.ArtefatosCompiladosProjeto
                        .Select(y => y.Replace(diretorioSolution, string.Empty))
                        .Where(a =>
                            artefatosDeltaCaminhoCorrigido.Contains(a))
                        .Any()
                        ||
                        x.ArtefatosNaoCompiladosImportadosProjeto
                        .Select(y => y.Replace(diretorioSolution, string.Empty))
                        .Where(a =>
                            artefatosDeltaCaminhoCorrigido.Contains(a))
                        .Any()
                        ||
                        x.ArtefatosNaoCompiladosProjeto
                        .Select(y => y.Replace(diretorioSolution, string.Empty))
                        .Where(a =>
                            artefatosDeltaCaminhoCorrigido.Contains(a))
                        .Any()
                    ).ToList();


            Dictionary<Projeto, List<ObjetoDeploy>> dicArtefatosNecessarios = new Dictionary<Projeto, List<ObjetoDeploy>>();

            //foreach (var a in projetosAfetados)
            //{
            //    List<ObjetoDeploy> artefatosNecessarios = new List<ObjetoDeploy>();
            //    RecuperaArtefatosRecursivo(lstProjeto, a, artefatosDelta, diretorioSolution, diretorioDeltaArtefatos, ref dicArtefatosNecessarios, ref artefatosNecessarios);
            //}

            foreach (var a in projetosAfetados)
            {
                List<ObjetoDeploy> artefatosNecessarios = new List<ObjetoDeploy>();
                RecuperaArtefatosRecursivo(lstProjeto, a, artefatosDelta, diretorioSolution, diretorioDeltaArtefatos, projetosAfetados, ref dicArtefatosNecessarios, ref artefatosNecessarios);
            }

            dicArtefatosNecessarios = dicArtefatosNecessarios.Where(x => x.Key.ConfiguracaoDeploy.RealizaDeploy).ToDictionary(x => x.Key, x => x.Value);

            foreach (var a in dicArtefatosNecessarios)
            {
                var diretorioPacoteProjeto = diretorioPacote + a.Key.ConfiguracaoDeploy.Servidor;

                foreach (var b in a.Value)
                {
                    if (b.Binario)
                    {
                        var diretorioDestino = diretorioPacoteProjeto + a.Key.ConfiguracaoDeploy.DiretorioBinario + Path.GetFileName(b.Diretorio);

                        if (!Directory.Exists(Path.GetDirectoryName(diretorioDestino)))
                        {
                            Directory.CreateDirectory(Path.GetDirectoryName(diretorioDestino));
                        }

                        File.Copy(b.Diretorio, diretorioDestino, true);
                    }
                    else
                    {

                        var teste = string.Empty;

                        teste = b.Diretorio.Remove(b.Diretorio.IndexOf(a.Key.DiretorioProjeto, 0), a.Key.DiretorioProjeto.Length);

                        if (teste.StartsWith(@"\"))
                        {
                            teste = teste.Remove(0, 1);
                        }

                        var diretorioDestino = diretorioPacoteProjeto + a.Key.ConfiguracaoDeploy.Diretorio + teste;

                        if (!Directory.Exists(Path.GetDirectoryName(diretorioDestino)))
                        {
                            Directory.CreateDirectory(Path.GetDirectoryName(diretorioDestino));
                        }

                        File.Copy(b.Diretorio, diretorioDestino, true);
                    }
                }
            }
        }

        private void RecuperaArtefatosRecursivo(List<Projeto> projetosSolution, Projeto projetoAfetado, List<string> artefatosDelta, string diretorioSolution, string diretorioDeltaArtefatos, List<Projeto> projetosAfetados, ref Dictionary<Projeto, List<ObjetoDeploy>> projetoArtefatos, ref List<ObjetoDeploy> artefatosNecessarios)
        {
            if (projetosAfetados.Contains(projetoAfetado))
            {
                artefatosNecessarios.Add(new ObjetoDeploy() { Diretorio = projetoAfetado.ArtefatoGerado, Binario = true });

                artefatosNecessarios.AddRange(projetoAfetado.ArtefatosNaoCompiladosImportadosProjeto
                        .Where(a =>
                            artefatosDelta
                                .Select(b => b.Replace(diretorioDeltaArtefatos, string.Empty))
                        .Contains(a.Replace(diretorioSolution, string.Empty)))
                        .Select(x => new ObjetoDeploy() { Diretorio = x, Binario = true }));
            }

            var projetosPai = projetosSolution.Where(x => x.ReferenciasProjeto.Contains(projetoAfetado.GUIDProjeto));

            foreach (var projetoPai in projetosPai)
            {
                RecuperaArtefatosRecursivo(projetosSolution, projetoPai, artefatosDelta, diretorioSolution, diretorioDeltaArtefatos, projetosAfetados, ref projetoArtefatos, ref artefatosNecessarios);
            }

            if (projetoAfetado.ConfiguracaoDeploy.RealizaDeploy)
            {
                if (!projetoArtefatos.Keys.Contains(projetoAfetado))
                {
                    projetoArtefatos.Add(projetoAfetado, new List<ObjetoDeploy>());
                    projetoArtefatos[projetoAfetado].Add(new ObjetoDeploy() { Diretorio = projetoAfetado.ArtefatoGerado, Binario = true });
                }

                var artefatosNaoCompiladosAlterados = projetoAfetado.ArtefatosNaoCompiladosProjeto
                   .Where(a =>
                       artefatosDelta
                           .Select(b => b.Replace(diretorioDeltaArtefatos, string.Empty))
                   .Contains(a.Replace(diretorioSolution, string.Empty)))
                   .Select(x => new ObjetoDeploy() { Diretorio = x, Binario = false });

                foreach (var item in artefatosNaoCompiladosAlterados)
                {
                    if (!projetoArtefatos[projetoAfetado].Where(x => x.Diretorio == item.Diretorio).Any())
                    {
                        projetoArtefatos[projetoAfetado].Add(item);
                    }
                }

                foreach (var item in artefatosNecessarios)
                {
                    if (!projetoArtefatos[projetoAfetado].Where(x => x.Diretorio == item.Diretorio).Any())
                    {
                        projetoArtefatos[projetoAfetado].Add(item);
                    }
                }
            }
        }

        private ConfiguracoesDeploy RecuperaConfiguracoesDeploy(string diretorioProjeto, string diretorioConfiguracao)
        {
            var retorno = new ConfiguracoesDeploy();

            if (File.Exists(diretorioConfiguracao))
            {
                var arquivo = File.ReadAllLines(diretorioConfiguracao).ToList();

                arquivo.RemoveAll(x => x.Trim() == string.Empty || x.TrimStart().StartsWith("#"));

                retorno.Diretorio = RecuperaConfiguracaoDeLinhas(arquivo, "diretorio").Last();
                retorno.Servidor = RecuperaConfiguracaoDeLinhas(arquivo, "servidor").Last();
                retorno.DiretorioBinario = RecuperaConfiguracaoDeLinhas(arquivo, "diretorioBinario").Last();

                var a = RecuperaConfiguracaoDeLinhas(arquivo, "diretoriosIgnorar");

                foreach (var b in a)
                {
                    retorno.DiretoriosIgnorar.Add(Infra.EncontraDiretorio(diretorioProjeto + Path.DirectorySeparatorChar, b));
                }

                retorno.RealizaDeploy = true;
            }

            return retorno;
        }

        private List<string> RecuperaConfiguracaoDeLinhas(List<string> linhas, string configuracao)
        {
            List<string> retorno = new List<string>();

            var aux = linhas.FirstOrDefault(x => x.StartsWith(configuracao + "="));

            if (aux != null)
            {
                var auxb = aux.Split('=').ToList().Last();

                foreach (var a in auxb.Split(' '))
                {
                    retorno.Add(a);
                }
            }

            return retorno;
        }
    }
}
