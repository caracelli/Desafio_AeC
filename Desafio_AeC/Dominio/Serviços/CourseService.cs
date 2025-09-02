using Dominio.Interfaces;
using Dominio.Entities;

namespace Dominio.serviços
{
    public class CourseService
    {
        private readonly ICourseRepository _courseRepository;
        private readonly ICourseScraper _courseScraper;

        public CourseService(ICourseRepository courseRepository, ICourseScraper courseScraper)
        {
            _courseRepository = courseRepository;
            _courseScraper = courseScraper;
        }

        public void ProcessCourses(string searchTerm)
        {
            var courses = _courseScraper.ScrapeCourses(searchTerm);

            foreach (var course in courses)
            {
                string cursoTitulo;
                if (!_courseRepository.Exists(course.Link, out cursoTitulo)) 
                {
                    _courseRepository.Save(course);// registra o curso na base de dados caso o mesmo não exista
                }
                else
                {
                    // Informa que o curso já existe na base de dados
                    Console.WriteLine($"Curso já existente: {cursoTitulo}");
                }
            }
        }

    }
}
