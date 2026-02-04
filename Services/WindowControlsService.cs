namespace WinFormTest.Services;

public class WindowControlsService
{
    private readonly Form form;
    private readonly Panel panelTopRibbon;
    private readonly Button btnClose;
    private readonly Button btnMinimize;

    public WindowControlsService(Form form, Panel panelTopRibbon, Button btnClose, Button btnMinimize)
    {
        this.form = form;
        this.panelTopRibbon = panelTopRibbon;
        this.btnClose = btnClose;
        this.btnMinimize = btnMinimize;
        
        SetupWindowControls();
    }

    private void SetupWindowControls()
    {
        // Enable dragging from top ribbon panel (acts like native title bar)
        panelTopRibbon.MouseDown += PanelTopRibbon_MouseDown;
        
        // Ensure buttons stay on top and are clickable
        btnClose.BringToFront();
        btnMinimize.BringToFront();
        
        // Wire up button event handlers
        btnClose.Click += btnClose_Click;
        btnClose.MouseEnter += btnClose_MouseEnter;
        btnClose.MouseLeave += btnClose_MouseLeave;
        
        btnMinimize.Click += btnMinimize_Click;
        btnMinimize.MouseEnter += btnMinimize_MouseEnter;
        btnMinimize.MouseLeave += btnMinimize_MouseLeave;
    }

    private void PanelTopRibbon_MouseDown(object? sender, MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left)
        {
            // Don't drag if clicking directly on buttons
            if (sender == btnClose || sender == btnMinimize)
                return;
            
            // Convert to form coordinates to check button positions
            Control? sourceControl = sender as Control;
            if (sourceControl != null)
            {
                Point formPoint = sourceControl.PointToScreen(e.Location);
                formPoint = form.PointToClient(formPoint);
                
                // Allow dragging everywhere except button area (x >= 1140)
                if (formPoint.X < 1140)
                {
                    WindowsApiHelper.ReleaseCapture();
                    WindowsApiHelper.SendMessage(form.Handle, WindowsApiHelper.WM_NCLBUTTONDOWN, 
                        WindowsApiHelper.HT_CAPTION, 0);
                }
            }
        }
    }

    private void btnClose_Click(object? sender, EventArgs e)
    {
        // Remove focus from button before closing
        form.Focus();
        form.Close();
    }

    private void btnClose_MouseEnter(object? sender, EventArgs e)
    {
        btnClose.ForeColor = Color.White;
        btnClose.BackColor = Color.FromArgb(232, 17, 35);
    }

    private void btnClose_MouseLeave(object? sender, EventArgs e)
    {
        btnClose.ForeColor = Color.FromArgb(100, 100, 100);
        btnClose.BackColor = Color.Transparent;
    }

    private void btnMinimize_Click(object? sender, EventArgs e)
    {
        // Remove focus from button before minimizing
        form.Focus();
        form.WindowState = FormWindowState.Minimized;
    }

    private void btnMinimize_MouseEnter(object? sender, EventArgs e)
    {
        btnMinimize.ForeColor = Color.FromArgb(45, 45, 48);
        btnMinimize.BackColor = Color.FromArgb(240, 240, 240);
    }

    private void btnMinimize_MouseLeave(object? sender, EventArgs e)
    {
        btnMinimize.ForeColor = Color.FromArgb(100, 100, 100);
        btnMinimize.BackColor = Color.Transparent;
    }
}
