using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Menü beim Spielstart: Spieler wählt pro Körperteil genau einen Move.
/// Script auf ein leeres GameObject legen – Canvas wird automatisch hinzugefügt.
///
/// SETUP: fightInputManager und allMoves im Inspector zuweisen.
/// </summary>
public class MoveSelectionMenu : MonoBehaviour
{
    [Header("Referenzen")]
    [SerializeField] private FightInputManager fightInputManager;

    [Header("Alle verfügbaren Moves")]
    [Tooltip("Alle MoveData-Assets die im Menü zur Auswahl stehen sollen.")]
    [SerializeField] private MoveData[] allMoves;

    [Header("Darstellung")]
    [SerializeField] private Font uiFont;

    // --- Farben ---------------------------------------------------------------
    private static readonly Color C_Overlay    = new Color(0.05f, 0.05f, 0.05f, 0.97f);
    private static readonly Color C_Panel      = new Color(0.12f, 0.12f, 0.14f, 1f);
    private static readonly Color C_Column     = new Color(0.16f, 0.16f, 0.20f, 1f);
    private static readonly Color C_Header     = new Color(0.22f, 0.22f, 0.30f, 1f);
    private static readonly Color C_Unselected = new Color(0.28f, 0.28f, 0.34f, 1f);
    private static readonly Color C_Selected   = new Color(0.18f, 0.58f, 0.22f, 1f);
    private static readonly Color C_Start      = new Color(0.75f, 0.28f, 0.10f, 1f);
    private static readonly Color C_Text       = new Color(0.92f, 0.92f, 0.92f, 1f);

    // --- State ----------------------------------------------------------------
    // Pro Körperteil: welcher Move ist gerade gewählt
    private readonly Dictionary<BodyPart, MoveData>  selection = new Dictionary<BodyPart, MoveData>();
    // Pro (Körperteil, Move): der zugehörige Button
    private readonly Dictionary<(BodyPart, MoveData), Button> buttons =
        new Dictionary<(BodyPart, MoveData), Button>();

    // -------------------------------------------------------------------------

    private void Awake()
    {
        foreach (BodyPart bp in System.Enum.GetValues(typeof(BodyPart)))
            selection[bp] = null;

        // Falls kein Move manuell zugewiesen: automatisch aus Resources/Moves laden
        if (allMoves == null || allMoves.Length == 0)
        {
            allMoves = Resources.LoadAll<MoveData>("Moves");

            if (allMoves.Length == 0)
                Debug.LogWarning("[MoveSelectionMenu] Keine Moves gefunden. " +
                    "Entweder MoveData-Assets im Inspector unter 'All Moves' eintragen, " +
                    "oder in den Ordner Assets/Resources/Moves/ legen.");
        }

        EnsureCanvas();
        EnsureEventSystem();
        Time.timeScale = 0f;
        BuildUI();
    }

    private void EnsureEventSystem()
    {
        if (FindObjectOfType<EventSystem>() == null)
        {
            GameObject es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<StandaloneInputModule>();
        }
    }

    private void EnsureCanvas()
    {
        if (GetComponent<Canvas>() == null)
        {
            Canvas c = gameObject.AddComponent<Canvas>();
            c.renderMode   = RenderMode.ScreenSpaceOverlay;
            c.sortingOrder = 100;
        }

        if (GetComponent<CanvasScaler>() == null)
        {
            CanvasScaler cs = gameObject.AddComponent<CanvasScaler>();
            cs.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            cs.referenceResolution = new Vector2(1920, 1080);
            cs.matchWidthOrHeight  = 0.5f;
        }

        if (GetComponent<GraphicRaycaster>() == null)
            gameObject.AddComponent<GraphicRaycaster>();
    }

    // -------------------------------------------------------------------------
    // UI aufbauen
    // -------------------------------------------------------------------------

    private void BuildUI()
    {
        GameObject overlay = MakeImage(transform, "Overlay", C_Overlay);
        Stretch(overlay);

        GameObject panel = MakeImage(overlay.transform, "Panel", C_Panel);
        RectTransform panelRT = panel.GetComponent<RectTransform>();
        panelRT.anchorMin = new Vector2(0.04f, 0.04f);
        panelRT.anchorMax = new Vector2(0.96f, 0.96f);
        panelRT.offsetMin = panelRT.offsetMax = Vector2.zero;

        VerticalLayoutGroup vl = panel.AddComponent<VerticalLayoutGroup>();
        vl.padding               = new RectOffset(24, 24, 24, 24);
        vl.spacing               = 18;
        vl.childForceExpandWidth  = true;
        vl.childForceExpandHeight = false;
        vl.childControlWidth  = true;
        vl.childControlHeight = true;

        // Titel
        FixedHeight(MakeText(panel.transform, "Wähle deine Moves", 30, FontStyle.Bold), 52f);
        FixedHeight(MakeImage(panel.transform, "Line", C_Header), 2f);

        // Untertitel
        FixedHeight(MakeText(panel.transform,
            "Wähle pro Körperteil genau einen Move  |  J = Linke Hand  ·  L = Rechte Hand  ·  K = Linkes Bein  ·  I = Rechtes Bein",
            13, FontStyle.Italic), 28f);

        // Spalten
        GameObject row = new GameObject("Columns", typeof(RectTransform));
        row.transform.SetParent(panel.transform, false);
        FixedHeight(row, 500f);

        HorizontalLayoutGroup hl = row.AddComponent<HorizontalLayoutGroup>();
        hl.spacing               = 14;
        hl.childForceExpandWidth  = true;
        hl.childForceExpandHeight = true;
        hl.childControlWidth  = true;
        hl.childControlHeight = true;

        BuildColumn(row.transform, BodyPart.L_Hand, "Linke Hand  [J]");
        BuildColumn(row.transform, BodyPart.R_Hand, "Rechte Hand  [L]");
        BuildColumn(row.transform, BodyPart.L_Leg,  "Linkes Bein  [K]");
        BuildColumn(row.transform, BodyPart.R_Leg,  "Rechtes Bein  [I]");

        FixedHeight(MakeImage(panel.transform, "Line2", C_Header), 2f);

        // Start-Button
        FixedHeight(MakeButton(panel.transform, "⚔  Kampf starten", C_Start, OnStartClicked, 22, FontStyle.Bold), 64f);
    }

    private void BuildColumn(Transform parent, BodyPart bodyPart, string label)
    {
        // Äußere Spalte: Header + ScrollRect untereinander
        GameObject col = MakeImage(parent, $"Col_{bodyPart}", C_Column);
        VerticalLayoutGroup colVL = col.AddComponent<VerticalLayoutGroup>();
        colVL.padding               = new RectOffset(10, 10, 10, 10);
        colVL.spacing               = 8;
        colVL.childForceExpandWidth  = true;
        colVL.childForceExpandHeight = false;
        colVL.childControlWidth  = true;
        colVL.childControlHeight = true;

        // Kopfzeile
        GameObject header = MakeImage(col.transform, "Header", C_Header);
        FixedHeight(header, 40f);
        Stretch(MakeText(header.transform, label, 15, FontStyle.Bold));

        // ScrollRect
        GameObject scrollGO = new GameObject("Scroll", typeof(RectTransform), typeof(ScrollRect));
        scrollGO.transform.SetParent(col.transform, false);
        LayoutElement scrollLE = scrollGO.AddComponent<LayoutElement>();
        scrollLE.minHeight      = 50f;
        scrollLE.flexibleHeight = 1f;

        ScrollRect scroll = scrollGO.GetComponent<ScrollRect>();
        scroll.horizontal        = false;
        scroll.vertical          = true;
        scroll.scrollSensitivity = 30f;
        scroll.movementType      = ScrollRect.MovementType.Clamped;

        // Viewport – RectMask2D clippt ohne Image-Komponente
        GameObject viewport = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D));
        viewport.transform.SetParent(scrollGO.transform, false);
        RectTransform vpRT = viewport.GetComponent<RectTransform>();
        vpRT.anchorMin = Vector2.zero;
        vpRT.anchorMax = Vector2.one;
        vpRT.offsetMin = vpRT.offsetMax = Vector2.zero;
        scroll.viewport = vpRT;

        // Content – wächst nach unten mit den Buttons
        GameObject content = new GameObject("Content", typeof(RectTransform));
        content.transform.SetParent(viewport.transform, false);
        RectTransform contentRT = content.GetComponent<RectTransform>();
        contentRT.anchorMin = new Vector2(0f, 1f);
        contentRT.anchorMax = new Vector2(1f, 1f);
        contentRT.pivot     = new Vector2(0.5f, 1f);
        contentRT.sizeDelta = Vector2.zero;
        scroll.content = contentRT;

        VerticalLayoutGroup contentVL = content.AddComponent<VerticalLayoutGroup>();
        contentVL.padding               = new RectOffset(4, 4, 4, 4);
        contentVL.spacing               = 8;
        contentVL.childForceExpandWidth  = true;
        contentVL.childForceExpandHeight = false;
        contentVL.childControlWidth  = true;
        contentVL.childControlHeight = true;

        content.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // Move-Buttons in den Content
        bool anyMove = false;

        if (allMoves != null)
        {
            foreach (MoveData move in allMoves)
            {
                if (move == null || move.bodyPart != bodyPart) continue;

                anyMove = true;
                MoveData capturedMove = move;
                BodyPart capturedPart = bodyPart;
                string   displayName  = string.IsNullOrEmpty(move.moveName) ? move.name : move.moveName;

                GameObject btn = MakeButton(content.transform, displayName, C_Unselected,
                    () => Select(capturedPart, capturedMove), 14, FontStyle.Normal);
                FixedHeight(btn, 46f);
                buttons[(bodyPart, move)] = btn.GetComponent<Button>();
            }
        }

        if (!anyMove)
        {
            GameObject empty = MakeText(content.transform, "– keine Moves –", 13, FontStyle.Italic);
            FixedHeight(empty, 36f);
            empty.GetComponent<Text>().color = new Color(0.5f, 0.5f, 0.5f);
        }
    }

    // -------------------------------------------------------------------------
    // Interaktion
    // -------------------------------------------------------------------------

    private void Select(BodyPart bodyPart, MoveData move)
    {
        // Vorherige Auswahl in dieser Spalte zurücksetzen
        MoveData previous = selection[bodyPart];
        if (previous != null && buttons.TryGetValue((bodyPart, previous), out Button prevBtn))
            SetBtnColor(prevBtn, C_Unselected);

        // Neuen Move auswählen (zweites Klicken desselben Moves hebt Auswahl auf)
        if (previous == move)
        {
            selection[bodyPart] = null;
            return;
        }

        selection[bodyPart] = move;
        SetBtnColor(buttons[(bodyPart, move)], C_Selected);
    }

    private void OnStartClicked()
    {
        fightInputManager.Configure(
            selection[BodyPart.L_Hand],
            selection[BodyPart.R_Hand],
            selection[BodyPart.L_Leg],
            selection[BodyPart.R_Leg]
        );

        Time.timeScale = 1f;
        Destroy(gameObject);
    }

    // -------------------------------------------------------------------------
    // UI-Helfer
    // -------------------------------------------------------------------------

    private Font ResolveFont()
    {
        if (uiFont != null) return uiFont;
        Font f = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        return f != null ? f : Resources.GetBuiltinResource<Font>("Arial.ttf");
    }

    private GameObject MakeImage(Transform parent, string name, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        go.GetComponent<Image>().color = color;
        return go;
    }

    private GameObject MakeText(Transform parent, string content, int size, FontStyle style)
    {
        GameObject go = new GameObject("Text", typeof(RectTransform), typeof(Text));
        go.transform.SetParent(parent, false);
        Text t   = go.GetComponent<Text>();
        t.text      = content;
        t.fontSize  = size;
        t.fontStyle = style;
        t.color     = C_Text;
        t.font      = ResolveFont();
        t.alignment = TextAnchor.MiddleCenter;
        return go;
    }

    private GameObject MakeButton(Transform parent, string label, Color color,
        UnityEngine.Events.UnityAction onClick, int fontSize, FontStyle style)
    {
        GameObject go = new GameObject("Button", typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);

        Image img = go.GetComponent<Image>();
        img.color = color;

        Button btn = go.GetComponent<Button>();
        btn.targetGraphic = img;
        ApplyColorBlock(btn, color);
        btn.onClick.AddListener(onClick);

        Stretch(MakeText(go.transform, label, fontSize, style));
        return go;
    }

    private void SetBtnColor(Button btn, Color color)
    {
        btn.GetComponent<Image>().color = color;
        ApplyColorBlock(btn, color);
    }

    private void ApplyColorBlock(Button btn, Color base_)
    {
        ColorBlock cb        = btn.colors;
        cb.normalColor       = base_;
        cb.highlightedColor  = Color.Lerp(base_, Color.white, 0.25f);
        cb.pressedColor      = Color.Lerp(base_, Color.black, 0.25f);
        cb.selectedColor     = base_;
        btn.colors           = cb;
    }

    private void Stretch(GameObject go)
    {
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }

    private void FixedHeight(GameObject go, float h)
    {
        LayoutElement le = go.GetComponent<LayoutElement>() ?? go.AddComponent<LayoutElement>();
        le.minHeight       = h;
        le.preferredHeight = h;

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(rt.sizeDelta.x, h);
    }
}
