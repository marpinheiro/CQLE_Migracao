using System;
using System.Windows.Forms;
using CQLE_MIGRACAO.Forms;

namespace CQLE_MIGRACAO
{
  static class Program
  {
    [STAThread]
    static void Main()
    {
      Application.SetHighDpiMode(HighDpiMode.SystemAware);
      Application.EnableVisualStyles();
      Application.SetCompatibleTextRenderingDefault(false);

      // Inicia o aplicativo com a tela de Login como principal
      Application.Run(new LoginForm());
    }
  }
}