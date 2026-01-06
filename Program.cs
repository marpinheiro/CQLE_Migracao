using System;
using System.Windows.Forms;
using CQLE_MIGRACAO.Forms; // Certifique-se que este é o namespace onde está seu MigrationForm

namespace CQLE_MIGRACAO
{
  static class Program
  {
    [STAThread]
    static void Main()
    {
      // Tenta pegar erros globais que o Visual Studio as vezes esconde
      AppDomain.CurrentDomain.UnhandledException += (sender, error) =>
      {
        MessageBox.Show("Erro Fatal (Unhandled): " + error.ExceptionObject.ToString(), "Erro Crítico", MessageBoxButtons.OK, MessageBoxIcon.Error);
      };

      try
      {
        Application.SetHighDpiMode(HighDpiMode.SystemAware);
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);

        // --- ATENÇÃO AQUI ---
        // Verifique se o nome do seu formulário principal é "MigrationForm" ou "Form1"
        // Se o nome do arquivo é MigrationForm.cs, a classe deve ser MigrationForm.
        var mainForm = new MigrationForm();

        Application.Run(mainForm);
      }
      catch (Exception ex)
      {
        // Se der erro ao abrir, mostra essa mensagem
        MessageBox.Show($"O programa falhou ao iniciar.\n\nERRO TÉCNICO:\n{ex.ToString()}",
                        "CQLE Automator - Falha de Inicialização",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
      }
    }
  }
}