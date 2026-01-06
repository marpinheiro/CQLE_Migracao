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
    private System.ComponentModel.IContainer? components = null;

    // --- Controles da Interface ---
    private TextBox txtConnectionString = null!;
    private CheckedListBox clbDatabases = null!;
    private CheckBox chkJobs = null!;
    private CheckBox chkLinkedServers = null!;
    private RadioButton rbOnline = null!;
    private RadioButton rbOffline = null!;
    private TextBox txtLog = null!;
    private Button btnListar = null!;
    private Button btnExecutar = null!;

    // Novos controles para informações
    private Label lblJobsCount = null!;
    private Label lblLinkedServersCount = null!;
    private Label lblDatabasesCount = null!;

    public MigrationForm()
    {
      InitializeComponent();
      ConfigurarInterfaceAvancada();
    }

    private void InitializeComponent()
    {
      this.components = new System.ComponentModel.Container();
      this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
      this.ClientSize = new System.Drawing.Size(900, 650);
      this.Text = "CQLE Migração - Dashboard Unificado v2.0";
      this.StartPosition = FormStartPosition.CenterScreen;
      this.BackColor = Color.FromArgb(240, 240, 245);
      this.Icon = SystemIcons.Application;
    }

    private void ConfigurarInterfaceAvancada()
    {
      // --- CABEÇALHO ---
      Panel panelHeader = new Panel
      {
        Location = new Point(0, 0),
        Size = new Size(900, 60),
        BackColor = Color.FromArgb(0, 120, 215)
      };

      Label lblTitle = new Label
      {
        Text = "🗄️ CQLE MIGRATION AUTOMATOR",
        Location = new Point(20, 12),
        Size = new Size(500, 20),
        Font = new Font("Segoe UI", 14, FontStyle.Bold),
        ForeColor = Color.White
      };

      Label lblSubtitle = new Label
      {
        Text = "Sistema Profissional de Migração SQL Server",
        Location = new Point(20, 35),
        Size = new Size(500, 15),
        Font = new Font("Segoe UI", 8),
        ForeColor = Color.FromArgb(200, 220, 255)
      };

      panelHeader.Controls.Add(lblTitle);
      panelHeader.Controls.Add(lblSubtitle);
      this.Controls.Add(panelHeader);

      // --- GRUPO 1: CONEXÃO ---
      GroupBox grpConexao = new GroupBox
      {
        Text = "  1️⃣ Conexão com Servidor de Origem  ",
        Location = new Point(20, 75),
        Size = new Size(850, 90),
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
        Size = new Size(670, 25),
        Font = new Font("Consolas", 9)
      };

      btnListar = new Button
      {
        Text = "🔍 Conectar & Inventariar",
        Location = new Point(700, 48),
        Size = new Size(130, 30),
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

      // --- GRUPO 2: SELEÇÃO DE OBJETOS ---
      GroupBox grpSelecao = new GroupBox
      {
        Text = "  2️⃣ Objetos para Migração  ",
        Location = new Point(20, 180),
        Size = new Size(420, 330),
        Font = new Font("Segoe UI", 9, FontStyle.Bold),
        ForeColor = Color.FromArgb(0, 120, 215)
      };

      // Bancos de Dados
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
        Location = new Point(150, 32),
        AutoSize = true,
        ForeColor = Color.Gray,
        Font = new Font("Segoe UI", 8)
      };

      clbDatabases = new CheckedListBox
      {
        Location = new Point(15, 55),
        Size = new Size(390, 160),
        CheckOnClick = true,
        Font = new Font("Consolas", 9),
        IntegralHeight = false
      };

      // Botões de seleção
      Button btnSelectAll = new Button
      {
        Text = "✓ Todos",
        Location = new Point(15, 220),
        Size = new Size(75, 25),
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
        Location = new Point(100, 220),
        Size = new Size(80, 25),
        FlatStyle = FlatStyle.Flat,
        Font = new Font("Segoe UI", 8),
        Cursor = Cursors.Hand
      };
      btnSelectNone.Click += (s, e) =>
      {
        for (int i = 0; i < clbDatabases.Items.Count; i++)
          clbDatabases.SetItemChecked(i, false);
      };

      // Separator
      Panel separator1 = new Panel
      {
        Location = new Point(15, 255),
        Size = new Size(390, 2),
        BackColor = Color.LightGray
      };

      // Objetos do Sistema
      Label lblSistema = new Label
      {
        Text = "⚙️ Objetos de Sistema:",
        Location = new Point(15, 265),
        AutoSize = true,
        Font = new Font("Segoe UI", 9, FontStyle.Bold)
      };

      chkLinkedServers = new CheckBox
      {
        Text = "🔗 Linked Servers",
        Location = new Point(30, 290),
        AutoSize = true,
        Font = new Font("Segoe UI", 9)
      };

      lblLinkedServersCount = new Label
      {
        Text = "(0 encontrados)",
        Location = new Point(180, 292),
        AutoSize = true,
        ForeColor = Color.Gray,
        Font = new Font("Segoe UI", 8)
      };

      chkJobs = new CheckBox
      {
        Text = "⏱️ SQL Agent Jobs",
        Location = new Point(270, 290),
        AutoSize = true,
        Font = new Font("Segoe UI", 9)
      };

      lblJobsCount = new Label
      {
        Text = "(0 encontrados)",
        Location = new Point(270, 307),
        AutoSize = true,
        ForeColor = Color.Gray,
        Font = new Font("Segoe UI", 8)
      };

      grpSelecao.Controls.Add(lblDb);
      grpSelecao.Controls.Add(lblDatabasesCount);
      grpSelecao.Controls.Add(clbDatabases);
      grpSelecao.Controls.Add(btnSelectAll);
      grpSelecao.Controls.Add(btnSelectNone);
      grpSelecao.Controls.Add(separator1);
      grpSelecao.Controls.Add(lblSistema);
      grpSelecao.Controls.Add(chkLinkedServers);
      grpSelecao.Controls.Add(lblLinkedServersCount);
      grpSelecao.Controls.Add(chkJobs);
      grpSelecao.Controls.Add(lblJobsCount);
      this.Controls.Add(grpSelecao);

      // --- GRUPO 3: ESTRATÉGIA ---
      GroupBox grpModo = new GroupBox
      {
        Text = "  3️⃣ Estratégia de Migração  ",
        Location = new Point(450, 180),
        Size = new Size(420, 160),
        Font = new Font("Segoe UI", 9, FontStyle.Bold),
        ForeColor = Color.FromArgb(0, 120, 215)
      };

      rbOnline = new RadioButton
      {
        Text = "🟢 ONLINE - Conectar e criar objetos direto no destino",
        Location = new Point(20, 35),
        Size = new Size(380, 25),
        Checked = true,
        Font = new Font("Segoe UI", 9, FontStyle.Bold),
        ForeColor = Color.FromArgb(0, 150, 0)
      };

      Label lblOnlineDesc = new Label
      {
        Text = "• Conecta automaticamente no servidor destino\n" +
               "• Cria bancos, linked servers e jobs em tempo real\n" +
               "• Ideal para ambientes homogêneos",
        Location = new Point(40, 55),
        Size = new Size(360, 45),
        Font = new Font("Segoe UI", 8),
        ForeColor = Color.DimGray
      };

      rbOffline = new RadioButton
      {
        Text = "🟠 OFFLINE - Gerar scripts SQL para execução manual",
        Location = new Point(20, 105),
        Size = new Size(380, 25),
        Font = new Font("Segoe UI", 9, FontStyle.Bold),
        ForeColor = Color.FromArgb(255, 140, 0)
      };

      Label lblOfflineDesc = new Label
      {
        Text = "• Gera arquivos .sql sem conectar no destino\n" +
               "• Permite revisão manual dos scripts\n" +
               "• Ideal para ambientes controlados/auditados",
        Location = new Point(40, 125),
        Size = new Size(360, 40),
        Font = new Font("Segoe UI", 8),
        ForeColor = Color.DimGray
      };

      grpModo.Controls.Add(rbOnline);
      grpModo.Controls.Add(lblOnlineDesc);
      grpModo.Controls.Add(rbOffline);
      grpModo.Controls.Add(lblOfflineDesc);
      this.Controls.Add(grpModo);

      // --- BOTÃO PRINCIPAL ---
      btnExecutar = new Button
      {
        Text = "🚀 INICIAR MIGRAÇÃO AUTOMATIZADA",
        Location = new Point(450, 355),
        Size = new Size(420, 70),
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

      // Info adicional
      Label lblInfo = new Label
      {
        Text = "💡 Dica: Use o modo OFFLINE para revisar scripts antes de executar",
        Location = new Point(450, 435),
        Size = new Size(420, 30),
        Font = new Font("Segoe UI", 8, FontStyle.Italic),
        ForeColor = Color.Gray,
        TextAlign = ContentAlignment.MiddleCenter
      };
      this.Controls.Add(lblInfo);

      // Botão Sobre
      Button btnAbout = new Button
      {
        Text = "ℹ️",
        Location = new Point(450, 470),
        Size = new Size(35, 35),
        FlatStyle = FlatStyle.Flat,
        Font = new Font("Segoe UI", 12, FontStyle.Bold),
        Cursor = Cursors.Hand,
        BackColor = Color.FromArgb(240, 240, 245)
      };
      btnAbout.FlatAppearance.BorderColor = Color.LightGray;
      btnAbout.Click += (s, e) =>
      {
        MessageBox.Show(
          "CQLE MIGRATION AUTOMATOR v2.0\n\n" +
          "Sistema Profissional de Migração SQL Server\n\n" +
          "📦 Funcionalidades:\n" +
          "  ✓ Migração de Bancos de Dados (Backup/Restore)\n" +
          "  ✓ Migração de Linked Servers (SMO Scripting)\n" +
          "  ✓ Migração de SQL Agent Jobs (SMO Scripting)\n" +
          "  ✓ Modos Online e Offline\n" +
          "  ✓ Logs detalhados exportáveis\n\n" +
          "🛠️ Tecnologias:\n" +
          "  • C# .NET\n" +
          "  • ADO.NET (SqlClient)\n" +
          "  • SQL Server Management Objects (SMO)\n\n" +
          "Desenvolvido para operações profissionais de DBA",
          "Sobre o Sistema",
          MessageBoxButtons.OK,
          MessageBoxIcon.Information
        );
      };
      this.Controls.Add(btnAbout);

      // --- LOG ---
      Label lblLog = new Label
      {
        Text = "📋 Log de Operações:",
        Location = new Point(20, 515),
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
        Location = new Point(20, 540),
        Size = new Size(850, 90)
      };
      this.Controls.Add(txtLog);
    }

    // --- EVENTOS ---

    private void BtnListar_Click(object? sender, EventArgs e)
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

        // Bancos de Dados
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

        // Linked Servers
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

        // Jobs
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

    private void BtnExecutar_Click(object? sender, EventArgs e)
    {
      // Validações
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

      // Dialog de destino
      DestinationDialog destDialog = new DestinationDialog();
      if (destDialog.ShowDialog() != DialogResult.OK)
      {
        AdicionarLog("⚠️ Operação cancelada pelo usuário");
        return;
      }

      string servidorDestino = destDialog.ServerName;

      // Prepara configuração
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

      // Validação pasta offline
      if (rbOffline.Checked && string.IsNullOrEmpty(config.OutputPath))
      {
        AdicionarLog("⚠️ Pasta de saída não informada");
        return;
      }

      // Confirmação final
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

      // Abre tela de progresso
      try
      {
        var progressForm = new UnifiedProgressForm(txtConnectionString.Text, config);
        this.Hide();
        progressForm.ShowDialog();
        this.Show();

        AdicionarLog("✅ Processo de migração finalizado!");
        AdicionarLog("   Verifique os logs detalhados para mais informações.");
      }
      catch (Exception ex)
      {
        this.Show();
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