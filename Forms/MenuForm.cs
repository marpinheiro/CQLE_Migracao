#nullable disable
using System;
using System.Drawing;
using System.Windows.Forms;

namespace CQLE_MIGRACAO.Forms
{
  public class MenuForm : Form
  {
    private Panel panelHeader;
    private Label lblTitulo;
    private Label lblSubtitulo;
    private Button btnMigracao;
    private Button btnAtualizacao;
    private Button btnSair;
    private Label lblRodape;

    private readonly string _connectionStringOrigem;

    public MenuForm(string connectionStringOrigem)
    {
      _connectionStringOrigem = connectionStringOrigem;

      ConfigurarInterface();
    }

    private void ConfigurarInterface()
    {
      this.Text = "CQLE Migração - Menu Principal";
      this.Size = new Size(600, 450);
      this.StartPosition = FormStartPosition.CenterScreen;
      this.FormBorderStyle = FormBorderStyle.FixedDialog;
      this.MaximizeBox = false;
      this.BackColor = Color.FromArgb(240, 240, 245);

      try
      {
        this.Icon = new Icon("Assets/CQLE.ico");
      }
      catch { }

      // Header
      panelHeader = new Panel
      {
        Dock = DockStyle.Top,
        Height = 80,
        BackColor = Color.FromArgb(0, 120, 215)
      };

      lblTitulo = new Label
      {
        Text = "🗄️ CQLE MIGRAÇÃO",
        Location = new Point(20, 15),
        AutoSize = true,
        Font = new Font("Segoe UI", 16, FontStyle.Bold),
        ForeColor = Color.White
      };

      lblSubtitulo = new Label
      {
        Text = "Escolha a operação desejada",
        Location = new Point(20, 45),
        AutoSize = true,
        Font = new Font("Segoe UI", 10),
        ForeColor = Color.FromArgb(200, 220, 255)
      };

      panelHeader.Controls.Add(lblTitulo);
      panelHeader.Controls.Add(lblSubtitulo);

      // Botão Migração Completa
      btnMigracao = new Button
      {
        Text = "🔄 Migração Completa\n(Servidor novo ou limpo)",
        Location = new Point(100, 120),
        Size = new Size(400, 80),
        Font = new Font("Segoe UI", 12, FontStyle.Bold),
        BackColor = Color.FromArgb(0, 120, 215),
        ForeColor = Color.White,
        FlatStyle = FlatStyle.Flat,
        TextAlign = ContentAlignment.MiddleCenter
      };
      btnMigracao.Click += (s, e) =>
      {
        this.Hide();
        var migrationForm = new MigrationForm(_connectionStringOrigem);
        migrationForm.FormClosed += (s2, e2) => this.Show();
        migrationForm.Show();
      };

      // Botão Atualização de Base Teste
      btnAtualizacao = new Button
      {
        Text = "🔄 Atualização de Base Teste\n(Substituir ou criar novo)",
        Location = new Point(100, 220),
        Size = new Size(400, 80),
        Font = new Font("Segoe UI", 12, FontStyle.Bold),
        BackColor = Color.FromArgb(0, 150, 0),
        ForeColor = Color.White,
        FlatStyle = FlatStyle.Flat,
        TextAlign = ContentAlignment.MiddleCenter
      };
      btnAtualizacao.Click += (s, e) =>
      {
        this.Hide();
        var updateForm = new UpdateForm(_connectionStringOrigem);
        updateForm.FormClosed += (s2, e2) => this.Show();
        updateForm.Show();
      };

      // Botão Sair
      btnSair = new Button
      {
        Text = "❌ Sair",
        Location = new Point(100, 320),
        Size = new Size(400, 60),
        Font = new Font("Segoe UI", 12, FontStyle.Bold),
        BackColor = Color.FromArgb(180, 0, 0),
        ForeColor = Color.White,
        FlatStyle = FlatStyle.Flat
      };
      btnSair.Click += (s, e) => Application.Exit();

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

      this.Controls.Add(panelHeader);
      this.Controls.Add(btnMigracao);
      this.Controls.Add(btnAtualizacao);
      this.Controls.Add(btnSair);
      this.Controls.Add(lblRodape);
    }
  }
}