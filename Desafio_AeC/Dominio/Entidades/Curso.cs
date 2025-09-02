namespace Dominio.Entities
{
    public class Curso
    {
        public string Link { get; private set; }
        public string Titulo { get; private set; }
        public string Professor { get; private set; }
        public string Duracao { get; private set; }
        public string Descricao { get; private set; }

        public Curso(string link, string titulo, string professor, string duracao, string descricao)
        {
            Link = link;
            Titulo = titulo;
            Professor = professor;
            Duracao = duracao;
            Descricao = descricao;
        }
    }
}
