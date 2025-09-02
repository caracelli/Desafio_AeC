using System;
using System.Data.Odbc;
using System.IO;

namespace Infra
{
    public class LogService : Dominio.Interfaces.ILogService
    {
        // String de conexão 
        private readonly string _odbcConnectionString =             $@"Driver={{Microsoft Access Driver (*.mdb, *.accdb)}};Dbq={Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\..\Infraestrutura\Data\AluraCourses.accdb")};";

        public void LogEvent(string tipo, string evento, string status, DateTime dataExecucao)
        {
            try
            {
                using (OdbcConnection connection = new OdbcConnection(_odbcConnectionString))
                {
                    connection.Open();

                    // Query para inserir os dados de log na tabela
                    string insertQuery = "INSERT INTO TBL_Logs (Data_Execucao, Data, Tipo, Evento, Status) VALUES (?, ?, ?, ?, ?)";

                    using (OdbcCommand command = new OdbcCommand(insertQuery, connection))
                    {
                        // Adiciona os parâmetros para evitar injeção de SQL
                        command.Parameters.Add("?", OdbcType.DateTime).Value = dataExecucao;  // Data de execução
                        command.Parameters.Add("?", OdbcType.DateTime).Value = DateTime.Now;  // Data atual
                        command.Parameters.Add("?", OdbcType.Text).Value = tipo; // Tipo de Status ( INFO/AVISO/ERRO )
                        command.Parameters.Add("?", OdbcType.Text).Value = evento;// Evento sendo executado no momento
                        command.Parameters.Add("?", OdbcType.Text).Value = status;// status (sucesso/falha)

                        // Executa a consulta de inserção
                        command.ExecuteNonQuery();
                    }
                }

                // Exibe no console a mesma mensagem
                Console.WriteLine($"[{DateTime.Now} - {tipo.ToUpper()}] - {evento}");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error saving log: " + ex.Message);
            }
        }
    }
}
