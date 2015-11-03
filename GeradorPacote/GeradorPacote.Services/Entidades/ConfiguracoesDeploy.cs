using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GeradorPacote.Entidades
{
    public class ConfiguracoesDeploy
    {
        public string Servidor { get; set; }
        public string Diretorio { get; set; }

        public string DiretorioBinario { get; set; }

        public bool RealizaDeploy { get; set; }

        public List<string> DiretoriosIgnorar { get; set; }

        public ConfiguracoesDeploy()
        {
            this.RealizaDeploy = false;
            this.DiretoriosIgnorar = new List<string>();
        }
    }
}
