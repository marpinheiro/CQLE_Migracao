namespace CQLE_MIGRACAO.Forms
{
  partial class LoginForm
  {
    private System.ComponentModel.IContainer components = null;

    private TextBox txtServidor;
    private TextBox txtUsuario;
    private TextBox txtSenha;
    private Button btnEntrar;
    private Label lblServidor;
    private Label lblUsuario;
    private Label lblSenha;

    protected override void Dispose(bool disposing)
    {
      if (disposing && (components != null))
        components.Dispose();

      base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
      txtServidor = new TextBox();
      txtUsuario = new TextBox();
      txtSenha = new TextBox();
      btnEntrar = new Button();
      lblServidor = new Label();
      lblUsuario = new Label();
      lblSenha = new Label();

      SuspendLayout();

      lblServidor.Text = "Servidor";
      lblServidor.Location = new Point(20, 20);

      txtServidor.Location = new Point(20, 40);
      txtServidor.Width = 260;

      lblUsuario.Text = "Usuário";
      lblUsuario.Location = new Point(20, 75);

      txtUsuario.Location = new Point(20, 95);
      txtUsuario.Width = 260;

      lblSenha.Text = "Senha";
      lblSenha.Location = new Point(20, 130);

      txtSenha.Location = new Point(20, 150);
      txtSenha.Width = 260;
      txtSenha.PasswordChar = '*';

      btnEntrar.Text = "Entrar";
      btnEntrar.Location = new Point(20, 190);
      btnEntrar.Width = 260;
      btnEntrar.Click += btnEntrar_Click;

      ClientSize = new Size(310, 250);
      Controls.AddRange(new Control[]
      {
                lblServidor, txtServidor,
                lblUsuario, txtUsuario,
                lblSenha, txtSenha,
                btnEntrar
      });

      Text = "Login - CQLE Migração";
      FormBorderStyle = FormBorderStyle.FixedDialog;
      MaximizeBox = false;
      StartPosition = FormStartPosition.CenterScreen;

      ResumeLayout(false);
    }
  }
}
