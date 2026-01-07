using System;
using System.Diagnostics;
using System.Windows.Forms;
using CQLE_MIGRACAO.Forms;

namespace CQLE_MIGRACAO
{
  internal static class Program
  {
    /// <summary>
    /// Ponto de entrada principal para o aplicativo.
    /// </summary>
    [STAThread]
    static void Main()
    {
      // 1. Verificação da versão do .NET (exige .NET 8.0)
      if (Environment.Version.Major != 8)
      {
        MessageBox.Show(
            "⚠️ ATENÇÃO: Versão do .NET incompatível!\n\n" +
            "Este aplicativo foi compilado para o .NET 8.0.\n" +
            $"Versão detectada: {Environment.Version}\n\n" +
            "Por favor, instale o .NET 8.0 Runtime ou SDK para executar corretamente.\n\n" +
            "Download oficial:\nhttps://dotnet.microsoft.com/pt-br/download/dotnet/8.0",
            "CQLE Migração - Erro de Versão",
            MessageBoxButtons.OK,
            MessageBoxIcon.Error);

        // Abre o link de download automaticamente
        try
        {
          Process.Start(new ProcessStartInfo
          {
            FileName = "https://dotnet.microsoft.com/pt-br/download/dotnet/8.0",
            UseShellExecute = true
          });
        }
        catch { /* Ignora erro ao abrir navegador */ }

        // Encerra o aplicativo
        return;
      }

      // 2. Configurações padrão do Windows Forms
      Application.EnableVisualStyles();
      Application.SetCompatibleTextRenderingDefault(false);

      // 3. Inicia o aplicativo com a tela de Login
      Application.Run(new LoginForm());
    }
  }
}