using Dominio.Interfaces;
using Dominio.Entities;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
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
        private readonly int maxRetries = 3;

        public SeleniumCourseScraper(IWebDriver driver, ICourseRepository courseRepository, ILogService logService)
        {
            _driver = driver;
            _courseRepository = courseRepository;
            _logService = logService;
            _wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(10));

            // Garantir que o navegador abra maximizado
            _driver.Manage().Window.Maximize();
        }

        public List<Curso> ScrapeCourses(string searchTerm)
        {
            var allCourseLinks = new List<string>();
            var courses = new List<Curso>();
            string encodedSearchTerm = Uri.EscapeDataString(searchTerm);
            string baseUrl = "https://www.alura.com.br";

            // Método auxiliar para retry com log
            void RetryAction(Action action, string description)
            {
                int attempt = 0;
                bool success = false;

                while (attempt < maxRetries && !success)
                {
                    attempt++;
                    try
                    {
                        action();
                        success = true;
                    }
                    catch (Exception ex) when (
                        ex.Message.Contains("Client requested cancel") ||
                        ex is WebDriverTimeoutException ||
                        ex is InvalidOperationException 
                    )
                    {
                        _logService.LogEvent(
                            "ERRO",
                            $"Tentativa {attempt} falhou para: {description}. Detalhes: {ex.Message}",
                            "FALHA",
                            DateTime.Now
                        );

                        if (attempt >= maxRetries)
                        {
                            _logService.LogEvent(
                                "ERRO",
                                $"Máximo de tentativas atingido para: {description}. Abortando etapa.",
                                "FALHA",
                                DateTime.Now
                            );
                           
                        }

                        System.Threading.Thread.Sleep(1000); // Espera antes de tentar novamente
                    }
                    catch (Exception ex)
                    {
                        // Qualquer outro erro inesperado
                        _logService.LogEvent(
                            "ERRO",
                            $"Erro inesperado em {description}: {ex.Message}",
                            "FALHA",
                            DateTime.Now
                        );
                        throw;
                    }
                }
            }


            try
            {
                // --- Fluxo de filtragem ---
                RetryAction(() => _driver.Navigate().GoToUrl(baseUrl), "Navegar para a página inicial");

                RetryAction(() =>
                {
                    var searchInput = _wait.Until(driver => driver.FindElement(By.XPath("/html/body/main/section[1]/header/div/nav/div[2]/form/input")));
                    searchInput.SendKeys(encodedSearchTerm);
                }, "Preencher campo de busca");

                RetryAction(() =>
                {
                    var searchButton = _wait.Until(driver => driver.FindElement(By.XPath("/html/body/main/section[1]/header/div/nav/div[2]/form/button")));
                    searchButton.Click();
                }, "Clicar no botão de busca");

                RetryAction(() =>
                {
                    _wait.Until(driver => driver.FindElement(By.XPath("/html/body/div[2]/div[2]/div/form/input[1]")));
                }, "Aguardar carregamento da página de cursos filtrados");

                // --- Clicar no checkbox "Cursos" ---
                RetryAction(() =>
                {
                    var firstLabel = _wait.Until(driver => driver.FindElement(By.XPath("/html/body/div[2]/div[1]/div[2]/ul/li[1]/label")));
                    firstLabel.Click();
                }, "Marcar checkbox de cursos");

                // --- Clicar no botão de filtragem/atualização ---
                RetryAction(() =>
                {
                    var filterButton = _wait.Until(driver => driver.FindElement(By.XPath("/html/body/div[2]/div[2]/div/form/input[3]")));
                    filterButton.Click();
                }, "Clicar para atualizar página");

                // --- Aguardar carregamento completo ---
                RetryAction(() =>
                {
                    _wait.Until(driver => ((IJavaScriptExecutor)driver).ExecuteScript("return document.readyState").Equals("complete"));
                }, "Aguardar carregamento completo da página");

                // --- Número máximo de páginas ---
                int maxPages = GetMaxPages();
                _logService.LogEvent("INFO", $"Número total de páginas: {maxPages}", "SUCESSO", DateTime.Now);

                // --- Coleta links de cursos ---
                for (int pageNum = 1; pageNum <= maxPages; pageNum++)
                {
                    string currentPageUrl = $"{baseUrl}/busca?pagina={pageNum}&query={encodedSearchTerm}&typeFilters=COURSE";

                    RetryAction(() =>
                    {
                        // Navegar para a página
                        _driver.Navigate().GoToUrl(currentPageUrl);

                        // Aguardar carregamento dos cursos
                        _wait.Until(driver => driver.FindElements(By.ClassName("busca-resultado-link")).Any());

                    }, $"Navegar e aguardar carregamento da página {pageNum}");


                    var courseLinks = _driver.FindElements(By.ClassName("busca-resultado-link"))
                                             .Select(course => course.GetDomAttribute("href"))
                                             .Where(link => !string.IsNullOrEmpty(link))
                                             .ToList();

                    allCourseLinks.AddRange(courseLinks);
                    _logService.LogEvent("INFO", $"Página {pageNum} processada. Cursos coletados: {courseLinks.Count}", "SUCESSO", DateTime.Now);
                }

                allCourseLinks = allCourseLinks.Distinct().ToList();
                _logService.LogEvent("INFO", $"Total de Cursos coletados: {allCourseLinks.Count}", "SUCESSO", DateTime.Now);

                // --- Processa cada curso ---
                foreach (var link in allCourseLinks)
                {
                    try
                    {
                        string cursoTitulo;
                        if (_courseRepository.Exists(link, out cursoTitulo))
                        {
                            _logService.LogEvent("AVISO", $"Curso [{cursoTitulo}] já existente na base", "SUCESSO", DateTime.Now);
                            continue;
                        }

                        string title = "Não Informado";
                        string professor = "Não Informado";
                        string duration = "Não Informado";
                        string description = "Não Informado";

                        RetryAction(() =>
                        {
                            _driver.Navigate().GoToUrl(link);

                            // --- Captura título ---
                            var titleElement = _wait.Until(driver => driver.FindElements(By.XPath("/html/body/section[1]/div/div[1]/h1")).FirstOrDefault());
                            if (titleElement != null && titleElement.Displayed)
                                title = titleElement.Text + " " + _driver.FindElements(By.XPath("/html/body/section[1]/div/div[1]/p[2]")).FirstOrDefault()?.Text;

                            // --- Captura professor ---
                            professor = _driver.FindElements(By.XPath("/html/body/section[2]/div[1]/section/div/div/div/h3")).FirstOrDefault()?.Text ?? "Não Informado";

                            // --- Captura duração ---
                            duration = _driver.FindElements(By.XPath("/html/body/section[1]/div/div[2]/div[1]/div/div[1]/div/p[1]")).FirstOrDefault()?.Text ?? "Não Informado";

                            // --- Captura descrição ---
                            description = _driver.FindElements(By.XPath("/html/body/section[2]/div[1]/div")).FirstOrDefault()?.Text ?? "Não Informado";

                        }, $"Carregar curso [{link}]");

                        var curso = new Curso(link, title, professor, duration, description);
                        courses.Add(curso);
                        _logService.LogEvent("INFO", $"Adicionando curso [{title}]", "SUCESSO", DateTime.Now);
                        _courseRepository.Save(curso);
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

            _logService.LogEvent("INFO", $"Total de cursos adicionados: {courses.Count}", "SUCESSO", DateTime.Now);
            return courses;
        }

        private int GetMaxPages()
        {
            try
            {
                var paginationLinks = _driver.FindElements(By.XPath("//nav[contains(@class, 'busca-paginacao-links')]//a[contains(@class, 'paginationLink')]"));
                if (!paginationLinks.Any()) return 1;

                var lastPageLink = paginationLinks.Last();
                var lastPageUrl = lastPageLink.GetDomAttribute("href");
                var match = Regex.Match(lastPageUrl, @"pagina=(\d+)");
                if (match.Success) return int.Parse(match.Groups[1].Value);
            }
            catch (Exception ex)
            {
                _logService.LogEvent("ERRO", $"Erro ao obter o número máximo de páginas: {ex.Message}", "FALHA", DateTime.Now);
            }
            return 1;
        }
    }
}
