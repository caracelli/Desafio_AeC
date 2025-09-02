using Dominio.Interfaces;
using Dominio.Entities;
using System;
using System.Data.Odbc;
using System.Data;
using System.IO;

namespace Infraestrutura.Repositorios
{
    public class DatabaseCourseRepository : ICourseRepository
    {
        private readonly string _odbcConnectionString = $@"Driver={{Microsoft Access Driver (*.mdb, *.accdb)}};Dbq={Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\..\Infraestrutura\Data\AluraCourses.accdb")};";

        // Retornar o título do curso, ou null se o curso não existir
        public string? GetCourseTitleByLink(string link)
        {
            using (OdbcConnection connection = new OdbcConnection(_odbcConnectionString))
            {
                connection.Open();
                string query = "SELECT Titulo FROM TBL_Cursos WHERE Link = ?";
                using (OdbcCommand command = new OdbcCommand(query, connection))
                {
                    command.Parameters.Add("?", OdbcType.Text).Value = link;
                    var result = command.ExecuteScalar();
                    return result?.ToString(); 
                }
            }
        }


        // Verifica existência e retornar o título do curso
        public bool Exists(string link, out string cursoTitulo)
        {
            cursoTitulo = string.Empty;

            using (OdbcConnection connection = new OdbcConnection(_odbcConnectionString))
            {
                connection.Open();
                string query = "SELECT Titulo FROM TBL_Cursos WHERE Link = ?";

                using (OdbcCommand command = new OdbcCommand(query, connection))
                {
                    command.Parameters.Add("?", OdbcType.Text).Value = link;
                    var result = command.ExecuteScalar();
                    if (result != null)
                    {
                        cursoTitulo = result.ToString() ?? string.Empty; 
                        return true; // Curso encontrado
                    }
                }
            }

            return false; // Curso não encontrado
        }









        public void Save(Curso curso)
        {
            using (OdbcConnection connection = new OdbcConnection(_odbcConnectionString))
            {
                connection.Open();
                string query = "INSERT INTO TBL_Cursos (Link, Titulo, Professor, Duracao, Descricao) VALUES (?, ?, ?, ?, ?)";
                using (OdbcCommand command = new OdbcCommand(query, connection))
                {
                    command.Parameters.Add("?", OdbcType.Text).Value = curso.Link;
                    command.Parameters.Add("?", OdbcType.Text).Value = curso.Titulo;
                    command.Parameters.Add("?", OdbcType.Text).Value = curso.Professor;
                    command.Parameters.Add("?", OdbcType.Text).Value = curso.Duracao;
                    command.Parameters.Add("?", OdbcType.Text).Value = curso.Descricao;
                    command.ExecuteNonQuery();
                }
            }
        }
    }
}
