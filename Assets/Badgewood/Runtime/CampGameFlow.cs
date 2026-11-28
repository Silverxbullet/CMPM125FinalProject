using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace Badgewood
{
    [DefaultExecutionOrder(-100)]
    public sealed class CampGameFlow : MonoBehaviour
    {
        public enum Page { Play, Main, Pause, Options, Help, Credits, DiveResult }
        public static CampGameFlow Instance { get; private set; }
        public CampFirstPerson player;
        public CliffDiveActivity diving;
        public Camera scenicCamera;
        public Campground.CampfireBuildQuest campfire;
        public BirdWatchingQuest birds;
        public bool GameplayActive => started && page == Page.Play && !diving.ResultVisible;
        public bool GameplayInputAllowed => GameplayActive && InputFocused && Time.frameCount > inputAfterFrame;
        private bool InputFocused
        {
            get
            {
                if (!Application.isFocused) return false;
#if UNITY_EDITOR
                UnityEditor.EditorWindow focused = UnityEditor.EditorWindow.focusedWindow;
                return focused != null && focused.GetType().Name == "GameView";
#else
                return true;
#endif
            }
        }
        public static bool CanInteract => Instance == null || Instance.GameplayInputAllowed && !Instance.diving.IsDiving;
        public float Sensitivity { get; private set; }
        public bool InvertY { get; private set; }
        private float fieldOfView;
        private bool started;
        private int inputAfterFrame;
        private Page page = Page.Main;
        private Page backPage = Page.Main;
        private GameObject[] pages;
        private GameObject hud;
        private GameObject menuShade;
        private Text startLabel, status, objective, badges, toast, resultTitle, resultBody;
        private Button nextStage;
        private string notification;
        private float notificationUntil;
        private Font font;
        private readonly HashSet<lb_Bird> observedBirds = new HashSet<lb_Bird>();
        private RectTransform design;
        private readonly Color forest = new Color(0.045f, 0.12f, 0.09f, 0.97f);
        private readonly Color cream = new Color(0.95f, 0.91f, 0.80f);
        private readonly Color gold = new Color(0.91f, 0.70f, 0.34f);
        private readonly Color sage = new Color(0.54f, 0.68f, 0.53f);

        private void Awake()
        {
            Instance = this;
            Sensitivity = PlayerPrefs.GetFloat("badgewood.sensitivity", 0.10f);
            InvertY = PlayerPrefs.GetInt("badgewood.invertY", 0) != 0;
            fieldOfView = PlayerPrefs.GetFloat("badgewood.fov", 70f);
            AudioListener.volume = PlayerPrefs.GetFloat("badgewood.volume", 0.8f);
            player.view.fieldOfView = fieldOfView;
            BuildUI();
            ShowPage(Page.Main);
        }

        private void Update()
        {
            Keyboard keys = InputFocused ? Keyboard.current : null;
            if (keys != null && keys.escapeKey.wasPressedThisFrame)
            {
                if (page == Page.Play) Pause();
                else if (page == Page.Pause) ReturnToPlay();
                else if (page == Page.Options || page == Page.Help || page == Page.Credits) ShowPage(backPage);
            }
            if (page == Page.DiveResult && keys != null)
            {
                if (keys.rKey.wasPressedThisFrame) diving.Retry();
                else if (keys.enterKey.wasPressedThisFrame)
                {
                    if (diving.LastSucceeded) diving.NextStage();
                    else diving.Retry();
                }
            }
            if (page == Page.Play && started)
            {
                status.text = diving.IsDiving ? "CLIFF DIVING  /  STAGE " + (diving.CourseIndex + 1) + " OF 3" : "CAMP BADGEWOOD";
                objective.text = diving.IsDiving
                    ? diving.Current.title + "   •   Rings " + diving.RingsPassed + " / " + diving.Current.rings.Length + "   •   R: retry"
                    : diving.CanLaunch ? "SPACE: dive   •   Stage " + (diving.CourseIndex + 1) + " — " + diving.Current.title
                    : campfire != null && campfire.Near(player.transform.position) ? campfire.CurrentHint
                    : "Explore the trails   •   F / click: interact   •   Esc: pause";
                badges.text = "BADGES  " + ((diving.BadgeEarned ? 1 : 0) + (campfire != null && campfire.Completed ? 1 : 0) + (birds != null && birds.Completed ? 1 : 0)) + " / 3\n" +
                    "Cliff stages  " + diving.StagesCleared + " / 3" +
                    (birds != null ? "     Birds  " + birds.BirdsSeen + " / 5" : "") +
                    (campfire != null ? "\nCampfire  " + (campfire.Completed ? "built" : campfire.Rocks + "/5 rocks  ·  " + campfire.Sticks + "/5 sticks") : "");
                toast.text = Time.unscaledTime < notificationUntil ? notification : "";
            }
            if (GameplayInputAllowed && Cursor.lockState != CursorLockMode.Locked && Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
                CaptureCursor();
        }

        public void Notify(string text)
        {
            notification = text;
            notificationUntil = Time.unscaledTime + 5f;
        }

        public void ObserveBird(lb_Bird bird)
        {
            if (!CanInteract || bird == null || birds == null || birds.Completed) return;
            if (!observedBirds.Add(bird)) { Notify("You've already observed that bird. Look for another one."); return; }
            birds.BirdClicked();
            Notify(birds.Completed ? "Bird Watching Badge earned!" : "Bird observed — " + birds.BirdsSeen + " / 5");
        }

        public void StartGame()
        {
            if (!started)
            {
                player.Teleport(player.spawnPoint);
                started = true;
                startLabel.text = "CONTINUE ADVENTURE";
            }
            ReturnToPlay();
        }

        public void Pause() { if (GameplayActive) ShowPage(Page.Pause); }
        public void ReturnToPlay()
        {
            inputAfterFrame = Time.frameCount + 1;
            ShowPage(Page.Play);
        }

        private void CaptureCursor()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private void ShowPage(Page target)
        {
            page = target;
            for (int i = 1; i < pages.Length; i++) pages[i].SetActive(i == (int)target);
            hud.SetActive(target == Page.Play);
            bool playing = target == Page.Play;
            menuShade.SetActive(!playing);
            Time.timeScale = playing ? 1f : 0f;
            AudioListener.pause = !playing;
            scenicCamera.enabled = target == Page.Main || !started;
            player.view.enabled = !scenicCamera.enabled;
            if (playing) CaptureCursor();
            else { Cursor.lockState = CursorLockMode.None; Cursor.visible = true; }
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
        }

        private void OpenSubpage(Page target)
        {
            backPage = page == Page.Main ? Page.Main : Page.Pause;
            ShowPage(target);
        }

        public void ShowDiveResult()
        {
            resultTitle.text = diving.LastSucceeded
                ? diving.BadgeEarned && diving.CourseIndex == 2 ? "CLIFF DIVER BADGE EARNED" : "STAGE " + (diving.CourseIndex + 1) + " COMPLETE"
                : "TAKE ANOTHER LEAP";
            resultBody.text = diving.Current.title + "\n\n" + diving.ResultMessage + "\n\nRings: " + diving.RingsPassed + " / " + diving.Current.rings.Length + "     Attempts: " + diving.Attempts;
            nextStage.gameObject.SetActive(diving.LastSucceeded);
            nextStage.GetComponentInChildren<Text>().text = diving.BadgeEarned && diving.CourseIndex == 2 ? "PLAY AGAIN" : "NEXT STAGE";
            ShowPage(Page.DiveResult);
        }

        private void Quit()
        {
            Time.timeScale = 1f;
            AudioListener.pause = false;
            Application.Quit();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#elif UNITY_WEBGL
            Notify("You can close this browser tab to leave the game.");
#endif
        }

        private void OnDestroy()
        {
            if (Instance != this) return;
            Instance = null;
            Time.timeScale = 1f;
            AudioListener.pause = false;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        private RectTransform Rect(string name, Transform parent, float x, float y, float width, float height)
        {
            RectTransform rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(x, -y);
            rect.sizeDelta = new Vector2(width, height);
            return rect;
        }

        private Image Fill(string name, Transform parent, float x, float y, float width, float height, Color color)
        {
            Image image = Rect(name, parent, x, y, width, height).gameObject.AddComponent<Image>();
            image.color = color;
            return image;
        }

        private Text Label(string name, string value, Transform parent, float x, float y, float width, float height, int size, Color color, FontStyle style = FontStyle.Normal)
        {
            Text text = Rect(name, parent, x, y, width, height).gameObject.AddComponent<Text>();
            text.font = font;
            text.text = value;
            text.fontSize = size;
            text.fontStyle = style;
            text.color = color;
            text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            return text;
        }

        private Button ActionButton(string name, string label, Transform parent, float x, float y, float width, Action action, bool primary = false)
        {
            Image image = Fill(name, parent, x, y, width, 48f, primary ? gold : new Color(0.11f, 0.23f, 0.17f));
            Button button = image.gameObject.AddComponent<Button>();
            ColorBlock colors = button.colors;
            colors.highlightedColor = new Color(1.12f, 1.12f, 1.12f);
            colors.selectedColor = colors.highlightedColor;
            colors.pressedColor = new Color(0.75f, 0.82f, 0.72f);
            button.colors = colors;
            button.targetGraphic = image;
            button.onClick.AddListener(() => action());
            Text text = Label("Label", label, image.transform, 16, 8, width - 32, 32, 18, primary ? forest : cream, FontStyle.Bold);
            text.alignment = TextAnchor.MiddleCenter;
            return button;
        }

        private GameObject Panel(Page which, string heading, string subtitle)
        {
            Image image = Fill(which + " panel", design, 0, 0, 1280, 720, new Color(0.025f, 0.07f, 0.05f, 0.90f));
            pages[(int)which] = image.gameObject;
            Fill("Gold rule", image.transform, 80, 76, 54, 4, gold);
            Label("Heading", heading, image.transform, 80, 104, 1100, 70, 42, cream, FontStyle.Bold);
            Label("Subtitle", subtitle, image.transform, 80, 180, 1100, 55, 18, sage);
            return image.gameObject;
        }

        private void BackButton(Transform parent)
        {
            ActionButton("Back", "BACK", parent, 80, 614, 200, () => ShowPage(backPage));
        }

        private void SliderRow(Transform parent, string title, float y, float low, float high, float initial, Action<float> changed, string format)
        {
            Label(title + " title", title, parent, 100, y, 350, 34, 20, cream);
            Text value = Label(title + " value", initial.ToString(format), parent, 1010, y, 140, 32, 19, gold);
            RectTransform rt = Rect(title + " slider", parent, 500, y + 4, 450, 28);
            Slider slider = rt.gameObject.AddComponent<Slider>();
            Image track = Fill("Track", rt, 0, 9, 450, 10, new Color(0.25f, 0.36f, 0.27f));
            RectTransform area = Rect("Handle area", rt, 8, 0, 434, 28);
            Image handle = Fill("Handle", area, 0, 0, 18, 28, gold);
            handle.rectTransform.sizeDelta = new Vector2(18f, 0f);
            slider.handleRect = handle.rectTransform;
            slider.targetGraphic = handle;
            slider.direction = Slider.Direction.LeftToRight;
            slider.minValue = low;
            slider.maxValue = high;
            slider.SetValueWithoutNotify(initial);
            slider.onValueChanged.AddListener(v => { value.text = v.ToString(format); changed(v); PlayerPrefs.Save(); });
        }

        private void ToggleRow(Transform parent, string title, float y, bool initial, Action<bool> changed)
        {
            Label(title + " title", title, parent, 100, y, 620, 34, 20, cream);
            Image background = Fill(title + " toggle", parent, 940, y, 32, 32, new Color(0.20f, 0.32f, 0.24f));
            Toggle toggle = background.gameObject.AddComponent<Toggle>();
            Image mark = Fill("Check", background.transform, 7, 7, 18, 18, gold);
            toggle.targetGraphic = background;
            toggle.graphic = mark;
            toggle.SetIsOnWithoutNotify(initial);
            toggle.onValueChanged.AddListener(v => { changed(v); PlayerPrefs.Save(); });
        }

        private void BuildUI()
        {
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            GameObject canvasObject = new GameObject("Badgewood UI", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(transform, false);
            canvasObject.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scale = canvasObject.GetComponent<CanvasScaler>();
            scale.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scale.referenceResolution = new Vector2(1280, 720);
            scale.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            RectTransform backdrop = new GameObject("Full screen shade", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
            backdrop.SetParent(canvasObject.transform, false);
            backdrop.anchorMin = Vector2.zero; backdrop.anchorMax = Vector2.one;
            backdrop.offsetMin = backdrop.offsetMax = Vector2.zero;
            backdrop.GetComponent<Image>().color = new Color(0.025f, 0.07f, 0.05f, 0.25f);
            backdrop.GetComponent<Image>().raycastTarget = false;
            menuShade = backdrop.gameObject;
            design = new GameObject("1280 x 720 layout", typeof(RectTransform)).GetComponent<RectTransform>();
            design.SetParent(canvasObject.transform, false);
            design.anchorMin = design.anchorMax = new Vector2(0.5f, 0.5f);
            design.sizeDelta = new Vector2(1280, 720);
            if (EventSystem.current == null)
                new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            pages = new GameObject[Enum.GetValues(typeof(Page)).Length];

            GameObject main = Panel(Page.Main, "CAMP BADGEWOOD", "A SUMMER OF SMALL ADVENTURES");
            Fill("Menu card", main.transform, 66, 248, 364, 400, forest);
            startLabel = ActionButton("Start", "START ADVENTURE", main.transform, 88, 270, 320, StartGame, true).GetComponentInChildren<Text>();
            ActionButton("Options", "OPTIONS", main.transform, 88, 331, 320, () => OpenSubpage(Page.Options));
            ActionButton("Help", "HELP & CONTROLS", main.transform, 88, 392, 320, () => OpenSubpage(Page.Help));
            ActionButton("Credits", "CREDITS", main.transform, 88, 453, 320, () => OpenSubpage(Page.Credits));
            ActionButton("Quit", "QUIT GAME", main.transform, 88, 536, 320, Quit);
            Label("Hero", "Welcome Campers, enjoy your Stay!", main.transform, 530, 278, 660, 180, 52, cream, FontStyle.Bold);
            Label("Activities", "Build a campfire. Spot the birds.\nFollow the woodland trail and take the leap.", main.transform, 534, 495, 620, 88, 22, sage);
            Label("Edition", "EXPLORE  /  CREATE  /  COLLECT", main.transform, 534, 622, 620, 30, 15, gold);

            GameObject pause = Panel(Page.Pause, "TAKE A BREATHER", "Your adventure is paused.");
            ActionButton("Resume", "RESUME", pause.transform, 90, 268, 350, ReturnToPlay, true);
            ActionButton("Options", "OPTIONS", pause.transform, 90, 330, 350, () => OpenSubpage(Page.Options));
            ActionButton("Help", "HELP & CONTROLS", pause.transform, 90, 392, 350, () => OpenSubpage(Page.Help));
            ActionButton("Main menu", "MAIN MENU", pause.transform, 90, 454, 350, () => ShowPage(Page.Main));
            ActionButton("Quit", "QUIT GAME", pause.transform, 90, 540, 350, Quit);
            Label("Pause note", "The camp will be here\nwhen you're ready.", pause.transform, 550, 320, 600, 200, 46, cream, FontStyle.Bold);

            GameObject options = Panel(Page.Options, "MAKE YOURSELF COMFORTABLE", "Settings are saved on this device.");
            SliderRow(options.transform, "Mouse sensitivity", 270, 0.04f, 0.25f, Sensitivity, v => { Sensitivity = v; PlayerPrefs.SetFloat("badgewood.sensitivity", v); }, "0.00");
            SliderRow(options.transform, "Master volume", 337, 0f, 1f, AudioListener.volume, v => { AudioListener.volume = v; PlayerPrefs.SetFloat("badgewood.volume", v); }, "0%");
            SliderRow(options.transform, "Field of view", 404, 60f, 90f, fieldOfView, v => { fieldOfView = v; player.view.fieldOfView = v; PlayerPrefs.SetFloat("badgewood.fov", v); }, "0");
            ToggleRow(options.transform, "Invert vertical mouse look", 480, InvertY, v => { InvertY = v; PlayerPrefs.SetInt("badgewood.invertY", v ? 1 : 0); });
            ToggleRow(options.transform, "Fullscreen (desktop build)", 537, Screen.fullScreen, v => Screen.fullScreen = v);
            BackButton(options.transform);

            GameObject help = Panel(Page.Help, "YOUR FIELD GUIDE", "Explore the campground and earn the three activity badges.");
            Label("Walking", "CAMPGROUND CONTROLS\n\nWASD / arrows    Walk\nMouse    Look around\nSpace    Jump\nShift    Run\nF / left click    Collect campfire supplies\nLeft click    Observe a bird under the crosshair\nEscape    Pause / release the mouse", help.transform, 270, 258, 850, 330, 22, cream);
            BackButton(help.transform);

            GameObject credits = Panel(Page.Credits, "MADE AROUND THE CAMPFIRE", "A CMPM 125 group project");
            Label("Credits", "To be added\n\nTHIRD-PARTY ART\nLow-Poly Simple Nature Pack — JustCreate\nLow Poly: Woods Lifestyle — Rad-Coders\nLiving Birds — included bird asset pack\n\nBuilt with Unity. Asset attribution is also included in the project README.", credits.transform, 92, 248, 1090, 355, 21, cream);
            BackButton(credits.transform);

            GameObject result = Panel(Page.DiveResult, "THE CLIFF DIVER'S TRAIL", "Three stages. One hard-earned badge.");
            resultTitle = Label("Result title", "", result.transform, 92, 250, 1080, 60, 34, gold, FontStyle.Bold);
            resultBody = Label("Result body", "", result.transform, 94, 330, 1060, 170, 23, cream);
            nextStage = ActionButton("Next stage", "NEXT STAGE", result.transform, 92, 554, 330, () => diving.NextStage(), true);
            ActionButton("Retry", "RETRY STAGE", result.transform, 472, 554, 330, () => diving.Retry());
            ActionButton("Return to camp", "RETURN TO CAMP", result.transform, 852, 554, 330, () => diving.ReturnToCamp());
            Label("Keyboard", "Enter: continue / retry     R: retry", result.transform, 94, 632, 1000, 32, 17, sage);

            hud = Rect("Gameplay HUD", design, 0, 0, 1280, 720).gameObject;
            Fill("HUD left", hud.transform, 22, 20, 620, 92, new Color(0.025f, 0.07f, 0.05f, 0.82f)).raycastTarget = false;
            status = Label("Location", "", hud.transform, 40, 32, 580, 30, 19, gold, FontStyle.Bold);
            objective = Label("Objective", "", hud.transform, 40, 69, 580, 34, 16, cream);
            Fill("HUD right", hud.transform, 842, 20, 416, 112, new Color(0.025f, 0.07f, 0.05f, 0.82f)).raycastTarget = false;
            badges = Label("Badge progress", "", hud.transform, 860, 32, 380, 92, 17, cream);
            Fill("Crosshair", hud.transform, 638, 358, 4, 4, cream).raycastTarget = false;
            toast = Label("Notification", "", hud.transform, 270, 620, 740, 74, 20, cream);
            toast.alignment = TextAnchor.MiddleCenter;
            Fill("Shade", hud.transform, 0, 0, 0, 0, Color.clear).raycastTarget = false;
        }
    }
}
