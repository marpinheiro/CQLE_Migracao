using System;
using System.Drawing;
using System.Windows.Forms;

namespace CQLE_MIGRACAO.Forms
{
  public class ScriptForm : Form
  {
    private TextBox txtScript;

    public ScriptForm(string scriptGerado)
    {
      this.Text = "CQLE - Janela de Execução (Modo Offline)";
      this.Size = new Size(800, 600);
      this.StartPosition = FormStartPosition.CenterScreen;

      // Área de texto do Script
      txtScript = new TextBox();
      txtScript.Multiline = true;
      txtScript.ScrollBars = ScrollBars.Vertical;
      txtScript.Dock = DockStyle.Top;
      txtScript.Height = 500;
      txtScript.Font = new Font("Consolas", 10F);
      txtScript.Text = scriptGerado;

      // Permite Ctrl+A para selecionar tudo
      txtScript.KeyDown += (s, e) =>
      {
        if (e.Control && e.KeyCode == Keys.A) txtScript.SelectAll();
      };

      this.Controls.Add(txtScript);

      // Botão Copiar
      Button btnCopy = new Button();
      btnCopy.Text = "Copiar para Área de Transferência";
      btnCopy.Location = new Point(20, 510);
      btnCopy.Size = new Size(250, 40);
      btnCopy.Click += (s, e) =>
      {
        Clipboard.SetText(txtScript.Text);
        MessageBox.Show("Script copiado com sucesso!", "CQLE");
      };
      this.Controls.Add(btnCopy);
    }
  }
}