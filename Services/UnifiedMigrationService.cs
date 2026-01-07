#nullable disable
using System;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;

namespace CQLE_MIGRACAO.Services
{
  public class UnifiedMigrationService
  {
    public string ConnectionStringOrigem => _connectionStringOrigem;
    private readonly string _connectionStringOrigem;
    private readonly MigrationEngine _databaseEngine;
    private readonly LinkedServerMigrationService _linkedServerService;
    private readonly JobMigrationService _jobService;
    private readonly LoginMigrationService _loginService;

    public UnifiedMigrationService(string connectionStringOrigem)
    {
      _connectionStringOrigem = connectionStringOrigem;
      _databaseEngine = new MigrationEngine(connectionStringOrigem);
      _linkedServerService = new LinkedServerMigrationService();
      _jobService = new JobMigrationService();
      _loginService = new LoginMigrationService();
    }

    public class MigrationConfig
    {
      public List<string> DatabaseNames { get; set; } = new List<string>();
      public bool IncludeJobs { get; set; } = true;
      public bool IncludeLinkedServers { get; set; } = true;
      public bool IncludeLogins { get; set; } = true;
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
      log($"Servidor Destino: {config.ServerDestino}");
      log("");

      int totalOperacoes = 0;
      if (config.IncludeLogins) totalOperacoes++;
      if (config.DatabaseNames.Count > 0) totalOperacoes += config.DatabaseNames.Count;
      if (config.IncludeLinkedServers) totalOperacoes++;
      if (config.IncludeJobs) totalOperacoes++;

      int operacaoAtual = 0;

      try
      {
        // FASE 1: LOGINS (PRIMEIRO!)
        if (config.IncludeLogins)
        {
          operacaoAtual++;
          log("┌─────────────────────────────────────────────────┐");
          log("│  FASE 1: MIGRAÇÃO E HABILITAÇÃO DE LOGINS      │");
          log("└─────────────────────────────────────────────────┘");

          if (string.IsNullOrWhiteSpace(config.ServerDestino))
          {
            log("⚠ Servidor destino não informado → Logins não migrados.");
          }
          else
          {
            log($"[{operacaoAtual}/{totalOperacoes}] Migrando e habilitando logins...");

            try
            {
              string connStringDestino = $"Server={config.ServerDestino};Database=master;Trusted_Connection=True;TrustServerCertificate=True;";
              bool gerarBackup = !string.IsNullOrEmpty(config.OutputPath);

              List<string> logLogins = _loginService.MigrarLogins(
                  connStringOrigem: _connectionStringOrigem,
                  connStringDestino: connStringDestino,
                  gerarScriptsBackup: gerarBackup,
                  caminhoOutput: config.OutputPath
              );

              foreach (var linha in logLogins)
                log(linha);

              // Habilita todos os logins migrados
              HabilitarTodosLogins(config.ServerDestino, log);

              log("✅ Todos os logins migrados e habilitados com sucesso.");
              log("");
            }
            catch (Exception ex)
            {
              log($"❌ ERRO na migração de Logins: {ex.Message}");
              log("");
            }
          }
        }

        // FASE 2: BANCOS DE DADOS
        if (config.DatabaseNames.Count > 0)
        {
          log("┌─────────────────────────────────────────────────┐");
          log("│  FASE 2: MIGRAÇÃO DE BANCOS DE DADOS            │");
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
                  true,
                  config.PastaBackup,
                  (msg) => log($"    {msg}")
              );

              log($"✅ Banco '{banco}' migrado com sucesso");

              // Correção de usuários órfãos
              if (!string.IsNullOrWhiteSpace(config.ServerDestino))
              {
                try
                {
                  CorrigirUsuariosOrfaos(config.ServerDestino, banco, log);
                  log($"    🛠 Usuários órfãos corrigidos em '{banco}'");
                }
                catch (Exception ex)
                {
                  log($"    ⚠ Falha ao corrigir órfãos em '{banco}': {ex.Message}");
                }
              }

              log("");
            }
            catch (Exception ex)
            {
              log($"❌ ERRO ao migrar '{banco}': {ex.Message}");
              log("");
            }
          }
        }

        // FASE 3: LINKED SERVERS
        if (config.IncludeLinkedServers)
        {
          operacaoAtual++;
          log("┌─────────────────────────────────────────────────┐");
          log("│  FASE 3: MIGRAÇÃO DE LINKED SERVERS            │");
          log("└─────────────────────────────────────────────────┘");

          if (string.IsNullOrWhiteSpace(config.ServerDestino))
          {
            log("⚠ Servidor destino não informado → Linked Servers não migrados.");
          }
          else
          {
            try
            {
              string connDestino = $"Server={config.ServerDestino};Database=master;Trusted_Connection=True;TrustServerCertificate=True;";

              _linkedServerService.ProcessarMigracao(
                  _connectionStringOrigem,
                  connDestino,
                  true,
                  config.OutputPath
              );

              log("✅ Linked Servers migrados com sucesso");
              log("");
            }
            catch (Exception ex)
            {
              log($"❌ ERRO ao migrar Linked Servers: {ex.Message}");
              log("");
            }
          }
        }

        // FASE 4: JOBS
        if (config.IncludeJobs)
        {
          operacaoAtual++;
          log("┌─────────────────────────────────────────────────┐");
          log("│  FASE 4: MIGRAÇÃO DIRETA DE SQL AGENT JOBS     │");
          log("└─────────────────────────────────────────────────┘");

          if (string.IsNullOrWhiteSpace(config.ServerDestino))
          {
            log("⚠ Servidor destino não informado → Jobs não migrados.");
          }
          else
          {
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
                log(linha);

              log("✅ Jobs migrados com sucesso");
              log("");
            }
            catch (Exception ex)
            {
              log($"❌ ERRO na migração de Jobs: {ex.Message}");
              log("");
            }
          }
        }

        log("╔════════════════════════════════════════════════════╗");
        log("║           MIGRAÇÃO CONCLUÍDA COM SUCESSO          ║");
        log("╚════════════════════════════════════════════════════╝");
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

    // Habilita todos os logins no destino (exceto sa e built-in)
    private void HabilitarTodosLogins(string servidorDestino, Action<string> log)
    {
      string connStr = $"Server={servidorDestino};Database=master;Trusted_Connection=True;TrustServerCertificate=True;";

      try
      {
        using var conn = new SqlConnection(connStr);
        conn.Open();

        string script = @"
                    DECLARE @LoginName nvarchar(128)
                    DECLARE cur CURSOR FOR
                        SELECT name FROM sys.server_principals
                        WHERE type IN ('S', 'U', 'G')
                          AND is_disabled = 1
                          AND name NOT LIKE 'NT %'
                          AND name NOT LIKE '##%'
                          AND name <> 'sa'

                    OPEN cur
                    FETCH NEXT FROM cur INTO @LoginName
                    WHILE @@FETCH_STATUS = 0
                    BEGIN
                        EXEC('ALTER LOGIN [' + @LoginName + '] ENABLE')
                        FETCH NEXT FROM cur INTO @LoginName
                    END
                    CLOSE cur
                    DEALLOCATE cur";

        using var cmd = new SqlCommand(script, conn);
        int afetados = cmd.ExecuteNonQuery();
        log($"    🔓 {afetados} login(s) desabilitado(s) foram habilitados.");
      }
      catch (Exception ex)
      {
        log($"    ⚠ Falha ao habilitar logins: {ex.Message}");
      }
    }

    private void CorrigirUsuariosOrfaos(string servidorDestino, string databaseName, Action<string> log)
    {
      var bancosSistema = new[] { "distribution", "ReportServer", "ReportServerTempDB", "SSISDB" };
      if (Array.Exists(bancosSistema, b => databaseName.Equals(b, StringComparison.OrdinalIgnoreCase)))
      {
        log($"    ℹ Banco '{databaseName}' é de sistema — correção de órfãos ignorada.");
        return;
      }

      string connStr = $"Server={servidorDestino};Database={databaseName};Trusted_Connection=True;TrustServerCertificate=True;";

      try
      {
        using var conn = new SqlConnection(connStr);
        conn.Open();

        string script = @"
                    DECLARE @UserName nvarchar(128)
                    DECLARE cur CURSOR FOR
                        SELECT name FROM sys.database_principals
                        WHERE type IN ('S', 'U', 'G')
                          AND authentication_type_desc = 'INSTANCE'
                          AND principal_id > 4
                          AND name NOT IN ('dbo', 'guest')
                          AND SUSER_SNAME(sid) IS NULL

                    OPEN cur
                    FETCH NEXT FROM cur INTO @UserName
                    WHILE @@FETCH_STATUS = 0
                    BEGIN
                        EXEC sp_change_users_login 'Auto_Fix', @UserName
                        FETCH NEXT FROM cur INTO @UserName
                    END
                    CLOSE cur
                    DEALLOCATE cur";

        using var cmd = new SqlCommand(script, conn);
        cmd.CommandTimeout = 300;
        cmd.ExecuteNonQuery();
      }
      catch (Exception ex)
      {
        log($"    ⚠ Falha ao corrigir órfãos em '{databaseName}': {ex.Message}");
      }
    }

    // ... (GetInventario, ListarLinkedServersNomes, ListarJobsNomes, MigrationInventory permanecem iguais)
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
        using var conn = new SqlConnection(_connectionStringOrigem);
        conn.Open();
        var cmd = new SqlCommand(
            @"SELECT name FROM sys.servers 
                      WHERE is_linked = 1 
                      AND name <> @@SERVERNAME
                      ORDER BY name",
            conn);

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
          lista.Add(reader["name"].ToString());
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
        using var conn = new SqlConnection(_connectionStringOrigem);
        conn.Open();
        var cmd = new SqlCommand(
            @"SELECT name FROM msdb.dbo.sysjobs 
                      ORDER BY name",
            conn);

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
          lista.Add(reader["name"].ToString());
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