#nullable disable  // Desabilita nullable reference types neste arquivo (necessário para compatibilidade com SMO)
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.Data.SqlClient;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlServer.Management.Smo.Agent;

namespace CQLE_MIGRACAO.Services
{
  public class JobMigrationService
  {
    /// <summary>
    /// Migra SQL Agent Jobs da origem para o destino.
    /// Prioriza migração DIRETA. Gera scripts apenas como backup.
    /// Inclui Operators, Alerts e habilita jobs no final.
    /// </summary>
    public List<string> MigrarJobs(
        string connStringOrigem,
        string connStringDestino,
        bool gerarScriptsBackup = false,
        string caminhoOutput = "")
    {
      var logOperacoes = new List<string>();

      Server servidorOrigem = GetSmoServer(connStringOrigem);
      Server servidorDestino = null;
      bool temDestino = !string.IsNullOrEmpty(connStringDestino);

      if (temDestino)
      {
        servidorDestino = GetSmoServer(connStringDestino);
      }

      try
      {
        if (servidorOrigem.JobServer == null)
          throw new Exception("SQL Server Agent não disponível na origem.");

        servidorOrigem.JobServer.Jobs.Refresh();
        int totalJobs = servidorOrigem.JobServer.Jobs.Count;
        int jobsMigrados = 0;

        logOperacoes.Add($"[INÍCIO] Migração de Jobs iniciada em {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        logOperacoes.Add(temDestino
            ? "[MODO] Migração DIRETA para servidor destino."
            : "[MODO] Apenas geração de scripts (destino não informado).");

        // Migrar Operators
        MigrarOperators(servidorOrigem, servidorDestino, logOperacoes, temDestino);

        // Migrar Alerts
        MigrarAlerts(servidorOrigem, servidorDestino, logOperacoes, temDestino);

        // Migrar Jobs
        foreach (Job job in servidorOrigem.JobServer.Jobs)
        {
          if (IsJobSistema(job))
          {
            logOperacoes.Add($"[IGNORADO] Job: {job.Name} (categoria de sistema)");
            continue;
          }

          jobsMigrados++;
          string prefix = $"[JOB {jobsMigrados}/{totalJobs}] {job.Name}";

          try
          {
            ScriptingOptions options = new ScriptingOptions
            {
              ScriptDrops = false,
              IncludeIfNotExists = true,
              AgentJobId = true,
              AgentNotify = true,
              Permissions = true,
              Indexes = true,
              Triggers = true
            };

            var scriptCollection = job.Script(options).Cast<string>();
            StringBuilder scriptCompleto = new StringBuilder();

            scriptCompleto.AppendLine($"-- CQLE MIGRAÇÃO - Job: {job.Name}");
            scriptCompleto.AppendLine($"-- Data: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            scriptCompleto.AppendLine("USE [msdb]");
            scriptCompleto.AppendLine("GO");
            scriptCompleto.AppendLine();

            scriptCompleto.AppendLine($"IF EXISTS (SELECT job_id FROM msdb.dbo.sysjobs WHERE name = N'{job.Name.Replace("'", "''")}')");
            scriptCompleto.AppendLine("BEGIN");
            scriptCompleto.AppendLine($"    EXEC msdb.dbo.sp_delete_job @job_name = N'{job.Name.Replace("'", "''")}'");
            scriptCompleto.AppendLine("END");
            scriptCompleto.AppendLine("GO");
            scriptCompleto.AppendLine();

            foreach (string line in scriptCollection)
            {
              if (!string.IsNullOrWhiteSpace(line))
              {
                scriptCompleto.AppendLine(line);
                scriptCompleto.AppendLine("GO");
              }
            }

            if (temDestino && servidorDestino != null)
            {
              servidorDestino.ConnectionContext.ExecuteNonQuery(scriptCompleto.ToString());
              logOperacoes.Add($"[SUCESSO] {prefix} migrado diretamente.");

              if (gerarScriptsBackup)
                SalvarScriptJob(scriptCompleto.ToString(), job.Name, caminhoOutput);
            }
            else if (gerarScriptsBackup)
            {
              SalvarScriptJob(scriptCompleto.ToString(), job.Name, caminhoOutput);
              logOperacoes.Add($"[BACKUP] {prefix} script gerado.");
            }
          }
          catch (Exception ex)
          {
            logOperacoes.Add($"[ERRO] {prefix}: {ex.Message}");
          }
        }

        // Habilitar jobs no destino
        if (temDestino && servidorDestino != null)
        {
          HabilitarJobs(servidorDestino, logOperacoes);
        }

        logOperacoes.Add($"[FIM] Migração de Jobs concluída em {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
      }
      catch (Exception ex)
      {
        logOperacoes.Add($"[ERRO CRÍTICO] Falha geral na migração de Jobs: {ex.Message}");
      }

      // Salva log
      if (!string.IsNullOrEmpty(caminhoOutput) && logOperacoes.Count > 0)
      {
        Directory.CreateDirectory(caminhoOutput);
        string logFile = Path.Combine(caminhoOutput, $"Log_Jobs_Migracao_{DateTime.Now:yyyyMMdd_HHmmss}.txt");
        File.WriteAllLines(logFile, logOperacoes, Encoding.UTF8);
        logOperacoes.Add($"[LOG] Relatório salvo em: {logFile}");
      }

      return logOperacoes;
    }

    private bool IsJobSistema(Job job)
    {
      string[] categoriasSistema = { "[Database Maintenance]", "[Log Shipping]", "[Replication]" };
      return categoriasSistema.Contains(job.Category) || job.Name.StartsWith("sysdbssis");
    }

    private void MigrarOperators(Server origem, Server destino, List<string> log, bool temDestino)
    {
      log.Add("[OPERATORS] Iniciando migração de Operators...");
      origem.JobServer.Operators.Refresh();

      foreach (Operator op in origem.JobServer.Operators)
      {
        try
        {
          var script = op.Script(new ScriptingOptions());
          if (temDestino && destino != null)
          {
            destino.ConnectionContext.ExecuteNonQuery(script.ToString());
          }
          log.Add($"[SUCESSO] Operator {op.Name} migrado.");
        }
        catch (Exception ex)
        {
          log.Add($"[ERRO] Operator {op.Name}: {ex.Message}");
        }
      }
      log.Add("[OPERATORS] Migração concluída.");
    }

    private void MigrarAlerts(Server origem, Server destino, List<string> log, bool temDestino)
    {
      log.Add("[ALERTS] Iniciando migração de Alerts...");
      origem.JobServer.Alerts.Refresh();

      foreach (Alert alert in origem.JobServer.Alerts)
      {
        try
        {
          var script = alert.Script(new ScriptingOptions());
          if (temDestino && destino != null)
          {
            destino.ConnectionContext.ExecuteNonQuery(script.ToString());
          }
          log.Add($"[SUCESSO] Alert {alert.Name} migrado.");
        }
        catch (Exception ex)
        {
          log.Add($"[ERRO] Alert {alert.Name}: {ex.Message}");
        }
      }
      log.Add("[ALERTS] Migração concluída.");
    }

    private void HabilitarJobs(Server destino, List<string> log)
    {
      log.Add("[HABILITAR] Habilitando jobs migrados...");
      destino.JobServer.Jobs.Refresh();

      int count = 0;
      foreach (Job job in destino.JobServer.Jobs)
      {
        try
        {
          if (!job.IsEnabled)
          {
            job.IsEnabled = true;
            job.Alter();
            count++;
          }
        }
        catch (Exception ex)
        {
          log.Add($"[ERRO] Habilitar job {job.Name}: {ex.Message}");
        }
      }
      log.Add($"[HABILITAR] {count} job(s) habilitado(s).");
    }

    private void SalvarScriptJob(string script, string jobName, string caminhoOutput)
    {
      if (string.IsNullOrEmpty(caminhoOutput)) return;

      Directory.CreateDirectory(caminhoOutput);
      string nomeSeguro = RemoverCaracteresInvalidos(jobName);
      string arquivo = Path.Combine(caminhoOutput, $"Job_{nomeSeguro}.sql");
      File.WriteAllText(arquivo, script, Encoding.UTF8);
    }

    private string RemoverCaracteresInvalidos(string nome)
    {
      var invalidos = Path.GetInvalidFileNameChars();
      foreach (char c in invalidos)
        nome = nome.Replace(c, '_');
      return nome.Length > 100 ? nome.Substring(0, 100) : nome;
    }

    private Server GetSmoServer(string connectionString)
    {
      var sqlConn = new SqlConnection(connectionString);
      var serverConn = new ServerConnection(sqlConn);
      return new Server(serverConn);
    }

    public List<JobInfo> ListarJobs(string connectionString)
    {
      var lista = new List<JobInfo>();
      Server servidor = GetSmoServer(connectionString);
      if (servidor.JobServer == null) return lista;

      servidor.JobServer.Jobs.Refresh();

      foreach (Job job in servidor.JobServer.Jobs)
      {
        lista.Add(new JobInfo
        {
          Name = job.Name,
          IsEnabled = job.IsEnabled,
          Category = job.Category,
          Owner = job.OwnerLoginName,
          Description = job.Description ?? "",
          DateCreated = job.DateCreated,
          DateLastModified = job.DateLastModified,
          LastRunDate = job.LastRunDate,
          StepCount = job.JobSteps.Count,
          ScheduleCount = job.JobSchedules.Count
        });
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