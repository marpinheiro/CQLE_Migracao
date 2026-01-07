#nullable disable
using System;
using System.Drawing;
using System.Windows.Forms;
using Microsoft.Data.SqlClient;
using CQLE_MIGRACAO.Security;
using CQLE_MIGRACAO.Data;

namespace CQLE_MIGRACAO.Forms
{
  public class DestinationLoginForm : Form
  {
    private TextBox txtServidor;
    private TextBox txtUsuario;
    private TextBox txtSenha;
    private Button btnConectar;
    private Button btnCancelar;
    private Label lblMensagem;

    public string ConnectionStringDestino { get; private set; }
    public bool Conectado { get; private set; } = false;

    private readonly string _servidorInicial;

    public DestinationLoginForm(string servidorInicial = "")
    {
      _servidorInicial = servidorInicial;
      ConfigurarInterface();
    }

    private void ConfigurarInterface()
    {
      this.Text = "CQLE Migração - Login no Servidor Destino";
      this.Size = new Size(500, 480); // Aumentado para caber tudo
      this.StartPosition = FormStartPosition.CenterParent;
      this.FormBorderStyle = FormBorderStyle.FixedDialog;
      this.MaximizeBox = false;
      this.BackColor = Color.FromArgb(240, 240, 245);

      try
      {
        this.Icon = new Icon("Assets/CQLE.ico");
      }
      catch { }

      // Header
      var panelHeader = new Panel
      {
        Dock = DockStyle.Top,
        Height = 80,
        BackColor = Color.FromArgb(0, 120, 215)
      };

      var lblTitulo = new Label
      {
        Text = "Conexão ao Destino",
        Location = new Point(20, 20),
        AutoSize = true,
        Font = new Font("Segoe UI", 16, FontStyle.Bold),
        ForeColor = Color.White
      };

      panelHeader.Controls.Add(lblTitulo);

      // Campos
      Label lblServidor = new Label
      {
        Text = "Servidor Destino:",
        Location = new Point(50, 100),
        AutoSize = true,
        Font = new Font("Segoe UI", 10)
      };

      txtServidor = new TextBox
      {
        Location = new Point(50, 125),
        Size = new Size(400, 28),
        Font = new Font("Segoe UI", 10),
        Text = _servidorInicial
      };

      Label lblUsuario = new Label
      {
        Text = "Usuário:",
        Location = new Point(50, 165),
        AutoSize = true,
        Font = new Font("Segoe UI", 10)
      };

      txtUsuario = new TextBox
      {
        Location = new Point(50, 190),
        Size = new Size(400, 28),
        Font = new Font("Segoe UI", 10)
      };

      Label lblSenha = new Label
      {
        Text = "Senha:",
        Location = new Point(50, 230),
        AutoSize = true,
        Font = new Font("Segoe UI", 10)
      };

      txtSenha = new TextBox
      {
        Location = new Point(50, 255),
        Size = new Size(400, 28),
        Font = new Font("Segoe UI", 10),
        PasswordChar = '*'
      };

      lblMensagem = new Label
      {
        Location = new Point(50, 295),
        Size = new Size(400, 40),
        ForeColor = Color.Red,
        TextAlign = ContentAlignment.MiddleCenter,
        Font = new Font("Segoe UI", 10)
      };

      // Botões - POSICIONADOS CORRETAMENTE E VISÍVEIS
      btnConectar = new Button
      {
        Text = "Conectar",
        Location = new Point(50, 350),
        Size = new Size(180, 50),
        BackColor = Color.FromArgb(0, 120, 215),
        ForeColor = Color.White,
        Font = new Font("Segoe UI", 11, FontStyle.Bold),
        FlatStyle = FlatStyle.Flat
      };
      btnConectar.Click += BtnConectar_Click;

      btnCancelar = new Button
      {
        Text = "Cancelar",
        Location = new Point(270, 350),
        Size = new Size(180, 50),
        BackColor = Color.Gray,
        ForeColor = Color.White,
        Font = new Font("Segoe UI", 11, FontStyle.Bold),
        FlatStyle = FlatStyle.Flat
      };
      btnCancelar.Click += (s, e) => this.Close();

      // Adiciona tudo
      this.Controls.Add(panelHeader);
      this.Controls.AddRange(new Control[]
      {
                lblServidor, txtServidor,
                lblUsuario, txtUsuario,
                lblSenha, txtSenha,
                lblMensagem,
                btnConectar, btnCancelar
      });
    }

    private void BtnConectar_Click(object sender, EventArgs e)
    {
      if (string.IsNullOrWhiteSpace(txtServidor.Text) ||
          string.IsNullOrWhiteSpace(txtUsuario.Text) ||
          string.IsNullOrWhiteSpace(txtSenha.Text))
      {
        lblMensagem.Text = "Preencha todos os campos.";
        return;
      }

      lblMensagem.Text = "Conectando...";
      lblMensagem.ForeColor = Color.Blue;
      btnConectar.Enabled = false;

      try
      {
        using SqlConnection conn = SqlServerConnection.Create(txtServidor.Text.Trim(), txtUsuario.Text.Trim(), txtSenha.Text);
        conn.Open();

        if (!SqlSecurityService.IsSysAdmin(conn))
        {
          lblMensagem.Text = "Usuário precisa ser sysadmin no destino.";
          lblMensagem.ForeColor = Color.Red;
          return;
        }

        ConnectionStringDestino = $"Server={txtServidor.Text.Trim()};Database=master;User ID={txtUsuario.Text.Trim()};Password={txtSenha.Text};TrustServerCertificate=True;";
        Conectado = true;

        MessageBox.Show("Conectado com sucesso ao servidor destino!", "Sucesso", MessageBoxButtons.OK, MessageBoxIcon.Information);
        this.DialogResult = DialogResult.OK;
        this.Close();
      }
      catch (Exception ex)
      {
        lblMensagem.Text = $"Erro: {ex.Message}";
        lblMensagem.ForeColor = Color.Red;
      }
      finally
      {
        btnConectar.Enabled = true;
      }
    }
  }
}