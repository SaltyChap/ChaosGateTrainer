using System.Runtime.InteropServices;

namespace ChaosGateTrainer;

public class MainForm : Form
{
    [DllImport("user32.dll")]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll")]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    private const int WM_HOTKEY = 0x0312;

    private readonly MemoryManager _memory;
    private readonly CheatManager _cheatManager;
    private readonly System.Windows.Forms.Timer _statusTimer;
    private readonly Dictionary<int, Cheat> _hotkeyMap = new();

    private Label _statusLabel = null!;
    private Panel _cheatsPanel = null!;
    private Button _attachButton = null!;
    private Button _rescanButton = null!;
    private readonly Dictionary<Cheat, CheckBox> _cheatCheckboxes = new();

    public MainForm()
    {
        _memory = new MemoryManager();
        _cheatManager = new CheatManager(_memory);

        _statusTimer = new System.Windows.Forms.Timer();
        _statusTimer.Interval = 1000;
        _statusTimer.Tick += StatusTimer_Tick;

        InitializeUI();
        RegisterHotkeys();

        _statusTimer.Start();
    }

    private void InitializeUI()
    {
        Text = "Chaos Gate: Daemonhunters Trainer";
        Size = new Size(450, 540);
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = Color.FromArgb(30, 30, 35);

        // Title
        var titleLabel = new Label
        {
            Text = "CHAOS GATE: DAEMONHUNTERS",
            Font = new Font("Segoe UI", 14, FontStyle.Bold),
            ForeColor = Color.FromArgb(200, 170, 80), // Gold color
            AutoSize = true,
            Location = new Point(20, 15)
        };
        Controls.Add(titleLabel);

        var subtitleLabel = new Label
        {
            Text = "TRAINER",
            Font = new Font("Segoe UI", 10, FontStyle.Regular),
            ForeColor = Color.FromArgb(150, 150, 150),
            AutoSize = true,
            Location = new Point(20, 42)
        };
        Controls.Add(subtitleLabel);

        // Status panel
        var statusPanel = new Panel
        {
            Location = new Point(20, 70),
            Size = new Size(395, 50),
            BackColor = Color.FromArgb(40, 40, 45)
        };
        Controls.Add(statusPanel);

        _statusLabel = new Label
        {
            Text = "Status: Not attached",
            Font = new Font("Segoe UI", 10),
            ForeColor = Color.FromArgb(220, 80, 80),
            Location = new Point(10, 8),
            AutoSize = true
        };
        statusPanel.Controls.Add(_statusLabel);

        _rescanButton = new Button
        {
            Text = "Re-scan",
            Font = new Font("Segoe UI", 9),
            Size = new Size(70, 30),
            Location = new Point(190, 10),
            BackColor = Color.FromArgb(60, 60, 65),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Enabled = false
        };
        _rescanButton.FlatAppearance.BorderColor = Color.FromArgb(80, 80, 85);
        _rescanButton.Click += RescanButton_Click;
        statusPanel.Controls.Add(_rescanButton);

        _attachButton = new Button
        {
            Text = "Attach to Game",
            Font = new Font("Segoe UI", 9),
            Size = new Size(120, 30),
            Location = new Point(265, 10),
            BackColor = Color.FromArgb(60, 60, 65),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat
        };
        _attachButton.FlatAppearance.BorderColor = Color.FromArgb(80, 80, 85);
        _attachButton.Click += AttachButton_Click;
        statusPanel.Controls.Add(_attachButton);

        // Cheats panel
        _cheatsPanel = new Panel
        {
            Location = new Point(20, 130),
            Size = new Size(395, 300),
            BackColor = Color.FromArgb(40, 40, 45),
            AutoScroll = true
        };
        Controls.Add(_cheatsPanel);

        var cheatsTitle = new Label
        {
            Text = "CHEATS",
            Font = new Font("Segoe UI", 9, FontStyle.Bold),
            ForeColor = Color.FromArgb(150, 150, 150),
            Location = new Point(10, 10),
            AutoSize = true
        };
        _cheatsPanel.Controls.Add(cheatsTitle);

        int yPos = 35;
        foreach (var cheat in _cheatManager.Cheats)
        {
            var cheatPanel = new Panel
            {
                Location = new Point(5, yPos),
                Size = new Size(365, 30),
                BackColor = Color.FromArgb(50, 50, 55)
            };

            var checkbox = new CheckBox
            {
                Text = $"{cheat.Name}",
                Font = new Font("Segoe UI", 9),
                ForeColor = Color.White,
                Location = new Point(10, 5),
                AutoSize = true,
                Enabled = false
            };
            checkbox.CheckedChanged += (s, e) =>
            {
                if (checkbox.Checked != cheat.IsEnabled)
                {
                    _cheatManager.ToggleCheat(cheat);
                    UpdateCheatStatus(cheat, checkbox);
                }
            };
            cheatPanel.Controls.Add(checkbox);
            _cheatCheckboxes[cheat] = checkbox;

            var hotkeyLabel = new Label
            {
                Text = $"[{cheat.Hotkey}]",
                Font = new Font("Segoe UI", 8),
                ForeColor = Color.FromArgb(120, 120, 120),
                Location = new Point(310, 7),
                AutoSize = true
            };
            cheatPanel.Controls.Add(hotkeyLabel);

            _cheatsPanel.Controls.Add(cheatPanel);
            yPos += 33;
        }

        // Instructions
        var instructionsLabel = new Label
        {
            Text = "1. Start the game and load a save\n2. Click 'Attach to Game'\n3. Use hotkeys or checkboxes to toggle cheats\n4. Click 'Re-scan' after entering combat to find combat-only cheats",
            Font = new Font("Segoe UI", 8),
            ForeColor = Color.FromArgb(120, 120, 120),
            Location = new Point(20, 440),
            AutoSize = true
        };
        Controls.Add(instructionsLabel);
    }

    private void RegisterHotkeys()
    {
        int id = 1;
        foreach (var cheat in _cheatManager.Cheats)
        {
            if (cheat.Hotkey != Keys.None)
            {
                RegisterHotKey(Handle, id, 0, (uint)cheat.Hotkey);
                _hotkeyMap[id] = cheat;
                id++;
            }
        }
    }

    private void UnregisterHotkeys()
    {
        foreach (var id in _hotkeyMap.Keys)
        {
            UnregisterHotKey(Handle, id);
        }
    }

    protected override void WndProc(ref Message m)
    {
        if (m.Msg == WM_HOTKEY)
        {
            int id = m.WParam.ToInt32();
            if (_hotkeyMap.TryGetValue(id, out var cheat) && _memory.IsAttached)
            {
                _cheatManager.ToggleCheat(cheat);
                if (_cheatCheckboxes.TryGetValue(cheat, out var checkbox))
                {
                    checkbox.Checked = cheat.IsEnabled;
                    UpdateCheatStatus(cheat, checkbox);
                }
            }
        }
        base.WndProc(ref m);
    }

    private void RescanButton_Click(object? sender, EventArgs e)
    {
        if (!_memory.IsAttached) return;

        // Disable cheats that were enabled before rescanning
        _cheatManager.DisableAllCheats();
        foreach (var checkbox in _cheatCheckboxes.Values)
        {
            checkbox.Checked = false;
        }

        // Rescan for patterns
        _cheatManager.ScanForAddresses();
        UpdateStatus();

        // Update checkboxes
        var newlyFound = new List<string>();
        var stillNotFound = new List<string>();
        foreach (var cheat in _cheatManager.Cheats)
        {
            if (_cheatCheckboxes.TryGetValue(cheat, out var checkbox))
            {
                bool wasEnabled = checkbox.Enabled;
                checkbox.Enabled = cheat.Address.HasValue;
                checkbox.ForeColor = cheat.Address.HasValue ? Color.White : Color.FromArgb(100, 100, 100);

                if (cheat.Address.HasValue && !wasEnabled)
                    newlyFound.Add($"{cheat.Name} [{cheat.Hotkey}]");
                else if (!cheat.Address.HasValue)
                    stillNotFound.Add($"{cheat.Name} [{cheat.Hotkey}]");
            }
        }

        string message = "";
        if (newlyFound.Count > 0)
            message += $"Newly found:\n• {string.Join("\n• ", newlyFound)}\n\n";
        if (stillNotFound.Count > 0)
            message += $"Still not found:\n• {string.Join("\n• ", stillNotFound)}";

        if (!string.IsNullOrEmpty(message))
        {
            MessageBox.Show(message.Trim(), "Re-scan Results", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        else
        {
            MessageBox.Show("All patterns found!", "Re-scan Results", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }

    private void AttachButton_Click(object? sender, EventArgs e)
    {
        if (_memory.IsAttached)
        {
            _cheatManager.DisableAllCheats();
            _memory.Detach();
            UpdateStatus();
            return;
        }

        if (_memory.Attach())
        {
            _cheatManager.ScanForAddresses();
            UpdateStatus();

            // Enable checkboxes for found cheats and build diagnostic info
            var notFound = new List<string>();
            foreach (var cheat in _cheatManager.Cheats)
            {
                if (_cheatCheckboxes.TryGetValue(cheat, out var checkbox))
                {
                    checkbox.Enabled = cheat.Address.HasValue;
                    if (!cheat.Address.HasValue)
                    {
                        checkbox.ForeColor = Color.FromArgb(100, 100, 100);
                        notFound.Add($"{cheat.Name} [{cheat.Hotkey}]");
                    }
                }
            }

            // Show which patterns weren't found
            if (notFound.Count > 0)
            {
                MessageBox.Show(
                    $"The following cheats were not found (pattern may need updating or code not loaded yet):\n\n• {string.Join("\n• ", notFound)}\n\nTry re-attaching during combat if these are combat-related cheats.",
                    "Some Patterns Not Found",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
        }
        else
        {
            MessageBox.Show("Could not find Chaos Gate: Daemonhunters.\n\nMake sure the game is running and try again.",
                "Attach Failed", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private void UpdateStatus()
    {
        if (_memory.IsAttached)
        {
            int foundCount = _cheatManager.Cheats.Count(c => c.Address.HasValue);
            _statusLabel.Text = $"Status: Attached ({foundCount}/{_cheatManager.Cheats.Count} cheats found)";
            _statusLabel.ForeColor = Color.FromArgb(80, 200, 80);
            _attachButton.Text = "Detach";
            _rescanButton.Enabled = true;
        }
        else
        {
            _statusLabel.Text = "Status: Not attached";
            _statusLabel.ForeColor = Color.FromArgb(220, 80, 80);
            _attachButton.Text = "Attach to Game";
            _rescanButton.Enabled = false;

            foreach (var checkbox in _cheatCheckboxes.Values)
            {
                checkbox.Enabled = false;
                checkbox.Checked = false;
                checkbox.ForeColor = Color.White;
            }
        }
    }

    private void UpdateCheatStatus(Cheat cheat, CheckBox checkbox)
    {
        checkbox.ForeColor = cheat.IsEnabled
            ? Color.FromArgb(80, 200, 80)
            : Color.White;
    }

    private void StatusTimer_Tick(object? sender, EventArgs e)
    {
        // Check if game is still running
        if (_memory.IsAttached)
        {
            // Recheck attachment
            if (!_memory.IsAttached)
            {
                UpdateStatus();
            }
        }
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        _statusTimer.Stop();
        UnregisterHotkeys();
        _cheatManager.DisableAllCheats();
        _memory.Dispose();
        base.OnFormClosing(e);
    }
}
