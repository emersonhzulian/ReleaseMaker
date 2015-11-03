using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GeradorPacote.Servicos;
using System.Xml;

namespace GeradorPacote.Entidades
{
    public class Projeto
    {
        /// <summary>
        /// GUID que identifica o projeto
        /// </summary>
        public string GUIDProjeto { get; set; }

        /// <summary>
        /// Nome da DLL por exemplo
        /// </summary>
        public string ArtefatoGerado { get; set; }

        /// <summary>
        /// Enumerator para separar EXE de WEB, pois terao logicas diferentes como por exemplo copiar o EXE
        /// </summary>
        public Constantes.TipoProjeto TipoProjeto;

        /// <summary>
        /// Todas as Referencias com outros Projetos que esse Projeto possui
        /// </summary>
        public List<string> ReferenciasProjeto { get; set; }

        /// <summary>
        /// Classes e itens que são compilados, ou seja, se essa lista estiver preenchida, é necessario enviar a DLL desse projeto para todos que fazem referencia a ele
        /// </summary>
        public List<string> ArtefatosCompiladosProjeto { get; set; }

        /// <summary>
        /// Arquivos como bibliotecas de terceiros importadas
        /// </summary>
        public List<string> ArtefatosNaoCompiladosImportadosProjeto { get; set; }

        /// <summary>
        /// Arquivos como XML/XSD/ETC
        /// </summary>
        public List<string> ArtefatosNaoCompiladosProjeto { get; set; }

        public string DiretorioProjeto { get; set; }

        public ConfiguracoesDeploy ConfiguracaoDeploy { get; set; }

        public Projeto()
        {
            this.ArtefatosCompiladosProjeto = new List<string>();
            this.ArtefatosNaoCompiladosImportadosProjeto = new List<string>();
            this.ArtefatosNaoCompiladosProjeto = new List<string>();
            this.ReferenciasProjeto = new List<string>();
        }
    }
}
