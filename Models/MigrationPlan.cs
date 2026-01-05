using System.Collections.Generic;

namespace CQLE_MIGRACAO.Models
{
  public class MigrationPlan
  {
    // Modo de migração
    public bool IsOnlineMigration { get; set; }

    // Itens selecionados
    public List<string> Databases { get; set; } = new();
    public List<string> Jobs { get; set; } = new();
    public List<string> LinkedServers { get; set; } = new();

    // Flags de controle
    public bool MigrateAllDatabases { get; set; }
    public bool MigrateAllJobs { get; set; }
    public bool MigrateAllLinkedServers { get; set; }
  }
}
