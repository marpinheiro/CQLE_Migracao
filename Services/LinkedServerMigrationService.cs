using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Microsoft.Data.SqlClient;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo;
using System.Collections.Specialized; // Necessário para StringCollection

namespace CQLE_MIGRACAO.Services
{
  public class LinkedServerMigrationService
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
        // Refresh garante que a lista de objetos está atualizada
        servidorOrigem.LinkedServers.Refresh();

        foreach (LinkedServer ls in servidorOrigem.LinkedServers)
        {
          // CORREÇÃO: Removemos ls.IsSystemObject que não existe.
          // Filtramos apenas se o nome for igual ao do servidor (referência local)
          if (ls.Name.Equals(servidorOrigem.Name, StringComparison.OrdinalIgnoreCase))
            continue;

          ScriptingOptions options = new ScriptingOptions
          {
            ScriptDrops = false,
            IncludeIfNotExists = true,
            ScriptSchema = true,
            WithDependencies = true
          };

          StringCollection scriptCollection = ls.Script(options);
          StringBuilder scriptCompleto = new StringBuilder();

          scriptCompleto.AppendLine($"-- Migração Linked Server: {ls.Name}");
          scriptCompleto.AppendLine($"-- Data: {DateTime.Now}");
          scriptCompleto.AppendLine("USE [master]");
          scriptCompleto.AppendLine("GO");

          // CORREÇÃO: Tratamento de nulos no loop
          foreach (string? line in scriptCollection)
          {
            if (!string.IsNullOrEmpty(line))
            {
              scriptCompleto.AppendLine(line);
              scriptCompleto.AppendLine("GO");
            }
          }

          if (isOnline && servidorDestino != null)
          {
            try
            {
              if (servidorDestino.LinkedServers.Contains(ls.Name))
              {
                logOperacoes.Add($"[AVISO] Linked Server '{ls.Name}' já existe no destino. Ignorado.");
              }
              else
              {
                servidorDestino.ConnectionContext.ExecuteNonQuery(scriptCompleto.ToString());
                logOperacoes.Add($"[SUCESSO] Linked Server '{ls.Name}' criado no destino.");
              }
            }
            catch (Exception ex)
            {
              logOperacoes.Add($"[ERRO] Falha ao criar '{ls.Name}' online: {ex.Message}");
            }
          }
          else
          {
            if (!Directory.Exists(caminhoOutput))
              Directory.CreateDirectory(caminhoOutput);

            string fileName = Path.Combine(caminhoOutput, $"LinkedServer_{ls.Name}.sql");
            File.WriteAllText(fileName, scriptCompleto.ToString());
            logOperacoes.Add($"[OFFLINE] Script gerado: {fileName}");
          }
        }
      }
      catch (Exception ex)
      {
        throw new Exception($"Erro durante a migração de Linked Servers: {ex.Message}");
      }
    }

    private Server GetSmoServer(string connectionString)
    {
      SqlConnection sqlConn = new SqlConnection(connectionString);
      ServerConnection serverConn = new ServerConnection(sqlConn);
      return new Server(serverConn);
    }
  }
}