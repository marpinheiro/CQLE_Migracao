#nullable disable
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using CQLE_MIGRACAO.Services;
using Microsoft.Data.SqlClient;

namespace CQLE_MIGRACAO.Forms
{
  public class UpdateForm : Form
  {
    // Controles de Origem
    private ComboBox cboBancoOrigem;

    // Controles de Destino
    private CheckedListBox lstBancosDestino;
    private Label lblStatusDestino;
    private Button btnConectarDestino;

    // Controles Gerais
    private Panel panelBotoes;
    private Button btnIniciar;
    private Button btnFecharVoltar;

    // Opções de Criação/Substituição
    private RadioButton radioSubstituir;
    private RadioButton radioNovo;
    private TextBox txtNovoNome;

    // Pastas
    private TextBox txtPastaBackup;
    private TextBox txtLocalMdf;
    private TextBox txtLocalLdf;

    private readonly UnifiedMigrationService _serviceOrigem;
    private UnifiedMigrationService _serviceDestino;
    private readonly string _connOrigem;
    private string _connDestino;

    private readonly string _servidorOrigem;
    private readonly string _usuarioOrigem;

    public UpdateForm(string connectionStringOrigem, string servidorOrigem, string usuarioOrigem)
    {
      _connOrigem = connectionStringOrigem;
      _serviceOrigem = new UnifiedMigrationService(connectionStringOrigem);

      _servidorOrigem = servidorOrigem;
      _usuarioOrigem = usuarioOrigem;

      InitializeComponent();
      CarregarBancosOrigem();
    }

    private void InitializeComponent()
    {
      this.Text = "CQLE Migração - Atualização de Base Teste";
      this.StartPosition = FormStartPosition.CenterScreen;
      this.WindowState = FormWindowState.Maximized;
      this.MinimumSize = new Size(900, 700);
      this.FormBorderStyle = FormBorderStyle.Sizable;
      this.BackColor = Color.WhiteSmoke;
      this.Font = new Font("Segoe UI", 9F);

      try { this.Icon = new Icon("Assets/CQLE.ico"); } catch { }

      // === CABEÇALHO ===
      var panelHeader = new Panel
      {
        Dock = DockStyle.Top,
        Height = 100,
        BackColor = Color.FromArgb(0, 120, 215)
      };

      var lblTitulo = new Label
      {
        Text = "🗄️ CQLE MIGRAÇÃO",
        ForeColor = Color.White,
        Font = new Font("Segoe UI", 22F, FontStyle.Bold),
        AutoSize = true,
        Location = new Point(30, 20)
      };

      var lblSubtitulo = new Label
      {
        Text = "Atualização de Base Teste",
        ForeColor = Color.FromArgb(200, 220, 255),
        Font = new Font("Segoe UI", 12F),
        AutoSize = true,
        Location = new Point(32, 65)
      };

      panelHeader.Controls.AddRange(new Control[] { lblTitulo, lblSubtitulo });

      // === CONTEÚDO PRINCIPAL ===
      var panelMain = new Panel
      {
        Dock = DockStyle.Fill,
        Padding = new Padding(60, 20, 60, 20),
        AutoScroll = true
      };

      var layoutPrincipal = new TableLayoutPanel
      {
        Dock = DockStyle.Top,
        AutoSize = true,
        ColumnCount = 1,
        RowCount = 5,
        Padding = new Padding(0, 0, 0, 20)
      };
      layoutPrincipal.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

      // ==========================================
      // 1. SEÇÃO ORIGEM
      // ==========================================
      var grpOrigem = new GroupBox
      {
        Text = "📦 Banco de Origem",
        Dock = DockStyle.Top,
        Height = 80,
        Font = new Font("Segoe UI", 10F, FontStyle.Bold),
        ForeColor = Color.FromArgb(0, 120, 215),
        Padding = new Padding(15)
      };

      cboBancoOrigem = new ComboBox
      {
        Dock = DockStyle.Top,
        DropDownStyle = ComboBoxStyle.DropDownList,
        Font = new Font("Segoe UI", 10F),
        Height = 30
      };
      cboBancoOrigem.SelectedIndexChanged += (s, e) => AtualizarEstadoInterface();

      grpOrigem.Controls.Add(cboBancoOrigem);
      layoutPrincipal.Controls.Add(grpOrigem);

      // ==========================================
      // 2. SEÇÃO DESTINO
      // ==========================================
      var grpDestino = new GroupBox
      {
        Text = "🎯 Servidor Destino",
        Dock = DockStyle.Top,
        AutoSize = true,
        Font = new Font("Segoe UI", 10F, FontStyle.Bold),
        ForeColor = Color.FromArgb(0, 120, 215),
        Padding = new Padding(15),
        Margin = new Padding(0, 15, 0, 0)
      };

      var layoutDestino = new TableLayoutPanel
      {
        Dock = DockStyle.Top,
        AutoSize = true,
        ColumnCount = 1,
        RowCount = 5
      };

      btnConectarDestino = new Button
      {
        Text = "🔌 Conectar ao Servidor Destino",
        Height = 40,
        Dock = DockStyle.Top,
        BackColor = Color.FromArgb(0, 120, 215),
        ForeColor = Color.White,
        FlatStyle = FlatStyle.Flat,
        Font = new Font("Segoe UI", 10F, FontStyle.Bold),
        Margin = new Padding(0, 0, 0, 10)
      };
      btnConectarDestino.FlatAppearance.BorderSize = 0;
      btnConectarDestino.Click += BtnConectarDestino_Click;

      lblStatusDestino = new Label
      {
        Text = "❌ Não conectado",
        ForeColor = Color.Red,
        Height = 25,
        Dock = DockStyle.Top,
        TextAlign = ContentAlignment.MiddleLeft,
        Margin = new Padding(0, 0, 0, 15),
        Font = new Font("Segoe UI", 9F)
      };

      var panelOpcoesDestino = new Panel
      {
        Height = 90,
        Dock = DockStyle.Top
      };

      radioSubstituir = new RadioButton
      {
        Text = "Substituir banco existente (Selecione na lista abaixo)",
        Location = new Point(0, 5),
        Enabled = false,
        AutoSize = true,
        Font = new Font("Segoe UI", 9F)
      };

      radioNovo = new RadioButton
      {
        Text = "Criar novo banco",
        Location = new Point(0, 35),
        Checked = true,
        Enabled = false,
        AutoSize = true,
        Font = new Font("Segoe UI", 9F)
      };

      txtNovoNome = new TextBox
      {
        Location = new Point(130, 33),
        Width = 350,
        Height = 25,
        Enabled = false, // Começa desabilitado até conectar e selecionar a opção
        Font = new Font("Segoe UI", 9F)
      };

      lstBancosDestino = new CheckedListBox
      {
        Dock = DockStyle.Top,
        Height = 120,
        Enabled = false,
        CheckOnClick = true,
        Font = new Font("Segoe UI", 9F)
      };

      // Eventos para controlar a habilitação/desabilitação
      radioNovo.CheckedChanged += (s, e) => AtualizarEstadoInterface();
      radioSubstituir.CheckedChanged += (s, e) => AtualizarEstadoInterface();

      panelOpcoesDestino.Controls.AddRange(new Control[] { radioSubstituir, radioNovo, txtNovoNome });

      layoutDestino.Controls.Add(btnConectarDestino);
      layoutDestino.Controls.Add(lblStatusDestino);
      layoutDestino.Controls.Add(panelOpcoesDestino);
      layoutDestino.Controls.Add(new Label { Text = "Selecione abaixo caso queira substituir:", AutoSize = true, ForeColor = Color.Gray, Font = new Font("Segoe UI", 8F) });
      layoutDestino.Controls.Add(lstBancosDestino);

      grpDestino.Controls.Add(layoutDestino);
      layoutPrincipal.Controls.Add(grpDestino);

      // ==========================================
      // 3. SEÇÃO PASTAS
      // ==========================================
      var grpPastas = new GroupBox
      {
        Text = "📁 Configurações de Arquivos",
        Dock = DockStyle.Top,
        Height = 190,
        Font = new Font("Segoe UI", 10F, FontStyle.Bold),
        ForeColor = Color.FromArgb(0, 120, 215),
        Padding = new Padding(15),
        Margin = new Padding(0, 15, 0, 0)
      };

      txtPastaBackup = CriarCampoPasta(grpPastas, "Pasta para Backup Temporário:", 20, 30, @"C:\TempBackups");
      txtLocalMdf = CriarCampoPasta(grpPastas, "Pasta para Arquivos MDF (Destino):", 20, 80, "");
      txtLocalLdf = CriarCampoPasta(grpPastas, "Pasta para Arquivos LDF (Destino):", 20, 130, "");

      layoutPrincipal.Controls.Add(grpPastas);

      // ==========================================
      // 4. SEÇÃO BOTÕES
      // ==========================================
      panelBotoes = new Panel
      {
        Dock = DockStyle.Top,
        Height = 80,
        Margin = new Padding(0, 20, 0, 0)
      };

      btnIniciar = new Button
      {
        Text = "🚀 INICIAR ATUALIZAÇÃO",
        Size = new Size(250, 50),
        Font = new Font("Segoe UI", 12F, FontStyle.Bold),
        BackColor = Color.SeaGreen,
        ForeColor = Color.White,
        FlatStyle = FlatStyle.Flat,
        Enabled = false
      };
      btnIniciar.FlatAppearance.BorderSize = 0;
      btnIniciar.Click += BtnIniciar_Click;

      btnFecharVoltar = new Button
      {
        Text = "⬅ Fechar e Voltar",
        Size = new Size(200, 50),
        Font = new Font("Segoe UI", 12F, FontStyle.Bold),
        BackColor = Color.FromArgb(100, 100, 100),
        ForeColor = Color.White,
        FlatStyle = FlatStyle.Flat
      };
      btnFecharVoltar.FlatAppearance.BorderSize = 0;
      btnFecharVoltar.Click += (s, e) => this.Close();

      panelBotoes.Controls.Add(btnIniciar);
      panelBotoes.Controls.Add(btnFecharVoltar);
      panelBotoes.Resize += (s, e) => CentralizarBotoes();
      CentralizarBotoes();

      layoutPrincipal.Controls.Add(panelBotoes);
      panelMain.Controls.Add(layoutPrincipal);

      // === RODAPÉ ===
      var panelRodape = new Panel
      {
        Dock = DockStyle.Bottom,
        Height = 40,
        BackColor = Color.FromArgb(245, 245, 245)
      };

      var tableRodape = new TableLayoutPanel
      {
        Dock = DockStyle.Fill,
        ColumnCount = 2,
        RowCount = 1,
        Padding = new Padding(40, 5, 40, 5)
      };
      tableRodape.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 70F));
      tableRodape.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30F));

      var lblConexao = new Label
      {
        Text = $"✅ Conectado à Origem: {_servidorOrigem}  •  Usuário: {_usuarioOrigem}",
        Font = new Font("Segoe UI", 9F),
        ForeColor = Color.DarkGreen,
        Dock = DockStyle.Fill,
        TextAlign = ContentAlignment.MiddleLeft
      };

      var lblCopyright = new Label
      {
        Text = "CQLE Softwares © 2026",
        Font = new Font("Segoe UI", 8F),
        ForeColor = Color.Gray,
        Dock = DockStyle.Fill,
        TextAlign = ContentAlignment.MiddleRight
      };

      tableRodape.Controls.Add(lblConexao, 0, 0);
      tableRodape.Controls.Add(lblCopyright, 1, 0);

      panelRodape.Controls.Add(tableRodape);

      this.Controls.Add(panelMain);
      this.Controls.Add(panelHeader);
      this.Controls.Add(panelRodape);
    }

    private void CentralizarBotoes()
    {
      if (panelBotoes == null || btnIniciar == null || btnFecharVoltar == null) return;

      int totalWidth = btnIniciar.Width + btnFecharVoltar.Width + 20;
      int startX = (panelBotoes.Width - totalWidth) / 2;

      if (startX < 0) startX = 0;

      btnIniciar.Location = new Point(startX, 15);
      btnFecharVoltar.Location = new Point(startX + btnIniciar.Width + 20, 15);
    }

    private TextBox CriarCampoPasta(Control parent, string labelText, int x, int y, string valorInicial)
    {
      var lbl = new Label
      {
        Text = labelText,
        Location = new Point(x, y),
        AutoSize = true,
        Font = new Font("Segoe UI", 9F, FontStyle.Bold)
      };

      var txt = new TextBox
      {
        Location = new Point(x, y + 20),
        Width = 600,
        Height = 25,
        Text = valorInicial,
        Font = new Font("Segoe UI", 9F)
      };

      var btn = new Button
      {
        Text = "...",
        Location = new Point(x + 610, y + 18),
        Size = new Size(35, 27),
        Font = new Font("Segoe UI", 9F, FontStyle.Bold)
      };
      btn.Click += (s, e) =>
      {
        using var fbd = new FolderBrowserDialog { SelectedPath = txt.Text };
        if (fbd.ShowDialog() == DialogResult.OK)
          txt.Text = fbd.SelectedPath;
      };

      parent.Controls.AddRange(new Control[] { lbl, txt, btn });
      return txt;
    }

    private void CarregarBancosOrigem()
    {
      cboBancoOrigem.Items.Clear();
      cboBancoOrigem.Items.Add("Escolha o database da Origem");

      foreach (var db in _serviceOrigem.GetInventario().Databases)
      {
        if (!new[] { "master", "model", "msdb", "tempdb" }.Contains(db))
          cboBancoOrigem.Items.Add(db);
      }

      if (cboBancoOrigem.Items.Count > 0)
        cboBancoOrigem.SelectedIndex = 0;
    }

    private void BtnConectarDestino_Click(object sender, EventArgs e)
    {
      using var frm = new DestinoConnectionForm();
      if (frm.ShowDialog() == DialogResult.OK)
      {
        _connDestino = frm.ConnectionString;
        _serviceDestino = new UnifiedMigrationService(_connDestino);

        lblStatusDestino.Text = "✅ Conectado com sucesso";
        lblStatusDestino.ForeColor = Color.DarkGreen;

        radioNovo.Enabled = true;
        radioSubstituir.Enabled = true;

        // Popula a lista
        lstBancosDestino.Items.Clear();
        foreach (var db in _serviceDestino.GetInventario().Databases)
        {
          if (!new[] { "master", "model", "msdb", "tempdb" }.Contains(db))
            lstBancosDestino.Items.Add(db);
        }

        // Dispara a lógica de estado visual
        AtualizarEstadoInterface();
      }
    }

    // Método centralizado para controlar o que fica habilitado/desabilitado
    private void AtualizarEstadoInterface()
    {
      bool conectadoDestino = _serviceDestino != null;
      bool origemSelecionada = cboBancoOrigem.SelectedIndex > 0;

      // Lógica do Novo Nome
      txtNovoNome.Enabled = radioNovo.Checked && conectadoDestino;

      // Lógica da Lista de Bancos (AQUI ESTÁ A MUDANÇA PRINCIPAL)
      // Só habilita se estiver no modo substituir E conectado
      lstBancosDestino.Enabled = radioSubstituir.Checked && conectadoDestino;

      // Opcional: Se desabilitar a lista, remove as seleções para evitar confusão visual
      if (!lstBancosDestino.Enabled)
      {
        for (int i = 0; i < lstBancosDestino.Items.Count; i++)
          lstBancosDestino.SetItemChecked(i, false);
      }

      // Habilita o botão iniciar apenas se tudo estiver ok
      btnIniciar.Enabled = origemSelecionada && conectadoDestino;
    }

    private void BtnIniciar_Click(object sender, EventArgs e)
    {
      if (cboBancoOrigem.SelectedIndex <= 0)
      {
        MessageBox.Show("Selecione um banco de origem válido.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        return;
      }

      string bancoDestino = "";

      if (radioNovo.Checked)
      {
        bancoDestino = txtNovoNome.Text.Trim();
        if (string.IsNullOrWhiteSpace(bancoDestino))
        {
          MessageBox.Show("Informe o nome do novo banco.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning);
          return;
        }
      }
      else if (radioSubstituir.Checked)
      {
        if (lstBancosDestino.CheckedItems.Count == 0)
        {
          MessageBox.Show("Selecione o banco de destino que deseja substituir.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning);
          return;
        }
        bancoDestino = lstBancosDestino.CheckedItems[0].ToString();
      }

      var bancosOrigem = new List<string> { cboBancoOrigem.SelectedItem.ToString() };

      this.Hide();
      new UpdateProgressForm(
          _connOrigem,
          _connDestino,
          bancosOrigem,
          bancoDestino,
          txtPastaBackup.Text,
          txtLocalMdf.Text,
          txtLocalLdf.Text
      ).ShowDialog();
      this.Show();
    }
  }
}