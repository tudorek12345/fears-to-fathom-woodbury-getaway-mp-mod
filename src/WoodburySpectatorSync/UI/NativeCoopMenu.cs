using System;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using WoodburySpectatorSync.Config;
using WoodburySpectatorSync.Coop;
using WoodburySpectatorSync.Diagnostics;

namespace WoodburySpectatorSync.UI
{
    // The entry copies the game's menu typography, inside its moving menu panel.
    // Connection controls share its font and use the game's existing Canvas/EventSystem.
    internal sealed class NativeCoopMenu : IDisposable
    {
        private sealed class Field { public GameObject Root; public Func<string> Read; public Action<string> Write; }
        private readonly Settings _settings;
        private readonly CoopServer _server;
        private readonly CoopClient _client;
        private readonly Action<string> _log;
        private UIManagerMainMenu _menu;
        private Button _entry, _host, _join, _stop, _story, _lan, _steam, _copy, _back;
        private GameObject _template;
        private GameObject _panel;
        private Graphic _labelTemplate, _status;
        private Field _name, _address, _port, _lobby;
        private bool _buttonsWereActive, _introWasActive;
        private string _error;
        private float _nextScan;
        private GameObject _coopPage, _characterPage;
        private Button _tabCoop, _tabCharacter, _applyCharacter;
        private RawImage _portrait;
        private Text _characterStatus;
        private readonly Text[] _characterValues = new Text[9];
        private readonly CharacterDraft _draft = new CharacterDraft();
        private readonly CharacterPreview _preview = new CharacterPreview();
        private RectTransform _entryAnchor;
        private Graphic _entryGraphic, _anchorGraphic, _nextGraphic;
        public bool IsBound => _entry != null;
        public bool IsOpen => _panel != null && _panel.activeSelf;
        internal Canvas PanelCanvas => _panel != null ? _panel.GetComponent<Canvas>() : null;

        public NativeCoopMenu(Settings settings, CoopServer server, CoopClient client, Action<string> log)
        { _settings = settings; _server = server; _client = client; _log = log; }

        public void Update()
        {
            if (!_settings.CoopMenuEnabled.Value || SceneManager.GetActiveScene().name != "MainMenu")
            { if (IsOpen) SetOpen(false); if (_entry != null) _entry.gameObject.SetActive(false); return; }
            if (_entry != null) _entry.gameObject.SetActive(true);
            RefreshLayout();
            if (!IsBound && Time.unscaledTime >= _nextScan)
            {
                _nextScan = Time.unscaledTime + 1f;
                try { Bind(); }
                catch (Exception ex) { _log?.Invoke("NativeMultiplayerMenu bind failed: " + ex.Message); Dispose(); }
            }
            if (!IsOpen) { _preview.Update(CharacterProfile.Default, false); return; }
            Cursor.visible = true; Cursor.lockState = CursorLockMode.None;
            if (Input.GetKeyDown(KeyCode.Escape)) SetOpen(false);
            bool busy = _server.IsRunning || _client.IsRunning;
            bool steam = _settings.Transport.Value == CoopTransport.Spacewar;
            _address.Root.SetActive(!steam); _port.Root.SetActive(!steam); _lobby.Root.SetActive(steam);
            _lan.interactable = _steam.interactable = !busy;
            _host.interactable = _join.interactable = !busy;
            _stop.interactable = busy; _story.interactable = _server.IsRunning;
            bool activeLobby = _server.IsRunning && _server.SpacewarLobbyId != 0;
            _copy.interactable = activeLobby;
            SetText(_lan.gameObject, steam ? "LAN / IP" : "LAN / IP  •");
            SetText(_steam.gameObject, steam ? "Spacewar  •" : "Spacewar");
            string idleStatus = _settings.ModeSetting.Value == Mode.CoopClient ? _client.LastDisconnectReason : _server.LastDisconnectReason;
            if (string.IsNullOrEmpty(idleStatus)) idleStatus = "Choose Host or Join. Both players need the same mod version.";
            string status = _error ?? (_server.IsRunning ? _server.ConnectionStatus : _client.IsRunning ? _client.Status : idleStatus);
            if (string.IsNullOrEmpty(status)) status = _server.IsClientConnected ? "Partner connected. Start the story, or go Back to choose a checkpoint." : "Hosting. Waiting for your partner to join.";
            if (activeLobby) status += "\nLobby ID: " + _server.SpacewarLobbyId;
            SetText(_status.gameObject, status);
            if (_characterPage.activeSelf)
            {
                _preview.Update(_draft.Value, true); _portrait.texture = _preview.Texture;
                _applyCharacter.interactable = _draft.Changed;
                _characterStatus.text = _draft.Changed ? "Unsaved changes  ·  Apply to share your look" : "Saved look  ·  Shared with your co-op partner";
            }
            else _preview.Update(_draft.Value, false);
        }

        private void Bind()
        {
            _menu = UnityEngine.Object.FindObjectOfType<UIManagerMainMenu>();
            if (_menu == null || _menu.mmButtonsPanel == null) return;
            var buttons = _menu.mmButtonsPanel.Cast<Transform>().Where(t => !string.IsNullOrEmpty(Label(t.gameObject))).ToArray();
            var source = buttons.FirstOrDefault(b => Label(b.gameObject).IndexOf("option", StringComparison.OrdinalIgnoreCase) >= 0) ?? buttons.FirstOrDefault();
            _template = source != null ? source.gameObject : null;
            if (_template == null) return;
            _labelTemplate = (Graphic)_template.GetComponentInChildren<TMP_Text>(true) ?? _template.GetComponentInChildren<Text>(true);
            if (_labelTemplate == null) return;
            _entry = CloneButton(_menu.mmButtonsPanel, "Multiplayer", () => SetOpen(true));
            var rect = (RectTransform)_entry.transform;
            _entry.transform.SetAsFirstSibling();
            if (_menu.mmButtonsPanel.GetComponent<LayoutGroup>() != null)
            {
                _entryAnchor = null;
            }
            else
            {
                // Different native buttons use different anchors; anchoredPosition differences
                // are not visual gaps. Place above the native list, without moving Lean's targets.
                var items = buttons.Where(b =>
                    b.GetComponentInChildren<TMP_Dropdown>(true) == null && b.GetComponentInChildren<Dropdown>(true) == null)
                    .Select(b => (Graphic)b.GetComponentInChildren<TMP_Text>(true) ?? b.GetComponentInChildren<Text>(true))
                    .Where(g => g != null).OrderByDescending(g => Centre(g).y).ToArray();
                // These native entries are stable in Woodbury's menu. Inactive localization
                // credits can have off-screen rectangles and must never anchor the new row.
                var episodes = buttons.FirstOrDefault(b => b.name.IndexOf("episode", StringComparison.OrdinalIgnoreCase) >= 0 || Label(b.gameObject).IndexOf("episode", StringComparison.OrdinalIgnoreCase) >= 0);
                var discord = buttons.FirstOrDefault(b => b.name.IndexOf("discord", StringComparison.OrdinalIgnoreCase) >= 0 || Label(b.gameObject).IndexOf("discord", StringComparison.OrdinalIgnoreCase) >= 0);
                _anchorGraphic = episodes != null ? (Graphic)episodes.GetComponentInChildren<TMP_Text>(true) ?? episodes.GetComponentInChildren<Text>(true) : items.FirstOrDefault(g => g.gameObject.activeInHierarchy);
                _nextGraphic = discord != null ? (Graphic)discord.GetComponentInChildren<TMP_Text>(true) ?? discord.GetComponentInChildren<Text>(true) : items.FirstOrDefault(g => g != _anchorGraphic && g.gameObject.activeInHierarchy);
                _entryAnchor = _anchorGraphic != null ? (RectTransform)_anchorGraphic.transform : null;
                _entryGraphic = _entry.GetComponentInChildren<Graphic>(true);
                Canvas.ForceUpdateCanvases(); RefreshLayout();
            }
            BuildPanel();
            _log?.Invoke("NativeMultiplayerMenu bound scene=MainMenu entry=Multiplayer template=" + Label(_template) +
                " font=" + _labelTemplate.GetType().Name + " buttons=" + buttons.Length + " persistentActions=" + _entry.onClick.GetPersistentEventCount() +
                " anchor=" + (_anchorGraphic != null ? Label(_anchorGraphic.gameObject) : "layout") + " next=" + (_nextGraphic != null ? Label(_nextGraphic.gameObject) : "none"));
            if (Environment.GetEnvironmentVariable("WSS_MENU_PROBE") == "1") Probe();
            var captureDirectory = Environment.GetEnvironmentVariable("WSS_CAPTURE_MENU");
            if (!string.IsNullOrEmpty(captureDirectory)) _menu.gameObject.AddComponent<MenuCaptureProbe>().Begin(this, captureDirectory, _log);
        }

        private static Vector3 Centre(Graphic graphic)
        { var rect = (RectTransform)graphic.transform; return rect.TransformPoint(rect.rect.center); }

        internal void RefreshLayout()
        {
            if (_entry == null || _anchorGraphic == null || _entryGraphic == null) return;
            var above = Centre(_anchorGraphic);
            var gap = _nextGraphic != null ? Vector3.Project(above - Centre(_nextGraphic), _anchorGraphic.transform.up)
                : _anchorGraphic.transform.up * ((RectTransform)_anchorGraphic.transform).rect.height * _anchorGraphic.transform.lossyScale.y * 1.5f;
            _entry.transform.position += above + gap - Centre(_entryGraphic);
        }

        private void BuildPanel()
        {
            // Native Canvas scaling is tuned for oversized menu labels. Give the
            // editor its own reference resolution so every control remains legible.
            var full = ModernMenuUI.Panel(null, "Woodbury Multiplayer", 0, 0, 1, 1, new Color(.025f,.035f,.045f,1));
            var canvas = full.gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 30000;
            var scaler = full.gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280,720); scaler.matchWidthOrHeight = .5f;
            full.gameObject.AddComponent<GraphicRaycaster>();
            _panel = full.gameObject;
            var frame = ModernMenuUI.Rect(full, "Menu frame", .055f, .055f, .89f, .89f);
            ModernMenuUI.Panel(frame, "Navigation", 0, 0, .20f, 1, ModernMenuUI.Ink);
            ModernMenuUI.Text(frame, "WOODBURY", .025f, .87f, .16f, .07f, 25);
            ModernMenuUI.Text(frame, "CO-OP  /  TWO PLAYERS", .025f, .815f, .16f, .04f, 16, ModernMenuUI.Accent);
            _tabCoop = ModernMenuUI.Button(frame, "Co-op", .02f, .65f, .16f, .08f, () => ShowCharacter(false));
            _tabCharacter = ModernMenuUI.Button(frame, "Character", .02f, .54f, .16f, .08f, () => ShowCharacter(true));
            _back = ModernMenuUI.Button(frame, "Back to game", .02f, .035f, .16f, .08f, () => SetOpen(false));
            ModernMenuUI.Text(frame, "ESC  Back", .025f, .14f, .15f, .04f, 18, ModernMenuUI.Muted);
            ModernMenuUI.Text(frame, "Make the getaway together.", .25f, .87f, .70f, .09f, 42);
            ModernMenuUI.Text(frame, "Host a session, join a friend, or create your character.", .25f, .80f, .70f, .055f, 23, ModernMenuUI.Muted);
            var connection = ModernMenuUI.Panel(frame, "Co-op", .24f, .035f, .74f, .71f, ModernMenuUI.Card);
            _coopPage = connection.gameObject;
            var content = ModernMenuUI.Rect(connection, "Connection controls", .045f, .045f, .91f, .91f);
            ModernMenuUI.Text(content, "CONNECTION", 0, .88f, .4f, .07f, 20, ModernMenuUI.Muted);
            _lan = ModernMenuUI.Button(content, "LAN / IP", .48f, .86f, .24f, .10f, () => ChangeTransport(CoopTransport.Lan));
            _steam = ModernMenuUI.Button(content, "Spacewar", .75f, .86f, .25f, .10f, () => ChangeTransport(CoopTransport.Spacewar));
            _name = ModernField(content, "Player name", 0, .63f, .46f, string.IsNullOrWhiteSpace(_settings.CoopDisplayName.Value) ? Environment.UserName : _settings.CoopDisplayName.Value, 32);
            _address = ModernField(content, "Host IP / name", .53f, .63f, .47f, _settings.SpectatorHostIP.Value, 253);
            _port = ModernField(content, "TCP port", .53f, .45f, .47f, _settings.HostPort.Value.ToString(), 5);
            _lobby = ModernField(content, "Lobby ID", .53f, .63f, .47f, _settings.SpacewarLobbyId.Value == 0 ? "" : _settings.SpacewarLobbyId.Value.ToString(), 20);
            ModernMenuUI.Text(content, "The host controls story progression.\nBoth players need the same mod version.", 0, .44f, .47f, .15f, 21, ModernMenuUI.Muted);
            _host = ModernMenuUI.Button(content, "Host game", 0, .30f, .30f, .105f, () => Connect(false), true);
            _join = ModernMenuUI.Button(content, "Join game", .35f, .30f, .30f, .105f, () => Connect(true));
            _stop = ModernMenuUI.Button(content, "Disconnect", .70f, .30f, .30f, .105f, Stop);
            _status = ModernMenuUI.Text(content, "", 0, .12f, 1, .15f, 22, ModernMenuUI.Muted);
            _copy = ModernMenuUI.Button(content, "Copy lobby ID", 0, 0, .32f, .10f, () => GUIUtility.systemCopyBuffer = _server.SpacewarLobbyId.ToString());
            _story = ModernMenuUI.Button(content, "Start story", .65f, 0, .35f, .10f, () => { if (_server.IsRunning) { SetOpen(false); _menu.NewGame(); } }, true);
            BuildCharacterPage(frame);
            _characterPage.SetActive(false); _panel.SetActive(false);
        }
        private Field ModernField(Transform parent, string label, float x, float y, float width, string value, int limit)
        {
            var input = ModernMenuUI.Field(parent, label, x, y, width, value, limit);
            return new Field { Root = input.transform.parent.gameObject, Read = () => input.text, Write = s => input.text = s };
        }
        private void BuildCharacterPage(Transform frame)
        {
            var page = ModernMenuUI.Rect(frame, "Character", .24f, .035f, .74f, .71f); _characterPage = page.gameObject;
            var preview = ModernMenuUI.Panel(page, "Preview", 0, .13f, .37f, .87f, ModernMenuUI.Card);
            var viewport = ModernMenuUI.Rect(preview, "Portrait viewport", .025f, .15f, .95f, .82f);
            var image = ModernMenuUI.Rect(viewport, "Live character", 0, 0, 1, 1);
            _portrait = image.gameObject.AddComponent<RawImage>(); _portrait.raycastTarget = false;
            var aspect = image.gameObject.AddComponent<AspectRatioFitter>(); aspect.aspectMode = AspectRatioFitter.AspectMode.FitInParent; aspect.aspectRatio = .8f;
            ModernMenuUI.Button(preview, "<", .035f, .025f, .18f, .10f, () => _preview.Rotate(-30));
            ModernMenuUI.Button(preview, "Face / body", .245f, .025f, .51f, .10f, () => _preview.ToggleFace());
            ModernMenuUI.Button(preview, ">", .785f, .025f, .18f, .10f, () => _preview.Rotate(30));
            var options = ModernMenuUI.Panel(page, "Wardrobe", .40f, .13f, .60f, .87f, ModernMenuUI.Card);
            for (int i = 0; i < CharacterProfile.Choices.Length; i++)
            {
                int slot = i; float y = .89f - i * .096f;
                ModernMenuUI.Text(options, CharacterProfile.Labels[i], .04f, y, .34f, .075f, 22, ModernMenuUI.Muted);
                ModernMenuUI.Button(options, "<", .40f, y, .10f, .075f, () => CycleCharacter(slot, -1));
                _characterValues[i] = ModernMenuUI.Text(options, "", .51f, y, .32f, .075f, 23);
                _characterValues[i].alignment = TextAnchor.MiddleCenter;
                ModernMenuUI.Button(options, ">", .85f, y, .10f, .075f, () => CycleCharacter(slot, 1));
            }
            ModernMenuUI.Button(page, "Reset", 0, .015f, .14f, .08f, () => { _draft.Reset(); RefreshCharacter(); });
            ModernMenuUI.Button(page, "Discard", .16f, .015f, .18f, .08f, () => { _draft.Discard(); RefreshCharacter(); });
            _characterStatus = ModernMenuUI.Text(page, "", .37f, .012f, .38f, .085f, 19, ModernMenuUI.Muted);
            _applyCharacter = ModernMenuUI.Button(page, "Apply look", .77f, .015f, .23f, .08f, () =>
            {
                _settings.CharacterAppearance.Value = _draft.Apply().ToString();
                _log?.Invoke("CharacterAppearance saved profile=" + _draft.Value);
                RefreshCharacter();
            }, true);
        }
        internal void ShowCharacter(bool show = true)
        {
            if (_characterPage == null) return;
            if (show && !_characterPage.activeSelf)
            { CharacterProfile.TryParse(_settings.CharacterAppearance.Value, out var profile); _draft.Begin(profile); RefreshCharacter(); }
            if (!show) _draft.Discard();
            _characterPage.SetActive(show); _coopPage.SetActive(!show);
            _tabCoop.GetComponent<Image>().color = show ? new Color(.14f,.17f,.19f) : ModernMenuUI.Accent;
            _tabCharacter.GetComponent<Image>().color = show ? ModernMenuUI.Accent : new Color(.14f,.17f,.19f);
            _preview.Update(_draft.Value, show && IsOpen); if (show) _portrait.texture = _preview.Texture;
        }
        private void CycleCharacter(int slot, int step) { _draft.Cycle(slot, step); RefreshCharacter(); }
        internal void PreviewFixture(int outfit, bool face)
        {
            _draft.Begin(CharacterProfile.Default); _draft.Cycle(4,outfit);
            if(outfit==1) { _draft.Cycle(2,2); _draft.Cycle(5,1); _draft.Cycle(8,2); }
            if(outfit==2) { _draft.Cycle(0,2); _draft.Cycle(1,3); _draft.Cycle(2,1); _draft.Cycle(5,4); _draft.Cycle(8,1); }
            if(face) _preview.ToggleFace(); RefreshCharacter();
        }
        private void RefreshCharacter()
        { for (int i = 0; i < _characterValues.Length; i++) _characterValues[i].text = CharacterProfile.Choices[i][_draft.Value.Get(i)]; }

        private void ChangeTransport(CoopTransport transport)
        { if (_server.IsRunning || _client.IsRunning) return; _settings.Transport.Value = transport; _error = null; }

        private void Connect(bool joining)
        {
            if (_server.IsRunning || _client.IsRunning) return;
            bool steam = _settings.Transport.Value == CoopTransport.Spacewar;
            if (!CoopMenuValidation.TryValidate(steam, joining, _address.Read(), _port.Read(), _lobby.Read(), _name.Read(), out int port, out ulong lobby, out _error)) return;
            _settings.CoopDisplayName.Value = _name.Read().Trim();
            if (steam) _settings.SpacewarLobbyId.Value = lobby;
            else { _settings.HostPort.Value = port; _settings.SpectatorHostIP.Value = _address.Read().Trim(); }
            _settings.ModeSetting.Value = joining ? Mode.CoopClient : Mode.CoopHost;
            try { if (joining) _client.Connect(); else _server.Start(); }
            catch (Exception ex) { _error = ex.Message; }
            _log?.Invoke("NativeMultiplayerMenu action=" + (joining ? "join" : "host") + " transport=" + _settings.Transport.Value);
        }

        private void Stop() { _server.Stop(); _client.Disconnect(); _error = null; }

        public void SetOpen(bool open)
        {
            if (_panel == null || _menu == null || open == IsOpen) return;
            if (open)
            {
                _buttonsWereActive = _menu.mmButtonsPanel.gameObject.activeSelf;
                _introWasActive = _menu.f2fintroPanel != null && _menu.f2fintroPanel.gameObject.activeSelf;
                _menu.mmButtonsPanel.gameObject.SetActive(false);
                if (_menu.f2fintroPanel != null) _menu.f2fintroPanel.gameObject.SetActive(false);
                _panel.SetActive(true);
                ShowCharacter(false);
                EventSystem.current?.SetSelectedGameObject(_lan.gameObject);
            }
            else
            {
                _draft.Discard(); _preview.Update(_draft.Value, false);
                _panel.SetActive(false); _menu.mmButtonsPanel.gameObject.SetActive(_buttonsWereActive);
                if (_menu.f2fintroPanel != null) _menu.f2fintroPanel.gameObject.SetActive(_introWasActive);
                EventSystem.current?.SetSelectedGameObject(_entry.gameObject);
            }
        }

        private Button CloneButton(Transform parent, string label, Action action)
        {
            // Woodbury uses LeanButton/LeanTransition, not Unity UI.Button. Copy only
            // typography and placement; cloning Lean's serialized actions can load a scene.
            var obj = new GameObject("Coop " + label, typeof(RectTransform)); obj.transform.SetParent(parent, false);
            var rect = (RectTransform)obj.transform; var source = (RectTransform)_template.transform;
            rect.anchorMin = source.anchorMin; rect.anchorMax = source.anchorMax; rect.pivot = source.pivot;
            rect.sizeDelta = source.sizeDelta; rect.anchoredPosition = source.anchoredPosition; rect.localScale = source.localScale;
            var graphic = AddLabel(obj.transform, label, 0, 0, 1, 1, 30);
            if (graphic is TMP_Text tmp && _labelTemplate is TMP_Text original)
            { tmp.fontSize = original.fontSize; tmp.enableAutoSizing = original.enableAutoSizing; tmp.fontSizeMin = original.fontSizeMin; tmp.fontSizeMax = original.fontSizeMax; tmp.alignment = original.alignment; }
            if (graphic is Text text && _labelTemplate is Text originalText)
            {
                text.fontSize = originalText.fontSize; text.alignment = originalText.alignment;
                text.resizeTextForBestFit = originalText.resizeTextForBestFit;
                text.resizeTextMinSize = originalText.resizeTextMinSize; text.resizeTextMaxSize = originalText.resizeTextMaxSize;
                text.horizontalOverflow = originalText.horizontalOverflow; text.verticalOverflow = originalText.verticalOverflow;
                text.lineSpacing = originalText.lineSpacing; text.material = originalText.material;
            }
            if (_labelTemplate.transform.parent == _template.transform)
            {
                var sourceLabel = (RectTransform)_labelTemplate.transform; var labelRect = (RectTransform)graphic.transform;
                labelRect.anchorMin = sourceLabel.anchorMin; labelRect.anchorMax = sourceLabel.anchorMax;
                labelRect.pivot = sourceLabel.pivot; labelRect.sizeDelta = sourceLabel.sizeDelta; labelRect.anchoredPosition = sourceLabel.anchoredPosition;
                labelRect.localScale = sourceLabel.localScale;
            }
            graphic.raycastTarget = true;
            var button = obj.AddComponent<Button>(); button.targetGraphic = graphic;
            var colors = button.colors; colors.normalColor = Color.white; colors.highlightedColor = new Color(0.7f,0.7f,0.7f,1f);
            colors.pressedColor = new Color(0.5f,0.5f,0.5f,1f); button.colors = colors;
            button.onClick = new Button.ButtonClickedEvent(); // Remove serialized native actions too.
            button.onClick.AddListener(() => action()); button.interactable = true;
            button.navigation = new Navigation { mode = Navigation.Mode.Automatic };
            SetText(obj, label); obj.SetActive(true); return button;
        }

        private Button AddButton(Transform parent, string label, float x, float y, float width, Action action)
        {
            var button = CloneButton(parent, label, action);
            Place((RectTransform)button.transform, x, y, width, 0.09f);
            foreach (var text in button.GetComponentsInChildren<Graphic>(true))
            {
                Style(text, 27);
                if ((text is Text || text is TMP_Text) && text.transform != button.transform)
                    Place((RectTransform)text.transform, 0f, 0f, 1f, 1f);
            }
            return button;
        }

        private Graphic AddLabel(Transform parent, string text, float x, float y, float width, float height, int size)
        {
            // Copy font/material settings without inheriting scene scripts or click handlers.
            var obj = new GameObject("Text " + text, typeof(RectTransform)); obj.transform.SetParent(parent, false);
            Graphic graphic;
            if (_labelTemplate is TMP_Text tmp)
            {
                var t = obj.AddComponent<TextMeshProUGUI>(); t.font = tmp.font; t.fontSharedMaterial = tmp.fontSharedMaterial;
                t.fontStyle = tmp.fontStyle; t.color = tmp.color; t.alignment = TextAlignmentOptions.MidlineLeft; graphic = t;
            }
            else
            {
                var source = (Text)_labelTemplate; var t = obj.AddComponent<Text>(); t.font = source.font;
                t.fontStyle = source.fontStyle; t.color = source.color; t.alignment = TextAnchor.MiddleLeft; graphic = t;
            }
            graphic.raycastTarget = false; Style(graphic, size); SetText(obj, text);
            Place((RectTransform)obj.transform, x, y, width, height); return graphic;
        }

        private Field AddField(Transform parent, string label, float y, string value, int limit)
        {
            var row = new GameObject(label, typeof(RectTransform)); row.transform.SetParent(parent, false);
            Place((RectTransform)row.transform, 0f, y, 0.96f, 0.09f);
            AddLabel(row.transform, label, 0f, 0f, 0.34f, 1f, 25);
            var box = new GameObject("Input", typeof(RectTransform), typeof(Image)); box.transform.SetParent(row.transform, false);
            Place((RectTransform)box.transform, 0.36f, 0f, 0.64f, 1f); box.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.08f);
            var text = AddLabel(box.transform, "", 0.025f, 0.05f, 0.95f, 0.9f, 26);
            var field = new Field { Root = row };
            if (text is TMP_Text tmp)
            {
                var input = box.AddComponent<TMP_InputField>(); input.textViewport = (RectTransform)box.transform;
                input.textComponent = tmp; input.targetGraphic = box.GetComponent<Image>(); input.characterLimit = limit;
                input.lineType = TMP_InputField.LineType.SingleLine; input.text = value;
                field.Read = () => input.text; field.Write = s => input.text = s;
            }
            else
            {
                var input = box.AddComponent<InputField>(); input.textComponent = (Text)text; input.targetGraphic = box.GetComponent<Image>();
                input.characterLimit = limit; input.lineType = InputField.LineType.SingleLine; input.text = value;
                field.Read = () => input.text; field.Write = s => input.text = s;
            }
            return field;
        }

        private static void Place(RectTransform rect, float x, float y, float width, float height)
        { rect.anchorMin = new Vector2(x, y); rect.anchorMax = new Vector2(x + width, y + height); rect.offsetMin = rect.offsetMax = Vector2.zero; rect.localScale = Vector3.one; }
        private static string Label(GameObject obj) => obj.GetComponentInChildren<TMP_Text>(true)?.text ?? obj.GetComponentInChildren<Text>(true)?.text ?? "";
        private static void SetText(GameObject obj, string value)
        { foreach (var t in obj.GetComponentsInChildren<TMP_Text>(true)) t.text = value; foreach (var t in obj.GetComponentsInChildren<Text>(true)) t.text = value; }
        private static void Style(Graphic g, int size)
        {
            if (g is TMP_Text tmp) { tmp.fontSize = size; tmp.enableAutoSizing = true; tmp.fontSizeMin = size * 0.65f; tmp.fontSizeMax = size; }
            if (g is Text text) { text.fontSize = size; text.resizeTextForBestFit = true; text.resizeTextMinSize = (int)(size * 0.65f); text.resizeTextMaxSize = size; }
        }

        private void Probe()
        {
            bool valid = _entry.onClick.GetPersistentEventCount() == 0 && _entry.transform.parent == _template.transform.parent;
            if (_entryAnchor != null) valid &= Vector3.Dot(Centre(_entryGraphic) - Centre(_anchorGraphic), _anchorGraphic.transform.up) > 0f;
            _entry.onClick.Invoke(); valid &= IsOpen && !_menu.mmButtonsPanel.gameObject.activeSelf;
            var old = _settings.Transport.Value;
            if (!_server.IsRunning && !_client.IsRunning)
            {
                _steam.onClick.Invoke(); valid &= _settings.Transport.Value == CoopTransport.Spacewar;
                _lan.onClick.Invoke(); valid &= _settings.Transport.Value == CoopTransport.Lan;
                _settings.Transport.Value = old;
            }
            string oldName = _name.Read(); _name.Write("Animation probe"); valid &= _name.Read() == "Animation probe"; _name.Write(oldName);
            _tabCharacter.onClick.Invoke(); valid &= _characterPage.activeSelf;
            var savedProfile = _draft.Value; CycleCharacter(4, 1); valid &= _draft.Changed;
            _draft.Discard(); RefreshCharacter(); valid &= _draft.Value.Equals(savedProfile) && !_draft.Changed;
            string oldAppearance = _settings.CharacterAppearance.Value;
            try
            {
                CycleCharacter(4, 1); var applied = _draft.Value;
                _applyCharacter.onClick.Invoke();
                valid &= !_draft.Changed && _settings.CharacterAppearance.Value == applied.ToString();
                _tabCoop.onClick.Invoke(); _tabCharacter.onClick.Invoke(); valid &= _draft.Value.Equals(applied);
                CycleCharacter(0, 1); _back.onClick.Invoke(); _entry.onClick.Invoke(); _tabCharacter.onClick.Invoke();
                valid &= _draft.Value.Equals(applied) && !_draft.Changed;
            }
            finally { _settings.CharacterAppearance.Value = oldAppearance; }
            _log?.Invoke("CharacterMenu probe=" + (valid ? "PASS" : "FAIL") + " draft/discard/apply/reopen/close scene=MainMenu");
            _tabCoop.onClick.Invoke(); valid &= _coopPage.activeSelf;
            _back.onClick.Invoke(); valid &= !IsOpen && _menu.mmButtonsPanel.gameObject.activeSelf == _buttonsWereActive;
            _entry.onClick.Invoke(); _back.onClick.Invoke(); valid &= !IsOpen;
            _log?.Invoke("NativeMultiplayerMenu probe=" + (valid ? "PASS" : "FAIL") + " entry/open/back/input/reopen scene=MainMenu");
            if (valid && Environment.GetEnvironmentVariable("WSS_MENU_CONNECT") == "1" && !_server.IsRunning && !_client.IsRunning)
            {
                // Exercise the real native button actions instead of launcher auto-start.
                if (_settings.ModeSetting.Value == Mode.CoopClient) _join.onClick.Invoke(); else _host.onClick.Invoke();
                _log?.Invoke("NativeMultiplayerMenu connection=" + (_client.IsRunning || _server.IsRunning ? "PASS" : "FAIL") + " mode=" + _settings.ModeSetting.Value + " error=" + _error);
            }
        }

        public void Dispose()
        {
            if (IsOpen) SetOpen(false);
            _preview.Dispose();
            if (_entry != null) UnityEngine.Object.Destroy(_entry.gameObject); if (_panel != null) UnityEngine.Object.Destroy(_panel);
        }
    }
}
