PROJECT SERUM - REFERENCE MAIN MENU

INSTALL
1. Open your Main menu scene and leave Play mode.
2. Import SerumMainMenu.unitypackage using Assets > Import Package > Custom Package.
3. Allow scripts to compile. The UI installs automatically if Main menu is open.
   If needed, choose Serum > Main Menu > Install Reference UI.

The installer saves a copy of the currently open scene before adding UI:
Assets/SerumMenu/Backup/Main menu.before-ui.unity
It then adds and saves an editable Main Menu UI canvas in your current scene.
Scene environment, lighting and camera settings are not edited.

NEW GAME
In File > Build Profiles > Scene List, enable Main menu followed immediately by
your intended gameplay scene. New Game loads the following enabled scene.
Alternatively, set New Game Build Index on Main Menu UI to an explicit index.
The default -1 means automatic next-scene selection.
An informative panel appears when the destination is not available.

CONTROLS
Mouse hover/click and keyboard arrows/submit select and activate menu items.
Continue is intentionally disabled; no save-game system was added.
Options provides session-only master volume and fullscreen controls.
Exit quits a built game or stops Play mode in the Unity Editor.

EDITING
The canvas uses a 1920 x 1080 reference resolution and scales to the viewport.
All menu objects are editable under Main Menu UI in the Hierarchy.
Georgia is copied from this computer's existing Windows font installation for
local use. Check the font's license before redistributing the source font.

VALIDATION
Scripts compile against this project's Unity 6000.3.11f1, TextMesh Pro,
Unity UI and Input System assemblies. Visual and Play mode testing remain
pending because project write access was not granted during preparation.
