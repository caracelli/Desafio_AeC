using Microsoft.Extensions.DependencyInjection;
using RPA.Scraper;
using Dominio.Interfaces;
using OpenQA.Selenium;
using System;
using InjeçãoDeDependência;

namespace MyApp
{
    public class Program
    {
        public static void Main(string[] args)
        {
            // Inicializa o searchTerm com o valor passado como argumento
            string? searchTerm = args.Length > 0 ? args[0] : null;

            // Verifica se searchTerm ainda está nulo ou vazio
            if (string.IsNullOrWhiteSpace(searchTerm))
            {
                Console.Write("Informe o termo de busca (pressione Enter para usar o valor padrão 'RPA'): ");
                searchTerm = Console.ReadLine();

                // Caso o usuário não insira nada, define o valor padrão
                if (string.IsNullOrWhiteSpace(searchTerm))
                {
                    searchTerm = "RPA";
                }
            }

            // Configura o contêiner de dependência
            var serviceCollection = new ServiceCollection();
            var serviceProvider = DependencyConfig.ConfigureServices(serviceCollection);

            // Resolve o ILogService
            var logService = serviceProvider.GetService<ILogService>();
            if (logService == null)
            {
                Console.WriteLine("Erro: LogService não foi registrado no contêiner de dependência.");
                return; // Encerra o programa se não houver log
            }

            IWebDriver? webDriver = null; // Variável para armazenar o navegador

            try
            {
                // Log de início
                logService.LogEvent("INFO", "Início da Automação", "SUCESSO", DateTime.Now);

                // Resolve o SeleniumCourseScraper e executa a lógica
                var courseScraper = serviceProvider.GetService<ICourseScraper>();
                if (courseScraper == null)
                {
                    logService.LogEvent("ERRO", "ICourseScraper não foi registrado no contêiner de dependência.", "FALHA", DateTime.Now);
                    return; // Encerra o programa se não houver scraper
                }

                // Captura o IWebDriver para encerramento posterior
                webDriver = serviceProvider.GetService<IWebDriver>();
                if (webDriver == null)
                {
                    logService.LogEvent("ERRO", "IWebDriver não foi registrado no contêiner de dependência.", "FALHA", DateTime.Now);
                    return; // Encerra o programa se não houver navegador
                }
                webDriver.Manage().Window.Maximize();
                logService.LogEvent("INFO", $"Buscando Cursos com a palavra '{searchTerm}'", "SUCESSO", DateTime.Now);
                courseScraper.ScrapeCourses(searchTerm);

                // Log de sucesso
                logService.LogEvent("INFO", "Execução concluída", "SUCESSO", DateTime.Now);
            }
            catch (Exception ex)
            {
                // Log de erro
                logService.LogEvent("ERRO", ex.Message, "FALHA", DateTime.Now);
            }
            finally
            {
                // Garante o encerramento do navegador
                webDriver?.Quit();
                logService.LogEvent("INFO", "Navegador encerrado", "SUCESSO", DateTime.Now);

                // Log de término
                logService.LogEvent("INFO", "Finalizando a Automação", "SUCESSO", DateTime.Now);
            }
        }
    }
}
