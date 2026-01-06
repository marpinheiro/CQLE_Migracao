using System;
using System.Drawing;
using System.Windows.Forms;

namespace CQLE_MIGRACAO.Forms
{
  public class DestinationDialog : Form
  {
    public string ConnectionString { get; private set; } = string.Empty;
    public string ServerName { get; private set; } = string.Empty;

    // CORREÇÃO: O '= null!' avisa o compilador que vamos instanciar no construtor
    private TextBox txtServer = null!;
    private TextBox txtUser = null!;
    private TextBox txtPass = null!;
    private CheckBox chkIntegrated = null!;

    public DestinationDialog()
    {
      this.Text = "Conexão de Destino";
      this.Size = new Size(400, 300);
      this.StartPosition = FormStartPosition.CenterParent;
      this.FormBorderStyle = FormBorderStyle.FixedDialog;
      this.MaximizeBox = false;
      this.MinimizeBox = false;

      int y = 20;

      Label lblInfo = new Label { Text = "Informe os dados da Instância de DESTINO:", AutoSize = true, Location = new Point(20, y), Font = new Font("Segoe UI", 9F, FontStyle.Bold) };
      this.Controls.Add(lblInfo); y += 30;

      // Server
      this.Controls.Add(new Label { Text = "Servidor / IP:", Location = new Point(20, y), AutoSize = true });
      txtServer = new TextBox { Location = new Point(120, y - 3), Width = 230, Text = "LOCALHOST" };
      this.Controls.Add(txtServer); y += 35;

      // Auth Checkbox
      chkIntegrated = new CheckBox { Text = "Autenticação do Windows", Location = new Point(120, y), AutoSize = true, Checked = false };
      this.Controls.Add(chkIntegrated); y += 35;

      // User
      this.Controls.Add(new Label { Text = "Usuário (sa):", Location = new Point(20, y), AutoSize = true });
      txtUser = new TextBox { Location = new Point(120, y - 3), Width = 230 };
      this.Controls.Add(txtUser); y += 35;

      // Pass
      this.Controls.Add(new Label { Text = "Senha:", Location = new Point(20, y), AutoSize = true });
      txtPass = new TextBox { Location = new Point(120, y - 3), Width = 230, PasswordChar = '*' };
      this.Controls.Add(txtPass); y += 45;

      // Evento do Checkbox (agora seguro porque os txts já foram criados acima)
      chkIntegrated.CheckedChanged += (s, e) =>
      {
        txtUser.Enabled = !chkIntegrated.Checked;
        txtPass.Enabled = !chkIntegrated.Checked;
      };

      // Botões
      Button btnOk = new Button { Text = "Confirmar", Location = new Point(180, y), DialogResult = DialogResult.OK, Width = 80, BackColor = Color.ForestGreen, ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
      Button btnCancel = new Button { Text = "Cancelar", Location = new Point(270, y), DialogResult = DialogResult.Cancel, Width = 80 };

      btnOk.Click += BtnOk_Click;

      this.Controls.Add(btnOk);
      this.Controls.Add(btnCancel);
      this.AcceptButton = btnOk;
    }

    private void BtnOk_Click(object? sender, EventArgs e)
    {
      ServerName = txtServer.Text;

      if (chkIntegrated.Checked)
        ConnectionString = $"Server={txtServer.Text};Database=master;Trusted_Connection=True;TrustServerCertificate=True;";
      else
        ConnectionString = $"Server={txtServer.Text};Database=master;User Id={txtUser.Text};Password={txtPass.Text};TrustServerCertificate=True;";
    }
  }
}