#nullable disable
using System;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using CQLE_MIGRACAO.Services;

namespace CQLE_MIGRACAO.Forms
{
  public class UnifiedProgressForm : Form
  {
    private ProgressBar pbGeral;
    private Label lblStatus;
    private RichTextBox txtLog;
    private Button btnInterromper;
    private Button btnSalvarLog;
    private Button btnConcluir; // Renomeado para Concluir
    private Label lblPercentage;
    private Panel panelHeader;

    private readonly UnifiedMigrationService _migrationService;
    private readonly UnifiedMigrationService.MigrationConfig _config;
    private CancellationTokenSource _cts;

    public UnifiedProgressForm(UnifiedMigrationService migrationService, UnifiedMigrationService.MigrationConfig config)
    {
      _migrationService = migrationService;
      _config = config;

      ConfigurarInterface();

      this.Shown += (s, e) => IniciarProcesso();

      try
      {
        this.Icon = new Icon("Assets/CQLE.ico");
      }
      catch { }
    }

    private void ConfigurarInterface()
    {
      this.Text = "CQLE Migração - Executando Migração";
      this.Size = new Size(950, 750);
      this.MinimumSize = this.Size;
      this.StartPosition = FormStartPosition.CenterScreen;
      this.FormBorderStyle = FormBorderStyle.FixedDialog;
      this.MaximizeBox = false;
      this.BackColor = Color.FromArgb(240, 240, 245);

      // Header
      panelHeader = new Panel
      {
        Location = new Point(0, 0),
        Size = new Size(950, 80),
        BackColor = Color.FromArgb(0, 120, 215)
      };

      var lblTitulo = new Label
      {
        Text = "🗄️ CQLE MIGRAÇÃO",
        Location = new Point(20, 15),
        Size = new Size(600, 30),
        Font = new Font("Segoe UI", 18, FontStyle.Bold),
        ForeColor = Color.White
      };

      var lblSubtitulo = new Label
      {
        Text = "Executando Migração",
        Location = new Point(20, 50),
        Size = new Size(600, 20),
        Font = new Font("Segoe UI", 10),
        ForeColor = Color.FromArgb(200, 220, 255)
      };

      panelHeader.Controls.Add(lblTitulo);
      panelHeader.Controls.Add(lblSubtitulo);

      lblStatus = new Label
      {
        Text = "Inicializando...",
        Location = new Point(20, 100),
        Size = new Size(700, 25),
        Font = new Font("Segoe UI", 11, FontStyle.Bold)
      };

      lblPercentage = new Label
      {
        Text = "0%",
        Location = new Point(850, 100),
        Size = new Size(60, 25),
        TextAlign = ContentAlignment.MiddleRight,
        Font = new Font("Segoe UI", 11, FontStyle.Bold)
      };

      pbGeral = new ProgressBar
      {
        Location = new Point(20, 130),
        Size = new Size(910, 30),
        Style = ProgressBarStyle.Continuous,
        Maximum = 100
      };

      var lblLogTitle = new Label
      {
        Text = "📋 Log Detalhado:",
        Location = new Point(20, 170),
        AutoSize = true,
        Font = new Font("Segoe UI", 10, FontStyle.Bold)
      };

      txtLog = new RichTextBox
      {
        Location = new Point(20, 195),
        Size = new Size(910, 400),
        ReadOnly = true,
        BackColor = Color.FromArgb(30, 30, 30),
        ForeColor = Color.LimeGreen,
        Font = new Font("Consolas", 9.5f),
        ScrollBars = RichTextBoxScrollBars.Vertical
      };

      // Botão Interromper
      btnInterromper = new Button
      {
        Text = "⏹ Interromper",
        Location = new Point(20, 610),
        Size = new Size(180, 50),
        Font = new Font("Segoe UI", 11, FontStyle.Bold),
        BackColor = Color.FromArgb(200, 0, 0),
        ForeColor = Color.White,
        FlatStyle = FlatStyle.Flat
      };
      btnInterromper.Click += BtnInterromper_Click;

      // Botão Salvar Log
      btnSalvarLog = new Button
      {
        Text = "💾 Salvar Log",
        Location = new Point(385, 610),
        Size = new Size(180, 50),
        Font = new Font("Segoe UI", 11, FontStyle.Bold),
        BackColor = Color.SteelBlue,
        ForeColor = Color.White,
        FlatStyle = FlatStyle.Flat,
        Enabled = false
      };
      btnSalvarLog.Click += BtnSalvarLog_Click;

      // Botão Concluir (verde, principal)
      btnConcluir = new Button
      {
        Text = "✅ Concluir",
        Location = new Point(750, 610),
        Size = new Size(180, 50),
        Font = new Font("Segoe UI", 11, FontStyle.Bold),
        BackColor = Color.FromArgb(0, 120, 0),
        ForeColor = Color.White,
        FlatStyle = FlatStyle.Flat,
        Enabled = false
      };
      btnConcluir.Click += BtnConcluir_Click; // Fecha direto, sem pergunta

      // Rodapé
      var lblRodape = new Label
      {
        Text = "Desenvolvido por Marciano Silva - CQLE Softwares © 2025",
        Dock = DockStyle.Bottom,
        Height = 30,
        TextAlign = ContentAlignment.MiddleCenter,
        Font = new Font("Segoe UI", 9),
        ForeColor = Color.Gray
      };

      this.Controls.AddRange(new Control[]
      {
                panelHeader, lblStatus, lblPercentage, pbGeral,
                lblLogTitle, txtLog,
                btnInterromper, btnSalvarLog, btnConcluir, lblRodape
      });
    }

    private void IniciarProcesso()
    {
      _cts = new CancellationTokenSource();

      btnInterromper.Enabled = true;
      btnSalvarLog.Enabled = false;
      btnConcluir.Enabled = false;

      AddLog("╔════════════════════════════════════════════════════╗");
      AddLog("║      CQLE MIGRAÇÃO - MIGRAÇÃO UNIFICADA INICIADA   ║");
      AddLog("╚════════════════════════════════════════════════════╝");
      AddLog("");

      Task.Run(() =>
      {
        try
        {
          _migrationService.ExecutarMigracaoCompleta(_config, msg =>
                {
                this.Invoke(new Action(() => AddLog(msg)));
              });
        }
        catch (Exception ex)
        {
          this.Invoke(new Action(() =>
                {
                AddLog($"ERRO CRÍTICO: {ex.Message}");
              }));
        }
        finally
        {
          this.Invoke(new Action(() => FinalizarProcesso()));
        }
      });
    }

    private void FinalizarProcesso()
    {
      btnInterromper.Enabled = false;
      btnSalvarLog.Enabled = true;
      btnConcluir.Enabled = true;
      lblStatus.Text = "Concluído";
      lblPercentage.Text = "100%";
      pbGeral.Value = 100;

      AddLog("");
      AddLog("╔════════════════════════════════════════════════════╗");
      AddLog("║           MIGRAÇÃO CONCLUÍDA COM SUCESSO          ║");
      AddLog("╚════════════════════════════════════════════════════╝");
    }

    private void BtnInterromper_Click(object sender, EventArgs e)
    {
      if (_cts != null && !_cts.IsCancellationRequested)
      {
        var result = MessageBox.Show(
            "Deseja interromper a migração?\n\nOs itens já processados permanecerão no destino.",
            "Confirmar Interrupção",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning);

        if (result == DialogResult.Yes)
        {
          _cts.Cancel();
          AddLog("INTERRUPÇÃO SOLICITADA - Aguarde...");
          btnInterromper.Enabled = false;
        }
      }
    }

    // Botão Concluir: fecha direto, sem pergunta
    private void BtnConcluir_Click(object sender, EventArgs e)
    {
      this.Close();
    }

    private void BtnSalvarLog_Click(object sender, EventArgs e)
    {
      using var sfd = new SaveFileDialog
      {
        Filter = "Texto (*.txt)|*.txt",
        FileName = $"Log_Migracao_{DateTime.Now:yyyyMMdd_HHmmss}.txt"
      };

      if (sfd.ShowDialog() == DialogResult.OK)
      {
        File.WriteAllText(sfd.FileName, txtLog.Text);
        MessageBox.Show("Log salvo com sucesso!", "Sucesso", MessageBoxButtons.OK, MessageBoxIcon.Information);
      }
    }

    // Só pergunta se fechar com X enquanto migração rodando
    private void UnifiedProgressForm_FormClosing(object sender, FormClosingEventArgs e)
    {
      if (_cts != null && !_cts.IsCancellationRequested)
      {
        var result = MessageBox.Show(
            "A migração ainda está em execução!\n\nFechar a janela irá interromper o processo.\n\nDeseja continuar?",
            "Migração em Andamento",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning);

        if (result == DialogResult.No)
        {
          e.Cancel = true;
        }
        else
        {
          _cts.Cancel();
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
        txtLog.AppendText($"[{DateTime.Now:HH:mm:ss}] {msg}{Environment.NewLine}");
        txtLog.SelectionStart = txtLog.Text.Length;
        txtLog.ScrollToCaret();
      }
    }
  }
}