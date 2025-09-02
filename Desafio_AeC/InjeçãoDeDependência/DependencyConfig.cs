using Microsoft.Extensions.DependencyInjection;
using RPA.Scraper; 
using Dominio.Interfaces;
using Infra; // Namespace onde está a implementação de LogService
using Infraestrutura.Repositorios; // Repositórios
using OpenQA.Selenium; // Selenium
using OpenQA.Selenium.Chrome;
using Dominio.serviços;

namespace InjeçãoDeDependência
{
    public static class DependencyConfig
    {
        public static IServiceProvider ConfigureServices(IServiceCollection services)
        {
            // Registra as dependências
            services.AddSingleton<IWebDriver, ChromeDriver>();
            services.AddSingleton<ICourseScraper, SeleniumCourseScraper>();  // Registra o Scraper
            services.AddSingleton<ICourseRepository, DatabaseCourseRepository>(); // Registra o repositório
            services.AddSingleton<ILogService, LogService>(); // Registra o serviço de log
            services.AddSingleton<CourseService>(); // Registra o serviço de curso

            // Retorna o provedor de serviços
            return services.BuildServiceProvider();
        }
    }
}
