using UnityEngine;

namespace ThreeCDemo
{
    public readonly struct AttackRequest
    {
        public string Label { get; }
        public Element Element { get; }
        public int Damage { get; }
        public float Radius { get; }
        public Color PulseColor { get; }

        public AttackRequest(string label, Element element, int damage, float radius, Color pulseColor)
        {
            Label = label;
            Element = element;
            Damage = damage;
            Radius = radius;
            PulseColor = pulseColor;
        }
    }

    public interface IAttackExecutor
    {
        string Feedback { get; }
        void Execute(AttackRequest request, double now);
        void SetMessage(string message);
    }

    public interface IPlayerAttackView
    {
        void ShowPulse(Vector3 position, float radius, Color color, double now);
        void TickPulse(double now);
        void SetChargeProgress(float progress);
        void HidePulse();
    }

    public sealed class PlayerAttackExecutor : IAttackExecutor
    {
        private readonly IAttackTarget target;
        private readonly IPositionSource origin;
        private readonly IPlayerAttackView view;
        private string feedback = "Move toward the ground-type training target.";

        public PlayerAttackExecutor(IAttackTarget target, IPositionSource origin, IPlayerAttackView view)
        {
            this.target = target;
            this.origin = origin;
            this.view = view;
        }

        public string Feedback => feedback;

        public void Execute(AttackRequest request, double now)
        {
            Vector3 offset = target.Position - origin.Position;
            offset.y = 0;
            if (offset.sqrMagnitude > request.Radius * request.Radius)
                feedback = request.Label + "  /  Out of range";
            else if (target.Hp == 0)
                feedback = request.Label + "  /  Target depleted. Press R to reset.";
            else
            {
                int applied = target.Hit(request.Element, request.Damage);
                feedback = applied == 0
                    ? request.Label + "  /  Immune: Electric vs Ground"
                    : request.Label + "  /  " + applied + " damage";
            }

            view.ShowPulse(origin.Position, request.Radius, request.PulseColor, now);
        }

        public void SetMessage(string message) => feedback = message;
    }

    public sealed class PlayerAttackView : IPlayerAttackView
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

        private readonly Transform pulse;
        private readonly Renderer pulseRenderer;
        private readonly Material[] chargeMaterials;
        private readonly MaterialPropertyBlock properties = new MaterialPropertyBlock();
        private double pulseStarted;
        private float pulseRadius;
        private Color pulseColor;

        public PlayerAttackView(Transform pulse, Renderer pulseRenderer, Material[] chargeMaterials)
        {
            this.pulse = pulse;
            this.pulseRenderer = pulseRenderer;
            this.chargeMaterials = chargeMaterials;
        }

        public void ShowPulse(Vector3 position, float radius, Color color, double now)
        {
            pulse.position = new Vector3(position.x, .04f, position.z);
            pulseStarted = now;
            pulseRadius = radius;
            pulseColor = color;
            pulse.gameObject.SetActive(true);
        }

        public void TickPulse(double now)
        {
            float progress = (float)((now - pulseStarted) / .42);
            if (pulse.gameObject.activeSelf && progress >= 1) pulse.gameObject.SetActive(false);
            if (!pulse.gameObject.activeSelf) return;

            float scale = Mathf.Lerp(.35f, pulseRadius * 2, Mathf.Sqrt(progress));
            pulse.localScale = new Vector3(scale, 1, scale);
            Tint(pulseRenderer, Color.Lerp(pulseColor, pulseColor * .45f, progress));
        }

        public void SetChargeProgress(float progress)
        {
            Color color = Color.Lerp(new Color(.18f, .92f, .86f), new Color(.95f, .91f, .27f), progress);
            if (chargeMaterials == null) return;

            foreach (Material material in chargeMaterials)
            {
                if (material == null) continue;
                material.SetColor(BaseColorId, color);
                material.SetColor(EmissionColorId, color * .5f);
            }
        }

        public void HidePulse() => pulse.gameObject.SetActive(false);

        private void Tint(Renderer renderer, Color color)
        {
            properties.Clear();
            properties.SetColor(BaseColorId, color);
            renderer.SetPropertyBlock(properties);
        }
    }
}
