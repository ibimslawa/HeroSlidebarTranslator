# HeroSlidebarTranslator

Ever wondered how to get use out of that slidebar on your Guitar Hero guitar controller?
Well, this tool is the solution.
It translates your taps and slides on the slidebar to keyboard button presses.
I also incorporated other useful features, so you turn off your controller via this app, check the battery status and get notified when the battery is running low.

## Features
- Translates slidebar taps (controller axis movements) to keyboard presses
- Individual settings for all four player slots
- Different activation modes: Always, Game running, Game in foreground
- Set custom values for the colors of your slidebar
- Displaying / preview of your actions on the slidebar
- Change the refresh rate (poll rate) for each translator
- Power off your controllers from the app or the taskbar
- See the battery status and get notified on low battery
- Enable auto-start, so the translator is always ready
- HST provides English and Deutsch as an interface language.

# Setup the "translator"

All the setup is handled in one single window, which can also be opened from the taskbar tray when the the app has started via auto-start.
### 1. Set the axis points
Set the values for each of the five colors to match your slidebar.
Simply tap a color on your slidebar and press [Capture] to read the corresponding value.
Or adjust the value manually. Do the same for the idle value (for when no finger touches the slidebar) and idle value dead-zone (safe value area around the idle value).
### 2. Choose the keyboard keys to match settings in "Clone Hero"
Now click on the textboxes for each color and press the corresponding key on your keyboard to match the settings in Clone Hero. (In future it might be possible to "sync" these settings to CH.)
### 3. Select an "activation mode"
With the "activation mode" you can control when the translators will become active:
Always? Only when CH is running? Or rather only when CH is running in the foreground?
Prevent unwanted keyboard inputs with this feature.
### 4. Enable your translator and hop into game
Now enable the translators for the players that are set up and using a guitar with a slidebar.
Enjoy playing CH with extra fun now!

# More settings and functions
### Battery status display and low battery warnings
Easily view the battery status of all connected controllers.
Enable "Notify on low battery" to receive notifications when the batteries of your controllers are running low.
### Auto-start with user log-in
The app can be set to start automatically with your Windows user login.
I advise to not use the "activation mode" _Always_ when auto-start is enabled.

# Current development status
### Problems, limitations, known bugs
- Taskbar context menu functionality not fully implemented yet
- Low battery notifications might not work correctly
### Possible future implementations
- Reading keyboard assignments from Clone Hero's settings file ("import from CH" or "sync to CH")

