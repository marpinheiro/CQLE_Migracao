
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using CQLE_MIGRACAO.Services;

namespace CQLE_MIGRACAO.Forms
{
  public class MigrationProgressForm : Form
  {
    // Controles de Tela
    private ProgressBar pbGeral = null!;
    private Label lblStatus = null!;
    private RichTextBox txtLog = null!; // Mudança importante: RichTextBox
    private Button btnFechar = null!;
    private Button btnCancelar = null!;
    private Label lblPercentage = null!;

    // Dados
    private readonly MigrationEngine _engine;
    private readonly List<string> _bancos;
    private readonly string _serverDestino;
    private readonly bool _isOnline;

    // Controle de Cancelamento
    private CancellationTokenSource? _cts;

    public MigrationProgressForm(MigrationEngine engine, List<string> bancos, string serverDestino, bool isOnline)
    {
      _engine = engine;
      _bancos = bancos;
      _serverDestino = serverDestino;
      _isOnline = isOnline;

      ConfigurarInterface();

      // Começa o processo assim que a tela é exibida
      this.Shown += async (s, e) => await IniciarProcessoGrafico();
    }

    private void ConfigurarInterface()
    {
      this.Text = "Executando Migração - CQLE Automator";
      this.Size = new Size(750, 600);
      this.StartPosition = FormStartPosition.CenterScreen;
      this.FormBorderStyle = FormBorderStyle.FixedDialog;
      this.MaximizeBox = false;

      // Labels e Barra
      lblStatus = new Label { Text = "Aguardando...", Location = new Point(20, 15), Size = new Size(500, 20), Font = new Font("Segoe UI", 10, FontStyle.Bold) };
      lblPercentage = new Label { Text = "0%", Location = new Point(650, 15), Size = new Size(60, 20), TextAlign = ContentAlignment.TopRight };
      pbGeral = new ProgressBar { Location = new Point(20, 40), Size = new Size(690, 25) };

      // Log: RichTextBox permite selecionar e copiar o texto
      txtLog = new RichTextBox
      {
        Location = new Point(20, 80),
        Size = new Size(690, 400),
        ReadOnly = true,
        ScrollBars = RichTextBoxScrollBars.Vertical,
        BackColor = Color.White,
        Font = new Font("Consolas", 9)
      };

      // Botões
      btnCancelar = new Button { Text = "Interromper", Location = new Point(20, 500), Size = new Size(120, 40), ForeColor = Color.Red };
      btnCancelar.Click += BtnCancelar_Click;

      btnFechar = new Button { Text = "Concluir", Location = new Point(315, 500), Size = new Size(120, 40), Enabled = false };
      btnFechar.Click += (s, e) => this.Close();

      this.Controls.AddRange(new Control[] { lblStatus, lblPercentage, pbGeral, txtLog, btnCancelar, btnFechar });
    }

    private void BtnCancelar_Click(object? sender, EventArgs e)
    {
      if (_cts != null && !_cts.IsCancellationRequested)
      {
        if (MessageBox.Show("Deseja realmente parar o processo? Os bancos pendentes não serão migrados.",
            "Confirmar Parada", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
        {
          _cts.Cancel();
          AddLog("!!! USUÁRIO SOLICITOU CANCELAMENTO. AGUARDE... !!!");
          btnCancelar.Enabled = false;
        }
      }
    }

    private async Task IniciarProcessoGrafico()
    {
      // 1. SOLICITA A PASTA PARA O USUÁRIO (Evita erro de permissão)
      string pastaBackup = "";
      using (var fbd = new FolderBrowserDialog())
      {
        fbd.Description = "Escolha a pasta para salvar os arquivos temporários (.bak):";
        fbd.ShowNewFolderButton = true;
        fbd.SelectedPath = @"C:\"; // Sugere raiz, mas usuário pode mudar

        if (fbd.ShowDialog() != DialogResult.OK)
        {
          AddLog("Cancelado pelo usuário.");
          btnFechar.Enabled = true;
          btnCancelar.Enabled = false;
          return;
        }
        pastaBackup = fbd.SelectedPath;
      }

      _cts = new CancellationTokenSource();
      int total = _bancos.Count;
      int atual = 0;
      pbGeral.Maximum = total * 100;

      AddLog($"Pasta de trabalho definida: {pastaBackup}");
      AddLog("Iniciando migração...");

      try
      {
        foreach (var banco in _bancos)
        {
          // Se o usuário clicou em cancelar, para aqui
          _cts.Token.ThrowIfCancellationRequested();

          lblStatus.Text = $"Migrando: {banco}...";
          AddLog("------------------------------------------------");
          AddLog($">>> Banco: {banco}");

          bool sucesso = await Task.Run(() =>
          {
            try
            {
              // CHAMADA CORRIGIDA: Enviando a pastaBackup para o motor
              _engine.ExecutarMigracaoAutomatizada(
                            banco,
                            _serverDestino,
                            _isOnline,
                            pastaBackup,
                            (msg) => this.Invoke(new Action(() => AddLog("   " + msg)))
                        );
              return true;
            }
            catch (Exception ex)
            {
              this.Invoke(new Action(() => AddLog($"   ❌ ERRO: {ex.Message}")));
              return false;
            }
          }, _cts.Token);

          atual++;
          pbGeral.Value = atual * 100;
          lblPercentage.Text = $"{(int)((atual / (float)total) * 100)}%";

          if (sucesso) AddLog($"✅ {banco} finalizado.");
          else AddLog($"⚠️ {banco} finalizado com falha.");
        }

        lblStatus.Text = "Concluído.";
        AddLog("=== FIM DO PROCESSO ===");
      }
      catch (OperationCanceledException)
      {
        lblStatus.Text = "Interrompido.";
        AddLog("=== PROCESSO PARADO PELO USUÁRIO ===");
      }
      finally
      {
        btnCancelar.Enabled = false;
        btnFechar.Enabled = true;
        _cts = null;
      }
    }

    private void AddLog(string msg)
    {
      if (txtLog.InvokeRequired) txtLog.Invoke(new Action(() => AddLog(msg)));
      else
      {
        txtLog.AppendText($"[{DateTime.Now:HH:mm:ss}] {msg}{Environment.NewLine}");
        // Rola para o fim
        txtLog.SelectionStart = txtLog.Text.Length;
        txtLog.ScrollToCaret();
      }
    }
  }
}