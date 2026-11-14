using UnityEngine;

namespace ThreeCDemo
{
    public readonly struct PlayerStatus
    {
        public CombatState State { get; }
        public bool Overview { get; }
        public string Feedback { get; }
        public int Hp { get; }
        public int MaxHp { get; }
        public int ComboStep { get; }
        public ComboActionType ComboAction { get; }
        public string ComboTrace { get; }
        public float ChargeProgress { get; }
        public float BufferProgress { get; }

        public PlayerStatus(
            CombatState state,
            bool overview,
            string feedback,
            int hp,
            int maxHp,
            int comboStep,
            ComboActionType comboAction,
            string comboTrace,
            float chargeProgress,
            float bufferProgress)
        {
            State = state;
            Overview = overview;
            Feedback = feedback;
            Hp = hp;
            MaxHp = maxHp;
            ComboStep = comboStep;
            ComboAction = comboAction;
            ComboTrace = comboTrace;
            ChargeProgress = chargeProgress;
            BufferProgress = bufferProgress;
        }
    }

    public sealed class PlayerHudView
    {
        private GUIStyle textStyle;
        private GUIStyle boldStyle;

        public void Draw(PlayerStatus status)
        {
            textStyle ??= NewLabelStyle(FontStyle.Normal);
            boldStyle ??= NewLabelStyle(FontStyle.Bold);
            Matrix4x4 previousMatrix = GUI.matrix;
            float scale = Mathf.Max(.52f, Mathf.Min(1.25f, Mathf.Min(Screen.width / 980f, Screen.height / 610f)));
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1));
            float width = Screen.width / scale;
            float height = Screen.height / scale;
            Fill(new Rect(20, 20, 326, 170), new Color(.04f, .07f, .12f, .94f));
            Fill(new Rect(20, 20, 3, 170), new Color(.22f, .91f, .82f));
            Label(36, 28, 300, "GAMEPLAY 3C  /  TRAINING LAB", 15, true);
            Label(36, 56, 300, $"State  {status.State}    |    Camera  {(status.Overview ? "Overview" : "Follow")}", 14);
            Label(36, 82, 300, $"Ground target  {status.Hp} / {status.MaxHp} HP", 14);
            Bar(36, 108, 290, (float)status.Hp / status.MaxHp, new Color(1, .54f, .25f));
            Label(36, 126, 300, status.Feedback, 12);
            Label(36, 151, 300,
                $"Combo  {status.ComboStep}/{PlayerCombatStateMachine.ComboLength}   {ActionLabel(status.ComboAction)}   [{status.ComboTrace}]", 12);

            float panelX = width - 300;
            Fill(new Rect(panelX, 20, 280, 94), new Color(.04f, .07f, .12f, .94f));
            Label(panelX + 12, 26, 252, "Q CHARGE  /  ELECTRIC", 12, true);
            Bar(panelX + 12, 49, 255, status.ChargeProgress,
                Color.Lerp(new Color(.3f, .9f, 1), new Color(.95f, .91f, .27f), status.ChargeProgress));
            Label(panelX + 12, 66, 252, "ENHANCED SKILL BUFFER", 12, true);
            Bar(panelX + 12, 89, 255, status.BufferProgress, new Color(.24f, .9f, .82f));

            Fill(new Rect(20, height - 77, width - 40, 56), new Color(.04f, .07f, .12f, .94f));
            Label(34, height - 74, width - 65,
                "WASD  Move     E chain E1-E3     LMB chain A1-A3     E-E-A / A-E-A  finishers", 13, true);
            Label(34, height - 50, width - 65,
                "Chain in finisher recovery: E-E-A / A-E-A     Q  Tap / Hold     Shift  Dodge     C  Camera     R  Reset", 13);
            GUI.matrix = previousMatrix;
        }

        private static string ActionLabel(ComboActionType action) => action switch
        {
            ComboActionType.AttackA => "A chain",
            ComboActionType.AttackE => "E chain",
            ComboActionType.DiDong_AEA => "Ground Quake",
            ComboActionType.ShanYao_EEA => "Mountain Shake",
            _ => "none",
        };

        private static void Bar(float x, float y, float width, float fraction, Color color)
        {
            Fill(new Rect(x, y, width, 5), new Color(.2f, .25f, .33f));
            Fill(new Rect(x, y, width * Mathf.Clamp01(fraction), 5), color);
        }

        private static void Fill(Rect rect, Color color)
        {
            Color before = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = before;
        }

        private static GUIStyle NewLabelStyle(FontStyle weight)
        {
            var style = new GUIStyle(GUI.skin.label) { fontStyle = weight };
            style.normal.textColor = new Color(.9f, .96f, 1);
            return style;
        }

        private void Label(float x, float y, float width, string text, int size, bool bold = false)
        {
            GUIStyle style = bold ? boldStyle : textStyle;
            style.fontSize = size;
            GUI.Label(new Rect(x, y, width, 25), text, style);
        }
    }
}
