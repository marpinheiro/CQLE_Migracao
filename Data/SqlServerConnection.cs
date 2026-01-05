using Microsoft.Data.SqlClient;

namespace CQLE_MIGRACAO.Data
{
  public static class SqlServerConnection
  {
    public static SqlConnection Create(
        string server,
        string user,
        string password)
    {
      var connectionString =
          $"Server={server};User Id={user};Password={password};TrustServerCertificate=True;";

      return new SqlConnection(connectionString);
    }
  }
}
