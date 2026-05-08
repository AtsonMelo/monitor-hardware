using System.ComponentModel;
using System.Windows.Forms;

class HardwareSelectionForm : Form
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

    private readonly HardwareSelectionService _selectionService;
    private readonly Icon? _ownedIcon;
    private readonly DataGridView _grid;
    private readonly Button _refreshButton;
    private readonly Button _selectAllButton;
    private readonly Button _clearAllButton;
    private readonly Button _applyButton;
    private readonly Button _closeButton;
    private readonly Label _statusLabel;
    private BindingList<HardwareSelectionItem> _items = new();

    public HardwareSelectionForm(HardwareSelectionService selectionService, Icon? windowIcon = null)
    {
        _selectionService = selectionService ?? throw new ArgumentNullException(nameof(selectionService));

        Text = "Selecionar hardwares monitorados";
        AutoScaleMode = AutoScaleMode.Dpi;
        StartPosition = FormStartPosition.CenterParent;
        MinimumSize = new Size(920, 560);
        Size = new Size(1120, 700);
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

        Label titleLabel = new Label
        {
            Text = "Selecionar hardwares monitorados",
            Dock = DockStyle.Fill,
            ForeColor = HeaderForeground,
            Font = new Font("Segoe UI", 15, FontStyle.Bold, GraphicsUnit.Point),
            TextAlign = ContentAlignment.MiddleLeft,
            AutoEllipsis = true
        };

        FlowLayoutPanel actions = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            BackColor = WindowBackground,
            Margin = new Padding(0),
            Padding = new Padding(0)
        };

        _refreshButton = CreateActionButton("Atualizar lista");
        _refreshButton.Click += (_, _) => RefreshHardwareList();
        _selectAllButton = CreateActionButton("Marcar todos");
        _selectAllButton.Click += (_, _) => SetAllSelections(true);
        _clearAllButton = CreateActionButton("Desmarcar todos");
        _clearAllButton.Click += (_, _) => SetAllSelections(false);
        _applyButton = CreateActionButton("Aplicar seleção");
        _applyButton.Click += (_, _) => ApplySelection();
        _closeButton = CreateActionButton("Fechar");
        _closeButton.Click += (_, _) => Close();

        actions.Controls.Add(_refreshButton);
        actions.Controls.Add(_selectAllButton);
        actions.Controls.Add(_clearAllButton);
        actions.Controls.Add(_applyButton);
        actions.Controls.Add(_closeButton);

        header.Controls.Add(titleLabel, 0, 0);
        header.Controls.Add(actions, 1, 0);

        _grid = new DataGridView
        {
            Dock = DockStyle.Fill,
            ReadOnly = false,
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

        _grid.CurrentCellDirtyStateChanged += (_, _) =>
        {
            if (_grid.IsCurrentCellDirty)
            {
                _grid.CommitEdit(DataGridViewDataErrorContexts.Commit);
            }
        };
        _grid.CellValueChanged += (_, _) => UpdateStatus();
        _grid.DataBindingComplete += (_, _) => UpdateStatus();

        _grid.Columns.Add(new DataGridViewCheckBoxColumn
        {
            HeaderText = "Selecionar",
            DataPropertyName = nameof(HardwareSelectionItem.IsSelected),
            SortMode = DataGridViewColumnSortMode.NotSortable,
            Width = 80,
            AutoSizeMode = DataGridViewAutoSizeColumnMode.None
        });
        _grid.Columns.Add(CreateTextColumn("Tipo", nameof(HardwareSelectionItem.HardwareType), 110));
        _grid.Columns.Add(CreateTextColumn("Nome", nameof(HardwareSelectionItem.HardwareName), 200));
        _grid.Columns.Add(CreateTextColumn("Identificador", nameof(HardwareSelectionItem.DisplayIdentifier), 220));

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

        Shown += (_, _) => RefreshHardwareList();
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

    private static Button CreateActionButton(string text)
    {
        Button button = new Button
        {
            Text = text,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Height = 40,
            MinimumSize = new Size(136, 40),
            Margin = new Padding(0, 0, 8, 0),
            Padding = new Padding(10, 0, 10, 0),
            FlatStyle = FlatStyle.Flat,
            ForeColor = Color.White,
            BackColor = ButtonBackground,
            UseVisualStyleBackColor = false,
            TextAlign = ContentAlignment.MiddleCenter
        };
        button.FlatAppearance.BorderColor = ButtonBorder;
        button.FlatAppearance.MouseOverBackColor = Color.FromArgb(44, 50, 57);
        button.FlatAppearance.MouseDownBackColor = Color.FromArgb(30, 34, 39);
        return button;
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

    private void RefreshHardwareList()
    {
        try
        {
            _refreshButton.Enabled = false;
            _refreshButton.Text = "Atualizando...";
            _statusLabel.Text = "Atualizando hardwares detectados...";

            List<HardwareSelectionItem> items = _selectionService.GetDetectedHardware()
                .OrderBy(item => item.HardwareType)
                .ThenBy(item => item.HardwareName)
                .ToList();

            _items = new BindingList<HardwareSelectionItem>(items);
            _grid.DataSource = null;
            _grid.DataSource = _items;
            UpdateStatus();
        }
        catch (Exception ex)
        {
            AppLogService.Error(ex, "Não foi possível carregar os hardwares detectados.");
            _items = new BindingList<HardwareSelectionItem>();
            _grid.DataSource = null;
            _statusLabel.Text = "Falha ao carregar hardwares.";
            MessageBox.Show(this, "Não foi possível carregar os hardwares detectados.", "Selecionar hardwares monitorados", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
        finally
        {
            _refreshButton.Text = "Atualizar lista";
            _refreshButton.Enabled = true;
        }
    }

    private void SetAllSelections(bool selected)
    {
        foreach (HardwareSelectionItem item in _items)
        {
            item.IsSelected = selected;
        }

        _grid.Refresh();
        UpdateStatus();
    }

    private void ApplySelection()
    {
        _selectionService.SetSelectedHardware(_items);
        UpdateStatus();
        MessageBox.Show(this, "Seleção aplicada em memória.", "Selecionar hardwares monitorados", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private void UpdateStatus()
    {
        int totalDetected = _items.Count;
        int totalSelected = _items.Count(item => item.IsSelected);
        _statusLabel.Text = $"Total detectado: {totalDetected} | Total selecionado: {totalSelected} | Seleção ativa em memória: {_selectionService.GetSelectedCount()}";
    }
}
