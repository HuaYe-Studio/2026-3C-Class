using System;
using System.Collections.Generic;
using Cinemachine;
using UnityEngine;
using UnityEngine.Serialization;

namespace ThreeCDemo
{
    [RequireComponent(typeof(PlayerInputReader), typeof(CharacterController))]
    public sealed class PlayerController : MonoBehaviour
    {
        [SerializeField] private TrainingDummy target;
        [SerializeField] private CinemachineVirtualCamera followCamera;
        [SerializeField] private CinemachineVirtualCamera overviewCamera;
        [SerializeField] private Transform pulse;
        [SerializeField] private Renderer pulseRenderer;
        [SerializeField] private Renderer visor;
        [SerializeField] private float moveSpeed = 5f;
        [SerializeField] private float turnSharpness = 10f;
        [SerializeField] private float attackSeconds = .25f;
        [SerializeField] private float recoverySeconds = .55f;
        [SerializeField, Min(0f)] private float finisherRecoverySeconds = .9f;
        [FormerlySerializedAs("bufferSeconds")]
        [SerializeField] private float comboBufferSeconds = .2f;
        [SerializeField] private float chargeSeconds = .3f;
        [SerializeField] private float dodgeSeconds = .4f;
        [SerializeField] private float iFrameSeconds = .2f;
        [SerializeField] private float dodgeStartSpeed = 12f;
        [SerializeField] private LayerMask dodgeThroughLayers;

        private IPlayerInputSource input;
        private CharacterController body;
        private IClock clock;
        private IAttackTarget attackTarget;
        private PlayerHealth health;
        private Collider targetCollider;
        private Material[] chargeMaterials;
        private readonly HashSet<Collider> ignoredColliders = new HashSet<Collider>();
        private bool ignoringDodgeLayers;
        private PlayerMotionController motion;
        private PlayerCombatController combat;
        private IPlayerAttackView attackView;
        private PlayerCameraRig cameraRig;
        private PlayerHudView hud;
        private Vector3 spawn;
        private Quaternion spawnRotation;

        public int AttacksExecuted => combat.AttacksExecuted;
        public CombatState CurrentState => combat.CurrentState;
        public int ComboStep => combat.ComboStep;
        public ComboActionType CurrentAction => combat.CurrentAction;
        public bool IsInvulnerable => combat.IsInvulnerable;
        public int Hp => health.Hp;
        public int MaxHp => health.MaxHp;

        public int TakeDamage(Element element, int damage) => health.TakeDamage(element, damage, clock.Now);

        public event Action PerfectDodge
        {
            add => health.PerfectDodge += value;
            remove => health.PerfectDodge -= value;
        }

        private void Awake()
        {
            input = GetComponent<PlayerInputReader>();
            body = GetComponent<CharacterController>();
            clock = new UnityClock();
            attackTarget = target;
            targetCollider = target.GetComponent<Collider>();

            motion = new PlayerMotionController(
                input,
                new CharacterControllerMover(body),
                new TransformRotator(transform),
                moveSpeed,
                turnSharpness,
                dodgeStartSpeed);

            var state = new PlayerCombatStateMachine(
                attackSeconds, recoverySeconds, dodgeSeconds, iFrameSeconds, finisherRecoverySeconds);
            health = new PlayerHealth(state);
            var comboBuffer = new CombatActionBuffer(state, comboBufferSeconds);
            var press = new AttackPressInterpreter(chargeSeconds);

            chargeMaterials = CreateChargeMaterials();
            attackView = new PlayerAttackView(pulse, pulseRenderer, chargeMaterials);
            var executor = new PlayerAttackExecutor(attackTarget, new TransformPositionSource(transform), attackView);
            combat = new PlayerCombatController(input, clock, state, comboBuffer, press, executor);
            cameraRig = new PlayerCameraRig(overviewCamera);
            hud = new PlayerHudView();

            spawn = transform.position;
            spawnRotation = transform.rotation;
        }

        private void OnEnable()
        {
            combat.DodgeStarted += motion.BeginDodge;
            combat.Enable();
            input.CameraToggled += ToggleCamera;
            input.ResetPressed += ResetDemo;
        }

        private void OnDisable()
        {
            input.CameraToggled -= ToggleCamera;
            input.ResetPressed -= ResetDemo;
            combat.DodgeStarted -= motion.BeginDodge;
            combat.Disable();
            EndDodgeCollision();
            ignoringDodgeLayers = false;
        }

        private void Update()
        {
            double now = clock.Now;
            combat.Tick(now, input.MoveInput.sqrMagnitude > .01f);
            SyncDodgeCollision();

            if (combat.CurrentState == CombatState.Dodging)
                motion.TickDodge(clock.DeltaTime, combat.GetStateNormalizedTime(now));
            else if (combat.CurrentState == CombatState.Moving)
                motion.Tick(clock.DeltaTime);

            attackView.TickPulse(now);
            attackView.SetChargeProgress(combat.ChargeProgress);
        }

        private Material[] CreateChargeMaterials()
        {
            Transform model = transform.Find("TrainingRobot");
            if (model == null) model = transform;

            var renderers = new HashSet<MeshRenderer>();
            if (visor is MeshRenderer visorRenderer) renderers.Add(visorRenderer);
            foreach (MeshRenderer renderer in model.GetComponentsInChildren<MeshRenderer>(true))
                renderers.Add(renderer);

            var materials = new List<Material>();
            foreach (MeshRenderer renderer in renderers)
            {
                Material[] slots = renderer.sharedMaterials;
                bool changed = false;
                for (int i = 0; i < slots.Length; i++)
                {
                    Material material = slots[i];
                    if (material == null || !material.name.StartsWith("Signal")) continue;

                    Material instance = new Material(material);
                    slots[i] = instance;
                    materials.Add(instance);
                    changed = true;
                }
                if (changed) renderer.sharedMaterials = slots;
            }
            return materials.ToArray();
        }

        private void OnDestroy()
        {
            if (chargeMaterials == null) return;
            foreach (Material material in chargeMaterials)
                if (material != null) Destroy(material);
        }

        private void SyncDodgeCollision()
        {
            bool dodging = combat.CurrentState == CombatState.Dodging;
            if (dodging == ignoringDodgeLayers) return;

            ignoringDodgeLayers = dodging;
            if (dodging)
                BeginDodgeCollision();
            else
                EndDodgeCollision();
        }

        private void BeginDodgeCollision()
        {
            int mask = dodgeThroughLayers.value;
            if (mask == 0)
            {
                int enemyLayer = LayerMask.NameToLayer("Enemy");
                if (enemyLayer >= 0) mask = 1 << enemyLayer;
            }

            if (mask != 0)
            {
                // 正式项目可通过物理层配置或缓存目标 Collider，避免每次闪避都搜索场景。
                foreach (Collider collider in FindObjectsByType<Collider>(FindObjectsSortMode.None))
                {
                    if (collider == body || !collider.enabled) continue;
                    if ((mask & (1 << collider.gameObject.layer)) == 0) continue;
                    IgnoreCollision(collider);
                }
            }

            if (targetCollider != null)
                IgnoreCollision(targetCollider);
        }

        private void IgnoreCollision(Collider collider)
        {
            if (ignoredColliders.Add(collider))
                Physics.IgnoreCollision(body, collider, true);
        }

        private void EndDodgeCollision()
        {
            foreach (Collider collider in ignoredColliders)
            {
                if (collider != null)
                    Physics.IgnoreCollision(body, collider, false);
            }
            ignoredColliders.Clear();
        }

        private void ToggleCamera() => cameraRig.Toggle();

        private void ResetDemo()
        {
            combat.Reset();
            SyncDodgeCollision();
            health.Reset();
            body.enabled = false;
            Vector3 offset = spawn - transform.position;
            transform.SetPositionAndRotation(spawn, spawnRotation);
            body.enabled = true;
            followCamera.OnTargetObjectWarped(transform, offset);
            attackTarget.ResetTarget();
            attackView.HidePulse();
            combat.SetMessage("Training reset");
        }

        private void OnGUI()
        {
            hud.Draw(new PlayerStatus(
                combat.CurrentState,
                cameraRig.Overview,
                combat.Feedback,
                attackTarget.Hp,
                attackTarget.MaxHp,
                combat.ComboStep,
                combat.CurrentAction,
                combat.ComboTrace,
                combat.ChargeProgress,
                combat.BufferProgress));
        }
    }
}
