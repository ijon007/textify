# TODO

## In Progress
- 

## Pending

### Other

## Completed
- **Code formatting** 
- **Add snippets and dictionary to model** - inject them in the model in some way for each ones feature. the model should detect if one of the words said is part of those in the db and edit the transcription accordingly before showing it to the user in the input field.
- **Speech bubble** (speech overlay) - fix the animation and the diff states (+ add an idle state)
- **Add ai correction and punctuation**
- **Add whisper voice model**
- **Settings Page** (panelSettingsPage) - general user settings, change push to talk shortcut etc (will have to think)
- **Style Page** (panelStylePage) - user will be able to configure how the speech should be transcribed: formal, informal etc.
- **Snippets Page** (panelSnippetsPage) - will be the place where the user can add kind of shortcut words for longer things: e.g 'Twitter' - adds twitter link to the message
- **Dictionary Page** (panelDictionaryPage) - will be the place where the user can save custom words that the ai doesnt understand
- **Home page stats**
- **Login Form** (Form1) - Complete with authentication
- **Home Page Base** (panelHomePage) - Complete with speech history, stats, and welcome message
---

## Notes
- **Add poppins font** 
- Dictionary, Snippets, Style, and Settings pages currently only have placeholder content created in `CreatePlaceholderPage()` method
- All pages are Panel controls docked to fill `panelMain` in DashboardForm
- Navigation between pages is already implemented via sidebar navigation
