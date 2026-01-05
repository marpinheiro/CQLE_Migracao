using System;
using System.Windows.Forms;
using CQLE_MIGRACAO.Data;
using CQLE_MIGRACAO.Security;
using Microsoft.Data.SqlClient;

namespace CQLE_MIGRACAO.Forms
{
  public partial class LoginForm : Form
  {
    public LoginForm()
    {
      InitializeComponent();
      this.Icon = new Icon("CQLE.ico");
    }

    private void btnEntrar_Click(object sender, EventArgs e)
    {
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
              "Usuário não é sysadmin no SQL Server.",
              "Acesso negado",
              MessageBoxButtons.OK,
              MessageBoxIcon.Error
          );
          return;
        }

        MessageBox.Show(
            "Login validado com sucesso.",
            "OK",
            MessageBoxButtons.OK,
            MessageBoxIcon.Information
        );

        this.Hide();
        new MigrationForm().Show();
      }
      catch (Exception ex)
      {
        MessageBox.Show(
            ex.Message,
            "Erro de conexão",
            MessageBoxButtons.OK,
            MessageBoxIcon.Error
        );
      }
    }
  }
}
