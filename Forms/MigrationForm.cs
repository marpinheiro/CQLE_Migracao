#nullable disable
using System;
using System.Drawing;
using System.Windows.Forms;
using CQLE_MIGRACAO.Services;
using System.Collections.Generic;

namespace CQLE_MIGRACAO.Forms
{
  public class MigrationForm : Form
  {
    private Panel panelHeader;
    private Label lblTitulo;
    private Label lblSubtitulo;
    private GroupBox grpOrigem;
    private GroupBox grpDestino;
    private GroupBox grpOpcoes;
    private GroupBox grpObjetos;
    private CheckedListBox lstBancos;
    private TextBox txtServidorDestino;
    private TextBox txtPastaBackup;
    private TextBox txtPastaOutput;
    private CheckBox chkMigrarJobs;
    private CheckBox chkMigrarLinkedServers;
    private CheckBox chkMigrarLogins;
    private Button btnSelecionarTodos;
    private Button btnDesmarcarTodos;
    private Button btnIniciar;
    private Button btnVoltar;
    private Label lblRodape;

    private readonly UnifiedMigrationService _migrationService;
    private readonly List<string> _todosBancos = new List<string>();

    public MigrationForm(string connectionStringOrigem)
    {
      _migrationService = new UnifiedMigrationService(connectionStringOrigem);

      InitializeComponent();
      CarregarInventario();
    }

    private void InitializeComponent()
    {
      this.Text = "CQLE Migração - Configuração";
      this.Size = new Size(950, 750);
      this.MinimumSize = new Size(950, 750);
      this.StartPosition = FormStartPosition.CenterScreen;
      this.FormBorderStyle = FormBorderStyle.Sizable;
      this.MaximizeBox = true;
      this.BackColor = Color.FromArgb(240, 240, 245);

      try
      {
        this.Icon = new Icon("Assets/CQLE.ico");
      }
      catch { }

      // Header
      panelHeader = new Panel
      {
        Location = new Point(0, 0),
        Size = new Size(950, 80),
        BackColor = Color.FromArgb(0, 120, 215)
      };

      lblTitulo = new Label
      {
        Text = "🗄️ CQLE MIGRAÇÃO",
        Location = new Point(20, 15),
        Size = new Size(600, 30),
        Font = new Font("Segoe UI", 18, FontStyle.Bold),
        ForeColor = Color.White
      };

      lblSubtitulo = new Label
      {
        Text = "Sistema Profissional de Migração SQL Server",
        Location = new Point(20, 50),
        Size = new Size(600, 20),
        Font = new Font("Segoe UI", 10),
        ForeColor = Color.FromArgb(200, 220, 255)
      };

      panelHeader.Controls.Add(lblTitulo);
      panelHeader.Controls.Add(lblSubtitulo);

      // Grupo Origem
      grpOrigem = new GroupBox
      {
        Text = " Origem (Conectado) ",
        Location = new Point(20, 100),
        Size = new Size(910, 70),
        Font = new Font("Segoe UI", 10, FontStyle.Bold),
        ForeColor = Color.FromArgb(0, 120, 215)
      };

      Label lblOrigemInfo = new Label
      {
        Text = "Servidor de origem já conectado via login. Todos os objetos serão lidos daqui.",
        Location = new Point(15, 25),
        Size = new Size(880, 30),
        Font = new Font("Segoe UI", 9.5f)
      };
      grpOrigem.Controls.Add(lblOrigemInfo);

      // Grupo Destino - LAYOUT MELHORADO
      grpDestino = new GroupBox
      {
        Text = " Destino ",
        Location = new Point(20, 190),
        Size = new Size(910, 160),
        Font = new Font("Segoe UI", 10, FontStyle.Bold),
        ForeColor = Color.FromArgb(0, 120, 215)
      };

      // Linha 1: Servidor Destino
      Label lblServidor = new Label
      {
        Text = "Servidor Destino:",
        Location = new Point(20, 30),
        AutoSize = true,
        Font = new Font("Segoe UI", 10)
      };

      txtServidorDestino = new TextBox
      {
        Location = new Point(20, 55),
        Size = new Size(870, 28),
        Font = new Font("Segoe UI", 10)
      };

      // Linha 2: Pasta de Backup
      Label lblBackup = new Label
      {
        Text = "Pasta de Backup:",
        Location = new Point(20, 95),
        AutoSize = true,
        Font = new Font("Segoe UI", 10)
      };

      txtPastaBackup = new TextBox
      {
        Location = new Point(20, 120),
        Size = new Size(800, 28),
        Font = new Font("Segoe UI", 10)
      };

      Button btnBrowseBackup = new Button
      {
        Text = "...",
        Location = new Point(830, 120),
        Size = new Size(60, 28),
        Font = new Font("Segoe UI", 10, FontStyle.Bold)
      };
      btnBrowseBackup.Click += (s, e) =>
      {
        using var fbd = new FolderBrowserDialog();
        if (fbd.ShowDialog() == DialogResult.OK)
          txtPastaBackup.Text = fbd.SelectedPath;
      };

      // Linha 3: Pasta para Scripts de Backup
      Label lblOutput = new Label
      {
        Text = "Pasta para Scripts de Backup:",
        Location = new Point(20, 160),
        AutoSize = true,
        Font = new Font("Segoe UI", 10)
      };

      txtPastaOutput = new TextBox
      {
        Location = new Point(20, 185),
        Size = new Size(800, 28),
        Font = new Font("Segoe UI", 10)
      };

      Button btnBrowseOutput = new Button
      {
        Text = "...",
        Location = new Point(830, 185),
        Size = new Size(60, 28),
        Font = new Font("Segoe UI", 10, FontStyle.Bold)
      };
      btnBrowseOutput.Click += (s, e) =>
      {
        using var fbd = new FolderBrowserDialog();
        if (fbd.ShowDialog() == DialogResult.OK)
          txtPastaOutput.Text = fbd.SelectedPath;
      };

      grpDestino.Controls.AddRange(new Control[]
      {
                lblServidor, txtServidorDestino,
                lblBackup, txtPastaBackup, btnBrowseBackup,
                lblOutput, txtPastaOutput, btnBrowseOutput
      });

      // Grupo Opções
      grpOpcoes = new GroupBox
      {
        Text = " Opções de Migração ",
        Location = new Point(20, 370),
        Size = new Size(910, 100),
        Font = new Font("Segoe UI", 10, FontStyle.Bold),
        ForeColor = Color.FromArgb(0, 120, 215)
      };

      chkMigrarJobs = new CheckBox { Text = "🔧 Migrar SQL Agent Jobs", Location = new Point(20, 30), Size = new Size(280, 30), Checked = true, Font = new Font("Segoe UI", 10) };
      chkMigrarLinkedServers = new CheckBox { Text = "🔗 Migrar Linked Servers", Location = new Point(20, 65), Size = new Size(280, 30), Checked = true, Font = new Font("Segoe UI", 10) };
      chkMigrarLogins = new CheckBox { Text = "🔑 Migrar Logins do Servidor", Location = new Point(320, 30), Size = new Size(300, 30), Checked = true, Font = new Font("Segoe UI", 10) };

      grpOpcoes.Controls.AddRange(new Control[] { chkMigrarJobs, chkMigrarLinkedServers, chkMigrarLogins });

      // Grupo Bancos
      grpObjetos = new GroupBox
      {
        Text = " Bancos de Dados Disponíveis ",
        Location = new Point(20, 480),
        Size = new Size(910, 180),
        Font = new Font("Segoe UI", 10, FontStyle.Bold),
        ForeColor = Color.FromArgb(0, 120, 215)
      };

      lstBancos = new CheckedListBox
      {
        Location = new Point(20, 30),
        Size = new Size(650, 130),
        Font = new Font("Segoe UI", 10),
        CheckOnClick = true
      };

      btnSelecionarTodos = new Button
      {
        Text = "Selecionar Todos",
        Location = new Point(690, 40),
        Size = new Size(200, 35),
        BackColor = Color.FromArgb(0, 120, 0),
        ForeColor = Color.White,
        Font = new Font("Segoe UI", 10, FontStyle.Bold),
        FlatStyle = FlatStyle.Flat
      };
      btnSelecionarTodos.Click += (s, e) =>
      {
        for (int i = 0; i < lstBancos.Items.Count; i++) lstBancos.SetItemChecked(i, true);
      };

      btnDesmarcarTodos = new Button
      {
        Text = "Desmarcar Todos",
        Location = new Point(690, 85),
        Size = new Size(200, 35),
        BackColor = Color.FromArgb(180, 0, 0),
        ForeColor = Color.White,
        Font = new Font("Segoe UI", 10, FontStyle.Bold),
        FlatStyle = FlatStyle.Flat
      };
      btnDesmarcarTodos.Click += (s, e) =>
      {
        for (int i = 0; i < lstBancos.Items.Count; i++) lstBancos.SetItemChecked(i, false);
      };

      grpObjetos.Controls.AddRange(new Control[] { lstBancos, btnSelecionarTodos, btnDesmarcarTodos });

      // Botões principais
      btnIniciar = new Button
      {
        Text = "🚀 Iniciar Migração",
        Location = new Point(20, 680),
        Size = new Size(300, 50),
        BackColor = Color.FromArgb(0, 120, 0),
        ForeColor = Color.White,
        Font = new Font("Segoe UI", 12, FontStyle.Bold),
        FlatStyle = FlatStyle.Flat
      };
      btnIniciar.Click += BtnIniciar_Click;

      btnVoltar = new Button
      {
        Text = "⬅ Voltar",
        Location = new Point(630, 680),
        Size = new Size(300, 50),
        BackColor = Color.FromArgb(100, 100, 100),
        ForeColor = Color.White,
        Font = new Font("Segoe UI", 12, FontStyle.Bold),
        FlatStyle = FlatStyle.Flat
      };
      btnVoltar.Click += (s, e) => this.Close();

      // Rodapé
      lblRodape = new Label
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
                panelHeader, grpOrigem, grpDestino, grpOpcoes, grpObjetos,
                btnIniciar, btnVoltar, lblRodape
      });
    }

    private void CarregarInventario()
    {
      try
      {
        var inventory = _migrationService.GetInventario();
        _todosBancos.AddRange(inventory.Databases);

        lstBancos.Items.Clear();
        foreach (var db in _todosBancos)
        {
          if (db != "master" && db != "tempdb" && db != "model" && db != "msdb")
            lstBancos.Items.Add(db);
        }
      }
      catch (Exception ex)
      {
        MessageBox.Show($"Erro ao carregar inventário: {ex.Message}", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
      }
    }

    private void BtnIniciar_Click(object sender, EventArgs e)
    {
      if (lstBancos.CheckedItems.Count == 0)
      {
        MessageBox.Show("Selecione pelo menos um banco de dados.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        return;
      }

      if (string.IsNullOrWhiteSpace(txtServidorDestino.Text))
      {
        MessageBox.Show("Informe o servidor destino.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        return;
      }

      var config = new UnifiedMigrationService.MigrationConfig
      {
        DatabaseNames = new List<string>(),
        IncludeJobs = chkMigrarJobs.Checked,
        IncludeLinkedServers = chkMigrarLinkedServers.Checked,
        IncludeLogins = chkMigrarLogins.Checked,
        ServerDestino = txtServidorDestino.Text.Trim(),
        PastaBackup = txtPastaBackup.Text.Trim(),
        OutputPath = txtPastaOutput.Text.Trim()
      };

      foreach (var item in lstBancos.CheckedItems)
        config.DatabaseNames.Add(item.ToString());

      this.Hide();
      var progressForm = new UnifiedProgressForm(_migrationService, config);
      progressForm.FormClosed += (s, args) => this.Close();
      progressForm.Show();
    }
  }
}