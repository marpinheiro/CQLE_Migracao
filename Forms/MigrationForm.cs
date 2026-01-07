#nullable disable
using System;
using System.Drawing;
using System.Windows.Forms;
using CQLE_MIGRACAO.Services;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;

namespace CQLE_MIGRACAO.Forms
{
  public class MigrationForm : Form
  {
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
    private Button btnConectarDestino;

    private readonly UnifiedMigrationService _migrationService;
    private readonly List<string> _todosBancos = new List<string>();
    private string _connectionStringDestino;

    private readonly string _servidorOrigem;
    private readonly string _usuarioOrigem;

    public MigrationForm(string connectionStringOrigem, string servidorOrigem, string usuarioOrigem)
    {
      _migrationService = new UnifiedMigrationService(connectionStringOrigem);
      _servidorOrigem = servidorOrigem;
      _usuarioOrigem = usuarioOrigem;

      InitializeComponent();
      CarregarInventario();
    }

    private void InitializeComponent()
    {
      this.Text = "CQLE Migração - Migração Completa";
      this.StartPosition = FormStartPosition.CenterScreen;
      this.WindowState = FormWindowState.Maximized;
      this.MinimumSize = new Size(900, 600);
      this.FormBorderStyle = FormBorderStyle.Sizable;
      this.BackColor = Color.WhiteSmoke;
      this.Font = new Font("Segoe UI", 10F);

      try { this.Icon = new Icon("Assets/CQLE.ico"); } catch { }

      // === CABEÇALHO AZUL ===
      var panelHeader = new Panel
      {
        Dock = DockStyle.Top,
        Height = 140,
        BackColor = Color.FromArgb(0, 120, 215)
      };

      var lblTitulo = new Label
      {
        Text = "🗄️ CQLE MIGRAÇÃO",
        ForeColor = Color.White,
        Font = new Font("Segoe UI", 28F, FontStyle.Bold),
        AutoSize = true,
        Location = new Point(40, 30)
      };

      var lblSubtitulo = new Label
      {
        Text = "Migração Completa para Servidor Novo ou Limpo",
        ForeColor = Color.FromArgb(200, 220, 255),
        Font = new Font("Segoe UI", 14F),
        AutoSize = true,
        Location = new Point(42, 85)
      };

      panelHeader.Controls.AddRange(new Control[] { lblTitulo, lblSubtitulo });

      // === CONTEÚDO PRINCIPAL ===
      var panelMain = new Panel
      {
        Dock = DockStyle.Fill,
        Padding = new Padding(40, 30, 40, 30),
        AutoScroll = true
      };

      var tableLayout = new TableLayoutPanel
      {
        Dock = DockStyle.Top,
        AutoSize = true,
        ColumnCount = 1,
        RowCount = 5,
        Padding = new Padding(0, 0, 0, 40)
      };

      // Grupo Destino
      var grpDestino = new GroupBox
      {
        Text = "🎯 Configurações do Servidor Destino",
        Dock = DockStyle.Top,
        Height = 240,
        Font = new Font("Segoe UI", 11F, FontStyle.Bold),
        ForeColor = Color.FromArgb(0, 120, 215),
        Padding = new Padding(20)
      };

      var lblServidor = new Label { Text = "Servidor Destino:", AutoSize = true, Font = new Font("Segoe UI", 10F, FontStyle.Bold), Location = new Point(20, 30) };
      txtServidorDestino = new TextBox { Location = new Point(20, 55), Width = 600, Height = 30, ReadOnly = true };

      btnConectarDestino = new Button
      {
        Text = "🔌 Conectar",
        Location = new Point(630, 53),
        Size = new Size(180, 35),
        BackColor = Color.FromArgb(0, 120, 215),
        ForeColor = Color.White,
        FlatStyle = FlatStyle.Flat,
        Font = new Font("Segoe UI", 11F, FontStyle.Bold),
        Cursor = Cursors.Hand
      };
      btnConectarDestino.FlatAppearance.BorderSize = 0;
      btnConectarDestino.Click += BtnConectarDestino_Click;

      txtPastaBackup = CriarCampoPasta(grpDestino, "Pasta para Arquivos de Backup (.bak):", 20, 100, "");
      txtPastaOutput = CriarCampoPasta(grpDestino, "Pasta para Scripts Offline (opcional):", 20, 150, "");

      grpDestino.Controls.AddRange(new Control[] { lblServidor, txtServidorDestino, btnConectarDestino });
      tableLayout.Controls.Add(grpDestino, 0, 0);

      // Grupo Opções
      var grpOpcoes = new GroupBox
      {
        Text = "⚙️ Opções Adicionais de Migração",
        Dock = DockStyle.Top,
        Height = 120,
        Font = new Font("Segoe UI", 11F, FontStyle.Bold),
        ForeColor = Color.FromArgb(0, 120, 215),
        Padding = new Padding(20)
      };

      chkMigrarJobs = new CheckBox { Text = "📅 Migrar Jobs do Agendador", Location = new Point(20, 35), Checked = true };
      chkMigrarLinkedServers = new CheckBox { Text = "🔗 Migrar Linked Servers", Location = new Point(20, 70), Checked = true };
      chkMigrarLogins = new CheckBox { Text = "🔑 Migrar Logins do Servidor", Location = new Point(400, 35), Checked = true };

      grpOpcoes.Controls.AddRange(new Control[] { chkMigrarJobs, chkMigrarLinkedServers, chkMigrarLogins });
      tableLayout.Controls.Add(grpOpcoes, 0, 1);

      // Grupo Bancos
      var grpBancos = new GroupBox
      {
        Text = "🗃️ Bancos de Dados Disponíveis",
        Dock = DockStyle.Top,
        Height = 280,
        Font = new Font("Segoe UI", 11F, FontStyle.Bold),
        ForeColor = Color.FromArgb(0, 120, 215),
        Padding = new Padding(20)
      };

      lstBancos = new CheckedListBox
      {
        Dock = DockStyle.Fill,
        CheckOnClick = true,
        Margin = new Padding(0, 0, 250, 0)
      };

      var panelBotoesBancos = new Panel { Dock = DockStyle.Right, Width = 250 };

      btnSelecionarTodos = new Button
      {
        Text = "Selecionar Todos",
        Size = new Size(220, 45),
        Location = new Point(15, 50),
        BackColor = Color.FromArgb(0, 150, 0),
        ForeColor = Color.White,
        FlatStyle = FlatStyle.Flat,
        Font = new Font("Segoe UI", 10F, FontStyle.Bold),
        Cursor = Cursors.Hand
      };
      btnSelecionarTodos.FlatAppearance.BorderSize = 0;
      btnSelecionarTodos.Click += (s, e) =>
      {
        for (int i = 0; i < lstBancos.Items.Count; i++)
          lstBancos.SetItemChecked(i, true);
      };

      btnDesmarcarTodos = new Button
      {
        Text = "Desmarcar Todos",
        Size = new Size(220, 45),
        Location = new Point(15, 110),
        BackColor = Color.FromArgb(180, 0, 0),
        ForeColor = Color.White,
        FlatStyle = FlatStyle.Flat,
        Font = new Font("Segoe UI", 10F, FontStyle.Bold),
        Cursor = Cursors.Hand
      };
      btnDesmarcarTodos.FlatAppearance.BorderSize = 0;
      btnDesmarcarTodos.Click += (s, e) =>
      {
        for (int i = 0; i < lstBancos.Items.Count; i++)
          lstBancos.SetItemChecked(i, false);
      };

      panelBotoesBancos.Controls.AddRange(new Control[] { btnSelecionarTodos, btnDesmarcarTodos });

      grpBancos.Controls.Add(lstBancos);
      grpBancos.Controls.Add(panelBotoesBancos);
      tableLayout.Controls.Add(grpBancos, 0, 2);

      // Botões principais
      var panelBotoes = new Panel { Dock = DockStyle.Top, Height = 100 };
      btnIniciar = new Button
      {
        Text = "🚀 Iniciar Migração Completa",
        Size = new Size(400, 60),
        Location = new Point(50, 20),
        Font = new Font("Segoe UI", 14F, FontStyle.Bold),
        BackColor = Color.FromArgb(0, 120, 215),
        ForeColor = Color.White,
        FlatStyle = FlatStyle.Flat,
        Enabled = false
      };
      btnIniciar.FlatAppearance.BorderSize = 0;
      btnIniciar.Click += BtnIniciar_Click;

      btnVoltar = new Button
      {
        Text = "⬅ Voltar ao Menu",
        Size = new Size(350, 60),
        Location = new Point(470, 20),
        Font = new Font("Segoe UI", 14F, FontStyle.Bold),
        BackColor = Color.FromArgb(100, 100, 100),
        ForeColor = Color.White,
        FlatStyle = FlatStyle.Flat
      };
      btnVoltar.FlatAppearance.BorderSize = 0;
      btnVoltar.Click += (s, e) => this.Close();

      panelBotoes.Controls.AddRange(new Control[] { btnIniciar, btnVoltar });
      tableLayout.Controls.Add(panelBotoes, 0, 3);

      panelMain.Controls.Add(tableLayout);

      // === RODAPÉ COM INFORMAÇÃO DE CONEXÃO (sem corte) ===
      var panelRodape = new Panel
      {
        Dock = DockStyle.Bottom,
        Height = 50,
        BackColor = Color.FromArgb(245, 245, 245)
      };

      var tableRodape = new TableLayoutPanel
      {
        Dock = DockStyle.Fill,
        ColumnCount = 2,
        RowCount = 1,
        Padding = new Padding(40, 10, 40, 10)
      };
      tableRodape.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 70F));
      tableRodape.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30F));

      var lblConexao = new Label
      {
        Text = $"✅ Conectado à Origem: {_servidorOrigem}  •  Usuário: {_usuarioOrigem}",
        Font = new Font("Segoe UI", 10F),
        ForeColor = Color.DarkGreen,
        Dock = DockStyle.Fill,
        TextAlign = ContentAlignment.MiddleLeft
      };

      var lblCopyright = new Label
      {
        Text = "Desenvolvido por Marciano Silva - CQLE Softwares © 2026",
        Font = new Font("Segoe UI", 9F),
        ForeColor = Color.Gray,
        Dock = DockStyle.Fill,
        TextAlign = ContentAlignment.MiddleRight
      };

      tableRodape.Controls.Add(lblConexao, 0, 0);
      tableRodape.Controls.Add(lblCopyright, 1, 0);

      panelRodape.Controls.Add(tableRodape);

      // Montagem final
      this.Controls.Add(panelMain);
      this.Controls.Add(panelHeader);
      this.Controls.Add(panelRodape);
    }

    private TextBox CriarCampoPasta(Control parent, string labelText, int x, int y, string valorInicial)
    {
      var lbl = new Label { Text = labelText, Location = new Point(x, y), AutoSize = true, Font = new Font("Segoe UI", 10F, FontStyle.Bold) };
      var txt = new TextBox { Location = new Point(x, y + 25), Width = 650, Height = 30, Text = valorInicial };

      var btn = new Button
      {
        Text = "...",
        Location = new Point(x + 660, y + 23),
        Size = new Size(40, 30)
      };
      btn.Click += (s, e) =>
      {
        using var fbd = new FolderBrowserDialog { SelectedPath = txt.Text };
        if (fbd.ShowDialog() == DialogResult.OK) txt.Text = fbd.SelectedPath;
      };

      parent.Controls.AddRange(new Control[] { lbl, txt, btn });
      return txt;
    }

    private void BtnConectarDestino_Click(object sender, EventArgs e)
    {
      using var frm = new DestinoConnectionForm();
      if (frm.ShowDialog() == DialogResult.OK)
      {
        _connectionStringDestino = frm.ConnectionString;
        var builder = new SqlConnectionStringBuilder(_connectionStringDestino);
        txtServidorDestino.Text = builder.DataSource;

        MessageBox.Show($"✅ Conectado com sucesso!\n\nServidor: {builder.DataSource}", "Conexão OK", MessageBoxButtons.OK, MessageBoxIcon.Information);
        btnIniciar.Enabled = true;
      }
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
          if (!new[] { "master", "tempdb", "model", "msdb" }.Contains(db))
            lstBancos.Items.Add(db);
        }
      }
      catch (Exception ex)
      {
        MessageBox.Show($"Erro ao carregar bancos: {ex.Message}", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
      }
    }

    private void BtnIniciar_Click(object sender, EventArgs e)
    {
      if (lstBancos.CheckedItems.Count == 0)
      {
        MessageBox.Show("Selecione pelo menos um banco.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        return;
      }

      if (string.IsNullOrWhiteSpace(_connectionStringDestino))
      {
        MessageBox.Show("Conecte-se ao servidor destino primeiro.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning);
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