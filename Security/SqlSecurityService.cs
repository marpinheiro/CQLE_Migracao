using System;
using Microsoft.Data.SqlClient;

namespace CQLE_MIGRACAO.Security
{
  public static class SqlSecurityService
  {
    public static bool IsSysAdmin(SqlConnection conn)
    {
      // Validação vital: garante que a conexão está aberta antes de checar
      if (conn.State != System.Data.ConnectionState.Open)
      {
        throw new InvalidOperationException("A conexão deve estar aberta para verificar privilégios.");
      }

      // Query T-SQL para verificar a role sysadmin
      string query = "SELECT IS_SRVROLEMEMBER('sysadmin')";

      using (SqlCommand cmd = new SqlCommand(query, conn))
      {
        object result = cmd.ExecuteScalar();

        if (result != null && result != DBNull.Value)
        {
          return Convert.ToInt32(result) == 1;
        }
      }

      return false;
    }
  }
}