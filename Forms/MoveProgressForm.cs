#nullable disable
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Data.SqlClient;

namespace CQLE_MIGRACAO.Forms
{
  public class ArquivoParaMover
  {
    public string LogicalName { get; set; }
    public string CaminhoOrigem { get; set; }
    public string CaminhoDestino { get; set; }
    public string Tipo { get; set; }
  }

  public class MoveProgressForm : Form
  {
    private RichTextBox txtLog;
    private ProgressBar progressBar;
    private Button btnFechar;
    private Button btnSalvarLog;
    private Label lblStatus;

    private readonly string _connectionString;
    private readonly string _dbName;
    private readonly List<ArquivoParaMover> _arquivos;

    public MoveProgressForm(string connectionString, string dbName, List<ArquivoParaMover> arquivos)
    {
      _connectionString = connectionString;
      _dbName = dbName;
      _arquivos = arquivos;

      InitializeComponent();
      // O ícone agora é carregado dentro do InitializeComponent para manter o padrão
      this.Load += (s, e) => IniciarProcessoAssincrono();
    }

    private void InitializeComponent()
    {
      this.Text = $"CQLE Migração - Movendo: {_dbName}";

      // === AJUSTE DE TAMANHO ===
      this.Size = new Size(800, 600);
      this.StartPosition = FormStartPosition.CenterScreen;
      this.FormBorderStyle = FormBorderStyle.Sizable;
      this.MinimumSize = new Size(600, 500);
      this.BackColor = Color.WhiteSmoke;

      // === ÍCONE PADRÃO (Inserido aqui para manter consistência) ===
      try { this.Icon = new Icon("Assets/CQLE.ico"); } catch { }

      // === HEADER ===
      var panelHeader = new Panel
      {
        Dock = DockStyle.Top,
        Height = 70,
        BackColor = Color.FromArgb(70, 130, 180),
        Padding = new Padding(15, 10, 15, 0)
      };

      var lblTitulo = new Label
      {
        Text = "🚀 MOVIMENTAÇÃO DE ARQUIVOS",
        ForeColor = Color.White,
        Font = new Font("Segoe UI", 14, FontStyle.Bold),
        AutoSize = true,
        Location = new Point(15, 12)
      };

      var lblSubtitulo = new Label
      {
        Text = "Fluxo Seguro: Backup > Offline > Copiar > Modify > Online > Limpar",
        ForeColor = Color.FromArgb(220, 230, 240),
        Font = new Font("Segoe UI", 9),
        AutoSize = true,
        Location = new Point(17, 40)
      };
      panelHeader.Controls.AddRange(new Control[] { lblTitulo, lblSubtitulo });

      // === FOOTER ===
      var panelBottom = new Panel
      {
        Dock = DockStyle.Bottom,
        Height = 85,
        BackColor = Color.WhiteSmoke,
        Padding = new Padding(15, 5, 15, 5)
      };

      lblStatus = new Label
      {
        Text = "Inicializando...",
        Dock = DockStyle.Top,
        Height = 25,
        Font = new Font("Segoe UI", 10, FontStyle.Bold),
        ForeColor = Color.FromArgb(70, 130, 180)
      };

      progressBar = new ProgressBar
      {
        Dock = DockStyle.Top,
        Height = 20,
        Style = ProgressBarStyle.Continuous
      };

      var panelBotoes = new Panel { Dock = DockStyle.Bottom, Height = 35, Padding = new Padding(0, 5, 0, 0) };

      btnSalvarLog = new Button
      {
        Text = "💾 Salvar Log",
        Width = 110,
        Dock = DockStyle.Left,
        BackColor = Color.SteelBlue,
        ForeColor = Color.White,
        FlatStyle = FlatStyle.Flat,
        Enabled = false,
        Cursor = Cursors.Hand
      };
      btnSalvarLog.FlatAppearance.BorderSize = 0;
      btnSalvarLog.Click += BtnSalvarLog_Click;

      btnFechar = new Button
      {
        Text = "Fechar",
        Width = 110,
        Dock = DockStyle.Right,
        BackColor = Color.Gray,
        ForeColor = Color.White,
        FlatStyle = FlatStyle.Flat,
        Enabled = false,
        Cursor = Cursors.Hand
      };
      btnFechar.FlatAppearance.BorderSize = 0;
      btnFechar.Click += (s, e) => this.Close();

      panelBotoes.Controls.Add(btnSalvarLog);
      panelBotoes.Controls.Add(btnFechar);

      panelBottom.Controls.Add(progressBar);
      panelBottom.Controls.Add(lblStatus);
      panelBottom.Controls.Add(panelBotoes);

      // === BODY (Log Responsivo) ===
      var panelBody = new Panel
      {
        Dock = DockStyle.Fill,
        Padding = new Padding(15, 10, 15, 0)
      };

      txtLog = new RichTextBox
      {
        Dock = DockStyle.Fill,
        Font = new Font("Consolas", 9),
        BackColor = Color.FromArgb(30, 30, 30),
        ForeColor = Color.LimeGreen,
        ReadOnly = true,
        ScrollBars = RichTextBoxScrollBars.Vertical,
        BorderStyle = BorderStyle.None
      };

      panelBody.Controls.Add(txtLog);

      this.Controls.Add(panelBody);
      this.Controls.Add(panelHeader);
      this.Controls.Add(panelBottom);
    }

    private void Log(string texto, bool erro = false, bool destaque = false)
    {
      if (txtLog.InvokeRequired) { txtLog.Invoke(new Action(() => Log(texto, erro, destaque))); return; }
      txtLog.SelectionStart = txtLog.TextLength;
      txtLog.SelectionColor = erro ? Color.Red : (destaque ? Color.Yellow : Color.LimeGreen);
      txtLog.SelectionFont = new Font("Consolas", 9, destaque ? FontStyle.Bold : FontStyle.Regular);
      txtLog.AppendText($"[{DateTime.Now:HH:mm:ss}] {texto}{Environment.NewLine}");
      txtLog.ScrollToCaret();
    }

    private void AtualizarStatus(string texto, int percentual)
    {
      if (lblStatus.InvokeRequired) { lblStatus.Invoke(new Action(() => AtualizarStatus(texto, percentual))); return; }
      lblStatus.Text = texto;
      progressBar.Value = Math.Min(Math.Max(percentual, 0), 100);
    }

    private bool IsRunningAsAdmin()
    {
      var identity = WindowsIdentity.GetCurrent();
      var principal = new WindowsPrincipal(identity);
      return principal.IsInRole(WindowsBuiltInRole.Administrator);
    }

    private async void IniciarProcessoAssincrono()
    {
      // Validação de segurança
      if (!IsRunningAsAdmin())
      {
        Log("❌ ERRO: O programa não está rodando como Administrador!", true, true);
        Log("O arquivo app.manifest não foi configurado corretamente ou ignorado.", true);
        AtualizarStatus("Erro: Falta de Privilégios", 0);

        this.Invoke(new Action(() =>
        {
          btnFechar.Enabled = true;
          btnFechar.BackColor = Color.Red;
          MessageBox.Show("O programa precisa ser compilado com o 'app.manifest' configurado para 'requireAdministrator'.\n\nAdicione o arquivo app.manifest ao projeto e recompile.", "Permissão Necessária", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }));
        return;
      }

      await Task.Run(async () => await ExecutarMovimentacao());
    }

    private void GarantirPermissaoArquivo(string caminhoArquivo)
    {
      try
      {
        var fileInfo = new FileInfo(caminhoArquivo);
        var fileSecurity = fileInfo.GetAccessControl();
        var usuarioAtual = WindowsIdentity.GetCurrent().Name;
        fileSecurity.AddAccessRule(new FileSystemAccessRule(usuarioAtual, FileSystemRights.FullControl, AccessControlType.Allow));
        fileInfo.SetAccessControl(fileSecurity);
        Log($"   -> Permissão corrigida para: {usuarioAtual}");
      }
      catch (Exception ex)
      {
        Log($"   -> (Info) Tentativa de ajuste ignorada: {ex.Message}");
      }
    }

    private void CopiarComTentativas(string origem, string destino)
    {
      int tentativas = 3;
      for (int i = 1; i <= tentativas; i++)
      {
        try
        {
          File.Copy(origem, destino, true);
          return;
        }
        catch (UnauthorizedAccessException)
        {
          Log($"   ⚠ Tentativa {i}: Acesso negado. Aplicando permissão...");
          GarantirPermissaoArquivo(origem);
          if (i == tentativas) throw;
        }
        catch (IOException)
        {
          Log($"   ⚠ Tentativa {i}: Arquivo em uso. Aguardando...");
          System.Threading.Thread.Sleep(2000);
          if (i == tentativas) throw;
        }
      }
    }

    private async Task ExecutarMovimentacao()
    {
      string backupFile = "";
      bool bancoOffline = false;

      try
      {
        Log("════════════════════════════════════════════════════", false, true);
        Log($"INICIANDO MOVIMENTAÇÃO: {_dbName}", false, true);
        Log("════════════════════════════════════════════════════", false, true);

        using (var conn = new SqlConnection(_connectionString))
        {
          conn.Open();

          // 1. BACKUP
          AtualizarStatus("1/7 - Backup...", 10);
          Log("📦 PASSO 1: BACKUP DE SEGURANÇA", false, true);
          string backupDir = @"C:\TempBackups\MoveFiles";
          if (!Directory.Exists(backupDir)) Directory.CreateDirectory(backupDir);
          backupFile = Path.Combine(backupDir, $"{_dbName}_PRE_MOVE_{DateTime.Now:yyyyMMdd_HHmmss}.bak");

          using (var cmd = new SqlCommand($@"BACKUP DATABASE [{_dbName}] TO DISK = '{backupFile}' WITH INIT, COMPRESSION, STATS = 10", conn))
          {
            cmd.CommandTimeout = 600;
            cmd.ExecuteNonQuery();
          }
          Log($"✅ Backup salvo: {backupFile}");
          Log("");

          // 2. OFFLINE
          AtualizarStatus("2/7 - Offline...", 20);
          Log("🔌 PASSO 2: OFFLINE", false, true);
          using (var cmd = new SqlCommand($@"ALTER DATABASE [{_dbName}] SET OFFLINE WITH ROLLBACK IMMEDIATE", conn))
          {
            cmd.CommandTimeout = 120;
            cmd.ExecuteNonQuery();
          }
          bancoOffline = true;
          Log("✅ Banco OFFLINE.");

          Log("⏳ Aguardando liberação do Windows (3s)...");
          await Task.Delay(3000);

          // 3. COPIAR
          int idx = 0;
          foreach (var arq in _arquivos)
          {
            idx++;
            AtualizarStatus($"3/7 - Copiando {idx}/{_arquivos.Count}...", 20 + (idx * 10));
            Log($"📂 PASSO 3.{idx}: COPIAR {Path.GetFileName(arq.CaminhoOrigem)}", false, true);

            string dirDest = Path.GetDirectoryName(arq.CaminhoDestino);
            if (!Directory.Exists(dirDest)) Directory.CreateDirectory(dirDest);

            if (!File.Exists(arq.CaminhoOrigem)) throw new FileNotFoundException("Origem não encontrada", arq.CaminhoOrigem);

            CopiarComTentativas(arq.CaminhoOrigem, arq.CaminhoDestino);
            Log("✅ Cópia OK");
          }
          Log("");

          // 4. MODIFY
          AtualizarStatus("4/7 - Atualizando SQL...", 60);
          Log("⚙️ PASSO 4: ALTERAR METADADOS", false, true);
          foreach (var arq in _arquivos)
          {
            Log($"Update: {arq.LogicalName}");
            using (var cmd = new SqlCommand($@"ALTER DATABASE [{_dbName}] MODIFY FILE (NAME = [{arq.LogicalName}], FILENAME = '{arq.CaminhoDestino}')", conn))
            {
              cmd.ExecuteNonQuery();
            }
          }
          Log("✅ Metadados OK");
          Log("");

          // 5. ONLINE
          AtualizarStatus("5/7 - Online...", 75);
          Log("🔌 PASSO 5: ONLINE", false, true);
          using (var cmd = new SqlCommand($"ALTER DATABASE [{_dbName}] SET ONLINE", conn))
          {
            cmd.ExecuteNonQuery();
          }
          bancoOffline = false;
          Log("✅ Banco ONLINE");
          Log("");

          // 6. VALIDAR
          AtualizarStatus("6/7 - Validando...", 85);
          Log("🔍 PASSO 6: VALIDAÇÃO", false, true);
          string estado = "";
          using (var cmd = new SqlCommand($"SELECT state_desc FROM sys.databases WHERE name = '{_dbName}'", conn))
          {
            estado = cmd.ExecuteScalar()?.ToString();
          }
          if (estado != "ONLINE") throw new Exception($"Status incorreto: {estado}");
          Log($"Estado: {estado} ✅");

          // 7. LIMPAR
          AtualizarStatus("7/7 - Limpando...", 95);
          Log("🗑️ PASSO 7: DELETAR ANTIGOS", false, true);
          foreach (var arq in _arquivos)
          {
            try
            {
              if (File.Exists(arq.CaminhoOrigem))
              {
                GarantirPermissaoArquivo(arq.CaminhoOrigem);
                File.Delete(arq.CaminhoOrigem);
                Log($"✅ Deletado: {arq.CaminhoOrigem}");
              }
            }
            catch (Exception ex)
            {
              Log($"⚠ Falha ao deletar origem: {ex.Message}. Apague manualmente.", true);
            }
          }

          // FIM
          AtualizarStatus("✅ Sucesso!", 100);
          Log("");
          Log("🏁 OPERAÇÃO CONCLUÍDA!", false, true);

          this.Invoke(new Action(() =>
          {
            btnFechar.Enabled = true;
            btnFechar.BackColor = Color.SeaGreen;
            btnSalvarLog.Enabled = true;
            MessageBox.Show("Movimentação concluída com sucesso!", "Sucesso", MessageBoxButtons.OK, MessageBoxIcon.Information);
          }));
        }
      }
      catch (Exception ex)
      {
        Log("");
        Log("❌ ERRO FATAL", true, true);
        Log(ex.Message, true);

        if (bancoOffline)
        {
          Log("Tentando recuperar banco para ONLINE...", false, true);
          try
          {
            using (var conn = new SqlConnection(_connectionString))
            {
              conn.Open();
              new SqlCommand($"ALTER DATABASE [{_dbName}] SET ONLINE", conn).ExecuteNonQuery();
            }
            Log("Recuperação: Banco ONLINE.");
          }
          catch { Log("Falha na recuperação.", true); }
        }

        AtualizarStatus("Erro Fatal", 0);
        this.Invoke(new Action(() =>
        {
          btnFechar.Enabled = true;
          btnFechar.BackColor = Color.Red;
          btnSalvarLog.Enabled = true;
          MessageBox.Show($"Erro:\n{ex.Message}", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }));
      }
    }

    private void BtnSalvarLog_Click(object sender, EventArgs e)
    {
      try
      {
        string pastaLog = @"C:\TempBackups\MoveFiles\Logs";
        if (!Directory.Exists(pastaLog)) Directory.CreateDirectory(pastaLog);
        string logFile = Path.Combine(pastaLog, $"Log_{_dbName}_{DateTime.Now:yyyyMMdd_HHmmss}.txt");
        File.WriteAllText(logFile, txtLog.Text);
        MessageBox.Show($"Log salvo: {logFile}", "Sucesso");
      }
      catch (Exception ex) { MessageBox.Show(ex.Message); }
    }
  }
}