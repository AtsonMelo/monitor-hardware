using System.ComponentModel;
using System.Globalization;
using System.Windows.Forms;

class SensorOriginsForm : Form
{
    private static readonly Color WindowBackground = Color.FromArgb(17, 19, 22);
    private static readonly Color PanelBackground = Color.FromArgb(24, 27, 31);
    private static readonly Color GridBackground = Color.FromArgb(30, 34, 39);
    private static readonly Color ButtonBackground = Color.FromArgb(36, 41, 47);
    private static readonly Color ButtonBorder = Color.FromArgb(58, 66, 74);
    private static readonly Color HeaderBackground = Color.FromArgb(24, 27, 31);
    private static readonly Color HeaderForeground = Color.FromArgb(230, 234, 238);
    private static readonly Color CellForeground = Color.FromArgb(230, 234, 238);
    private static readonly Color MutedText = Color.FromArgb(170, 176, 184);
    private static readonly Color ErrorText = Color.FromArgb(255, 185, 0);

    private readonly WindowsHardwareOriginService _originService;
    private readonly Icon? _ownedIcon;
    private readonly DataGridView _grid;
    private readonly Button _refreshButton;
    private readonly Label _statusLabel;
    private List<SensorOriginInfo> _origins = new();

    public SensorOriginsForm(Icon? windowIcon = null)
    {
        _originService = new WindowsHardwareOriginService();

        Text = "Origem dos sensores, drivers e firmware";
        AutoScaleMode = AutoScaleMode.Dpi;
        StartPosition = FormStartPosition.CenterParent;
        MinimumSize = new Size(900, 520);
        Size = new Size(1100, 650);
        BackColor = WindowBackground;
        ForeColor = HeaderForeground;
        Font = new Font("Segoe UI", 10, FontStyle.Regular, GraphicsUnit.Point);
        ShowInTaskbar = false;

        if (windowIcon != null)
        {
            _ownedIcon = (Icon)windowIcon.Clone();
            Icon = _ownedIcon;
        }

        TableLayoutPanel root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 4,
            Padding = new Padding(18),
            BackColor = WindowBackground
        };

        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 12));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));

        TableLayoutPanel header = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            Margin = new Padding(0, 0, 0, 12),
            BackColor = WindowBackground
        };

        header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        header.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        header.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        Label titleLabel = new Label
        {
            Text = "Origem dos sensores, drivers e firmware",
            Dock = DockStyle.Fill,
            ForeColor = HeaderForeground,
            Font = new Font("Segoe UI", 15, FontStyle.Bold, GraphicsUnit.Point),
            TextAlign = ContentAlignment.MiddleLeft,
            AutoEllipsis = true
        };

        _refreshButton = new Button
        {
            Text = "Atualizar"
        };

        ConfigureActionButton(_refreshButton);
        _refreshButton.Click += (_, _) => RefreshOrigins();

        header.Controls.Add(titleLabel, 0, 0);
        header.Controls.Add(_refreshButton, 1, 0);

        _grid = new DataGridView
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            AllowUserToResizeRows = false,
            MultiSelect = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            AutoGenerateColumns = false,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            ScrollBars = ScrollBars.Vertical,
            BackgroundColor = GridBackground,
            BorderStyle = BorderStyle.FixedSingle,
            GridColor = Color.FromArgb(52, 58, 64),
            RowHeadersVisible = false,
            EnableHeadersVisualStyles = false,
            ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize,
            Font = new Font("Segoe UI", 9.5f, FontStyle.Regular, GraphicsUnit.Point),
            DefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = GridBackground,
                ForeColor = CellForeground,
                SelectionBackColor = Color.FromArgb(0, 120, 212),
                SelectionForeColor = Color.White,
                WrapMode = DataGridViewTriState.False
            },
            AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = PanelBackground,
                ForeColor = CellForeground,
                SelectionBackColor = Color.FromArgb(0, 120, 212),
                SelectionForeColor = Color.White
            },
            ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = HeaderBackground,
                ForeColor = HeaderForeground,
                SelectionBackColor = HeaderBackground,
                SelectionForeColor = HeaderForeground,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold, GraphicsUnit.Point),
                Alignment = DataGridViewContentAlignment.MiddleLeft,
                WrapMode = DataGridViewTriState.False
            }
        };

        ConfigureGridColumns();

        _statusLabel = new Label
        {
            Text = "Aguardando carregamento...",
            Dock = DockStyle.Fill,
            ForeColor = MutedText,
            TextAlign = ContentAlignment.MiddleLeft,
            AutoEllipsis = true,
            Font = new Font("Segoe UI", 9.5f, FontStyle.Regular, GraphicsUnit.Point)
        };

        root.Controls.Add(header, 0, 0);
        root.Controls.Add(_grid, 0, 1);
        root.Controls.Add(new Panel { Dock = DockStyle.Fill, BackColor = WindowBackground, Margin = new Padding(0) }, 0, 2);
        root.Controls.Add(_statusLabel, 0, 3);

        Controls.Add(root);

        Shown += (_, _) => RefreshOrigins();
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        WindowThemeService.ApplyNativeTitleBarTheme(Handle);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _ownedIcon?.Dispose();
        }

        base.Dispose(disposing);
    }

    private static void ConfigureActionButton(Button button)
    {
        button.AutoSize = true;
        button.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        button.Dock = DockStyle.Fill;
        button.Height = 40;
        button.MinimumSize = new Size(142, 40);
        button.Margin = new Padding(0);
        button.Padding = new Padding(10, 0, 10, 0);
        button.FlatStyle = FlatStyle.Flat;
        button.ForeColor = Color.White;
        button.BackColor = ButtonBackground;
        button.UseVisualStyleBackColor = false;
        button.TextAlign = ContentAlignment.MiddleCenter;
        button.FlatAppearance.BorderColor = ButtonBorder;
        button.FlatAppearance.MouseOverBackColor = Color.FromArgb(44, 50, 57);
        button.FlatAppearance.MouseDownBackColor = Color.FromArgb(30, 34, 39);
    }

    private void ConfigureGridColumns()
    {
        _grid.Columns.Clear();
        _grid.Columns.Add(CreateTextColumn("Tipo", nameof(SensorOriginInfo.HardwareType), 90));
        _grid.Columns.Add(CreateTextColumn("Nome", nameof(SensorOriginInfo.Name), 150));
        _grid.Columns.Add(CreateTextColumn("Modelo", nameof(SensorOriginInfo.Model), 125));
        _grid.Columns.Add(CreateTextColumn("Fabricante", nameof(SensorOriginInfo.Manufacturer), 120));
        _grid.Columns.Add(CreateTextColumn("Driver", nameof(SensorOriginInfo.DriverProvider), 100));
        _grid.Columns.Add(CreateTextColumn("Versão do driver", nameof(SensorOriginInfo.DriverVersion), 110));
        _grid.Columns.Add(CreateTextColumn("Data do driver", nameof(SensorOriginInfo.DriverDate), 115));
        _grid.Columns.Add(CreateTextColumn("Firmware", nameof(SensorOriginInfo.FirmwareVersion), 100));
        _grid.Columns.Add(CreateTextColumn("PNP Device ID", nameof(SensorOriginInfo.PnpDeviceId), 180));
        _grid.Columns.Add(CreateTextColumn("Fonte", nameof(SensorOriginInfo.ProbableSensorSource), 160));
    }

    private static DataGridViewTextBoxColumn CreateTextColumn(string headerText, string propertyName, int fillWeight)
    {
        return new DataGridViewTextBoxColumn
        {
            HeaderText = headerText,
            DataPropertyName = propertyName,
            SortMode = DataGridViewColumnSortMode.NotSortable,
            AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
            FillWeight = fillWeight,
            MinimumWidth = 60,
            DefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = GridBackground,
                ForeColor = CellForeground,
                SelectionBackColor = Color.FromArgb(0, 120, 212),
                SelectionForeColor = Color.White
            }
        };
    }

    private void RefreshOrigins()
    {
        try
        {
            _refreshButton.Enabled = false;
            _refreshButton.Text = "Atualizando...";
            _statusLabel.ForeColor = MutedText;
            _statusLabel.Text = "Atualizando origens dos sensores...";

            List<SensorOriginInfo> origins = _originService.GetSensorOrigins();
            _origins = origins.OrderBy(origin => origin.HardwareType).ThenBy(origin => origin.Name).ToList();

            _grid.DataSource = null;
            _grid.DataSource = new BindingList<SensorOriginInfo>(_origins);

            UpdateSuccessStatus();
        }
        catch (Exception ex)
        {
            _origins = new List<SensorOriginInfo>();
            _grid.DataSource = null;
            _grid.Rows.Clear();
            _grid.Refresh();

            try
            {
                AppLogService.Error(ex, "Não foi possível carregar a origem dos sensores, drivers e firmware.");
            }
            catch
            {
            }

            _statusLabel.ForeColor = ErrorText;
            _statusLabel.Text = "Falha ao carregar as origens.";

            MessageBox.Show(
                this,
                "Não foi possível carregar a origem dos sensores, drivers e firmware.\n\nTente atualizar novamente.",
                "Origem dos sensores",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
        }
        finally
        {
            _refreshButton.Text = "Atualizar";
            _refreshButton.Enabled = true;
        }
    }

    private void UpdateSuccessStatus()
    {
        string updatedAt = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss", CultureInfo.CurrentCulture);

        if (_origins.Count == 0)
        {
            _statusLabel.ForeColor = MutedText;
            _statusLabel.Text = $"Nenhuma origem encontrada | Última atualização: {updatedAt}";
            return;
        }

        _statusLabel.ForeColor = MutedText;
        _statusLabel.Text = $"{_origins.Count} origens carregadas em {updatedAt}";
    }
}
