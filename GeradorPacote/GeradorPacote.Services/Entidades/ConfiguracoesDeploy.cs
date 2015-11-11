using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GeradorPacote.Entidades
{
    public class ConfiguracoesDeploy
    {
        /// <summary>
        /// Nome da pasta que sera gerada para agrupar os arquivos
        /// </summary>
        public List<string> Servidor { get; set; }

        /// <summary>
        /// Diretorio dentro da pasta (Servidor) que sera gerada para agrupar os arquivos que não sejam binarios (exemplo .aspx, .asmx, etc)
        /// </summary>
        public List<string> Diretorio { get; set; }

        /// <summary>
        /// Diretorio dentro da pasta (Servidor) que sera gerada para agrupar os arquivos que sejam binarios (.dll) 
        /// </summary>
        public List<string> DiretorioBinario { get; set; }

        /// <summary>
        /// Flag que identifica se realmente precisa realizar o deploy desse projeto
        /// </summary>
        public bool RealizaDeploy { get; set; }

        public ConfiguracoesDeploy()
        {
            this.RealizaDeploy = false;
        }
    }
}
