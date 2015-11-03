using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GeradorPacote.Entidades
{
    public class ObjetoDeploy
    {
        public string Diretorio { get; set; }

        public bool Binario { get; set; }

        public ObjetoDeploy()
        {
            this.Binario = false;
        }
    }
}
