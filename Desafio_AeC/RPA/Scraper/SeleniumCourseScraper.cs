using Dominio.Interfaces;
using Dominio.Entities;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace RPA.Scraper
{
    public class SeleniumCourseScraper : ICourseScraper
    {
        private readonly IWebDriver _driver;
        private readonly ICourseRepository _courseRepository;
        private readonly ILogService _logService;
        private readonly WebDriverWait _wait;

        public SeleniumCourseScraper(IWebDriver driver, ICourseRepository courseRepository, ILogService logService)
        {
            _driver = driver;
            _courseRepository = courseRepository;
            _logService = logService;
            _wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(10));
        }

        public List<Curso> ScrapeCourses(string searchTerm)
        {
            var allCourseLinks = new List<string>();
            var courses = new List<Curso>();
            string encodedSearchTerm = Uri.EscapeDataString(searchTerm);
            string baseUrl = "https://www.alura.com.br/busca?pagina=";

            try
            {
                // Navegar para a página inicial
                _driver.Navigate().GoToUrl($"https://www.alura.com.br/busca?query={encodedSearchTerm}&typeFilters=COURSE");

                // Obter o número máximo de páginas
                int maxPages = GetMaxPages();
                _logService.LogEvent("INFO", $"Número total de páginas: {maxPages}", "SUCESSO", DateTime.Now);

                // Coleta todos os links de cursos
                for (int pageNum = 1; pageNum <= maxPages; pageNum++)
                {
                    string currentPageUrl = $"{baseUrl}{pageNum}&query={encodedSearchTerm}&typeFilters=COURSE";
                    _driver.Navigate().GoToUrl(currentPageUrl);

                    _wait.Until(driver => driver.FindElements(By.ClassName("busca-resultado-link")).Any());

                    var courseLinks = _driver.FindElements(By.ClassName("busca-resultado-link"))
                                             .Select(course => course.GetDomAttribute("href"))
                                             .Where(link => !string.IsNullOrEmpty(link))
                                             .ToList();

                    allCourseLinks.AddRange(courseLinks);
                    _logService.LogEvent("INFO", $"Página {pageNum} processada. Cursos coletados: {courseLinks.Count}", "SUCESSO", DateTime.Now);
                }

                // Remover duplicatas
                allCourseLinks = allCourseLinks.Distinct().ToList();
                _logService.LogEvent("INFO", $"Total de Cursos coletados: {allCourseLinks.Count}", "SUCESSO", DateTime.Now);

                // Processar cada curso
                foreach (var link in allCourseLinks)
                {
                    try
                    {
                        // Verifica se o curso já existe na base usando o link como chave
                        string cursoTitulo; // Declaração da variável para armazenar o título do curso
                        if (_courseRepository.Exists(link, out cursoTitulo)) // Passando ambos os parâmetros
                        {
                            _logService.LogEvent("AVISO", $"Curso [{cursoTitulo}] já existente na base", "SUCESSO", DateTime.Now);
                            continue; // Pula para o próximo link se o curso já existe
                        }

                        // Variáveis para armazenar os dados do curso
                        string title = "Não Informado";
                        string professor = "Não Informado";
                        string duration = "Não Informado";
                        string description = "Não Informado";

                        int maxRetries = 3; // Número máximo de tentativas
                        int attempt = 0;    // Contador de tentativas
                        bool success = false;

                        while (attempt < maxRetries && !success)
                        {
                            attempt++;
                            try
                            {
                                // Navega até o curso somente se ele não existir na base
                                _driver.Navigate().GoToUrl(link);
                                _wait.Until(driver => driver.FindElement(By.XPath("/html/body/section[1]/div/div[1]/h1")));

                                // Extração de dados do curso
                                try { title = _driver.FindElement(By.XPath("/html/body/section[1]/div/div[1]/h1")).Text + " " + _driver.FindElement(By.XPath("/html/body/section[1]/div/div[1]/p[2]")).Text; }
                                catch (NoSuchElementException) { }

                                try { professor = _driver.FindElement(By.XPath("//*[@id=\"section-icon\"]/div[1]/section/div/div/div/h3")).Text; }
                                catch (NoSuchElementException) { }

                                try { duration = _driver.FindElement(By.XPath("/html/body/section[1]/div/div[2]/div[1]/div/div[1]/div/p[1]")).Text; }
                                catch (NoSuchElementException) { }

                                try { description = _driver.FindElement(By.XPath("//*[@id=\"section-icon\"]/div[1]/div")).Text; }
                                catch (NoSuchElementException) { }

                                success = true; // Marca a tentativa como bem-sucedida
                            }
                            catch (WebDriverTimeoutException ex)
                            {
                                _logService.LogEvent("ERRO", $"Tentativa {attempt} falhou para o curso: {link}. Detalhes: {ex.Message}", "FALHA", DateTime.Now);

                                if (attempt >= maxRetries)
                                {
                                    _logService.LogEvent("ERRO", $"Falha ao processar o curso após {maxRetries} tentativas. URL: {link}", "FALHA", DateTime.Now);
                                    throw; // Repropaga a exceção para tratamento global, se necessário
                                }
                            }
                        }

                        // Armazena o curso somente se as informações foram obtidas com sucesso
                        if (success)
                        {
                            var curso = new Curso(link, title, professor, duration, description);

                            courses.Add(curso);
                            _logService.LogEvent("INFO", $"Adicionando curso [{title}] ", "SUCESSO", DateTime.Now);
                            _courseRepository.Save(curso);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logService.LogEvent("ERRO", $"Erro ao processar curso no link: {link}. Detalhes: {ex.Message}", "FALHA", DateTime.Now);
                    }
                }

            }
            catch (Exception ex)
            {
                _logService.LogEvent("ERRO", $"Erro geral no scraper: {ex.Message}", "FALHA", DateTime.Now);
            }

            _logService.LogEvent("INFO", $"Total de cursos Adicionados: {courses.Count}", "SUCESSO", DateTime.Now);
            return courses;
        }

        private int GetMaxPages()
        {
            try
            {
                var paginationLinks = _driver.FindElements(By.XPath("//nav[contains(@class, 'busca-paginacao-links')]//a[contains(@class, 'paginationLink')]"));

                if (!paginationLinks.Any())
                {
                    return 1;
                }

                var lastPageLink = paginationLinks.Last();
                var lastPageUrl = lastPageLink.GetDomAttribute("href");
                var match = Regex.Match(lastPageUrl, @"pagina=(\d+)");

                if (match.Success)
                {
                    return int.Parse(match.Groups[1].Value);
                }
            }
            catch (Exception ex)
            {
                _logService.LogEvent("ERRO", $"Erro ao obter o número máximo de páginas: {ex.Message}", "FALHA", DateTime.Now);
            }

            return 1; // Default para 1 página
        }
    }
}
