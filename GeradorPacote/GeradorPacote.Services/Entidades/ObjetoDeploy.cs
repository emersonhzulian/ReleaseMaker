using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GeradorPacote.Entidades
{
    public class ObjetoDeploy
    {
        /// <summary>
        /// Arquivo que será coletado para montar o pacote
        /// </summary>
        public string Arquivo { get; set; }

        /// <summary>
        /// Flag que identifica se ele será colocado na pasta de binarios ou não
        /// </summary>
        public bool Binario { get; set; }

        public ObjetoDeploy()
        {
            this.Binario = false;
        }
    }
}
