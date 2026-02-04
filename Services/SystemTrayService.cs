namespace WinFormTest.Services;

public class SystemTrayService
{
    private readonly Form form;
    private readonly DatabaseService? databaseService;
    private readonly string username;
    private NotifyIcon? notifyIcon;
    private CheckBox? chkMinimizeToTray;

    public SystemTrayService(Form form, DatabaseService? databaseService, string username)
    {
        this.form = form;
        this.databaseService = databaseService;
        this.username = username;
    }

    public void Initialize()
    {
        // Create NotifyIcon
        notifyIcon = new NotifyIcon();
        notifyIcon.Text = "Textify";
        
        // Set icon from assets folder
        string iconPath = Path.Combine(Application.StartupPath, "assets", "cp-black.ico");
        if (File.Exists(iconPath))
        {
            notifyIcon.Icon = new Icon(iconPath);
        }
        else
        {
            // Use default icon if custom icon not found
            notifyIcon.Icon = SystemIcons.Application;
        }
        
        // Add context menu with Show option
        ContextMenuStrip contextMenu = new ContextMenuStrip();
        ToolStripMenuItem showMenuItem = new ToolStripMenuItem("Show");
        showMenuItem.Click += (s, e) => ShowFromTray();
        ToolStripMenuItem exitMenuItem = new ToolStripMenuItem("Exit");
        exitMenuItem.Click += (s, e) => form.Close();
        
        contextMenu.Items.Add(showMenuItem);
        contextMenu.Items.Add(new ToolStripSeparator());
        contextMenu.Items.Add(exitMenuItem);
        notifyIcon.ContextMenuStrip = contextMenu;
        
        // Double-click to restore
        notifyIcon.DoubleClick += (s, e) => ShowFromTray();
        
        notifyIcon.Visible = false; // Start hidden, will show when minimized to tray
    }

    public void SetMinimizeToTrayCheckbox(CheckBox checkbox)
    {
        chkMinimizeToTray = checkbox;
    }

    public void HandleFormResize()
    {
        if (form.WindowState == FormWindowState.Minimized)
        {
            // Check if minimize to tray is enabled
            bool minimizeToTray = false;
            if (chkMinimizeToTray != null && chkMinimizeToTray.Checked)
            {
                minimizeToTray = true;
            }
            else if (databaseService != null)
            {
                // Fallback: check database if checkbox not loaded yet
                try
                {
                    var (_, minimizeToTrayPref) = databaseService.GetUserApplicationPreferences(username);
                    minimizeToTray = minimizeToTrayPref;
                }
                catch
                {
                    // If we can't read from database, default to false
                }
            }
            
            if (minimizeToTray && notifyIcon != null)
            {
                // Hide form and show tray icon
                form.Hide();
                notifyIcon.Visible = true;
                notifyIcon.ShowBalloonTip(1000, "Textify", "Application minimized to system tray", ToolTipIcon.Info);
            }
        }
    }

    public void ShowFromTray()
    {
        if (notifyIcon != null)
        {
            notifyIcon.Visible = false;
        }
        
        form.Show();
        form.WindowState = FormWindowState.Normal;
        form.Activate();
        form.BringToFront();
    }

    public void Dispose()
    {
        if (notifyIcon != null)
        {
            notifyIcon.Visible = false;
            notifyIcon.Dispose();
            notifyIcon = null;
        }
    }
}
