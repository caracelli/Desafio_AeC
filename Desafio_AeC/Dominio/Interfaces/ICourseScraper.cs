using Dominio.Entities;

namespace Dominio.Interfaces
{
    public interface ICourseScraper
    {
        List<Curso> ScrapeCourses(string searchTerm);
    }
}
