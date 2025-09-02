using Dominio.Entities;

namespace Dominio.Interfaces
{
    public interface ICourseRepository
    {
        bool Exists(string link, out string cursoTitulo);
        void Save(Curso curso);
    }
}
