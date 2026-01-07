#nullable disable
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using Microsoft.Data.SqlClient;

namespace CQLE_MIGRACAO.Forms
{
  public class MoveFilesForm : Form
  {
    private ComboBox cboDatabases;
    private DataGridView gridArquivos;
    private Button btnExecutar;
    private Button btnVoltar;
    private Label lblStatus;

    private readonly string _connectionString;
    private readonly string _servidor;
    private readonly string _usuario;

    private List<ArquivoParaMover> _currentFiles = new List<ArquivoParaMover>();

    public MoveFilesForm(string connectionString, string servidor, string usuario)
    {
      _connectionString = connectionString;
      _servidor = servidor;
      _usuario = usuario;

      InitializeComponent();
      CarregarDatabases();
    }

    private void InitializeComponent()
    {
      this.Text = "CQLE Migração - Mover Datafiles";
      this.StartPosition = FormStartPosition.CenterScreen;
      this.WindowState = FormWindowState.Maximized;
      this.MinimumSize = new Size(1000, 700);
      this.BackColor = Color.WhiteSmoke;
      this.Font = new Font("Segoe UI", 9F);

      try { this.Icon = new Icon("Assets/CQLE.ico"); } catch { }

      // === CABEÇALHO ===
      var panelHeader = new Panel
      {
        Dock = DockStyle.Top,
        Height = 100,
        BackColor = Color.FromArgb(70, 130, 180)
      };

      var lblTitulo = new Label
      {
        Text = "📂 MOVER ARQUIVOS DE BANCO",
        ForeColor = Color.White,
        Font = new Font("Segoe UI", 22F, FontStyle.Bold),
        AutoSize = true,
        Location = new Point(30, 20)
      };

      var lblSubtitulo = new Label
      {
        Text = "Fluxo Completo: Backup > Offline > Copiar > Alterar > Online > Limpar",
        ForeColor = Color.FromArgb(220, 230, 240),
        Font = new Font("Segoe UI", 11F),
        AutoSize = true,
        Location = new Point(32, 65)
      };
      panelHeader.Controls.AddRange(new Control[] { lblTitulo, lblSubtitulo });

      // === BODY ===
      var panelMain = new Panel { Dock = DockStyle.Fill, Padding = new Padding(40, 20, 40, 20) };

      var lblBanco = new Label { Text = "Selecione o Database:", Font = new Font("Segoe UI", 10F, FontStyle.Bold), Dock = DockStyle.Top, Height = 25 };

      cboDatabases = new ComboBox { Dock = DockStyle.Top, Height = 30, DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Segoe UI", 10F) };
      cboDatabases.SelectedIndexChanged += CboDatabases_SelectedIndexChanged;

      var spacer = new Panel { Dock = DockStyle.Top, Height = 20 };

      var lblGrid = new Label { Text = "Configure os novos caminhos (Coluna PARA):", Font = new Font("Segoe UI", 10F, FontStyle.Bold), Dock = DockStyle.Top, Height = 25 };

      gridArquivos = new DataGridView
      {
        Dock = DockStyle.Fill,
        BackgroundColor = Color.White,
        AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
        AllowUserToAddRows = false,
        AllowUserToDeleteRows = false,
        RowHeadersVisible = false,
        Font = new Font("Segoe UI", 9F)
      };
      ConfigurarGrid();
      gridArquivos.CellContentClick += GridArquivos_CellContentClick;

      var panelGrid = new Panel { Dock = DockStyle.Top, Height = 400 };
      panelGrid.Controls.Add(gridArquivos);

      // Montagem do PanelMain (Ordem inversa por causa do Dock Top)
      panelMain.Controls.Add(panelGrid);
      panelMain.Controls.Add(lblGrid);
      panelMain.Controls.Add(spacer);
      panelMain.Controls.Add(cboDatabases);
      panelMain.Controls.Add(lblBanco);

      // === FOOTER ===
      var panelBottom = new Panel { Dock = DockStyle.Bottom, Height = 80 };

      lblStatus = new Label
      {
        Text = "Aguardando seleção...",
        Dock = DockStyle.Top,
        TextAlign = ContentAlignment.MiddleCenter,
        ForeColor = Color.Gray
      };

      btnExecutar = new Button
      {
        Text = "⚙️ INICIAR MOVIMENTAÇÃO",
        Size = new Size(300, 50),
        Anchor = AnchorStyles.Top | AnchorStyles.Right,
        Location = new Point(panelBottom.Width - 540, 20),
        BackColor = Color.SeaGreen,
        ForeColor = Color.White,
        Font = new Font("Segoe UI", 11F, FontStyle.Bold),
        FlatStyle = FlatStyle.Flat,
        Enabled = false
      };
      btnExecutar.FlatAppearance.BorderSize = 0;
      btnExecutar.Click += BtnExecutar_Click;

      btnVoltar = new Button
      {
        Text = "⬅ Voltar",
        Size = new Size(200, 50),
        Anchor = AnchorStyles.Top | AnchorStyles.Right,
        Location = new Point(panelBottom.Width - 220, 20),
        BackColor = Color.Gray,
        ForeColor = Color.White,
        Font = new Font("Segoe UI", 11F, FontStyle.Bold),
        FlatStyle = FlatStyle.Flat
      };
      btnVoltar.FlatAppearance.BorderSize = 0;
      btnVoltar.Click += (s, e) => this.Close();

      panelBottom.Controls.AddRange(new Control[] { lblStatus, btnExecutar, btnVoltar });

      this.Controls.Add(panelMain);
      this.Controls.Add(panelHeader);
      this.Controls.Add(panelBottom);
    }

    private void ConfigurarGrid()
    {
      gridArquivos.Columns.Clear();
      gridArquivos.Columns.Add(new DataGridViewTextBoxColumn { Name = "LogicalName", HeaderText = "Nome Lógico", ReadOnly = true, FillWeight = 20 });
      gridArquivos.Columns.Add(new DataGridViewTextBoxColumn { Name = "Type", HeaderText = "Tipo", ReadOnly = true, FillWeight = 10 });
      gridArquivos.Columns.Add(new DataGridViewTextBoxColumn { Name = "CurrentPath", HeaderText = "DE (Origem)", ReadOnly = true, FillWeight = 35 });
      gridArquivos.Columns.Add(new DataGridViewTextBoxColumn { Name = "NewPath", HeaderText = "PARA (Destino)", ReadOnly = false, FillWeight = 35 });

      var btn = new DataGridViewButtonColumn
      {
        HeaderText = "Selecionar Pasta",
        Text = "...",
        UseColumnTextForButtonValue = true,
        FillWeight = 10
      };
      gridArquivos.Columns.Add(btn);
    }

    private void CarregarDatabases()
    {
      try
      {
        using var conn = new SqlConnection(_connectionString);
        conn.Open();
        var cmd = new SqlCommand("SELECT name FROM sys.databases WHERE name NOT IN ('master', 'model', 'msdb', 'tempdb') ORDER BY name", conn);
        using var reader = cmd.ExecuteReader();
        cboDatabases.Items.Clear();
        cboDatabases.Items.Add("Selecione...");
        while (reader.Read()) cboDatabases.Items.Add(reader["name"].ToString());
        cboDatabases.SelectedIndex = 0;
      }
      catch (Exception ex) { MessageBox.Show(ex.Message); }
    }

    private void CboDatabases_SelectedIndexChanged(object sender, EventArgs e)
    {
      gridArquivos.Rows.Clear();
      _currentFiles.Clear();
      btnExecutar.Enabled = false;

      if (cboDatabases.SelectedIndex <= 0) return;

      string dbName = cboDatabases.SelectedItem.ToString();

      try
      {
        using var conn = new SqlConnection(_connectionString);
        conn.Open();
        var cmd = new SqlCommand($"SELECT name, physical_name, type_desc FROM sys.master_files WHERE database_id = DB_ID('{dbName}')", conn);
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
          var file = new ArquivoParaMover
          {
            LogicalName = reader["name"].ToString(),
            CaminhoOrigem = reader["physical_name"].ToString(),
            CaminhoDestino = reader["physical_name"].ToString(), // Inicialmente igual
            Tipo = reader["type_desc"].ToString()
          };
          _currentFiles.Add(file);
          gridArquivos.Rows.Add(file.LogicalName, file.Tipo, file.CaminhoOrigem, file.CaminhoDestino, "...");
        }
        btnExecutar.Enabled = true;
        lblStatus.Text = $"{_currentFiles.Count} arquivos carregados.";
      }
      catch (Exception ex) { MessageBox.Show(ex.Message); }
    }

    private void GridArquivos_CellContentClick(object sender, DataGridViewCellEventArgs e)
    {
      if (e.RowIndex >= 0 && e.ColumnIndex == 4) // Botão ...
      {
        string currentPath = gridArquivos.Rows[e.RowIndex].Cells["NewPath"].Value.ToString();
        string fileName = Path.GetFileName(currentPath);

        using var fbd = new FolderBrowserDialog { SelectedPath = Path.GetDirectoryName(currentPath) };
        if (fbd.ShowDialog() == DialogResult.OK)
        {
          string newFull = Path.Combine(fbd.SelectedPath, fileName);
          gridArquivos.Rows[e.RowIndex].Cells["NewPath"].Value = newFull;

          // Highlight visual se mudou
          bool mudou = newFull != gridArquivos.Rows[e.RowIndex].Cells["CurrentPath"].Value.ToString();
          gridArquivos.Rows[e.RowIndex].Cells["NewPath"].Style.BackColor = mudou ? Color.LightYellow : Color.White;
        }
      }
    }

    private void BtnExecutar_Click(object sender, EventArgs e)
    {
      string dbName = cboDatabases.SelectedItem.ToString();
      var arquivosParaMover = new List<ArquivoParaMover>();

      // Coletar mudanças
      for (int i = 0; i < gridArquivos.Rows.Count; i++)
      {
        string origem = gridArquivos.Rows[i].Cells["CurrentPath"].Value.ToString();
        string destino = gridArquivos.Rows[i].Cells["NewPath"].Value.ToString();

        if (!origem.Equals(destino, StringComparison.OrdinalIgnoreCase))
        {
          arquivosParaMover.Add(new ArquivoParaMover
          {
            LogicalName = gridArquivos.Rows[i].Cells["LogicalName"].Value.ToString(),
            Tipo = gridArquivos.Rows[i].Cells["Type"].Value.ToString(),
            CaminhoOrigem = origem,
            CaminhoDestino = destino
          });
        }
      }

      if (arquivosParaMover.Count == 0)
      {
        MessageBox.Show("Nenhuma alteração de caminho detectada.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Information);
        return;
      }

      var msg = $"Deseja iniciar o processo para {arquivosParaMover.Count} arquivos?\n\n" +
                "1. Backup\n2. Offline\n3. Copiar\n4. Alterar SQL\n5. Online\n6. Deletar Origem";

      if (MessageBox.Show(msg, "Confirmação", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
      {
        this.Hide();
        new MoveProgressForm(_connectionString, dbName, arquivosParaMover).ShowDialog();
        this.Show();
        // Recarrega para mostrar o estado atual
        CboDatabases_SelectedIndexChanged(null, null);
      }
    }
  }
}