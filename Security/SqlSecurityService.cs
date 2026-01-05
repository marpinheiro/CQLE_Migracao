using Microsoft.Data.SqlClient;

namespace CQLE_MIGRACAO.Security
{
  public static class SqlSecurityService
  {
    public static bool IsSysAdmin(SqlConnection connection)
    {
      using var cmd = new SqlCommand(
          "SELECT IS_SRVROLEMEMBER('sysadmin')",
          connection);

      var result = cmd.ExecuteScalar();

      return result != null && result.ToString() == "1";
    }
  }
}
