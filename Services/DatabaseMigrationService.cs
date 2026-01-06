using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Data.SqlClient;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo;

namespace CQLE_MIGRACAO.Services
{
  public class DatabaseMigrationService
  {
    /// <summary>
    /// Migra Bancos de Dados usando metodologia Backup/Restore.
    /// </summary>
    /// <param name="connStringOrigem">Conexão Origem (SysAdmin)</param>
    /// <param name="connStringDestino">Conexão Destino (SysAdmin) - opcional se Offline</param>
    /// <param name="listaBancos">Lista de nomes dos bancos a migrar</param>
    /// <param name="isOnline">Se true, restaura no destino. Se false, apenas gera o .bak</param>
    /// <param name="pastaBackup">Pasta local ou de rede onde o .bak será salvo temporariamente</param>
    public void ProcessarMigracao(string connStringOrigem, string connStringDestino, List<string> listaBancos, bool isOnline, string pastaBackup)
    {
      Server servidorOrigem = GetSmoServer(connStringOrigem);
      Server? servidorDestino = null;

      if (isOnline && !string.IsNullOrEmpty(connStringDestino))
      {
        servidorDestino = GetSmoServer(connStringDestino);
      }

      // Garante que a pasta de backup existe
      if (!Directory.Exists(pastaBackup))
      {
        Directory.CreateDirectory(pastaBackup);
      }

      foreach (string dbName in listaBancos)
      {
        // Pula bancos de sistema
        if (IsSystemDatabase(dbName)) continue;

        string arquivoBackup = Path.Combine(pastaBackup, $"{dbName}_{DateTime.Now:yyyyMMddHHmm}.bak");

        try
        {
          // 1. REALIZAR BACKUP (Origem)
          Console.WriteLine($"Iniciando Backup de {dbName}...");

          Backup bkp = new Backup();
          bkp.Action = BackupActionType.Database;
          bkp.Database = dbName;
          bkp.Devices.AddDevice(arquivoBackup, DeviceType.File);
          bkp.Initialize = true; // Sobrescreve se o arquivo já existir
          bkp.Checksum = true;   // Garante integridade
          bkp.SqlBackup(servidorOrigem);

          Console.WriteLine($"Backup concluído: {arquivoBackup}");

          // 2. REALIZAR RESTORE (Destino - Apenas Online)
          if (isOnline && servidorDestino != null)
          {
            Console.WriteLine($"Iniciando Restore de {dbName} no destino...");

            // Verifica se banco já existe
            if (servidorDestino.Databases.Contains(dbName))
            {
              throw new Exception($"O banco {dbName} já existe no destino. Abortando para evitar sobrescrita acidental.");
            }

            Restore res = new Restore();
            res.Database = dbName;
            res.Action = RestoreActionType.Database;
            res.Devices.AddDevice(arquivoBackup, DeviceType.File);
            res.ReplaceDatabase = false; // Segurança: não substitui existente
            res.NoRecovery = false;

            // --- LÓGICA DE REALOCAÇÃO DE ARQUIVOS ---
            // É crucial ler o arquivo de backup para saber os nomes lógicos (Ex: 'Dados', 'Log')
            // e mapeá-los para a pasta padrão do servidor de DESTINO.

            System.Data.DataTable fileList = res.ReadFileList(servidorDestino);

            // Obtém diretórios padrão do destino (ou usa c:\temp se falhar)
            string dataPath = !string.IsNullOrEmpty(servidorDestino.Settings.DefaultFile)
                              ? servidorDestino.Settings.DefaultFile
                              : servidorDestino.MasterDBPath;

            string logPath = !string.IsNullOrEmpty(servidorDestino.Settings.DefaultLog)
                             ? servidorDestino.Settings.DefaultLog
                             : servidorDestino.MasterDBLogPath;

            foreach (System.Data.DataRow row in fileList.Rows)
            {
              string logicalName = row["LogicalName"].ToString()!;
              string type = row["Type"].ToString()!; // D = Data, L = Log

              // Cria o novo caminho físico
              string physicalName = (type == "L")
                  ? Path.Combine(logPath, $"{dbName}_Log.ldf")
                  : Path.Combine(dataPath, $"{dbName}.mdf");

              res.RelocateFiles.Add(new RelocateFile(logicalName, physicalName));
            }

            // Executa o Restore
            res.SqlRestore(servidorDestino);
            Console.WriteLine($"[SUCESSO] Banco {dbName} migrado com sucesso.");
          }
        }
        catch (Exception ex)
        {
          // Em caso de erro, apenas loga e continua para o próximo banco
          Console.WriteLine($"[ERRO] Falha ao migrar {dbName}: {ex.Message}");
          // Opcional: throw ex; se quiser parar tudo no primeiro erro
        }
      }
    }

    private bool IsSystemDatabase(string dbName)
    {
      return dbName.Equals("master", StringComparison.OrdinalIgnoreCase) ||
             dbName.Equals("model", StringComparison.OrdinalIgnoreCase) ||
             dbName.Equals("msdb", StringComparison.OrdinalIgnoreCase) ||
             dbName.Equals("tempdb", StringComparison.OrdinalIgnoreCase);
    }

    private Server GetSmoServer(string connectionString)
    {
      SqlConnection sqlConn = new SqlConnection(connectionString);
      ServerConnection serverConn = new ServerConnection(sqlConn);
      return new Server(serverConn);
    }
  }
}