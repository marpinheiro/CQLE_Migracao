#nullable disable
using System;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;
using System.IO;
using System.Linq;

namespace CQLE_MIGRACAO.Services
{
  public class MigrationEngine
  {
    private string _connectionStringOrigem;

    public MigrationEngine()
    {
      _connectionStringOrigem = "Data Source=localhost;Integrated Security=True;TrustServerCertificate=True";
    }

    public MigrationEngine(string connectionString)
    {
      _connectionStringOrigem = connectionString;
    }

    public List<string> ListarBancosDeDados()
    {
      var lista = new List<string>();
      try
      {
        using (var conn = new SqlConnection(_connectionStringOrigem))
        {
          conn.Open();
          string sql = "SELECT name FROM sys.databases WHERE name NOT IN ('master', 'tempdb', 'model', 'msdb') ORDER BY name";
          using (var cmd = new SqlCommand(sql, conn))
          using (var reader = cmd.ExecuteReader())
          {
            while (reader.Read()) lista.Add(reader["name"].ToString());
          }
        }
      }
      catch (Exception ex) { throw new Exception($"Erro ao listar bancos: {ex.Message}"); }
      return lista;
    }

    // --- MIGRAÇÃO DE BANCOS (COM CORREÇÃO DE PATH) ---
    public void ExecutarMigracaoAutomatizada(string banco, string servidorDestino, bool isOnline, string pastaBackup, Action<string> log)
    {
      if (string.IsNullOrEmpty(pastaBackup)) throw new Exception("Pasta de backup não informada.");

      string arquivoBackup = Path.Combine(pastaBackup, $"{banco}.bak");

      string connOrigem = _connectionStringOrigem;
      if (!connOrigem.Contains("TrustServerCertificate", StringComparison.OrdinalIgnoreCase)) connOrigem += ";TrustServerCertificate=True";
      string connDestino = $"Data Source={servidorDestino};Integrated Security=True;TrustServerCertificate=True";

      // 1. BACKUP
      log($"[ORIGEM] Iniciando Backup de {banco}...");
      try
      {
        using (var conn = new SqlConnection(connOrigem))
        {
          conn.Open();
          string sqlBackup = $@"BACKUP DATABASE [{banco}] TO DISK = '{arquivoBackup}' WITH FORMAT, INIT, COMPRESSION, STATS = 10";
          using (var cmd = new SqlCommand(sqlBackup, conn)) { cmd.CommandTimeout = 600; cmd.ExecuteNonQuery(); }
        }
        log("Backup concluído.");
      }
      catch (Exception ex) { throw new Exception($"Erro Backup: {ex.Message}"); }

      // 2. RESTORE COM MOVE
      log($"[DESTINO] Conectando em {servidorDestino}...");
      try
      {
        using (var conn = new SqlConnection(connDestino))
        {
          conn.Open();
          string dataPath = GetDefaultPath(conn, "InstanceDefaultDataPath");
          string logPath = GetDefaultPath(conn, "InstanceDefaultLogPath");

          var fileMoves = new List<string>();
          string sqlFileList = $"RESTORE FILELISTONLY FROM DISK = '{arquivoBackup}'";

          using (var cmd = new SqlCommand(sqlFileList, conn))
          using (var reader = cmd.ExecuteReader())
          {
            while (reader.Read())
            {
              string logicalName = reader["LogicalName"].ToString();
              string type = reader["Type"].ToString();
              string targetFolder = type.ToUpper() == "L" ? logPath : dataPath;
              string ext = type.ToUpper() == "L" ? ".ldf" : ".mdf";
              string physicalName = Path.Combine(targetFolder, $"{banco}_{logicalName}{ext}");
              fileMoves.Add($"MOVE '{logicalName}' TO '{physicalName}'");
            }
          }

          string sqlKill = $"IF DB_ID('{banco}') IS NOT NULL BEGIN ALTER DATABASE [{banco}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [{banco}]; END";
          using (var cmd = new SqlCommand(sqlKill, conn)) cmd.ExecuteNonQuery();

          log("Restaurando arquivos...");
          string sqlRestore = $@"RESTORE DATABASE [{banco}] FROM DISK = '{arquivoBackup}' WITH RECOVERY, REPLACE, STATS = 10, {string.Join(", ", fileMoves)}";

          using (var cmd = new SqlCommand(sqlRestore, conn)) { cmd.CommandTimeout = 600; cmd.ExecuteNonQuery(); }
        }
        log("Restore finalizado.");
      }
      catch (Exception ex) { throw new Exception($"Erro Restore: {ex.Message}"); }

      try { if (File.Exists(arquivoBackup)) File.Delete(arquivoBackup); } catch { }
    }

    // --- NOVA FUNÇÃO MELHORADA: MIGRAR LINKED SERVERS ---
    public class LinkedServerInfo
    {
      public string Name { get; set; }
      public string Product { get; set; }
      public string Provider { get; set; }
      public string DataSource { get; set; }
      public string Catalog { get; set; }
      public bool UseSelfCredential { get; set; }
      public string RemoteUser { get; set; }
      public List<string> LocalLogins { get; set; } = new List<string>();
    }

    public List<LinkedServerInfo> ListarLinkedServers()
    {
      var lista = new List<LinkedServerInfo>();
      string connOrigem = _connectionStringOrigem;
      if (!connOrigem.Contains("TrustServerCertificate")) connOrigem += ";TrustServerCertificate=True";

      using (var conn = new SqlConnection(connOrigem))
      {
        conn.Open();

        // Busca os Linked Servers
        string sql = @"
                    SELECT s.name, s.product, s.provider, s.data_source, s.catalog,
                           ISNULL(l.uses_self_credential, 0) as uses_self_credential,
                           l.remote_name
                    FROM sys.servers s
                    LEFT JOIN sys.linked_logins l ON s.server_id = l.server_id AND l.local_principal_id = 0
                    WHERE s.is_linked = 1 AND s.name <> @@SERVERNAME
                    ORDER BY s.name";

        using (var cmd = new SqlCommand(sql, conn))
        using (var reader = cmd.ExecuteReader())
        {
          while (reader.Read())
          {
            lista.Add(new LinkedServerInfo
            {
              Name = reader["name"].ToString(),
              Product = reader["product"].ToString(),
              Provider = reader["provider"].ToString(),
              DataSource = reader["data_source"].ToString(),
              Catalog = reader["catalog"]?.ToString() ?? "",
              UseSelfCredential = Convert.ToBoolean(reader["uses_self_credential"]),
              RemoteUser = reader["remote_name"]?.ToString() ?? ""
            });
          }
        }
      }

      return lista;
    }

    public void MigrarLinkedServers(string servidorDestino, List<LinkedServerInfo> servidores,
                                    bool usarIntegratedSecurity, string usuarioPadrao, string senhaPadrao,
                                    Action<string> log)
    {
      log(">>> Iniciando Migração de Linked Servers...");

      string connDestino = $"Data Source={servidorDestino};Integrated Security=True;TrustServerCertificate=True";

      using (var connDest = new SqlConnection(connDestino))
      {
        connDest.Open();

        // Valida providers disponíveis
        var providersDisponiveis = new List<string>();
        using (var cmd = new SqlCommand("SELECT name FROM sys.servers WHERE is_linked = 0", connDest))
        using (var reader = cmd.ExecuteReader())
        {
          while (reader.Read()) providersDisponiveis.Add(reader["name"].ToString());
        }

        foreach (var srv in servidores)
        {
          try
          {
            log($"Processando Linked Server: [{srv.Name}]...");

            // Remove se já existir
            string sqlDrop = $"IF EXISTS (SELECT * FROM sys.servers WHERE name = '{srv.Name}') " +
                           $"EXEC master.dbo.sp_dropserver @server=N'{srv.Name}', @droplogins='droplogins'";
            using (var cmd = new SqlCommand(sqlDrop, connDest))
            {
              cmd.CommandTimeout = 30;
              cmd.ExecuteNonQuery();
            }

            // Cria o Linked Server
            string sqlCreate = $@"
                            EXEC master.dbo.sp_addlinkedserver 
                                @server = N'{srv.Name}', 
                                @srvproduct = N'{srv.Product}', 
                                @provider = N'{srv.Provider}', 
                                @datasrc = N'{srv.DataSource}'";

            if (!string.IsNullOrEmpty(srv.Catalog))
              sqlCreate += $", @catalog = N'{srv.Catalog}'";

            using (var cmd = new SqlCommand(sqlCreate, connDest))
            {
              cmd.CommandTimeout = 30;
              cmd.ExecuteNonQuery();
            }

            // Configurações Padrão (RPC, etc)
            string sqlOptions = $@"
                            EXEC master.dbo.sp_serveroption @server=N'{srv.Name}', @optname=N'rpc', @optvalue=N'true';
                            EXEC master.dbo.sp_serveroption @server=N'{srv.Name}', @optname=N'rpc out', @optvalue=N'true';
                            EXEC master.dbo.sp_serveroption @server=N'{srv.Name}', @optname=N'data access', @optvalue=N'true';";
            using (var cmd = new SqlCommand(sqlOptions, connDest))
            {
              cmd.CommandTimeout = 30;
              cmd.ExecuteNonQuery();
            }

            // Configura Login
            if (usarIntegratedSecurity || srv.UseSelfCredential)
            {
              // Usa credenciais do Windows
              string sqlLogin = $@"
                                EXEC master.dbo.sp_addlinkedsrvlogin 
                                    @rmtsrvname = N'{srv.Name}',
                                    @useself = N'True',
                                    @locallogin = NULL";
              using (var cmd = new SqlCommand(sqlLogin, connDest))
              {
                cmd.CommandTimeout = 30;
                cmd.ExecuteNonQuery();
              }
              log($"   ✓ Configurado com Integrated Security");
            }
            else
            {
              // Usa SQL Authentication
              string remoteUser = string.IsNullOrEmpty(srv.RemoteUser) ? usuarioPadrao : srv.RemoteUser;

              string sqlLogin = $@"
                                EXEC master.dbo.sp_addlinkedsrvlogin 
                                    @rmtsrvname = N'{srv.Name}',
                                    @useself = N'False',
                                    @locallogin = NULL,
                                    @rmtuser = N'{remoteUser}',
                                    @rmtpassword = N'{senhaPadrao}'";
              using (var cmd = new SqlCommand(sqlLogin, connDest))
              {
                cmd.CommandTimeout = 30;
                cmd.ExecuteNonQuery();
              }
              log($"   ✓ Configurado com SQL Auth (usuário: {remoteUser})");
              log($"   ⚠ ATENÇÃO: Senha definida como padrão. Revisar manualmente!");
            }

            log($"   ✅ [{srv.Name}] criado com sucesso");
          }
          catch (Exception ex)
          {
            log($"   ❌ Erro ao criar [{srv.Name}]: {ex.Message}");
          }
        }
      }

      log("=== Fim da migração de Linked Servers ===");
      log("⚠ IMPORTANTE: Valide as credenciais dos Linked Servers no servidor destino!");
    }

    private string GetDefaultPath(SqlConnection conn, string property)
    {
      using (var cmd = new SqlCommand($"SELECT SERVERPROPERTY('{property}')", conn))
      {
        object result = cmd.ExecuteScalar();
        if (result != null && result != DBNull.Value) return result.ToString();

        string fallback = property.Contains("Log") ? "ldf" : "mdf";
        using (var cmd2 = new SqlCommand($"SELECT TOP 1 physical_name FROM sys.master_files WHERE type_desc = 'ROWS'", conn))
        {
          return Path.GetDirectoryName(cmd2.ExecuteScalar().ToString());
        }
      }
    }
  }
}