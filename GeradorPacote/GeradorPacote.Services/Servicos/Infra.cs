using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GeradorPacote.Servicos.Servicos
{
    public static class Infra
    {
        public static string EncontraDiretorio(string diretorioProjeto, string diretorioReferencia)
        {
            string retorno;

            diretorioProjeto = Path.GetDirectoryName(diretorioProjeto);

            if (!Path.IsPathRooted(diretorioReferencia))
            {
                retorno = Path.Combine(diretorioProjeto, diretorioReferencia);
            }
            else
            {
                retorno = diretorioReferencia;
            }

            return Path.GetFullPath(retorno);
        }

        public static List<string> BuscaArquivosDiretorio(string diretorio)
        {
            List<string> retorno = new List<string>();

            retorno.AddRange(Directory.GetFiles(diretorio));

            foreach (var pastas in Directory.GetDirectories(diretorio))
            {
                retorno.AddRange(BuscaArquivosDiretorio(pastas));
            }

            return retorno;
        }

        public static string RemoveTagXmlTexto(string xmlTexto, string tag)
        {
            int inicioTag = xmlTexto.IndexOf(tag);

            int fimTag = xmlTexto.IndexOf("\"", inicioTag + tag.Length);

            return xmlTexto.Remove(inicioTag, (fimTag - inicioTag) + 1);
        }

    }
}
