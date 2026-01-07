using CQLE_MIGRACAO.Forms;
using CQLE_MIGRACAO.Services;
using System;
using System.Windows.Forms;

namespace CQLE_MIGRACAO
{
  internal static class Program
  {
    [STAThread]
    static void Main()
    {
      ApplicationConfiguration.Initialize();

      // ========================================================
      // 🔒 LÓGICA DO TRIAL (30 DIAS NA MÁQUINA)
      // ========================================================

      // Verifica o status atual
      var check = TrialSystem.CheckTrial(30); // 30 dias de período

      if (check.status != TrialSystem.TrialStatus.Valid)
      {
        string msgErro = "";
        string titulo = "Licença Inválida";

        switch (check.status)
        {
          case TrialSystem.TrialStatus.Expired:
            msgErro = "O período de testes de 30 dias expirou!\n\nPara continuar utilizando, adquira a licença.";
            titulo = "Trial Expirado";
            break;
          case TrialSystem.TrialStatus.ClockTampered:
            msgErro = "Data do sistema inconsistente.\nFoi detectada alteração no relógio do Windows para burlar o sistema.";
            break;
          case TrialSystem.TrialStatus.Corrupted:
            msgErro = "Erro na validação da licença. Os arquivos de registro foram corrompidos ou alterados manualmente.";
            break;
        }

        MessageBox.Show(
            msgErro + "\n\nContato: atendimento@cqle.com.br",
            titulo,
            MessageBoxButtons.OK,
            MessageBoxIcon.Error
        );

        return; // Fecha o programa
      }

      // AVISO DE CONTAGEM REGRESSIVA (Só nos últimos 5 dias ou no primeiro)
      if (check.daysLeft == 30)
      {
        MessageBox.Show(
           "Obrigado por testar o CQLE Migração!\n\nSeu período de avaliação de 30 dias começou agora.",
           "Bem-vindo", MessageBoxButtons.OK, MessageBoxIcon.Information);
      }
      else if (check.daysLeft <= 5)
      {
        MessageBox.Show(
           $"Atenção: Seu período de testes expira em {check.daysLeft} dias.",
           "Trial", MessageBoxButtons.OK, MessageBoxIcon.Warning);
      }
      // ========================================================

      // SE PASSOU, ABRE O SISTEMA
      // Importante: No seu código original você talvez chame o LoginForm primeiro
      // Como não tenho o LoginForm aqui, vou chamar o Menu direto ou Login se você tiver.
      // Ajuste abaixo conforme sua necessidade real:

      Application.Run(new LoginForm());
    }
  }
}