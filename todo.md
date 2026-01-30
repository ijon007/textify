# TODO

## In Progress
- **Settings Page** (panelSettingsPage) - general user settings, change push to talk shortcut etc (will have to think)
- 

## Pending
### Pages to Build

### Other
- **Add whisper voice model**
- **Add ai correction and punctuation**
- **Speech bubble** (speech overlay) - fix the animation and the diff states (+ add an idle state)
- **Add poppins font** 

## Completed
- **Style Page** (panelStylePage) - user will be able to configure how the speech should be transcribed: formal, informal etc.
- **Snippets Page** (panelSnippetsPage) - will be the place where the user can add kind of shortcut words for longer things: e.g 'Twitter' - adds twitter link to the message
- **Dictionary Page** (panelDictionaryPage) - will be the place where the user can save custom words that the ai doesnt understand
- **Home page stats**
- **Login Form** (Form1) - Complete with authentication
- **Home Page Base** (panelHomePage) - Complete with speech history, stats, and welcome message
---

## Notes
- Dictionary, Snippets, Style, and Settings pages currently only have placeholder content created in `CreatePlaceholderPage()` method
- All pages are Panel controls docked to fill `panelMain` in DashboardForm
- Navigation between pages is already implemented via sidebar navigation
