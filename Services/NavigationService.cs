namespace WinFormTest.Services;

public class NavigationService
{
    private readonly Panel panelHomePage;
    private readonly Panel panelDictionaryPage;
    private readonly Panel panelSnippetsPage;
    private readonly Panel panelStylePage;
    private readonly Panel panelSettingsPage;
    
    private readonly Label lblNavHome;
    private readonly Label lblNavDictionary;
    private readonly Label lblNavSnippets;
    private readonly Label lblNavStyle;
    private readonly Label lblNavSettings;
    
    private Panel? activePagePanel;
    private Label? activeNavItem;

    public NavigationService(
        Panel panelHomePage,
        Panel panelDictionaryPage,
        Panel panelSnippetsPage,
        Panel panelStylePage,
        Panel panelSettingsPage,
        Label lblNavHome,
        Label lblNavDictionary,
        Label lblNavSnippets,
        Label lblNavStyle,
        Label lblNavSettings)
    {
        this.panelHomePage = panelHomePage;
        this.panelDictionaryPage = panelDictionaryPage;
        this.panelSnippetsPage = panelSnippetsPage;
        this.panelStylePage = panelStylePage;
        this.panelSettingsPage = panelSettingsPage;
        
        this.lblNavHome = lblNavHome;
        this.lblNavDictionary = lblNavDictionary;
        this.lblNavSnippets = lblNavSnippets;
        this.lblNavStyle = lblNavStyle;
        this.lblNavSettings = lblNavSettings;
        
        // Wire up event handlers
        lblNavHome.Click += navItem_Click;
        lblNavHome.MouseEnter += navItem_MouseEnter;
        lblNavHome.MouseLeave += navItem_MouseLeave;
        
        lblNavDictionary.Click += navItem_Click;
        lblNavDictionary.MouseEnter += navItem_MouseEnter;
        lblNavDictionary.MouseLeave += navItem_MouseLeave;
        
        lblNavSnippets.Click += navItem_Click;
        lblNavSnippets.MouseEnter += navItem_MouseEnter;
        lblNavSnippets.MouseLeave += navItem_MouseLeave;
        
        lblNavStyle.Click += navItem_Click;
        lblNavStyle.MouseEnter += navItem_MouseEnter;
        lblNavStyle.MouseLeave += navItem_MouseLeave;
        
        lblNavSettings.Click += navItem_Click;
        lblNavSettings.MouseEnter += navItem_MouseEnter;
        lblNavSettings.MouseLeave += navItem_MouseLeave;
    }

    public void SwitchPage(Panel targetPage, Label activeNavLabel)
    {
        // Hide all page panels
        panelHomePage.Visible = false;
        panelDictionaryPage.Visible = false;
        panelSnippetsPage.Visible = false;
        panelStylePage.Visible = false;
        panelSettingsPage.Visible = false;
        
        // Show target page
        targetPage.Visible = true;
        activePagePanel = targetPage;
        
        // Update active navigation item
        SetActiveNavItem(activeNavLabel);
    }

    public void SetActiveNavItem(Label navItem)
    {
        // Reset all navigation items to inactive state
        lblNavHome.ForeColor = Color.FromArgb(100, 100, 100);
        lblNavHome.BackColor = Color.Transparent;
        lblNavDictionary.ForeColor = Color.FromArgb(100, 100, 100);
        lblNavDictionary.BackColor = Color.Transparent;
        lblNavSnippets.ForeColor = Color.FromArgb(100, 100, 100);
        lblNavSnippets.BackColor = Color.Transparent;
        lblNavStyle.ForeColor = Color.FromArgb(100, 100, 100);
        lblNavStyle.BackColor = Color.Transparent;
        lblNavSettings.ForeColor = Color.FromArgb(100, 100, 100);
        lblNavSettings.BackColor = Color.Transparent;
        
        // Set active navigation item styling
        navItem.ForeColor = Color.Black;
        navItem.BackColor = Color.FromArgb(245, 245, 245);
        activeNavItem = navItem;
    }

    public void SetInitialPage(Panel initialPage, Label initialNavItem)
    {
        SwitchPage(initialPage, initialNavItem);
    }

    private void navItem_Click(object? sender, EventArgs e)
    {
        if (sender is Label label)
        {
            if (label == lblNavHome)
            {
                SwitchPage(panelHomePage, lblNavHome);
            }
            else if (label == lblNavDictionary)
            {
                SwitchPage(panelDictionaryPage, lblNavDictionary);
            }
            else if (label == lblNavSnippets)
            {
                SwitchPage(panelSnippetsPage, lblNavSnippets);
            }
            else if (label == lblNavStyle)
            {
                SwitchPage(panelStylePage, lblNavStyle);
            }
            else if (label == lblNavSettings)
            {
                SwitchPage(panelSettingsPage, lblNavSettings);
            }
        }
    }

    private void navItem_MouseEnter(object? sender, EventArgs e)
    {
        if (sender is Label label)
        {
            label.BackColor = Color.FromArgb(245, 245, 245);
            label.ForeColor = Color.Black;
        }
    }

    private void navItem_MouseLeave(object? sender, EventArgs e)
    {
        if (sender is Label label)
        {
            // Only restore gray if it's not the active navigation item
            if (activeNavItem == null || label != activeNavItem)
            {
                label.BackColor = Color.Transparent;
                label.ForeColor = Color.FromArgb(100, 100, 100);
            }
        }
    }
}
