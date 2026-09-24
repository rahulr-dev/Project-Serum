using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Serum.MenuUI.Editor
{
    public static class SerumMenuInstaller
    {
        const string Folder = "Assets/SerumMenu";
        static TMP_FontAsset font;
        static Color Ivory = new Color(.77f, .76f, .71f, 1);
        static Color Ember = new Color(1f, .43f, .13f, 1);

        [InitializeOnLoadMethod]
        static void ScheduleInstall()
        {
            EditorApplication.delayCall += () =>
            {
                if (!File.Exists(Folder + "/Installed.txt") && !EditorApplication.isPlayingOrWillChangePlaymode
                    && SceneManager.GetActiveScene().name == "Main menu") Install();
            };
        }

        [MenuItem("Serum/Main Menu/Install Reference UI")]
        public static void Install()
        {
            var scene = SceneManager.GetActiveScene();
            if (EditorApplication.isPlayingOrWillChangePlaymode || scene.name != "Main menu") return;
            if (GameObject.Find("Main Menu UI") != null) return;
            Directory.CreateDirectory(Folder + "/Backup");
            EditorSceneManager.SaveScene(scene, Folder + "/Backup/Main menu.before-ui.unity", true);
            font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(Folder + "/MenuSerif.asset");
            if (font == null)
            {
                var source = AssetDatabase.LoadAssetAtPath<Font>(Folder + "/Georgia.ttf");
                if (source == null) { Debug.LogError("Menu UI: Georgia font has not imported yet."); return; }
                font = TMP_FontAsset.CreateFontAsset(source, 90, 9, UnityEngine.TextCore.LowLevel.GlyphRenderMode.SDFAA, 1024, 1024);
                font.name = "Menu Serif";
                AssetDatabase.CreateAsset(font, Folder + "/MenuSerif.asset");
                AssetDatabase.AddObjectToAsset(font.material, font);
                foreach (var atlas in font.atlasTextures) AssetDatabase.AddObjectToAsset(atlas, font);
                font.TryAddCharacters("ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789 .,:;!?'-/()");
                EditorUtility.SetDirty(font);
            }

            var root = new GameObject("Main Menu UI", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(SerumMainMenu));
            Undo.RegisterCreatedObjectUndo(root, "Create reference main menu UI");
            root.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            root.GetComponent<Canvas>().sortingOrder = 100;
            var scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = .5f;
            var menu = root.GetComponent<SerumMainMenu>();

            // Only screen-space graphics: the scene, lighting and camera remain intact.
            var shade = Image("Left readability gradient", root.transform, Color.white);
            Stretch(shade.rectTransform);
            shade.sprite = GradientSprite();
            shade.raycastTarget = false;

            var group = Rect("Menu", root.transform, new Vector2(440, 360));
            group.anchorMin = group.anchorMax = new Vector2(.085f, .532f);
            group.pivot = new Vector2(0, 1);
            group.anchoredPosition = Vector2.zero;
            menu.menuGroup = group.gameObject;

            var disabled = Button("Continue", group, "CONTINUE", new Vector2(0, 0), 38);
            disabled.interactable = false;
            disabled.GetComponentInChildren<TMP_Text>().color = new Color(.53f, .53f, .51f, .65f);
            var newGame = Button("New Game", group, "NEW GAME", new Vector2(0, -84), 38);
            var options = Button("Options", group, "OPTIONS", new Vector2(0, -168), 38);
            var exit = Button("Exit", group, "EXIT", new Vector2(0, -252), 38);
            menu.newGameButton = newGame;
            foreach (var b in new[] { newGame, options, exit })
            {
                var item = b.gameObject.AddComponent<SerumMenuItem>();
                item.menu = menu;
                item.label = b.GetComponentInChildren<TMP_Text>();
            }
            Nav(newGame, exit, options); Nav(options, newGame, exit); Nav(exit, options, newGame);
            UnityEventTools.AddPersistentListener(newGame.onClick, menu.NewGame);
            UnityEventTools.AddPersistentListener(options.onClick, menu.OpenOptions);
            UnityEventTools.AddPersistentListener(exit.onClick, menu.Exit);

            var selection = Rect("Ember selection ornament", group, new Vector2(22, 22));
            selection.anchorMin = selection.anchorMax = new Vector2(0, 1);
            selection.anchoredPosition = new Vector2(-48, -84);
            var diamond = Image("Diamond rim", selection, Ember);
            diamond.rectTransform.sizeDelta = new Vector2(13, 13);
            diamond.rectTransform.localRotation = Quaternion.Euler(0, 0, 45);
            var center = Image("Dark inset", diamond.transform, new Color(.11f, .04f, .02f));
            center.rectTransform.sizeDelta = new Vector2(9, 9);
            var spark = Image("Gold center", center.transform, new Color(1, .83f, .48f));
            spark.rectTransform.sizeDelta = new Vector2(4, 4);
            var trail = Image("Fine orange trail", selection, new Color(1, .38f, .09f, .55f));
            trail.rectTransform.sizeDelta = new Vector2(110, 1);
            trail.rectTransform.anchoredPosition = new Vector2(-64, 0);
            menu.selection = selection;
            var line = Image("Selected underline", group, new Color(1f, .42f, .12f, .55f));
            line.rectTransform.anchorMin = line.rectTransform.anchorMax = new Vector2(0, 1);
            line.rectTransform.pivot = new Vector2(0, .5f);
            line.rectTransform.sizeDelta = new Vector2(270, 1);
            menu.underline = line.rectTransform;

            var panel = Panel("Options Panel", root.transform);
            menu.optionsPanel = panel.gameObject;
            Label("Title", panel, "OPTIONS", new Vector2(44, -52), new Vector2(510, 55), 36);
            Label("Volume label", panel, "MASTER VOLUME", new Vector2(44, -138), new Vector2(460, 40), 22);
            var sliderRect = Rect("Master Volume", panel, new Vector2(480, 36));
            sliderRect.anchorMin = sliderRect.anchorMax = new Vector2(0, 1);
            sliderRect.pivot = new Vector2(0, .5f); sliderRect.anchoredPosition = new Vector2(44, -188);
            var slider = sliderRect.gameObject.AddComponent<Slider>();
            var track = Image("Track", sliderRect, new Color(.3f, .26f, .23f)); track.rectTransform.sizeDelta = new Vector2(480, 3);
            var fillArea = Rect("Fill area", sliderRect, new Vector2(480, 3));
            var fill = Image("Fill", fillArea, Ember); Stretch(fill.rectTransform);
            var handleArea = Rect("Handle area", sliderRect, new Vector2(480, 36));
            var handle = Image("Handle", handleArea, new Color(1, .86f, .64f)); handle.rectTransform.sizeDelta = new Vector2(12, 22);
            slider.fillRect = fill.rectTransform; slider.handleRect = handle.rectTransform; slider.targetGraphic = handle; slider.value = 1;
            menu.volume = slider; UnityEventTools.AddPersistentListener(slider.onValueChanged, menu.SetVolume);
            var toggleRect = Rect("Fullscreen", panel, new Vector2(480, 44));
            toggleRect.anchorMin = toggleRect.anchorMax = new Vector2(0, 1); toggleRect.pivot = new Vector2(0, .5f); toggleRect.anchoredPosition = new Vector2(44, -266);
            var toggle = toggleRect.gameObject.AddComponent<Toggle>();
            var box = Image("Checkbox", toggleRect, new Color(.3f, .26f, .23f)); box.rectTransform.sizeDelta = new Vector2(26, 26); box.rectTransform.anchoredPosition = new Vector2(-225, 0);
            var check = Image("Check", box.transform, Ember); check.rectTransform.sizeDelta = new Vector2(16, 16);
            Label("Label", toggleRect, "FULLSCREEN", new Vector2(48, -22), new Vector2(350, 44), 22);
            toggle.targetGraphic = box; toggle.graphic = check; menu.fullscreen = toggle;
            UnityEventTools.AddPersistentListener(toggle.onValueChanged, menu.SetFullscreen);
            menu.optionsBack = Button("Back", panel, "BACK", new Vector2(44, -368), 28);
            UnityEventTools.AddPersistentListener(menu.optionsBack.onClick, menu.ClosePanel);
            panel.gameObject.SetActive(false);

            var message = Panel("Message Panel", root.transform);
            menu.messagePanel = message.gameObject;
            menu.messageText = Label("Message", message, "THE JOURNEY AWAITS", new Vector2(44, -140), new Vector2(490, 240), 32);
            menu.messageText.textWrappingMode = TextWrappingModes.Normal;
            menu.messageBack = Button("Back", message, "BACK", new Vector2(44, -368), 28);
            UnityEventTools.AddPersistentListener(menu.messageBack.onClick, menu.ClosePanel);
            message.gameObject.SetActive(false);

            var fade = Image("Scene transition fade", root.transform, Color.black); Stretch(fade.rectTransform);
            menu.fade = fade.gameObject.AddComponent<CanvasGroup>(); menu.fade.alpha = 0; menu.fade.blocksRaycasts = false;
            if (Object.FindFirstObjectByType<EventSystem>() == null)
            {
                var eventObject = new GameObject("Menu EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
                eventObject.transform.SetParent(root.transform, false);
                eventObject.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
            }
            menu.Highlight(newGame.GetComponent<SerumMenuItem>());
            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            File.WriteAllText(Folder + "/Installed.txt", "Reference menu installed in " + scene.path + ".\n");
            Selection.activeGameObject = root;
            Debug.Log("SERUM_MENU_INSTALLED: Editable main-menu UI installed and saved. New Game loads the next enabled build scene.");
        }

        static RectTransform Rect(string name, Transform parent, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform)); go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>(); rect.sizeDelta = size; return rect;
        }
        static void Stretch(RectTransform rect) { rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero; }
        static Image Image(string name, Transform parent, Color color)
        {
            var rect = Rect(name, parent, new Vector2(100, 100)); var image = rect.gameObject.AddComponent<Image>(); image.color = color; image.raycastTarget = false; return image;
        }
        static TMP_Text Label(string name, Transform parent, string text, Vector2 position, Vector2 size, float pointSize)
        {
            var rect = Rect(name, parent, size); rect.anchorMin = rect.anchorMax = new Vector2(0, 1); rect.pivot = new Vector2(0, .5f); rect.anchoredPosition = position;
            var label = rect.gameObject.AddComponent<TextMeshProUGUI>(); label.font = font; label.text = text; label.fontSize = pointSize; label.characterSpacing = 5;
            label.color = Ivory; label.alignment = TextAlignmentOptions.MidlineLeft; label.raycastTarget = false; label.textWrappingMode = TextWrappingModes.NoWrap; return label;
        }
        static Button Button(string name, Transform parent, string text, Vector2 position, float pointSize)
        {
            var rect = Rect(name, parent, new Vector2(400, 70)); rect.anchorMin = rect.anchorMax = new Vector2(0, 1); rect.pivot = new Vector2(0, .5f); rect.anchoredPosition = position;
            var image = rect.gameObject.AddComponent<Image>(); image.color = Color.clear;
            var button = rect.gameObject.AddComponent<Button>(); button.targetGraphic = image;
            var colors = button.colors; colors.normalColor = Color.white; colors.highlightedColor = new Color(1, .8f, .5f); colors.selectedColor = colors.highlightedColor; button.colors = colors;
            var label = Label("Label", rect, text, new Vector2(0, -35), new Vector2(400, 70), pointSize);
            return button;
        }
        static RectTransform Panel(string name, Transform parent)
        {
            var image = Image(name, parent, new Color(.035f, .025f, .025f, .96f)); image.raycastTarget = true;
            image.rectTransform.sizeDelta = new Vector2(590, 440);
            var border = Image("Ember top edge", image.transform, new Color(1, .43f, .13f, .6f)); border.rectTransform.sizeDelta = new Vector2(500, 1); border.rectTransform.anchoredPosition = new Vector2(0, 219);
            return image.rectTransform;
        }
        static void Nav(Button b, Button up, Button down) { var nav = new Navigation { mode = Navigation.Mode.Explicit, selectOnUp = up, selectOnDown = down }; b.navigation = nav; }
        static Sprite GradientSprite()
        {
            const string path = Folder + "/ReadabilityGradient.png";
            var texture = new Texture2D(256, 4, TextureFormat.RGBA32, false);
            for (int x = 0; x < 256; x++) for (int y = 0; y < 4; y++)
            { float t = x / 255f; texture.SetPixel(x, y, new Color(.006f, .004f, .01f, .68f * Mathf.Pow(1 - Mathf.Clamp01(t / .53f), 2))); }
            texture.Apply(); File.WriteAllBytes(path, texture.EncodeToPNG()); Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(path);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path); importer.textureType = TextureImporterType.Sprite; importer.alphaIsTransparency = true; importer.mipmapEnabled = false; importer.wrapMode = TextureWrapMode.Clamp; importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }
    }
}
