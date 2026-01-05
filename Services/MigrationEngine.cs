using CQLE_MIGRACAO.Models;
using CQLE_MIGRACAO.Validation;

namespace CQLE_MIGRACAO.Services
{
  public class MigrationEngine
  {
    public void Execute(MigrationPlan plan)
    {
      MigrationValidator.Validate(plan);

      foreach (var db in plan.Databases)
      {
        // FUTURO: backup / restore ou online
      }

      foreach (var job in plan.Jobs)
      {
        // FUTURO: script job
      }

      foreach (var ls in plan.LinkedServers)
      {
        // FUTURO: script linked server
      }
    }
  }
}
