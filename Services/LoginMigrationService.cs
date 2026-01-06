using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.Data.SqlClient;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo;

namespace CQLE_MIGRACAO.Services
{
  public class LoginMigrationService
  {
    /// <summary>
    /// Migra todos os Logins do servidor origem para o destino.
    /// Prioriza migração DIRETA. Gera scripts como backup opcional.
    /// </summary>
    public List<string> MigrarLogins(
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
        servidorOrigem.Logins.Refresh();
        int totalLogins = servidorOrigem.Logins.Count;
        int loginsProcessados = 0;

        logOperacoes.Add($"[INÍCIO] Migração de {totalLogins} Login(s) - {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        logOperacoes.Add(temDestino
            ? "[MODO] Migração DIRETA + backup opcional de scripts."
            : "[MODO] Apenas geração de scripts (destino não informado).");

        foreach (Login login in servidorOrigem.Logins)
        {
          // Pula logins de sistema
          if (login.IsSystemObject ||
              login.Name.StartsWith("NT AUTHORITY\\", StringComparison.OrdinalIgnoreCase) ||
              login.Name.StartsWith("NT SERVICE\\", StringComparison.OrdinalIgnoreCase) ||
              login.Name.Equals("sa", StringComparison.OrdinalIgnoreCase))
          {
            continue;
          }

          loginsProcessados++;
          string prefix = $"[{loginsProcessados}/{totalLogins}] Login: {login.Name}";

          try
          {
            // Opções MÍNIMAS e CORRETAS para versões recentes do SMO
            ScriptingOptions options = new ScriptingOptions
            {
              IncludeIfNotExists = true,   // IF NOT EXISTS
              LoginSid = true,             // Mantém SID original (crucial!)
              Permissions = true           // Permissões no servidor
                                           // NÃO use: DefaultDatabase, ScriptPassword, ScriptForCreate → não existem mais
            };

            var scriptCollection = login.Script(options).Cast<string>();

            StringBuilder scriptCompleto = new StringBuilder();

            // Cabeçalho
            scriptCompleto.AppendLine($"-- ==============================================");
            scriptCompleto.AppendLine($"-- CQLE MIGRAÇÃO - Login: {login.Name}");
            scriptCompleto.AppendLine($"-- Tipo: {(login.LoginType == LoginType.SqlLogin ? "SQL Login" : "Windows Login")}");
            scriptCompleto.AppendLine($"-- Origem: {servidorOrigem.Name}");
            scriptCompleto.AppendLine($"-- Data/Hora: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            scriptCompleto.AppendLine($"-- Database Padrão: {login.DefaultDatabase}");
            scriptCompleto.AppendLine($"-- Habilitado: {(login.IsDisabled ? "Não" : "Sim")}");
            scriptCompleto.AppendLine($"-- ==============================================");
            scriptCompleto.AppendLine();

            // Remove login existente
            scriptCompleto.AppendLine($"-- Remove login existente");
            scriptCompleto.AppendLine($"IF EXISTS (SELECT name FROM sys.server_principals WHERE name = N'{login.Name.Replace("'", "''")}')");
            scriptCompleto.AppendLine("BEGIN");
            scriptCompleto.AppendLine($"    DROP LOGIN [{login.Name.Replace("'", "''")}]");
            scriptCompleto.AppendLine("END");
            scriptCompleto.AppendLine("GO");
            scriptCompleto.AppendLine();

            // Scripts do SMO (já incluem hash da senha e database padrão automaticamente!)
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
                  SalvarScriptLogin(scriptCompleto.ToString(), login.Name, caminhoOutput);
              }
              catch (Exception ex)
              {
                logOperacoes.Add($"[ERRO] {prefix} → falha na criação direta: {ex.Message}");
                if (gerarScriptsBackup)
                {
                  SalvarScriptLogin(scriptCompleto.ToString(), login.Name, caminhoOutput);
                  logOperacoes.Add($"[BACKUP] Script salvo para execução manual.");
                }
              }
            }
            else if (gerarScriptsBackup)
            {
              SalvarScriptLogin(scriptCompleto.ToString(), login.Name, caminhoOutput);
              logOperacoes.Add($"[BACKUP] {prefix} → script gerado.");
            }
          }
          catch (Exception ex)
          {
            logOperacoes.Add($"[ERRO] {prefix} → {ex.Message}");
          }
        }

        logOperacoes.Add($"[FIM] Migração de Logins concluída - {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
      }
      catch (Exception ex)
      {
        logOperacoes.Add($"[ERRO CRÍTICO] Falha na migração de Logins: {ex.Message}");
      }

      // Salva log
      if (!string.IsNullOrEmpty(caminhoOutput) && logOperacoes.Count > 0)
      {
        Directory.CreateDirectory(caminhoOutput);
        string logFile = Path.Combine(caminhoOutput, $"Log_Logins_Migracao_{DateTime.Now:yyyyMMdd_HHmmss}.txt");
        File.WriteAllLines(logFile, logOperacoes, Encoding.UTF8);
        logOperacoes.Add($"[LOG] Relatório salvo em: {logFile}");
      }

      return logOperacoes;
    }

    private void SalvarScriptLogin(string script, string loginName, string caminhoOutput)
    {
      if (string.IsNullOrEmpty(caminhoOutput)) return;

      Directory.CreateDirectory(caminhoOutput);
      string nomeSeguro = RemoverCaracteresInvalidos(loginName);
      string arquivo = Path.Combine(caminhoOutput, $"Login_{nomeSeguro}.sql");
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

    public List<LoginInfo> ListarLogins(string connectionString)
    {
      var lista = new List<LoginInfo>();
      Server servidor = GetSmoServer(connectionString);
      servidor.Logins.Refresh();

      foreach (Login login in servidor.Logins)
      {
        if (login.IsSystemObject ||
            login.Name.StartsWith("NT AUTHORITY\\", StringComparison.OrdinalIgnoreCase) ||
            login.Name.StartsWith("NT SERVICE\\", StringComparison.OrdinalIgnoreCase) ||
            login.Name.Equals("sa", StringComparison.OrdinalIgnoreCase))
          continue;

        lista.Add(new LoginInfo
        {
          Name = login.Name,
          LoginType = login.LoginType,
          DefaultDatabase = login.DefaultDatabase,
          IsDisabled = login.IsDisabled,
          CreateDate = login.CreateDate,
          DateLastModified = login.DateLastModified
        });
      }

      return lista;
    }

    public class LoginInfo
    {
      public string Name { get; set; } = "";
      public LoginType LoginType { get; set; }
      public string DefaultDatabase { get; set; } = "";
      public bool IsDisabled { get; set; }
      public DateTime CreateDate { get; set; }
      public DateTime DateLastModified { get; set; }
    }
  }
}