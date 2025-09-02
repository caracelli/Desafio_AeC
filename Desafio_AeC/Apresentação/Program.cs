using Microsoft.Extensions.DependencyInjection;
using RPA.Scraper;
using Dominio.Interfaces;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using System;
using InjeçãoDeDependência;
using WebDriverManager;
using WebDriverManager.DriverConfigs.Impl;

namespace MyApp
{
    public class Program
    {
        public static void Main(string[] args)
        {
            // Inicializa a variável de busca para efetuar busca de um curso diferente favor ajustar aqui
            string searchTerm = "Excel";

            // Configura o Repósitório de dependência
            var serviceCollection = new ServiceCollection();

            // Configura o WebDriver 
            new DriverManager().SetUpDriver(new ChromeConfig());
            serviceCollection.AddSingleton<IWebDriver>(sp =>
            {
                var options = new ChromeOptions();
                options.AddArgument("--start-maximized");
                return new ChromeDriver(options);
            });

            // Configura outros serviços do projeto
            DependencyConfig.ConfigureServices(serviceCollection);

            var serviceProvider = serviceCollection.BuildServiceProvider();

            // Executa a automação
            ExecuteAutomation(serviceProvider, searchTerm);
        }


        private static void ExecuteAutomation(IServiceProvider serviceProvider, string searchTerm)
        {
            var logService = serviceProvider.GetService<ILogService>();
            var scraper = serviceProvider.GetService<ICourseScraper>();
            var driver = serviceProvider.GetService<IWebDriver>();

            if (logService == null || scraper == null || driver == null)
            {
                Console.WriteLine("Erro: Algum serviço não foi registrado corretamente.");
                return;
            }

            try
            {
                logService.LogEvent("INFO", "Início da Automação", "SUCESSO", DateTime.Now);
                logService.LogEvent("INFO", $"Buscando Cursos com a palavra '{searchTerm}'", "SUCESSO", DateTime.Now);

                scraper.ScrapeCourses(searchTerm);

                logService.LogEvent("INFO", "Execução concluída", "SUCESSO", DateTime.Now);
            }
            catch (Exception ex)
            {
                logService.LogEvent("ERRO", ex.Message, "FALHA", DateTime.Now);
            }
            finally
            {
                driver.Quit();
                logService.LogEvent("INFO", "Navegador encerrado", "SUCESSO", DateTime.Now);
                logService.LogEvent("INFO", "Finalizando a Automação", "SUCESSO", DateTime.Now);
            }
        }
    }
}
