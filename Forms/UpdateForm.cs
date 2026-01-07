#nullable disable
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using CQLE_MIGRACAO.Services;

namespace CQLE_MIGRACAO.Forms
{
  public class UpdateForm : Form
  {
    private CheckedListBox lstBancosOrigem;
    private CheckedListBox lstBancosDestino;

    private Label lblStatusDestino;
    private Button btnConectarDestino;
    private Button btnIniciar;

    private RadioButton radioSubstituir;
    private RadioButton radioNovo;
    private TextBox txtNovoNome;

    private TextBox txtPastaBackup;
    private TextBox txtLocalMdf;
    private TextBox txtLocalLdf;

    private readonly UnifiedMigrationService _serviceOrigem;
    private UnifiedMigrationService _serviceDestino;

    private readonly string _connOrigem;
    private string _connDestino;

    public UpdateForm(string connectionStringOrigem)
    {
      _connOrigem = connectionStringOrigem;
      _serviceOrigem = new UnifiedMigrationService(connectionStringOrigem);

      InitializeComponent();
      CarregarBancosOrigem();
    }

    private void InitializeComponent()
    {
      Text = "CQLE Migração - Atualização Base Teste";
      WindowState = FormWindowState.Maximized;

      var panel = new Panel { Dock = DockStyle.Fill, AutoScroll = true };
      Controls.Add(panel);

      // ORIGEM
      var grpOrigem = new GroupBox
      {
        Text = "📦 Bancos de Origem",
        Location = new Point(20, 20),
        Size = new Size(450, 300)
      };

      lstBancosOrigem = new CheckedListBox { Dock = DockStyle.Fill };
      grpOrigem.Controls.Add(lstBancosOrigem);

      // DESTINO
      var grpDestino = new GroupBox
      {
        Text = "🎯 Servidor Destino",
        Location = new Point(500, 20),
        Size = new Size(450, 320)
      };

      btnConectarDestino = new Button
      {
        Text = "🔌 Conectar Destino",
        Location = new Point(20, 30),
        Width = 400
      };
      btnConectarDestino.Click += BtnConectarDestino_Click;

      lblStatusDestino = new Label
      {
        Text = "❌ Não conectado",
        Location = new Point(20, 70),
        ForeColor = Color.Red
      };

      lstBancosDestino = new CheckedListBox
      {
        Location = new Point(20, 100),
        Size = new Size(400, 120),
        Enabled = false
      };

      radioSubstituir = new RadioButton
      {
        Text = "Substituir banco existente",
        Location = new Point(20, 230),
        Enabled = false
      };

      radioNovo = new RadioButton
      {
        Text = "Criar novo banco",
        Location = new Point(20, 255),
        Enabled = false
      };

      txtNovoNome = new TextBox
      {
        Location = new Point(20, 280),
        Width = 420,
        Enabled = false
      };

      radioNovo.CheckedChanged += (s, e) =>
      {
        txtNovoNome.Enabled = radioNovo.Checked;
        lstBancosDestino.Enabled = !radioNovo.Checked;
      };

      grpDestino.Controls.AddRange(new Control[]
      {
        btnConectarDestino, lblStatusDestino,
        lstBancosDestino, radioSubstituir,
        radioNovo, txtNovoNome
      });

      // PASTAS
      int yBase = 360;
      txtPastaBackup = CriarCampo(panel, "📁 Pasta Backup", 20, yBase, @"C:\TempBackups");
      txtLocalMdf = CriarCampo(panel, "💾 Pasta MDF", 20, yBase + 70, "");
      txtLocalLdf = CriarCampo(panel, "📋 Pasta LDF", 20, yBase + 140, "");

      btnIniciar = new Button
      {
        Text = "🚀 INICIAR ATUALIZAÇÃO",
        Location = new Point(20, yBase + 220),
        Width = 930,
        Height = 40,
        Enabled = false,
        BackColor = Color.SeaGreen,
        ForeColor = Color.White
      };
      btnIniciar.Click += BtnIniciar_Click;

      panel.Controls.AddRange(new Control[]
      {
        grpOrigem, grpDestino, btnIniciar
      });
    }

    private TextBox CriarCampo(Control parent, string label, int x, int y, string valor)
    {
      parent.Controls.Add(new Label { Text = label, Location = new Point(x, y) });

      var txt = new TextBox
      {
        Location = new Point(x, y + 20),
        Width = 420,
        Text = valor
      };

      var btn = new Button
      {
        Text = "📂",
        Location = new Point(x + 430, y + 18),
        Width = 40
      };

      btn.Click += (s, e) =>
      {
        using var fbd = new FolderBrowserDialog();
        if (fbd.ShowDialog() == DialogResult.OK)
          txt.Text = fbd.SelectedPath;
      };

      parent.Controls.Add(txt);
      parent.Controls.Add(btn);
      return txt;
    }

    private void CarregarBancosOrigem()
    {
      foreach (var db in _serviceOrigem.GetInventario().Databases)
        if (!new[] { "master", "model", "msdb", "tempdb" }.Contains(db))
          lstBancosOrigem.Items.Add(db);
    }

    private void BtnConectarDestino_Click(object sender, EventArgs e)
    {
      using var frm = new DestinoConnectionForm();
      if (frm.ShowDialog() == DialogResult.OK)
      {
        _connDestino = frm.ConnectionString;
        _serviceDestino = new UnifiedMigrationService(_connDestino);

        lblStatusDestino.Text = "✅ Conectado";
        lblStatusDestino.ForeColor = Color.Green;

        lstBancosDestino.Enabled = true;
        radioSubstituir.Enabled = radioNovo.Enabled = true;
        btnIniciar.Enabled = true;

        lstBancosDestino.Items.Clear();
        foreach (var db in _serviceDestino.GetInventario().Databases)
          if (!new[] { "master", "model", "msdb", "tempdb" }.Contains(db))
            lstBancosDestino.Items.Add(db);
      }
    }

    private void BtnIniciar_Click(object sender, EventArgs e)
    {
      var bancosOrigem = lstBancosOrigem.CheckedItems.Cast<string>().ToList();
      var bancoDestino = radioNovo.Checked
        ? txtNovoNome.Text
        : lstBancosDestino.CheckedItems[0].ToString();

      new UpdateProgressForm(
        _connOrigem,
        _connDestino,
        bancosOrigem,
        bancoDestino,
        txtPastaBackup.Text,
        txtLocalMdf.Text,
        txtLocalLdf.Text
      ).ShowDialog();
    }
  }
}
