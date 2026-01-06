#nullable disable
using System;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;

namespace CQLE_MIGRACAO.Services
{
  public class UnifiedMigrationService
  {
    private readonly string _connectionStringOrigem;
    private readonly MigrationEngine _databaseEngine;
    private readonly LinkedServerMigrationService _linkedServerService;
    private readonly JobMigrationService _jobService;

    public UnifiedMigrationService(string connectionStringOrigem)
    {
      _connectionStringOrigem = connectionStringOrigem;
      _databaseEngine = new MigrationEngine(connectionStringOrigem);
      _linkedServerService = new LinkedServerMigrationService();
      _jobService = new JobMigrationService();
    }

    public class MigrationConfig
    {
      public List<string> DatabaseNames { get; set; } = new List<string>();
      public bool IncludeJobs { get; set; }
      public bool IncludeLinkedServers { get; set; }
      public bool IsOnline { get; set; }
      public string ServerDestino { get; set; } = "";
      public string OutputPath { get; set; } = "";
      public string PastaBackup { get; set; } = "";
    }

    public void ExecutarMigracaoCompleta(MigrationConfig config, Action<string> log)
    {
      log("╔════════════════════════════════════════════════════╗");
      log("║      CQLE MIGRAÇÃO - MIGRAÇÃO UNIFICADA INICIADA   ║");
      log("╚════════════════════════════════════════════════════╝");
      log("");
      log($"Modo: {(config.IsOnline ? "ONLINE (direto)" : "OFFLINE (gera scripts)")}");
      log($"Servidor Destino: {config.ServerDestino}");
      log("");

      int totalOperacoes = config.DatabaseNames.Count;
      if (config.IncludeLinkedServers) totalOperacoes++;
      if (config.IncludeJobs) totalOperacoes++;

      int operacaoAtual = 0;

      try
      {
        // FASE 1: BANCOS DE DADOS
        if (config.DatabaseNames.Count > 0)
        {
          log("┌─────────────────────────────────────────────────┐");
          log("│  FASE 1: MIGRAÇÃO DE BANCOS DE DADOS            │");
          log("└─────────────────────────────────────────────────┘");

          foreach (var banco in config.DatabaseNames)
          {
            operacaoAtual++;
            log($"[{operacaoAtual}/{totalOperacoes}] Processando banco: {banco}");

            try
            {
              _databaseEngine.ExecutarMigracaoAutomatizada(
                  banco,
                  config.ServerDestino,
                  config.IsOnline,
                  config.PastaBackup,
                  (msg) => log($"    {msg}")
              );

              log($"✅ Banco '{banco}' migrado com sucesso");
              log("");
            }
            catch (Exception ex)
            {
              log($"❌ ERRO ao migrar '{banco}': {ex.Message}");
              log("");
            }
          }
        }

        // FASE 2: LINKED SERVERS
        if (config.IncludeLinkedServers)
        {
          operacaoAtual++;
          log("┌─────────────────────────────────────────────────┐");
          log("│  FASE 2: MIGRAÇÃO DE LINKED SERVERS            │");
          log("└─────────────────────────────────────────────────┘");
          log($"[{operacaoAtual}/{totalOperacoes}] Processando Linked Servers...");

          try
          {
            string connDestino = config.IsOnline
                ? $"Server={config.ServerDestino};Database=master;Trusted_Connection=True;TrustServerCertificate=True;"
                : "";

            _linkedServerService.ProcessarMigracao(
                _connectionStringOrigem,
                connDestino,
                config.IsOnline,
                config.OutputPath
            );

            log("✅ Linked Servers processados com sucesso");

            if (!config.IsOnline)
            {
              log($"📁 Scripts salvos em: {config.OutputPath}");
            }

            log("");
          }
          catch (Exception ex)
          {
            log($"❌ ERRO ao processar Linked Servers: {ex.Message}");
            log("");
          }
        }

        // FASE 3: JOBS - MIGRAÇÃO DIRETA SEMPRE (quando destino informado)
        if (config.IncludeJobs)
        {
          operacaoAtual++;
          log("┌─────────────────────────────────────────────────┐");
          log("│  FASE 3: MIGRAÇÃO DIRETA DE SQL AGENT JOBS     │");
          log("└─────────────────────────────────────────────────┘");

          if (string.IsNullOrWhiteSpace(config.ServerDestino))
          {
            log("⚠ Servidor destino não informado → Jobs não serão migrados.");
            log("");
          }
          else
          {
            log($"[{operacaoAtual}/{totalOperacoes}] Migrando Jobs diretamente para: {config.ServerDestino}");

            try
            {
              string connStringDestino = $"Server={config.ServerDestino};Database=msdb;Trusted_Connection=True;TrustServerCertificate=True;";

              bool gerarBackup = !string.IsNullOrEmpty(config.OutputPath);

              List<string> logJobs = _jobService.MigrarJobs(
                  connStringOrigem: _connectionStringOrigem,
                  connStringDestino: connStringDestino,
                  gerarScriptsBackup: gerarBackup,
                  caminhoOutput: config.OutputPath
              );

              foreach (var linha in logJobs)
              {
                log(linha);
              }

              log("✅ Jobs migrados diretamente com sucesso.");

              if (gerarBackup)
                log($"📁 Backup dos scripts salvo em: {config.OutputPath}");

              log("");
            }
            catch (Exception ex)
            {
              log($"❌ ERRO na migração direta de Jobs: {ex.Message}");
              if (ex.InnerException != null)
                log($"   Detalhe: {ex.InnerException.Message}");
              log("");
            }
          }
        }

        log("╔════════════════════════════════════════════════════╗");
        log("║           MIGRAÇÃO CONCLUÍDA COM SUCESSO          ║");
        log("╚════════════════════════════════════════════════════╝");

        if (!config.IsOnline)
        {
          log("");
          log("⚠ Modo OFFLINE ativo.");
          log($"   Scripts gerados em: {config.OutputPath}");
          log("   Execute-os manualmente no servidor destino.");
        }
      }
      catch (Exception ex)
      {
        log("");
        log("╔════════════════════════════════════════════════════╗");
        log("║              ERRO CRÍTICO NA MIGRAÇÃO             ║");
        log("╚════════════════════════════════════════════════════╝");
        log($"Erro: {ex.Message}");
        throw;
      }
    }

    public MigrationInventory GetInventario()
    {
      var inventory = new MigrationInventory();

      try
      {
        inventory.Databases = _databaseEngine.ListarBancosDeDados();
        inventory.LinkedServers = ListarLinkedServersNomes();
        inventory.Jobs = ListarJobsNomes();
      }
      catch (Exception ex)
      {
        throw new Exception($"Erro ao inventariar objetos: {ex.Message}", ex);
      }

      return inventory;
    }

    private List<string> ListarLinkedServersNomes()
    {
      var lista = new List<string>();

      try
      {
        using (var conn = new SqlConnection(_connectionStringOrigem))
        {
          conn.Open();
          var cmd = new SqlCommand(
              @"SELECT name FROM sys.servers 
                          WHERE is_linked = 1 
                          AND name <> @@SERVERNAME
                          ORDER BY name",
              conn);

          using (var reader = cmd.ExecuteReader())
          {
            while (reader.Read())
            {
              lista.Add(reader["name"].ToString());
            }
          }
        }
      }
      catch { }

      return lista;
    }

    private List<string> ListarJobsNomes()
    {
      var lista = new List<string>();

      try
      {
        using (var conn = new SqlConnection(_connectionStringOrigem))
        {
          conn.Open();
          var cmd = new SqlCommand(
              @"SELECT name FROM msdb.dbo.sysjobs 
                          ORDER BY name",
              conn);

          using (var reader = cmd.ExecuteReader())
          {
            while (reader.Read())
            {
              lista.Add(reader["name"].ToString());
            }
          }
        }
      }
      catch { }

      return lista;
    }

    public class MigrationInventory
    {
      public List<string> Databases { get; set; } = new List<string>();
      public List<string> LinkedServers { get; set; } = new List<string>();
      public List<string> Jobs { get; set; } = new List<string>();
    }
  }
}