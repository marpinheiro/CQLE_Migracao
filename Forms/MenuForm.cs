#nullable disable
using System;
using System.Drawing;
using System.Windows.Forms;

namespace CQLE_MIGRACAO.Forms
{
  public class MenuForm : Form
  {
    private readonly string _connectionStringOrigem;
    private readonly string _servidorOrigem;
    private readonly string _usuarioOrigem;

    public MenuForm(string connectionStringOrigem, string servidorOrigem, string usuarioOrigem)
    {
      _connectionStringOrigem = connectionStringOrigem;
      _servidorOrigem = servidorOrigem;
      _usuarioOrigem = usuarioOrigem;

      InitializeComponent();
    }

    private void InitializeComponent()
    {
      this.Text = "CQLE Migração - Menu Principal";
      this.Size = new Size(620, 680);
      this.FormBorderStyle = FormBorderStyle.FixedDialog;
      this.MaximizeBox = false;
      this.StartPosition = FormStartPosition.CenterScreen;
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
        Font = new Font("Segoe UI", 24F, FontStyle.Bold),
        AutoSize = true,
        Location = new Point(30, 30)
      };

      var lblSubtitulo = new Label
      {
        Text = "Escolha a operação desejada",
        ForeColor = Color.FromArgb(200, 220, 255),
        Font = new Font("Segoe UI", 12F),
        AutoSize = true,
        Location = new Point(32, 80)
      };

      panelHeader.Controls.AddRange(new Control[] { lblTitulo, lblSubtitulo });

      // === BOTÕES CENTRAIS ===
      var btnMigracaoCompleta = CriarBotaoGrande(
          "🔄 Migração Completa",
          "Para servidor novo ou limpo",
          Color.FromArgb(0, 120, 215),
          new Point(60, 180)
      );
      btnMigracaoCompleta.Click += (s, e) =>
      {
        this.Hide();
        var migrationForm = new MigrationForm(_connectionStringOrigem, _servidorOrigem, _usuarioOrigem);
        migrationForm.FormClosed += (s2, e2) => this.Show();
        migrationForm.Show();
      };

      var btnAtualizacaoTeste = CriarBotaoGrande(
          "🔄 Atualização de Base Teste",
          "Substituir ou criar novo banco",
          Color.FromArgb(0, 150, 0),
          new Point(60, 290)
      );
      btnAtualizacaoTeste.Click += (s, e) =>
      {
        this.Hide();
        var updateForm = new UpdateForm(_connectionStringOrigem, _servidorOrigem, _usuarioOrigem);
        updateForm.FormClosed += (s2, e2) => this.Show();
        updateForm.Show();
      };

      // Botão Mover Arquivos de Banco
      var btnMoverArquivos = CriarBotaoGrande(
          "📂 Mover Arquivos de Banco",
          "Mover arquivos .mdf e .ldf para outra pasta",
          Color.FromArgb(70, 130, 180),
          new Point(60, 400)
      );
      btnMoverArquivos.Click += (s, e) =>
      {
        MessageBox.Show(
                  "Funcionalidade em desenvolvimento.\n\n" +
                  "Esta opção permitirá selecionar um banco, detach, mover os arquivos físicos (.mdf/.ldf) e attach novamente.",
                  "Mover Arquivos de Banco",
                  MessageBoxButtons.OK,
                  MessageBoxIcon.Information);
      };

      var btnSair = new Button
      {
        Text = "❌ Sair do Sistema",
        Location = new Point(60, 530),
        Size = new Size(500, 60),
        Font = new Font("Segoe UI", 14F, FontStyle.Bold),
        BackColor = Color.FromArgb(180, 0, 0),
        ForeColor = Color.White,
        FlatStyle = FlatStyle.Flat,
        Cursor = Cursors.Hand
      };
      btnSair.FlatAppearance.BorderSize = 0;
      btnSair.Click += (s, e) => Application.Exit();

      // === RODAPÉ ===
      var lblRodape = new Label
      {
        Text = "Desenvolvido por Marciano Silva - CQLE Softwares © 2026",
        Dock = DockStyle.Bottom,
        Height = 40,
        TextAlign = ContentAlignment.MiddleCenter,
        Font = new Font("Segoe UI", 9F),
        ForeColor = Color.Gray,
        BackColor = Color.FromArgb(240, 240, 245)
      };

      // === MONTAGEM FINAL ===
      this.Controls.AddRange(new Control[]
      {
                panelHeader,
                btnMigracaoCompleta,
                btnAtualizacaoTeste,
                btnMoverArquivos,
                btnSair,
                lblRodape
      });
    }

    private Button CriarBotaoGrande(string titulo, string subtitulo, Color corFundo, Point localizacao)
    {
      var btn = new Button
      {
        Location = localizacao,
        Size = new Size(500, 90),
        FlatStyle = FlatStyle.Flat,
        BackColor = corFundo,
        ForeColor = Color.White,
        Font = new Font("Segoe UI", 13F, FontStyle.Bold),
        TextAlign = ContentAlignment.MiddleLeft,
        Padding = new Padding(20, 0, 0, 0),
        Cursor = Cursors.Hand
      };
      btn.FlatAppearance.BorderSize = 0;

      btn.Text = titulo + "\r\n" + subtitulo;

      // Efeito hover
      btn.MouseEnter += (s, e) => btn.BackColor = ControlPaint.Light(corFundo, 0.2f);
      btn.MouseLeave += (s, e) => btn.BackColor = corFundo;

      return btn;
    }
  }
}