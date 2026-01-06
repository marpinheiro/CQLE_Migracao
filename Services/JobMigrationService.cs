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
    /// SEMPRE tenta migração DIRETA se connStringDestino for fornecida.
    /// Gera scripts .sql apenas como backup se caminhoOutput for informado.
    /// </summary>
    public List<string> MigrarJobs(
        string connStringOrigem,
        string connStringDestino,
        bool gerarScriptsBackup = false,
        string caminhoOutput = "")
    {
      var logOperacoes = new List<string>();

      Server servidorOrigem = GetSmoServer(connStringOrigem);
      Server? servidorDestino = null;
      bool temDestino = !string.IsNullOrEmpty(connStringDestino);

      if (temDestino)
      {
        servidorDestino = GetSmoServer(connStringDestino);
      }

      try
      {
        if (servidorOrigem.JobServer == null)
          throw new Exception("SQL Server Agent não está disponível no servidor de origem.");

        servidorOrigem.JobServer.Jobs.Refresh();
        int totalJobs = servidorOrigem.JobServer.Jobs.Count;
        int jobsProcessados = 0;

        logOperacoes.Add($"[INÍCIO] Migração de {totalJobs} SQL Agent Job(s) - {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        logOperacoes.Add(temDestino
            ? "[MODO] Migração DIRETA para servidor destino + backup opcional."
            : "[MODO] Apenas geração de scripts de backup (destino não informado).");

        foreach (Job job in servidorOrigem.JobServer.Jobs)
        {
          jobsProcessados++;
          string prefix = $"[{jobsProcessados}/{totalJobs}] Job: {job.Name}";

          try
          {
            var options = new ScriptingOptions
            {
              ScriptDrops = false,
              IncludeIfNotExists = false,
              AgentJobId = true,
              AgentNotify = true,
              Permissions = true,
              Indexes = true,
              Triggers = true
            };

            var scriptCollection = job.Script(options).Cast<string>();

            StringBuilder scriptCompleto = new StringBuilder();

            scriptCompleto.AppendLine($"-- ==============================================");
            scriptCompleto.AppendLine($"-- CQLE MIGRAÇÃO - SQL Agent Job: {job.Name}");
            scriptCompleto.AppendLine($"-- Origem: {servidorOrigem.Name}");
            scriptCompleto.AppendLine($"-- Data/Hora: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            scriptCompleto.AppendLine($"-- Habilitado: {(job.IsEnabled ? "Sim" : "Não")}");
            scriptCompleto.AppendLine($"-- Owner: {job.OwnerLoginName} | Categoria: {job.Category}");
            scriptCompleto.AppendLine($"-- Steps: {job.JobSteps.Count} | Agendamentos: {job.JobSchedules.Count}");
            scriptCompleto.AppendLine($"-- ==============================================");
            scriptCompleto.AppendLine("USE [msdb]");
            scriptCompleto.AppendLine("GO");
            scriptCompleto.AppendLine();

            scriptCompleto.AppendLine($"-- Remove job existente");
            scriptCompleto.AppendLine($"IF EXISTS (SELECT 1 FROM msdb.dbo.sysjobs WHERE name = N'{job.Name.Replace("'", "''")}')");
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

            // MIGRAÇÃO DIRETA
            if (temDestino && servidorDestino != null)
            {
              try
              {
                servidorDestino.ConnectionContext.ExecuteNonQuery(scriptCompleto.ToString());
                logOperacoes.Add($"[SUCESSO] {prefix} → criado diretamente no destino.");

                if (gerarScriptsBackup)
                  SalvarScriptJob(scriptCompleto.ToString(), job.Name, caminhoOutput);
              }
              catch (Exception ex)
              {
                logOperacoes.Add($"[ERRO] {prefix} → falha na criação direta: {ex.Message}");
                if (gerarScriptsBackup)
                {
                  SalvarScriptJob(scriptCompleto.ToString(), job.Name, caminhoOutput);
                  logOperacoes.Add($"[BACKUP] Script salvo para execução manual.");
                }
              }
            }
            else if (gerarScriptsBackup)
            {
              SalvarScriptJob(scriptCompleto.ToString(), job.Name, caminhoOutput);
              logOperacoes.Add($"[BACKUP] {prefix} → script gerado.");
            }
          }
          catch (Exception ex)
          {
            logOperacoes.Add($"[ERRO] {prefix} → {ex.Message}");
          }
        }

        logOperacoes.Add($"[FIM] Processamento de Jobs concluído - {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
      }
      catch (Exception ex)
      {
        logOperacoes.Add($"[ERRO CRÍTICO] {ex.Message}");
      }

      // Salva log consolidado
      if (!string.IsNullOrEmpty(caminhoOutput) && logOperacoes.Count > 0)
      {
        Directory.CreateDirectory(caminhoOutput);
        string logFile = Path.Combine(caminhoOutput, $"Log_Jobs_Migracao_{DateTime.Now:yyyyMMdd_HHmmss}.txt");
        File.WriteAllLines(logFile, logOperacoes, Encoding.UTF8);
        logOperacoes.Add($"[LOG] Relatório salvo em: {logFile}");
      }

      return logOperacoes;
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

      if (servidor.JobServer == null)
        throw new Exception("SQL Server Agent não disponível.");

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