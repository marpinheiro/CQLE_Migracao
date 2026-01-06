using System;
using Microsoft.Data.SqlClient;

namespace CQLE_MIGRACAO.Data
{
  public static class SqlServerConnection
  {
    public static SqlConnection Create(string servidor, string usuario, string senha)
    {
      // Monta a string de conexão segura
      var builder = new SqlConnectionStringBuilder
      {
        DataSource = servidor,
        UserID = usuario,
        Password = senha,
        InitialCatalog = "master", // Sempre conecta na master para tarefas administrativas
        TrustServerCertificate = true, // Necessário para evitar erros de SSL em redes locais
        ConnectTimeout = 30 // Timeout razoável para validação
      };

      return new SqlConnection(builder.ConnectionString);
    }
  }
}