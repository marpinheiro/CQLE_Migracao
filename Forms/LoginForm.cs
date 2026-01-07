#nullable disable
using System;
using System.Drawing;
using System.Windows.Forms;
using CQLE_MIGRACAO.Data;
using CQLE_MIGRACAO.Security;
using Microsoft.Data.SqlClient;

namespace CQLE_MIGRACAO.Forms
{
  public class LoginForm : Form
  {
    private TextBox txtServidor;
    private TextBox txtUsuario;
    private TextBox txtSenha;
    private Button btnEntrar;
    private Button btnSair;

    public LoginForm()
    {
      InitializeComponent();
    }

    private void InitializeComponent()
    {
      this.Text = "CQLE Migração - Autenticação";
      this.Size = new Size(520, 580);
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
        Text = "Sistema Profissional de Migração SQL Server",
        ForeColor = Color.FromArgb(200, 220, 255),
        Font = new Font("Segoe UI", 11F),
        AutoSize = true,
        Location = new Point(32, 80)
      };

      panelHeader.Controls.AddRange(new Control[] { lblTitulo, lblSubtitulo });

      // === GRUPO DE LOGIN ===
      var grpLogin = new GroupBox
      {
        Text = "Autenticação SQL Server",
        Location = new Point(40, 160),
        Size = new Size(440, 240),
        Font = new Font("Segoe UI", 11F, FontStyle.Bold),
        ForeColor = Color.FromArgb(0, 120, 215)
      };

      var lblServidor = new Label { Text = "Servidor:", Location = new Point(30, 40), AutoSize = true };
      txtServidor = new TextBox
      {
        Location = new Point(30, 65),
        Size = new Size(380, 30),
        Text = "localhost"
      };

      var lblUsuario = new Label { Text = "Usuário:", Location = new Point(30, 110), AutoSize = true };
      txtUsuario = new TextBox
      {
        Location = new Point(30, 135),
        Size = new Size(380, 30)
      };

      var lblSenha = new Label { Text = "Senha:", Location = new Point(30, 180), AutoSize = true };
      txtSenha = new TextBox
      {
        Location = new Point(30, 205),
        Size = new Size(380, 30),
        PasswordChar = '*',
        UseSystemPasswordChar = true
      };

      grpLogin.Controls.AddRange(new Control[]
      {
                lblServidor, txtServidor,
                lblUsuario, txtUsuario,
                lblSenha, txtSenha
      });

      // === BOTÕES ===
      btnEntrar = new Button
      {
        Text = "🔓 Entrar",
        Location = new Point(40, 430),
        Size = new Size(200, 50),
        Font = new Font("Segoe UI", 12F, FontStyle.Bold),
        BackColor = Color.FromArgb(0, 120, 215),
        ForeColor = Color.White,
        FlatStyle = FlatStyle.Flat,
        Cursor = Cursors.Hand
      };
      btnEntrar.FlatAppearance.BorderSize = 0;
      btnEntrar.Click += BtnEntrar_Click;

      btnSair = new Button
      {
        Text = "❌ Sair",
        Location = new Point(280, 430),
        Size = new Size(200, 50),
        Font = new Font("Segoe UI", 12F, FontStyle.Bold),
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
                grpLogin,
                btnEntrar,
                btnSair,
                lblRodape
      });

      // Enter no campo senha para logar
      txtSenha.KeyPress += (s, e) =>
      {
        if (e.KeyChar == (char)Keys.Enter)
        {
          BtnEntrar_Click(null, null);
          e.Handled = true;
        }
      };

      this.FormClosing += (s, e) => Application.Exit();
    }

    private void BtnEntrar_Click(object sender, EventArgs e)
    {
      if (string.IsNullOrWhiteSpace(txtServidor.Text) ||
          string.IsNullOrWhiteSpace(txtUsuario.Text) ||
          string.IsNullOrWhiteSpace(txtSenha.Text))
      {
        MessageBox.Show("Preencha todos os campos.", "Validação", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        return;
      }

      btnEntrar.Enabled = false;
      btnEntrar.Text = "⏳ Conectando...";
      this.Cursor = Cursors.WaitCursor;

      try
      {
        using SqlConnection conn = SqlServerConnection.Create(
            txtServidor.Text.Trim(),
            txtUsuario.Text.Trim(),
            txtSenha.Text
        );

        conn.Open();

        if (!SqlSecurityService.IsSysAdmin(conn))
        {
          MessageBox.Show("O usuário não possui permissão SYSADMIN.", "Acesso Negado", MessageBoxButtons.OK, MessageBoxIcon.Error);
          return;
        }

        MessageBox.Show(
            "✅ Login validado com sucesso!\n\n" +
            $"Servidor: {txtServidor.Text.Trim()}\n" +
            $"Usuário: {txtUsuario.Text.Trim()}\n" +
            "Permissões: SYSADMIN\n\n" +
            "Bem-vindo ao CQLE Migração!",
            "Autenticação Bem-Sucedida",
            MessageBoxButtons.OK,
            MessageBoxIcon.Information);

        this.Hide();

        string connectionStringOrigem = $"Server={txtServidor.Text.Trim()};" +
                                        $"Database=master;" +
                                        $"User ID={txtUsuario.Text.Trim()};" +
                                        $"Password={txtSenha.Text};" +
                                        $"TrustServerCertificate=True;";

        // PASSA SERVIDOR E USUÁRIO PARA O MENU
        var menuForm = new MenuForm(connectionStringOrigem, txtServidor.Text.Trim(), txtUsuario.Text.Trim());
        menuForm.FormClosed += (s, args) => this.Close();
        menuForm.Show();
      }
      catch (Exception ex)
      {
        MessageBox.Show($"Erro ao conectar:\n\n{ex.Message}", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
      }
      finally
      {
        btnEntrar.Enabled = true;
        btnEntrar.Text = "🔓 Entrar";
        this.Cursor = Cursors.Default;
      }
    }
  }
}