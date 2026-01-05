using Microsoft.Data.SqlClient;

namespace CQLE_MIGRACAO.Security
{
  public class LoginService
  {
    public bool ValidarSysAdmin(string servidor, string usuario, string senha)
    {
      string connectionString =
          $"Server={servidor};User Id={usuario};Password={senha};TrustServerCertificate=True;";

      using SqlConnection conn = new SqlConnection(connectionString);
      conn.Open();

      string sql = @"
                SELECT IS_SRVROLEMEMBER('sysadmin')
            ";

      using SqlCommand cmd = new SqlCommand(sql, conn);
      int resultado = (int)cmd.ExecuteScalar();

      return resultado == 1;
    }
  }
}
