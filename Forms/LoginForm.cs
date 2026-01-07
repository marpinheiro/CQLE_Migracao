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
    private Panel panelHeader;
    private Label lblTitulo;
    private Label lblSubtitulo;
    private GroupBox grpLogin;
    private Label lblServidor;
    private TextBox txtServidor;
    private Label lblUsuario;
    private TextBox txtUsuario;
    private Label lblSenha;
    private TextBox txtSenha;
    private Button btnEntrar;
    private Button btnSair;
    private Label lblRodape;

    public LoginForm()
    {
      ConfigurarInterface();

      try
      {
        this.Icon = new Icon("Assets/CQLE.ico");
      }
      catch { }
    }

    private void ConfigurarInterface()
    {
      this.Text = "CQLE Migração - Login";
      this.Size = new Size(500, 520);
      this.StartPosition = FormStartPosition.CenterScreen;
      this.FormBorderStyle = FormBorderStyle.FixedDialog;
      this.MaximizeBox = false;
      this.BackColor = Color.FromArgb(240, 240, 245);

      // Cabeçalho
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
        Text = "Sistema Profissional de Migração SQL Server",
        Location = new Point(22, 45),
        AutoSize = true,
        Font = new Font("Segoe UI", 9),
        ForeColor = Color.FromArgb(200, 220, 255)
      };

      panelHeader.Controls.Add(lblTitulo);
      panelHeader.Controls.Add(lblSubtitulo);

      // Grupo Login
      grpLogin = new GroupBox
      {
        Text = "  Autenticação SQL Server  ",
        Size = new Size(400, 220),
        Location = new Point(50, 110),
        Font = new Font("Segoe UI", 10, FontStyle.Bold),
        ForeColor = Color.FromArgb(0, 120, 215)
      };

      lblServidor = new Label
      {
        Text = "Servidor:",
        Location = new Point(20, 35),
        AutoSize = true
      };

      txtServidor = new TextBox
      {
        Location = new Point(20, 55),
        Size = new Size(360, 25),
        Font = new Font("Segoe UI", 10),
        Text = "localhost"
      };

      lblUsuario = new Label
      {
        Text = "Usuário:",
        Location = new Point(20, 90),
        AutoSize = true
      };

      txtUsuario = new TextBox
      {
        Location = new Point(20, 110),
        Size = new Size(360, 25),
        Font = new Font("Segoe UI", 10)
      };

      lblSenha = new Label
      {
        Text = "Senha:",
        Location = new Point(20, 145),
        AutoSize = true
      };

      txtSenha = new TextBox
      {
        Location = new Point(20, 165),
        Size = new Size(360, 25),
        Font = new Font("Segoe UI", 10),
        PasswordChar = '*'
      };

      grpLogin.Controls.Add(lblServidor);
      grpLogin.Controls.Add(txtServidor);
      grpLogin.Controls.Add(lblUsuario);
      grpLogin.Controls.Add(txtUsuario);
      grpLogin.Controls.Add(lblSenha);
      grpLogin.Controls.Add(txtSenha);

      // Botão Entrar
      btnEntrar = new Button
      {
        Text = "🔓 Entrar",
        Location = new Point(50, 355),
        Size = new Size(140, 45),
        BackColor = Color.FromArgb(0, 120, 215),
        ForeColor = Color.White,
        Font = new Font("Segoe UI", 11, FontStyle.Bold),
        FlatStyle = FlatStyle.Flat,
        Cursor = Cursors.Hand
      };
      btnEntrar.FlatAppearance.BorderSize = 0;
      btnEntrar.Click += BtnEntrar_Click;

      // Botão Sair
      btnSair = new Button
      {
        Text = "❌ Sair",
        Location = new Point(310, 355),
        Size = new Size(140, 45),
        BackColor = Color.FromArgb(120, 120, 120),
        ForeColor = Color.White,
        Font = new Font("Segoe UI", 11, FontStyle.Bold),
        FlatStyle = FlatStyle.Flat,
        Cursor = Cursors.Hand
      };
      btnSair.FlatAppearance.BorderSize = 0;
      btnSair.Click += (s, e) => Application.Exit();

      // Rodapé
      lblRodape = new Label
      {
        Text = "Desenvolvido por Marciano Silva - CQLE Softwares © 2025",
        Dock = DockStyle.Bottom,
        Height = 25,
        TextAlign = ContentAlignment.MiddleCenter,
        Font = new Font("Segoe UI", 8),
        ForeColor = Color.Gray
      };

      // Adiciona controles ao form
      this.Controls.Add(panelHeader);
      this.Controls.Add(grpLogin);
      this.Controls.Add(btnEntrar);
      this.Controls.Add(btnSair);
      this.Controls.Add(lblRodape);

      // Enter para logar
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
        MessageBox.Show(
            "Preencha todos os campos.",
            "Validação",
            MessageBoxButtons.OK,
            MessageBoxIcon.Warning
        );
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
          MessageBox.Show(
              "O usuário não possui permissão SYSADMIN.",
              "Acesso Negado",
              MessageBoxButtons.OK,
              MessageBoxIcon.Error
          );
          return;
        }

        MessageBox.Show(
            "✅ Login validado com sucesso!\n\n" +
            $"Servidor: {txtServidor.Text}\n" +
            $"Usuário: {txtUsuario.Text}\n" +
            "Permissões: SYSADMIN\n\n" +
            "Bem-vindo ao CQLE Migração!",
            "Autenticação Bem-Sucedida",
            MessageBoxButtons.OK,
            MessageBoxIcon.Information
        );

        this.Hide();

        // Monta connection string 
        string connectionStringOrigem = $"Server={txtServidor.Text.Trim()};" +
                                        $"Database=master;" +
                                        $"User ID={txtUsuario.Text.Trim()};" +
                                        $"Password={txtSenha.Text};" +
                                        $"TrustServerCertificate=True;";

        var menuForm = new MenuForm(connectionStringOrigem);
        menuForm.FormClosed += (s, args) => this.Close();
        menuForm.Show();
      }
      catch (Exception ex)
      {
        MessageBox.Show(
            $"Erro ao conectar:\n\n{ex.Message}",
            "Erro",
            MessageBoxButtons.OK,
            MessageBoxIcon.Error
        );
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