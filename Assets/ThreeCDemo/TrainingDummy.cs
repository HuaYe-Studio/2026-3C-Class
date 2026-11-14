using UnityEngine;

namespace ThreeCDemo
{
    public interface IAttackTarget
    {
        Vector3 Position { get; }
        int Hp { get; }
        int MaxHp { get; }
        int Hit(Element element, int damage);
        void ResetTarget();
    }

    public sealed class TrainingDummy : MonoBehaviour, IAttackTarget
    {
        [SerializeField] private Renderer indicator;
        private readonly Health health = new Health(new CharacterAttributes(Element.Ground, 10000));
        private readonly Color baseColor = new Color(1f, .48f, .24f);
        private MaterialPropertyBlock properties;
        private double flashUntil;

        public Vector3 Position => transform.position;
        public int Hp => health.Hp;
        public int MaxHp => health.MaxHp;

        private void Awake() => properties = new MaterialPropertyBlock();

        public int Hit(Element element, int damage)
        {
            int applied = DamageRules.ApplyMove(element, damage, health);
            flashUntil = Time.timeAsDouble + .13;
            return applied;
        }

        public void ResetTarget()
        {
            health.Reset();
            flashUntil = 0;
        }

        private void Update()
        {
            Color color = Hp == 0 ? Color.gray : baseColor;
            if (Time.timeAsDouble < flashUntil) color = Color.white;
            properties.SetColor("_BaseColor", color);
            indicator.SetPropertyBlock(properties);
        }
    }
}
