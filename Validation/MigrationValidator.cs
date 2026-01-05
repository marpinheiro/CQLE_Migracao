using System;
using CQLE_MIGRACAO.Models;

namespace CQLE_MIGRACAO.Validation
{
  public static class MigrationValidator
  {
    public static void Validate(MigrationPlan plan)
    {
      if (plan.Databases.Count == 0 &&
          plan.Jobs.Count == 0 &&
          plan.LinkedServers.Count == 0)
      {
        throw new Exception("Nenhum item selecionado para migração.");
      }
    }
  }
}
