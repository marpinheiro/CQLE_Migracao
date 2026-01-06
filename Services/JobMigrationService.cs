using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Microsoft.Data.SqlClient;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlServer.Management.Smo.Agent;
using System.Collections.Specialized;

namespace CQLE_MIGRACAO.Services
{
  public class JobMigrationService
  {
    public void ProcessarMigracao(string connStringOrigem, string connStringDestino, bool isOnline, string caminhoOutput)
    {
      Server servidorOrigem = GetSmoServer(connStringOrigem);
      Server? servidorDestino = null;

      if (isOnline && !string.IsNullOrEmpty(connStringDestino))
      {
        servidorDestino = GetSmoServer(connStringDestino);
      }

      List<string> logOperacoes = new List<string>();

      try
      {
        // Garante que o SQL Agent está acessível
        if (servidorOrigem.JobServer == null)
        {
          throw new Exception("SQL Server Agent não está disponível no servidor de origem.");
        }

        servidorOrigem.JobServer.Jobs.Refresh();

        int totalJobs = servidorOrigem.JobServer.Jobs.Count;
        int jobsProcessados = 0;

        foreach (Job job in servidorOrigem.JobServer.Jobs)
        {
          jobsProcessados++;

          try
          {
            // Configurações de script
            ScriptingOptions options = new ScriptingOptions
            {
              ScriptDrops = false,
              IncludeIfNotExists = false,
              ScriptSchema = true,
              WithDependencies = false,
              // Jobs precisam de tratamento especial
              AgentJobId = true,
              AgentNotify = true
            };

            StringCollection scriptCollection = job.Script(options);
            StringBuilder scriptCompleto = new StringBuilder();

            scriptCompleto.AppendLine($"-- ============================================");
            scriptCompleto.AppendLine($"-- Migração SQL Agent Job: {job.Name}");
            scriptCompleto.AppendLine($"-- Data: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            scriptCompleto.AppendLine($"-- Categoria: {job.Category}");
            scriptCompleto.AppendLine($"-- Habilitado: {(job.IsEnabled ? "Sim" : "Não")}");
            scriptCompleto.AppendLine($"-- Owner: {job.OwnerLoginName}");
            scriptCompleto.AppendLine($"-- ============================================");
            scriptCompleto.AppendLine("USE [msdb]");
            scriptCompleto.AppendLine("GO");
            scriptCompleto.AppendLine();

            // Adiciona verificação se o job já existe
            scriptCompleto.AppendLine($"-- Remove job se já existir");
            scriptCompleto.AppendLine($"IF EXISTS (SELECT job_id FROM msdb.dbo.sysjobs WHERE name = N'{job.Name.Replace("'", "''")}')");
            scriptCompleto.AppendLine("BEGIN");
            scriptCompleto.AppendLine($"    EXEC msdb.dbo.sp_delete_job @job_name=N'{job.Name.Replace("'", "''")}', @delete_unused_schedule=1");
            scriptCompleto.AppendLine("END");
            scriptCompleto.AppendLine("GO");
            scriptCompleto.AppendLine();

            // Adiciona os scripts gerados pelo SMO
            foreach (string? line in scriptCollection)
            {
              if (!string.IsNullOrEmpty(line))
              {
                scriptCompleto.AppendLine(line);
                scriptCompleto.AppendLine("GO");
              }
            }

            // Adiciona informações de schedules
            if (job.JobSchedules.Count > 0)
            {
              scriptCompleto.AppendLine();
              scriptCompleto.AppendLine($"-- Job possui {job.JobSchedules.Count} agendamento(s)");
            }

            // Adiciona informações de steps
            if (job.JobSteps.Count > 0)
            {
              scriptCompleto.AppendLine($"-- Job possui {job.JobSteps.Count} step(s)");
              foreach (JobStep step in job.JobSteps)
              {
                scriptCompleto.AppendLine($"--   Step {step.ID}: {step.Name} (Subsystem: {step.SubSystem})");
              }
            }

            scriptCompleto.AppendLine();
            scriptCompleto.AppendLine("-- Fim do script");
            scriptCompleto.AppendLine();

            // Execução
            if (isOnline && servidorDestino != null)
            {
              try
              {
                // Verifica se o job já existe no destino
                bool jobExists = false;
                servidorDestino.JobServer.Jobs.Refresh();

                foreach (Job destJob in servidorDestino.JobServer.Jobs)
                {
                  if (destJob.Name.Equals(job.Name, StringComparison.OrdinalIgnoreCase))
                  {
                    jobExists = true;
                    break;
                  }
                }

                if (jobExists)
                {
                  logOperacoes.Add($"[AVISO] Job '{job.Name}' já existe no destino. Script executado para recriar.");
                }

                // Executa o script no destino
                servidorDestino.ConnectionContext.ExecuteNonQuery(scriptCompleto.ToString());
                logOperacoes.Add($"[SUCESSO] Job '{job.Name}' criado no destino ({jobsProcessados}/{totalJobs})");
              }
              catch (Exception ex)
              {
                logOperacoes.Add($"[ERRO] Falha ao criar Job '{job.Name}' online: {ex.Message}");
              }
            }
            else
            {
              // Modo OFFLINE: salva script
              if (!Directory.Exists(caminhoOutput))
                Directory.CreateDirectory(caminhoOutput);

              // Remove caracteres inválidos do nome do arquivo
              string nomeArquivoSeguro = RemoverCaracteresInvalidos(job.Name);
              string fileName = Path.Combine(caminhoOutput, $"Job_{nomeArquivoSeguro}.sql");

              File.WriteAllText(fileName, scriptCompleto.ToString(), Encoding.UTF8);
              logOperacoes.Add($"[OFFLINE] Script gerado: {fileName} ({jobsProcessados}/{totalJobs})");
            }
          }
          catch (Exception ex)
          {
            logOperacoes.Add($"[ERRO] Falha ao processar Job '{job.Name}': {ex.Message}");
          }
        }

        // Salva log de operações
        if (!string.IsNullOrEmpty(caminhoOutput) && logOperacoes.Count > 0)
        {
          string logFile = Path.Combine(caminhoOutput, $"_Log_Jobs_Migration_{DateTime.Now:yyyyMMdd_HHmmss}.txt");
          File.WriteAllLines(logFile, logOperacoes, Encoding.UTF8);
        }
      }
      catch (Exception ex)
      {
        throw new Exception($"Erro durante a migração de SQL Agent Jobs: {ex.Message}", ex);
      }
    }

    /// <summary>
    /// Remove caracteres inválidos para nomes de arquivo
    /// </summary>
    private string RemoverCaracteresInvalidos(string nome)
    {
      string invalidos = new string(Path.GetInvalidFileNameChars()) + new string(Path.GetInvalidPathChars());

      foreach (char c in invalidos)
      {
        nome = nome.Replace(c, '_');
      }

      return nome;
    }

    /// <summary>
    /// Cria conexão SMO com o servidor
    /// </summary>
    private Server GetSmoServer(string connectionString)
    {
      SqlConnection sqlConn = new SqlConnection(connectionString);
      ServerConnection serverConn = new ServerConnection(sqlConn);
      return new Server(serverConn);
    }

    /// <summary>
    /// Lista jobs disponíveis (para preview)
    /// </summary>
    public List<JobInfo> ListarJobs(string connectionString)
    {
      var lista = new List<JobInfo>();

      try
      {
        Server servidor = GetSmoServer(connectionString);
        servidor.JobServer.Jobs.Refresh();

        foreach (Job job in servidor.JobServer.Jobs)
        {
          lista.Add(new JobInfo
          {
            Name = job.Name,
            IsEnabled = job.IsEnabled,
            Category = job.Category,
            Owner = job.OwnerLoginName,
            Description = job.Description,
            DateCreated = job.DateCreated,
            DateLastModified = job.DateLastModified,
            LastRunDate = job.LastRunDate,
            StepCount = job.JobSteps.Count,
            ScheduleCount = job.JobSchedules.Count
          });
        }
      }
      catch (Exception ex)
      {
        throw new Exception($"Erro ao listar Jobs: {ex.Message}", ex);
      }

      return lista;
    }

    public class JobInfo
    {
      public string Name { get; set; } = "";
      public bool IsEnabled { get; set; }
      public string Category { get; set; } = "";
      public string Owner { get; set; } = "";
      public string Description { get; set; } = "";
      public DateTime DateCreated { get; set; }
      public DateTime DateLastModified { get; set; }
      public DateTime LastRunDate { get; set; }
      public int StepCount { get; set; }
      public int ScheduleCount { get; set; }
    }
  }
}