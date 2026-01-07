#nullable enable
using System;
using System.Windows.Forms;
using Microsoft.Data.SqlClient;

namespace CQLE_MIGRACAO.Forms
{
  public class DestinoConnectionForm : Form
  {
    private TextBox txtServidor = null!;
    private TextBox txtUsuario = null!;
    private TextBox txtSenha = null!;

    public string? ConnectionString { get; private set; }

    public DestinoConnectionForm()
    {
      InitializeComponent();
    }

    private void InitializeComponent()
    {
      Text = "Conectar Servidor Destino";
      Size = new System.Drawing.Size(400, 260);
      StartPosition = FormStartPosition.CenterParent;
      FormBorderStyle = FormBorderStyle.FixedDialog;
      MaximizeBox = false;

      Controls.Add(new Label { Text = "Servidor", Left = 20, Top = 20 });
      txtServidor = new TextBox { Left = 20, Top = 40, Width = 340 };

      Controls.Add(new Label { Text = "Usuário", Left = 20, Top = 75 });
      txtUsuario = new TextBox { Left = 20, Top = 95, Width = 340 };

      Controls.Add(new Label { Text = "Senha", Left = 20, Top = 130 });
      txtSenha = new TextBox
      {
        Left = 20,
        Top = 150,
        Width = 340,
        UseSystemPasswordChar = true
      };

      var btnConectar = new Button
      {
        Text = "Conectar",
        Left = 20,
        Top = 190,
        Width = 340
      };

      btnConectar.Click += BtnConectar_Click;

      Controls.AddRange(new Control[]
      {
        txtServidor,
        txtUsuario,
        txtSenha,
        btnConectar
      });
    }

    private void BtnConectar_Click(object? sender, EventArgs e)
    {
      try
      {
        var cs = new SqlConnectionStringBuilder
        {
          DataSource = txtServidor.Text.Trim(),
          UserID = txtUsuario.Text.Trim(),
          Password = txtSenha.Text,
          InitialCatalog = "master",
          TrustServerCertificate = true
        };

        using var conn = new SqlConnection(cs.ConnectionString);
        conn.Open();

        using var cmd = new SqlCommand(
          "SELECT IS_SRVROLEMEMBER('sysadmin')",
          conn);

        if (Convert.ToInt32(cmd.ExecuteScalar()) != 1)
          throw new Exception("O usuário não possui permissão SYSADMIN.");

        ConnectionString = cs.ConnectionString;
        DialogResult = DialogResult.OK;
        Close();
      }
      catch (Exception ex)
      {
        MessageBox.Show(
          ex.Message,
          "Erro de Conexão",
          MessageBoxButtons.OK,
          MessageBoxIcon.Error);
      }
    }
  }
}
