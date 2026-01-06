#nullable disable
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using CQLE_MIGRACAO.Services;

namespace CQLE_MIGRACAO.Forms
{
  public partial class MigrationForm : Form
  {
    private System.ComponentModel.IContainer components = null;
    private TextBox txtConnectionString;
    private CheckedListBox clbDatabases;
    private CheckBox chkJobs;
    private CheckBox chkLinkedServers;
    private RadioButton rbOnline;
    private RadioButton rbOffline;
    private TextBox txtLog;
    private Button btnListar;
    private Button btnExecutar;
    private Button btnSair;
    private Label lblJobsCount;
    private Label lblLinkedServersCount;
    private Label lblDatabasesCount;

    public MigrationForm()
    {
      InitializeComponent();
      ConfigurarInterfaceAvancada();

      try
      {
        this.Icon = new Icon("CQLE.ico");
      }
      catch { }
    }

    private void InitializeComponent()
    {
      this.components = new System.ComponentModel.Container();
      this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
      this.ClientSize = new System.Drawing.Size(950, 700);
      this.Text = "CQLE Migração - Dashboard Principal";
      this.StartPosition = FormStartPosition.CenterScreen;
      this.BackColor = Color.FromArgb(240, 240, 245);
      this.FormBorderStyle = FormBorderStyle.Sizable;
      this.MinimumSize = new Size(950, 700);
    }

    private void ConfigurarInterfaceAvancada()
    {
      // Cabeçalho
      Panel panelHeader = new Panel
      {
        Location = new Point(0, 0),
        Size = new Size(950, 70),
        BackColor = Color.FromArgb(0, 120, 215)
      };

      Label lblTitle = new Label
      {
        Text = "🗄️ CQLE MIGRAÇÃO",
        Location = new Point(20, 12),
        Size = new Size(500, 22),
        Font = new Font("Segoe UI", 14, FontStyle.Bold),
        ForeColor = Color.White
      };

      Label lblSubtitle = new Label
      {
        Text = "Sistema Profissional de Migração SQL Server",
        Location = new Point(20, 38),
        Size = new Size(500, 18),
        Font = new Font("Segoe UI", 9),
        ForeColor = Color.FromArgb(200, 220, 255)
      };

      Button btnSobre = new Button
      {
        Text = "ℹ️ Sobre",
        Location = new Point(780, 15),
        Size = new Size(80, 40),
        FlatStyle = FlatStyle.Flat,
        BackColor = Color.FromArgb(0, 100, 180),
        ForeColor = Color.White,
        Cursor = Cursors.Hand,
        Font = new Font("Segoe UI", 9, FontStyle.Bold)
      };
      btnSobre.FlatAppearance.BorderSize = 0;
      btnSobre.Click += BtnSobre_Click;

      btnSair = new Button
      {
        Text = "❌ Sair",
        Location = new Point(865, 15),
        Size = new Size(70, 40),
        FlatStyle = FlatStyle.Flat,
        BackColor = Color.FromArgb(200, 50, 50),
        ForeColor = Color.White,
        Cursor = Cursors.Hand,
        Font = new Font("Segoe UI", 9, FontStyle.Bold)
      };
      btnSair.FlatAppearance.BorderSize = 0;
      btnSair.Click += BtnSair_Click;

      panelHeader.Controls.Add(lblTitle);
      panelHeader.Controls.Add(lblSubtitle);
      panelHeader.Controls.Add(btnSobre);
      panelHeader.Controls.Add(btnSair);
      this.Controls.Add(panelHeader);

      // Grupo 1: Conexão
      GroupBox grpConexao = new GroupBox
      {
        Text = "  Conexão com Servidor de Origem  ",
        Location = new Point(20, 85),
        Size = new Size(910, 90),
        Font = new Font("Segoe UI", 9, FontStyle.Bold),
        ForeColor = Color.FromArgb(0, 120, 215)
      };

      Label lblConn = new Label
      {
        Text = "Connection String:",
        Location = new Point(15, 30),
        AutoSize = true,
        Font = new Font("Segoe UI", 9),
        ForeColor = Color.Black
      };

      txtConnectionString = new TextBox
      {
        Text = "Server=localhost;Database=master;Trusted_Connection=True;TrustServerCertificate=True;",
        Location = new Point(15, 50),
        Size = new Size(730, 25),
        Font = new Font("Consolas", 9)
      };

      btnListar = new Button
      {
        Text = "🔍 Conectar & Inventariar",
        Location = new Point(760, 48),
        Size = new Size(135, 30),
        BackColor = Color.FromArgb(0, 120, 215),
        ForeColor = Color.White,
        FlatStyle = FlatStyle.Flat,
        Cursor = Cursors.Hand,
        Font = new Font("Segoe UI", 9, FontStyle.Bold)
      };
      btnListar.FlatAppearance.BorderSize = 0;
      btnListar.Click += BtnListar_Click;

      grpConexao.Controls.Add(lblConn);
      grpConexao.Controls.Add(txtConnectionString);
      grpConexao.Controls.Add(btnListar);
      this.Controls.Add(grpConexao);

      // Grupo 2: Objetos
      GroupBox grpSelecao = new GroupBox
      {
        Text = "   Objetos para Migração  ",
        Location = new Point(20, 190),
        Size = new Size(450, 340),
        Font = new Font("Segoe UI", 9, FontStyle.Bold),
        ForeColor = Color.FromArgb(0, 120, 215)
      };

      Label lblDb = new Label
      {
        Text = "📦 Bancos de Dados:",
        Location = new Point(15, 30),
        AutoSize = true,
        Font = new Font("Segoe UI", 9, FontStyle.Bold)
      };

      lblDatabasesCount = new Label
      {
        Text = "(0 encontrados)",
        Location = new Point(160, 32),
        AutoSize = true,
        ForeColor = Color.Gray,
        Font = new Font("Segoe UI", 8)
      };

      clbDatabases = new CheckedListBox
      {
        Location = new Point(15, 55),
        Size = new Size(420, 180),
        CheckOnClick = true,
        Font = new Font("Consolas", 9),
        IntegralHeight = false
      };

      Button btnSelectAll = new Button
      {
        Text = "✓ Todos",
        Location = new Point(15, 240),
        Size = new Size(80, 25),
        FlatStyle = FlatStyle.Flat,
        Font = new Font("Segoe UI", 8),
        Cursor = Cursors.Hand
      };
      btnSelectAll.Click += (s, e) =>
      {
        for (int i = 0; i < clbDatabases.Items.Count; i++)
          clbDatabases.SetItemChecked(i, true);
      };

      Button btnSelectNone = new Button
      {
        Text = "✗ Nenhum",
        Location = new Point(105, 240),
        Size = new Size(85, 25),
        FlatStyle = FlatStyle.Flat,
        Font = new Font("Segoe UI", 8),
        Cursor = Cursors.Hand
      };
      btnSelectNone.Click += (s, e) =>
      {
        for (int i = 0; i < clbDatabases.Items.Count; i++)
          clbDatabases.SetItemChecked(i, false);
      };

      Panel separator = new Panel
      {
        Location = new Point(15, 275),
        Size = new Size(420, 2),
        BackColor = Color.LightGray
      };

      Label lblSistema = new Label
      {
        Text = "⚙️ Objetos de Sistema:",
        Location = new Point(15, 285),
        AutoSize = true,
        Font = new Font("Segoe UI", 9, FontStyle.Bold)
      };

      chkLinkedServers = new CheckBox
      {
        Text = "🔗 Linked Servers",
        Location = new Point(30, 305),
        AutoSize = true,
        Font = new Font("Segoe UI", 9)
      };

      lblLinkedServersCount = new Label
      {
        Text = "(0 encontrados)",
        Location = new Point(200, 307),
        AutoSize = true,
        ForeColor = Color.Gray,
        Font = new Font("Segoe UI", 8)
      };

      chkJobs = new CheckBox
      {
        Text = "⏱️ SQL Agent Jobs",
        Location = new Point(290, 305),
        AutoSize = true,
        Font = new Font("Segoe UI", 9)
      };

      lblJobsCount = new Label
      {
        Text = "(0 encontrados)",
        Location = new Point(290, 322),
        AutoSize = true,
        ForeColor = Color.Gray,
        Font = new Font("Segoe UI", 8)
      };

      grpSelecao.Controls.Add(lblDb);
      grpSelecao.Controls.Add(lblDatabasesCount);
      grpSelecao.Controls.Add(clbDatabases);
      grpSelecao.Controls.Add(btnSelectAll);
      grpSelecao.Controls.Add(btnSelectNone);
      grpSelecao.Controls.Add(separator);
      grpSelecao.Controls.Add(lblSistema);
      grpSelecao.Controls.Add(chkLinkedServers);
      grpSelecao.Controls.Add(lblLinkedServersCount);
      grpSelecao.Controls.Add(chkJobs);
      grpSelecao.Controls.Add(lblJobsCount);
      this.Controls.Add(grpSelecao);

      // Grupo 3: Estratégia
      GroupBox grpModo = new GroupBox
      {
        Text = "  Estratégia de Migração  ",
        Location = new Point(480, 190),
        Size = new Size(450, 180),
        Font = new Font("Segoe UI", 9, FontStyle.Bold),
        ForeColor = Color.FromArgb(0, 120, 215)
      };

      rbOnline = new RadioButton
      {
        Text = "🟢 ONLINE - Em desenvolvimento (Log Shipping)",
        Location = new Point(20, 35),
        Size = new Size(410, 22),
        Checked = false,
        Enabled = false,
        Font = new Font("Segoe UI", 9, FontStyle.Bold),
        ForeColor = Color.FromArgb(0, 150, 0)
      };

      Label lblOnlineDesc = new Label
      {
        Text = "• Modo ONLINE estará disponível futuramente\n" +
               "• Implementação baseada em Log Shipping\n" +
               "• Execução automática no servidor destino" +
               "• Conecta automaticamente no servidor destino\n" +
               "• Cria bancos, linked servers e jobs em tempo real\n" +
               "• Ideal para ambientes homogêneos",
        Location = new Point(40, 58),
        Size = new Size(390, 50),
        Font = new Font("Segoe UI", 8),
        ForeColor = Color.DimGray
      };

      rbOffline = new RadioButton
      {
        Text = "🟠 OFFLINE - Gerar scripts SQL para execução manual",
        Location = new Point(20, 115),
        Size = new Size(410, 22),
        Font = new Font("Segoe UI", 9, FontStyle.Bold),
        ForeColor = Color.FromArgb(255, 140, 0),
        Checked = true  // Padrao selecionado
      };

      Label lblOfflineDesc = new Label
      {
        Text = "• Gera arquivos de log da migração\n" +
               "• Maneira rapida e eficiente\n" +
               "• Ideal para todos os ambientes ",
        Location = new Point(40, 138),
        Size = new Size(390, 50),
        Font = new Font("Segoe UI", 8),
        ForeColor = Color.DimGray
      };

      grpModo.Controls.Add(rbOnline);
      grpModo.Controls.Add(lblOnlineDesc);
      grpModo.Controls.Add(rbOffline);
      grpModo.Controls.Add(lblOfflineDesc);
      this.Controls.Add(grpModo);

      // Botão Principal
      btnExecutar = new Button
      {
        Text = "🚀 INICIAR MIGRAÇÃO",
        Location = new Point(480, 385),
        Size = new Size(450, 75),
        BackColor = Color.FromArgb(0, 120, 0),
        ForeColor = Color.White,
        Font = new Font("Segoe UI", 14, FontStyle.Bold),
        FlatStyle = FlatStyle.Flat,
        Enabled = false,
        Cursor = Cursors.Hand
      };
      btnExecutar.FlatAppearance.BorderSize = 0;
      btnExecutar.Click += BtnExecutar_Click;
      this.Controls.Add(btnExecutar);

      Label lblInfo = new Label
      {
        Text = "💡 OFFLINE,testado e válido",
        Location = new Point(480, 470),
        Size = new Size(450, 30),
        Font = new Font("Segoe UI", 8, FontStyle.Italic),
        ForeColor = Color.Gray,
        TextAlign = ContentAlignment.MiddleCenter
      };
      this.Controls.Add(lblInfo);

      // Log
      Label lblLog = new Label
      {
        Text = "📋 Log de Operações:",
        Location = new Point(20, 540),
        AutoSize = true,
        Font = new Font("Segoe UI", 9, FontStyle.Bold)
      };
      this.Controls.Add(lblLog);

      txtLog = new TextBox
      {
        Multiline = true,
        ScrollBars = ScrollBars.Vertical,
        ReadOnly = true,
        BackColor = Color.FromArgb(30, 30, 30),
        ForeColor = Color.FromArgb(0, 255, 0),
        Font = new Font("Consolas", 9F),
        Location = new Point(20, 565),
        Size = new Size(910, 115)
      };
      this.Controls.Add(txtLog);

      // Rodapé
      Label lblRodape = new Label
      {
        Text = "Desenvolvido por Marciano Silva - CQLE Softwares © 2025",
        Location = new Point(0, 683),
        Size = new Size(950, 17),
        TextAlign = ContentAlignment.MiddleCenter,
        Font = new Font("Segoe UI", 7),
        ForeColor = Color.Gray,
        BackColor = Color.FromArgb(240, 240, 245)
      };
      this.Controls.Add(lblRodape);

      // Evento de fechamento
      this.FormClosing += MigrationForm_FormClosing;
    }

    private void BtnListar_Click(object sender, EventArgs e)
    {
      txtLog.Clear();
      AdicionarLog("═══════════════════════════════════════════════");
      AdicionarLog("🔍 INICIANDO INVENTÁRIO DE OBJETOS");
      AdicionarLog("═══════════════════════════════════════════════");

      clbDatabases.Items.Clear();
      lblDatabasesCount.Text = "(0 encontrados)";
      lblLinkedServersCount.Text = "(0 encontrados)";
      lblJobsCount.Text = "(0 encontrados)";

      chkLinkedServers.Enabled = false;
      chkJobs.Enabled = false;

      try
      {
        var service = new UnifiedMigrationService(txtConnectionString.Text);
        var inventario = service.GetInventario();

        if (inventario.Databases.Count > 0)
        {
          foreach (var db in inventario.Databases)
          {
            clbDatabases.Items.Add(db, CheckState.Checked);
          }
          lblDatabasesCount.Text = $"({inventario.Databases.Count} encontrados)";
          lblDatabasesCount.ForeColor = Color.Green;
          AdicionarLog($"✅ {inventario.Databases.Count} banco(s) de dados encontrado(s)");
        }
        else
        {
          AdicionarLog("⚠️ Nenhum banco de usuário encontrado");
        }

        if (inventario.LinkedServers.Count > 0)
        {
          lblLinkedServersCount.Text = $"({inventario.LinkedServers.Count} encontrados)";
          lblLinkedServersCount.ForeColor = Color.Green;
          chkLinkedServers.Enabled = true;

          AdicionarLog($"✅ {inventario.LinkedServers.Count} Linked Server(s):");
          foreach (var ls in inventario.LinkedServers.Take(3))
          {
            AdicionarLog($"   • {ls}");
          }
          if (inventario.LinkedServers.Count > 3)
            AdicionarLog($"   ... e mais {inventario.LinkedServers.Count - 3}");
        }
        else
        {
          AdicionarLog("ℹ️ Nenhum Linked Server encontrado");
        }

        if (inventario.Jobs.Count > 0)
        {
          lblJobsCount.Text = $"({inventario.Jobs.Count} encontrados)";
          lblJobsCount.ForeColor = Color.Green;
          chkJobs.Enabled = true;

          AdicionarLog($"✅ {inventario.Jobs.Count} SQL Agent Job(s):");
          foreach (var job in inventario.Jobs.Take(3))
          {
            AdicionarLog($"   • {job}");
          }
          if (inventario.Jobs.Count > 3)
            AdicionarLog($"   ... e mais {inventario.Jobs.Count - 3}");
        }
        else
        {
          AdicionarLog("ℹ️ Nenhum Job encontrado (SQL Agent pode estar parado)");
        }

        AdicionarLog("");
        AdicionarLog("═══════════════════════════════════════════════");
        AdicionarLog("✅ INVENTÁRIO CONCLUÍDO!");
        AdicionarLog("═══════════════════════════════════════════════");
        btnExecutar.Enabled = true;
      }
      catch (Exception ex)
      {
        AdicionarLog($"❌ ERRO: {ex.Message}");
        MessageBox.Show(
          $"Não foi possível conectar ao servidor.\n\n" +
          $"Erro: {ex.Message}\n\n" +
          "Verifique:\n" +
          "• Connection String está correta?\n" +
          "• Servidor está acessível?\n" +
          "• Credenciais têm permissões adequadas?",
          "Erro de Conexão",
          MessageBoxButtons.OK,
          MessageBoxIcon.Error
        );
      }
    }

    private void BtnExecutar_Click(object sender, EventArgs e)
    {
      bool temAlgoSelecionado =
        clbDatabases.CheckedItems.Count > 0 ||
        chkLinkedServers.Checked ||
        chkJobs.Checked;

      if (!temAlgoSelecionado)
      {
        MessageBox.Show(
          "⚠️ Nenhum objeto selecionado!\n\n" +
          "Selecione pelo menos:\n" +
          "  • Um banco de dados, ou\n" +
          "  • Linked Servers, ou\n" +
          "  • SQL Agent Jobs",
          "Seleção Vazia",
          MessageBoxButtons.OK,
          MessageBoxIcon.Warning
        );
        return;
      }

      DestinationDialog destDialog = new DestinationDialog();
      if (destDialog.ShowDialog() != DialogResult.OK)
      {
        AdicionarLog("⚠️ Operação cancelada pelo usuário");
        return;
      }

      string servidorDestino = destDialog.ServerName;

      var config = new UnifiedMigrationService.MigrationConfig
      {
        DatabaseNames = clbDatabases.CheckedItems.Cast<string>().ToList(),
        IncludeLinkedServers = chkLinkedServers.Checked,
        IncludeJobs = chkJobs.Checked,
        IsOnline = rbOnline.Checked,
        ServerDestino = servidorDestino,
        OutputPath = rbOffline.Checked ? ObterPastaOutput() : "",
        PastaBackup = ""
      };

      if (rbOffline.Checked && string.IsNullOrEmpty(config.OutputPath))
      {
        AdicionarLog("⚠️ Pasta de saída não informada");
        return;
      }

      string resumo = $"Confirma a migração?\n\n" +
                     $"🎯 Destino: {servidorDestino}\n" +
                     $"📦 Bancos: {config.DatabaseNames.Count}\n" +
                     $"🔗 Linked Servers: {(config.IncludeLinkedServers ? "Sim" : "Não")}\n" +
                     $"⏱️ Jobs: {(config.IncludeJobs ? "Sim" : "Não")}\n" +
                     $"🔄 Modo: {(config.IsOnline ? "ONLINE" : "OFFLINE")}";

      var resultado = MessageBox.Show(
        resumo,
        "Confirmar Migração",
        MessageBoxButtons.YesNo,
        MessageBoxIcon.Question
      );

      if (resultado != DialogResult.Yes)
      {
        AdicionarLog("⚠️ Migração cancelada pelo usuário");
        return;
      }

      try
      {
        var progressForm = new UnifiedProgressForm(txtConnectionString.Text, config);
        progressForm.ShowDialog();

        AdicionarLog("✅ Processo de migração finalizado!");
        AdicionarLog("   Verifique os logs detalhados para mais informações.");
      }
      catch (Exception ex)
      {
        MessageBox.Show(
          $"Erro ao iniciar migração:\n\n{ex.Message}",
          "Erro",
          MessageBoxButtons.OK,
          MessageBoxIcon.Error
        );
        AdicionarLog($"❌ ERRO: {ex.Message}");
      }
    }

    private string ObterPastaOutput()
    {
      using (var fbd = new FolderBrowserDialog())
      {
        fbd.Description = "Escolha a pasta para salvar os scripts SQL:";
        fbd.ShowNewFolderButton = true;
        fbd.SelectedPath = @"C:\MigracaoSQL_Scripts";

        if (fbd.ShowDialog() == DialogResult.OK)
        {
          return fbd.SelectedPath;
        }
      }
      return "";
    }

    private void BtnSobre_Click(object sender, EventArgs e)
    {
      MessageBox.Show(
        "CQLE MIGRAÇÃO v2.0\n\n" +
        "Sistema Profissional de Migração SQL Server\n\n" +
        "📦 Funcionalidades:\n" +
        "  ✓ Migração de Bancos de Dados\n" +
        "  ✓ Migração de Linked Servers\n" +
        "  ✓ Migração de SQL Agent Jobs\n" +
        "  ✓ Modos Online e Offline\n" +
        "  ✓ Logs detalhados exportáveis\n\n" +
        "Desenvolvido por Marciano Silva\n" +
        "CQLE Softwares © 2025",
        "Sobre o Sistema",
        MessageBoxButtons.OK,
        MessageBoxIcon.Information
      );
    }

    private void BtnSair_Click(object sender, EventArgs e)
    {
      var resultado = MessageBox.Show(
        "Deseja realmente sair do sistema?",
        "Confirmar Saída",
        MessageBoxButtons.YesNo,
        MessageBoxIcon.Question
      );

      if (resultado == DialogResult.Yes)
      {
        Application.Exit();
      }
    }

    private void MigrationForm_FormClosing(object sender, FormClosingEventArgs e)
    {
      var resultado = MessageBox.Show(
        "Deseja realmente sair do sistema?",
        "Confirmar Saída",
        MessageBoxButtons.YesNo,
        MessageBoxIcon.Question
      );

      if (resultado == DialogResult.No)
      {
        e.Cancel = true;
      }
      else
      {
        Application.Exit();
      }
    }

    private void AdicionarLog(string mensagem)
    {
      string logEntry = $"[{DateTime.Now:HH:mm:ss}] {mensagem}\r\n";

      if (txtLog.InvokeRequired)
      {
        txtLog.Invoke(new Action(() => txtLog.AppendText(logEntry)));
      }
      else
      {
        txtLog.AppendText(logEntry);
        txtLog.SelectionStart = txtLog.Text.Length;
        txtLog.ScrollToCaret();
      }
    }

    protected override void Dispose(bool disposing)
    {
      if (disposing && (components != null))
      {
        components.Dispose();
      }
      base.Dispose(disposing);
    }
  }
}