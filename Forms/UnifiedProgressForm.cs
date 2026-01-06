#nullable disable
using System;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using CQLE_MIGRACAO.Services;

namespace CQLE_MIGRACAO.Forms
{
  public class UnifiedProgressForm : Form
  {
    private ProgressBar pbGeral;
    private Label lblStatus;
    private RichTextBox txtLog;
    private Button btnFechar;
    private Button btnCancelar;
    private Button btnSalvarLog;
    private Label lblPercentage;
    private Panel panelStatus;

    private readonly string connectionString;
    private readonly UnifiedMigrationService.MigrationConfig config;
    private CancellationTokenSource cts;

    public UnifiedProgressForm(string connString, UnifiedMigrationService.MigrationConfig cfg)
    {
      connectionString = connString;
      config = cfg;

      ConfigurarInterface();
      this.Shown += async (s, e) => await IniciarProcesso();
    }

    private void ConfigurarInterface()
    {
      this.Text = "Executando Migração - CQLE Automator";
      this.Size = new Size(900, 650);
      this.StartPosition = FormStartPosition.CenterScreen;
      this.FormBorderStyle = FormBorderStyle.FixedDialog;
      this.MaximizeBox = false;
      this.BackColor = Color.WhiteSmoke;

      panelStatus = new Panel
      {
        Location = new Point(0, 0),
        Size = new Size(900, 80),
        BackColor = Color.FromArgb(0, 120, 215)
      };

      lblStatus = new Label
      {
        Text = "Preparando...",
        Location = new Point(20, 15),
        Size = new Size(800, 25),
        Font = new Font("Segoe UI", 12, FontStyle.Bold),
        ForeColor = Color.White
      };

      lblPercentage = new Label
      {
        Text = "0%",
        Location = new Point(820, 15),
        Size = new Size(60, 25),
        TextAlign = ContentAlignment.TopRight,
        Font = new Font("Segoe UI", 12, FontStyle.Bold),
        ForeColor = Color.White
      };

      pbGeral = new ProgressBar
      {
        Location = new Point(20, 45),
        Size = new Size(860, 25),
        Style = ProgressBarStyle.Continuous
      };

      panelStatus.Controls.Add(lblStatus);
      panelStatus.Controls.Add(lblPercentage);
      panelStatus.Controls.Add(pbGeral);

      Label lblLogTitle = new Label
      {
        Text = "Log Detalhado:",
        Location = new Point(20, 90),
        AutoSize = true,
        Font = new Font("Segoe UI", 10, FontStyle.Bold)
      };

      txtLog = new RichTextBox
      {
        Location = new Point(20, 115),
        Size = new Size(860, 430),
        ReadOnly = true,
        BackColor = Color.FromArgb(30, 30, 30),
        ForeColor = Color.LimeGreen,
        Font = new Font("Consolas", 9),
        ScrollBars = RichTextBoxScrollBars.Vertical
      };

      btnSalvarLog = new Button
      {
        Text = "Salvar Log",
        Location = new Point(20, 560),
        Size = new Size(120, 40),
        FlatStyle = FlatStyle.Flat,
        BackColor = Color.SteelBlue,
        ForeColor = Color.White,
        Cursor = Cursors.Hand
      };
      btnSalvarLog.Click += BtnSalvarLog_Click;

      btnCancelar = new Button
      {
        Text = "Interromper",
        Location = new Point(380, 560),
        Size = new Size(120, 40),
        FlatStyle = FlatStyle.Flat,
        BackColor = Color.OrangeRed,
        ForeColor = Color.White,
        Cursor = Cursors.Hand
      };
      btnCancelar.Click += BtnCancelar_Click;

      btnFechar = new Button
      {
        Text = "Concluir",
        Location = new Point(760, 560),
        Size = new Size(120, 40),
        FlatStyle = FlatStyle.Flat,
        BackColor = Color.SeaGreen,
        ForeColor = Color.White,
        Enabled = false,
        Cursor = Cursors.Hand
      };
      btnFechar.Click += (s, e) => this.Close();

      this.Controls.Add(panelStatus);
      this.Controls.Add(lblLogTitle);
      this.Controls.Add(txtLog);
      this.Controls.Add(btnSalvarLog);
      this.Controls.Add(btnCancelar);
      this.Controls.Add(btnFechar);
    }

    private async Task IniciarProcesso()
    {
      if (config.DatabaseNames.Count > 0)
      {
        using (var fbd = new FolderBrowserDialog())
        {
          fbd.Description = "Escolha a pasta temporária para arquivos de backup (.bak):";
          fbd.ShowNewFolderButton = true;
          fbd.SelectedPath = @"C:\TempBackups";

          if (fbd.ShowDialog() != DialogResult.OK)
          {
            AddLog("Cancelado pelo usuário - pasta de backup não informada");
            btnFechar.Enabled = true;
            btnCancelar.Enabled = false;
            return;
          }

          config.PastaBackup = fbd.SelectedPath;
          AddLog($"Pasta de trabalho: {config.PastaBackup}");
        }
      }

      cts = new CancellationTokenSource();

      AddLog("════════════════════════════════════════════════════");
      AddLog("         INICIANDO MIGRAÇÃO UNIFICADA              ");
      AddLog("════════════════════════════════════════════════════");
      AddLog("");

      try
      {
        var service = new UnifiedMigrationService(connectionString);

        await Task.Run(() =>
        {
          service.ExecutarMigracaoCompleta(config, (msg) =>
          {
            if (cts.Token.IsCancellationRequested)
              throw new OperationCanceledException();

            this.Invoke(new Action(() => AddLog(msg)));
          });
        }, cts.Token);

        this.Invoke(new Action(() =>
        {
          lblStatus.Text = "Migração Concluída com Sucesso!";
          panelStatus.BackColor = Color.SeaGreen;
          pbGeral.Value = pbGeral.Maximum;
          lblPercentage.Text = "100%";
          btnCancelar.Enabled = false;
          btnFechar.Enabled = true;

          MessageBox.Show(
            "Migração concluída!\n\n" +
            $"Bancos: {config.DatabaseNames.Count}\n" +
            $"Linked Servers: {(config.IncludeLinkedServers ? "Sim" : "Não")}\n" +
            $"Jobs: {(config.IncludeJobs ? "Sim" : "Não")}\n\n" +
            "Verifique o log para detalhes.",
            "Sucesso",
            MessageBoxButtons.OK,
            MessageBoxIcon.Information
          );
        }));
      }
      catch (OperationCanceledException)
      {
        this.Invoke(new Action(() =>
        {
          lblStatus.Text = "Processo Interrompido";
          panelStatus.BackColor = Color.OrangeRed;
          AddLog("");
          AddLog("PROCESSO CANCELADO PELO USUÁRIO");
          btnCancelar.Enabled = false;
          btnFechar.Enabled = true;
        }));
      }
      catch (Exception ex)
      {
        this.Invoke(new Action(() =>
        {
          lblStatus.Text = "Erro na Migração";
          panelStatus.BackColor = Color.DarkRed;
          AddLog("");
          AddLog("════════════════════════════════════════════════════");
          AddLog("                 ERRO CRÍTICO                      ");
          AddLog("════════════════════════════════════════════════════");
          AddLog($"Mensagem: {ex.Message}");
          btnCancelar.Enabled = false;
          btnFechar.Enabled = true;

          MessageBox.Show(
            $"Erro durante a migração:\n\n{ex.Message}\n\n" +
            "Verifique o log para mais detalhes.",
            "Erro",
            MessageBoxButtons.OK,
            MessageBoxIcon.Error
          );
        }));
      }
      finally
      {
        cts = null;
      }
    }

    private void BtnCancelar_Click(object sender, EventArgs e)
    {
      if (cts != null && !cts.IsCancellationRequested)
      {
        var resultado = MessageBox.Show(
          "Deseja realmente interromper o processo?\n\n" +
          "Os objetos já migrados permanecerão no destino,\n" +
          "mas os pendentes não serão processados.",
          "Confirmar Cancelamento",
          MessageBoxButtons.YesNo,
          MessageBoxIcon.Warning
        );

        if (resultado == DialogResult.Yes)
        {
          cts.Cancel();
          AddLog("CANCELAMENTO SOLICITADO - Aguarde...");
          btnCancelar.Enabled = false;
        }
      }
    }

    private void BtnSalvarLog_Click(object sender, EventArgs e)
    {
      using (var sfd = new SaveFileDialog())
      {
        sfd.Filter = "Arquivo de Texto (*.txt)|*.txt|Todos os arquivos (*.*)|*.*";
        sfd.FileName = $"Log_Migracao_{DateTime.Now:yyyyMMdd_HHmmss}.txt";
        sfd.Title = "Salvar Log da Migração";

        if (sfd.ShowDialog() == DialogResult.OK)
        {
          try
          {
            File.WriteAllText(sfd.FileName, txtLog.Text);
            MessageBox.Show(
              $"Log salvo com sucesso!\n\n{sfd.FileName}",
              "Sucesso",
              MessageBoxButtons.OK,
              MessageBoxIcon.Information
            );
          }
          catch (Exception ex)
          {
            MessageBox.Show(
              $"Erro ao salvar log:\n\n{ex.Message}",
              "Erro",
              MessageBoxButtons.OK,
              MessageBoxIcon.Error
            );
          }
        }
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
        txtLog.AppendText($"{msg}{Environment.NewLine}");
        txtLog.SelectionStart = txtLog.Text.Length;
        txtLog.ScrollToCaret();
      }
    }
  }
}