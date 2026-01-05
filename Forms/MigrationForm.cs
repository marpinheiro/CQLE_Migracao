using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace CQLE_MIGRACAO.Forms
{
  public class MigrationForm : Form
  {
    private TextBox? txtLog;


    public MigrationForm()
    {
      Text = "CQLE Migração";
      StartPosition = FormStartPosition.CenterScreen;
      Size = new Size(900, 500);
      FormBorderStyle = FormBorderStyle.FixedSingle;
      MaximizeBox = false;

      if (File.Exists("CQLE.ico"))
        Icon = new Icon("CQLE.ico");

      CriarLayout();
    }

    private void CriarLayout()
    {
      Label lblTitulo = new Label
      {
        Text = "Migração SQL Server",
        Font = new Font("Segoe UI", 16, FontStyle.Bold),
        AutoSize = true,
        Location = new Point(20, 20)
      };

      txtLog = new TextBox
      {
        Multiline = true,
        ScrollBars = ScrollBars.Vertical,
        Location = new Point(20, 70),
        Size = new Size(840, 350),
        ReadOnly = true
      };

      Controls.Add(lblTitulo);
      Controls.Add(txtLog);
    }
  }
}
