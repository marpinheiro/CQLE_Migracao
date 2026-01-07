#nullable disable
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Data.SqlClient;

namespace CQLE_MIGRACAO.Forms
{
  public class UpdateProgressForm : Form
  {
    private RichTextBox txtLog;
    private ProgressBar pbGeral;
    private Label lblStatus;
    private Button btnConcluir;
    private Button btnSalvarLog;
    private Panel panelStatus;

    private readonly string _connOrigem;
    private readonly string _connDestino;
    private readonly List<string> _bancosOrigem;
    private readonly string _bancoDestino;
    private readonly string _pastaBackup;
    private readonly string _localMdf;
    private readonly string _localLdf;

    public UpdateProgressForm(string connOrigem, string connDestino, List<string> bancosOrigem,
                              string bancoDestino, string pastaBackup, string localMdf, string localLdf)
    {
      _connOrigem = connOrigem;
      _connDestino = connDestino;
      _bancosOrigem = bancosOrigem;
      _bancoDestino = bancoDestino;
      _pastaBackup = string.IsNullOrWhiteSpace(pastaBackup) ? @"C:\TempBackups" : pastaBackup;
      _localMdf = localMdf; // Vazio = usa padrão do SQL
      _localLdf = localLdf; // Vazio = usa padrão do SQL

      InitializeComponent();

      try
      {
        this.Icon = new Icon("CQLE.ico");
      }
      catch { }

      this.Shown += async (s, e) => await IniciarAtualizacao();
    }

    private void InitializeComponent()
    {
      this.Text = "CQLE Migração - Atualizando Base Teste";
      this.Size = new Size(950, 700);
      this.StartPosition = FormStartPosition.CenterScreen;
      this.FormBorderStyle = FormBorderStyle.FixedDialog;
      this.MaximizeBox = false;
      this.BackColor = Color.WhiteSmoke;

      panelStatus = new Panel
      {
        Location = new Point(0, 0),
        Size = new Size(950, 80),
        BackColor = Color.FromArgb(0, 150, 0)
      };

      lblStatus = new Label
      {
        Text = "Inicializando...",
        Location = new Point(20, 15),
        Size = new Size(900, 50),
        Font = new Font("Segoe UI", 12, FontStyle.Bold),
        ForeColor = Color.White
      };

      pbGeral = new ProgressBar
      {
        Location = new Point(20, 45),
        Size = new Size(910, 25),
        Style = ProgressBarStyle.Continuous
      };

      panelStatus.Controls.Add(lblStatus);
      panelStatus.Controls.Add(pbGeral);

      Label lblLogTitle = new Label
      {
        Text = "📋 Log da Atualização:",
        Location = new Point(20, 90),
        AutoSize = true,
        Font = new Font("Segoe UI", 10, FontStyle.Bold)
      };

      txtLog = new RichTextBox
      {
        Location = new Point(20, 115),
        Size = new Size(910, 480),
        ReadOnly = true,
        BackColor = Color.FromArgb(30, 30, 30),
        ForeColor = Color.LimeGreen,
        Font = new Font("Consolas", 9),
        ScrollBars = RichTextBoxScrollBars.Vertical
      };

      btnSalvarLog = new Button
      {
        Text = "💾 Salvar Log",
        Location = new Point(20, 610),
        Size = new Size(120, 40),
        FlatStyle = FlatStyle.Flat,
        BackColor = Color.SteelBlue,
        ForeColor = Color.White,
        Enabled = false,
        Cursor = Cursors.Hand
      };
      btnSalvarLog.FlatAppearance.BorderSize = 0;
      btnSalvarLog.Click += BtnSalvarLog_Click;

      btnConcluir = new Button
      {
        Text = "✓ Concluir",
        Location = new Point(810, 610),
        Size = new Size(120, 40),
        FlatStyle = FlatStyle.Flat,
        BackColor = Color.SeaGreen,
        ForeColor = Color.White,
        Enabled = false,
        Cursor = Cursors.Hand
      };
      btnConcluir.FlatAppearance.BorderSize = 0;
      btnConcluir.Click += (s, e) => this.Close();

      Label lblRodape = new Label
      {
        Text = "Desenvolvido por Marciano Silva - CQLE Softwares © 2025",
        Location = new Point(0, 660),
        Size = new Size(950, 15),
        TextAlign = ContentAlignment.MiddleCenter,
        Font = new Font("Segoe UI", 7),
        ForeColor = Color.Gray
      };

      this.Controls.Add(panelStatus);
      this.Controls.Add(lblLogTitle);
      this.Controls.Add(txtLog);
      this.Controls.Add(btnSalvarLog);
      this.Controls.Add(btnConcluir);
      this.Controls.Add(lblRodape);
    }

    private async Task IniciarAtualizacao()
    {
      AddLog("════════════════════════════════════════════════════");
      AddLog("      ATUALIZAÇÃO DE BASE TESTE INICIADA           ");
      AddLog("════════════════════════════════════════════════════");
      AddLog($"Banco destino: {_bancoDestino}");
      AddLog($"Bancos origem: {string.Join(", ", _bancosOrigem)}");
      AddLog("");

      // Cria pastas se não existirem
      try
      {
        if (!Directory.Exists(_pastaBackup))
          Directory.CreateDirectory(_pastaBackup);

        AddLog($"📁 Pasta de backup: {_pastaBackup}");

        if (!string.IsNullOrWhiteSpace(_localMdf))
          AddLog($"💾 Local MDF: {_localMdf}");
        else
          AddLog($"💾 Local MDF: (Padrão do SQL Server)");

        if (!string.IsNullOrWhiteSpace(_localLdf))
          AddLog($"📋 Local LDF: {_localLdf}");
        else
          AddLog($"📋 Local LDF: (Padrão do SQL Server)");

        AddLog("");
      }
      catch (Exception ex)
      {
        AddLog($"❌ Erro ao criar pastas: {ex.Message}");
        FinalizarComErro();
        return;
      }

      pbGeral.Maximum = _bancosOrigem.Count * 4 + 2; // Etapas por banco
      int passo = 0;

      try
      {
        // ETAPA 1: Backup do banco destino (se existir)
        lblStatus.Text = "Fazendo backup de segurança do destino...";
        passo++;
        pbGeral.Value = passo;

        await Task.Run(() =>
        {
          using (var connDestino = new SqlConnection(_connDestino))
          {
            connDestino.Open();

            // Verifica se o banco existe
            var cmdCheck = new SqlCommand($"SELECT database_id FROM sys.databases WHERE name = '{_bancoDestino}'", connDestino);
            var existe = cmdCheck.ExecuteScalar();

            if (existe != null)
            {
              AddLog($"⚠ Banco '{_bancoDestino}' já existe no destino");
              AddLog("📦 Criando backup de segurança...");

              string backupFile = Path.Combine(_pastaBackup, $"{_bancoDestino}_BACKUP_SEGURANCA_{DateTime.Now:yyyyMMdd_HHmmss}.bak");

              var cmdBackup = new SqlCommand(
                $@"BACKUP DATABASE [{_bancoDestino}] 
                   TO DISK = '{backupFile}' 
                   WITH FORMAT, INIT, COMPRESSION, STATS = 10",
                connDestino);
              cmdBackup.CommandTimeout = 600;
              cmdBackup.ExecuteNonQuery();

              AddLog($"✅ Backup de segurança salvo: {backupFile}");
              AddLog("");
            }
            else
            {
              AddLog($"ℹ Banco '{_bancoDestino}' não existe no destino (será criado)");
              AddLog("");
            }
          }
        });

        passo++;
        pbGeral.Value = passo;

        // ETAPA 2: Processar cada banco de origem
        foreach (var bancoOrigem in _bancosOrigem)
        {
          AddLog("────────────────────────────────────────────────────");
          AddLog($"📦 Processando banco: {bancoOrigem}");
          AddLog("────────────────────────────────────────────────────");

          lblStatus.Text = $"Processando {bancoOrigem}...";

          // 2.1: Backup do banco origem
          AddLog($"📤 [1/4] Fazendo backup de '{bancoOrigem}'...");
          passo++;
          pbGeral.Value = passo;

          string arquivoBackup = Path.Combine(_pastaBackup, $"{bancoOrigem}_TEMP_{DateTime.Now:yyyyMMdd_HHmmss}.bak");

          await Task.Run(() =>
          {
            using (var connOrigem = new SqlConnection(_connOrigem))
            {
              connOrigem.Open();

              var cmdBackup = new SqlCommand(
                $@"BACKUP DATABASE [{bancoOrigem}] 
                   TO DISK = '{arquivoBackup}' 
                   WITH FORMAT, INIT, COMPRESSION, STATS = 10",
                connOrigem);
              cmdBackup.CommandTimeout = 600;
              cmdBackup.ExecuteNonQuery();
            }
          });

          AddLog($"✅ Backup concluído: {arquivoBackup}");

          // 2.2: Drop do banco destino (se for substituir)
          AddLog($"🗑️ [2/4] Removendo banco destino '{_bancoDestino}' (se existir)...");
          passo++;
          pbGeral.Value = passo;

          await Task.Run(() =>
          {
            using (var connDestino = new SqlConnection(_connDestino))
            {
              connDestino.Open();

              var cmdDrop = new SqlCommand(
                $@"IF DB_ID('{_bancoDestino}') IS NOT NULL
                   BEGIN
                     ALTER DATABASE [{_bancoDestino}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
                     DROP DATABASE [{_bancoDestino}];
                   END",
                connDestino);
              cmdDrop.CommandTimeout = 120;
              cmdDrop.ExecuteNonQuery();
            }
          });

          AddLog($"✅ Banco '{_bancoDestino}' removido");

          // 2.3: Obter estrutura de arquivos do backup
          AddLog($"🔍 [3/4] Analisando estrutura do backup...");
          passo++;
          pbGeral.Value = passo;

          List<(string LogicalName, string Type)> arquivos = new List<(string, string)>();

          await Task.Run(() =>
          {
            using (var connDestino = new SqlConnection(_connDestino))
            {
              connDestino.Open();

              var cmdFileList = new SqlCommand($"RESTORE FILELISTONLY FROM DISK = '{arquivoBackup}'", connDestino);
              using (var reader = cmdFileList.ExecuteReader())
              {
                while (reader.Read())
                {
                  string logicalName = reader["LogicalName"].ToString();
                  string type = reader["Type"].ToString();
                  arquivos.Add((logicalName, type));
                }
              }
            }
          });

          AddLog($"✅ {arquivos.Count} arquivo(s) encontrado(s)");

          // 2.4: Restore no destino com novo nome
          AddLog($"📥 [4/4] Restaurando como '{_bancoDestino}'...");
          passo++;
          pbGeral.Value = passo;

          await Task.Run(() =>
          {
            using (var connDestino = new SqlConnection(_connDestino))
            {
              connDestino.Open();

              // Obter caminhos padrão do SQL Server ou usar os especificados
              string dataPath;
              string logPath;

              if (string.IsNullOrWhiteSpace(_localMdf))
              {
                var cmdDataPath = new SqlCommand("SELECT SERVERPROPERTY('InstanceDefaultDataPath')", connDestino);
                dataPath = cmdDataPath.ExecuteScalar()?.ToString() ?? @"C:\Program Files\Microsoft SQL Server\MSSQL\DATA";
              }
              else
              {
                dataPath = _localMdf;
              }

              if (string.IsNullOrWhiteSpace(_localLdf))
              {
                var cmdLogPath = new SqlCommand("SELECT SERVERPROPERTY('InstanceDefaultLogPath')", connDestino);
                logPath = cmdLogPath.ExecuteScalar()?.ToString() ?? dataPath;
              }
              else
              {
                logPath = _localLdf;
              }

              AddLog($"   📂 Pasta MDF: {dataPath}");
              AddLog($"   📂 Pasta LDF: {logPath}");

              // Montar comando RESTORE com MOVE
              var moveClauses = new List<string>();
              foreach (var (logicalName, type) in arquivos)
              {
                string targetPath = type == "L" ? logPath : dataPath;
                string extension = type == "L" ? ".ldf" : ".mdf";
                string physicalFile = Path.Combine(targetPath, $"{_bancoDestino}_{logicalName}{extension}");
                moveClauses.Add($"MOVE '{logicalName}' TO '{physicalFile}'");
              }

              string sqlRestore = $@"
                RESTORE DATABASE [{_bancoDestino}] 
                FROM DISK = '{arquivoBackup}' 
                WITH RECOVERY, REPLACE, STATS = 10,
                {string.Join(",\n", moveClauses)}";

              var cmdRestore = new SqlCommand(sqlRestore, connDestino);
              cmdRestore.CommandTimeout = 600;
              cmdRestore.ExecuteNonQuery();
            }
          });

          AddLog($"✅ Restore concluído com sucesso!");
          AddLog($"✅ Banco '{bancoOrigem}' atualizado como '{_bancoDestino}'");
          AddLog("");

          // Limpa arquivo de backup temporário
          try
          {
            File.Delete(arquivoBackup);
            AddLog($"🗑️ Arquivo temporário removido: {arquivoBackup}");
          }
          catch { }
        }







        // ETAPA 3: Verificação final
        AddLog("════════════════════════════════════════════════════");
        AddLog("🔍 Verificando integridade do banco atualizado...");
        AddLog("════════════════════════════════════════════════════");

        await Task.Run(() =>
        {
          using (var connDestino = new SqlConnection(_connDestino))
          {
            connDestino.Open();

            //Garante contexto master
            using (var cmdUse = new SqlCommand("Use master;", connDestino))
              cmdUse.ExecuteNonQuery();

            // Verifica se o banco existe
            using (var cmdExists = new SqlCommand(
              "SELECT COUNT(*) FROM sys.databases WHERE name = @db",
              connDestino))
            {
              cmdExists.Parameters.AddWithValue("@db", _bancoDestino);

              int existe = (int)cmdExists.ExecuteScalar();

              if (existe == 1)
              {
                using (var cmdCheck = new SqlCommand(
                  $"DBCC CHECKDB([{_bancoDestino}]) WITH NO_INFOMSGS",
                  connDestino))
                {
                  cmdCheck.CommandTimeout = 0; // DBCC pode demorar
                  cmdCheck.ExecuteNonQuery();
                }
              }
              else
              {
                AddLog($"DBCC CHECKDB ignorado: banco {_bancoDestino} não existe.");
              }
            }
          }
        });

        AddLog("✅ Verificação de integridade concluída sem erros");
        AddLog("");

        // Sucesso
        pbGeral.Value = pbGeral.Maximum;
        lblStatus.Text = "✅ Atualização Concluída com Sucesso!";
        panelStatus.BackColor = Color.SeaGreen;

        AddLog("════════════════════════════════════════════════════");
        AddLog("      ATUALIZAÇÃO CONCLUÍDA COM SUCESSO            ");
        AddLog("════════════════════════════════════════════════════");
        AddLog("");
        AddLog($"📊 Resumo:");
        AddLog($"   • Bancos processados: {_bancosOrigem.Count}");
        AddLog($"   • Banco destino: {_bancoDestino}");
        AddLog($"   • Backups salvos em: {_pastaBackup}");
        AddLog("");
        AddLog("⚠ IMPORTANTE:");
        AddLog("   • Valide os dados no banco atualizado");
        AddLog("   • Teste as aplicações conectadas");
        AddLog("   • Mantenha os backups de segurança");

        FinalizarComSucesso();
      }
      catch (Exception ex)
      {
        AddLog("");
        AddLog("════════════════════════════════════════════════════");
        AddLog("              ERRO CRÍTICO                         ");
        AddLog("════════════════════════════════════════════════════");
        AddLog($"Mensagem: {ex.Message}");
        AddLog($"Stack: {ex.StackTrace}");

        FinalizarComErro();

        MessageBox.Show(
          $"❌ Erro durante a atualização:\n\n{ex.Message}\n\n" +
          "Verifique o log para mais detalhes.\n\n" +
          "Os backups de segurança foram mantidos.",
          "Erro",
          MessageBoxButtons.OK,
          MessageBoxIcon.Error
        );
      }
    }

    private void FinalizarComSucesso()
    {
      btnSalvarLog.Enabled = true;
      btnConcluir.Enabled = true;

      MessageBox.Show(
        "✅ Atualização concluída com sucesso!\n\n" +
        $"Banco destino: {_bancoDestino}\n" +
        $"Bancos processados: {_bancosOrigem.Count}\n\n" +
        "Valide os dados antes de usar em produção.",
        "Sucesso",
        MessageBoxButtons.OK,
        MessageBoxIcon.Information
      );
    }

    private void FinalizarComErro()
    {
      lblStatus.Text = "❌ Erro na Atualização";
      panelStatus.BackColor = Color.DarkRed;
      btnSalvarLog.Enabled = true;
      btnConcluir.Enabled = true;
    }

    private void BtnSalvarLog_Click(object sender, EventArgs e)
    {
      try
      {
        string pastaLog = Path.Combine(_pastaBackup, "Logs");
        if (!Directory.Exists(pastaLog))
          Directory.CreateDirectory(pastaLog);

        string logFile = Path.Combine(pastaLog, $"Log_Atualizacao_{DateTime.Now:yyyyMMdd_HHmmss}.txt");
        File.WriteAllText(logFile, txtLog.Text);

        MessageBox.Show(
          $"✅ Log salvo com sucesso!\n\n{logFile}",
          "Sucesso",
          MessageBoxButtons.OK,
          MessageBoxIcon.Information
        );
      }
      catch (Exception ex)
      {
        MessageBox.Show(
          $"❌ Erro ao salvar log:\n\n{ex.Message}",
          "Erro",
          MessageBoxButtons.OK,
          MessageBoxIcon.Error
        );
      }
    }

    private void AddLog(string msg)
    {
      if (txtLog.InvokeRequired)
      {
        txtLog.Invoke(new Action(() => AddLog(msg)));
      }
      else
      {
        txtLog.AppendText($"[{DateTime.Now:HH:mm:ss}] {msg}{Environment.NewLine}");
        txtLog.SelectionStart = txtLog.Text.Length;
        txtLog.ScrollToCaret();
      }
    }
  }
}