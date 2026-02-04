namespace WinFormTest.Services;

public class LoadingUIService
{
    private Panel? loadingPanel;
    private Label? loadingLabel;
    private readonly Form parentForm;

    public LoadingUIService(Form parentForm)
    {
        this.parentForm = parentForm;
    }

    public void Initialize()
    {
        // Create loading panel
        loadingPanel = new Panel();
        loadingPanel.Dock = DockStyle.Fill;
        loadingPanel.BackColor = Color.FromArgb(45, 45, 48);
        loadingPanel.Visible = true;
        loadingPanel.BringToFront();

        // Create loading label
        loadingLabel = new Label();
        loadingLabel.Text = "Loading speech recognition model...\nPlease wait...";
        loadingLabel.Font = new Font("Poppins", 12F, FontStyle.Regular, GraphicsUnit.Point);
        loadingLabel.ForeColor = Color.White;
        loadingLabel.AutoSize = false;
        loadingLabel.TextAlign = ContentAlignment.MiddleCenter;
        loadingLabel.Dock = DockStyle.Fill;
        loadingLabel.Padding = new Padding(20);

        loadingPanel.Controls.Add(loadingLabel);
        parentForm.Controls.Add(loadingPanel);
        loadingPanel.BringToFront();
    }

    public void UpdateText(string text)
    {
        if (loadingLabel != null && loadingLabel.InvokeRequired)
        {
            loadingLabel.Invoke(new Action(() => loadingLabel.Text = text));
        }
        else if (loadingLabel != null)
        {
            loadingLabel.Text = text;
        }
    }

    public void Hide()
    {
        if (loadingPanel != null)
        {
            loadingPanel.Visible = false;
            loadingPanel.Dispose();
            loadingPanel = null;
            loadingLabel = null;
        }
    }

    public bool IsVisible => loadingPanel != null && loadingPanel.Visible;
}
