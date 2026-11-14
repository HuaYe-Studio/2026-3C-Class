using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ThreeCDemo
{
    public interface IMoveInput
    {
        Vector2 MoveInput { get; }
    }

    public interface ICombatInputSource
    {
        event Action<CombatInput> CombatInputPressed;
        event Action<double> ChargeAttackPressed;
        event Action<double> ChargeAttackReleased;
    }

    public interface ISystemInputSource
    {
        event Action CameraToggled;
        event Action ResetPressed;
    }

    public interface IPlayerInputSource : IMoveInput, ICombatInputSource, ISystemInputSource
    {
    }

    public sealed class PlayerInputReader : MonoBehaviour, IPlayerInputSource
    {
        [SerializeField] private InputActionAsset actions;
        private InputActionAsset instance;
        private InputActionMap player;
        private InputAction move;
        private InputAction skill;
        private InputAction attack;
        private InputAction dodge;
        private InputAction charge;
        private InputAction cameraToggle;
        private InputAction reset;
        private Vector2 cachedMoveInput;
        private int moveInputFrame = -1;

        public Vector2 MoveInput
        {
            get
            {
            #if UNITY_EDITOR
                if (TestMoveInput.HasValue) return TestMoveInput.Value;
            #endif
                if (moveInputFrame != Time.frameCount)
                {
                    cachedMoveInput = move.ReadValue<Vector2>();
                    moveInputFrame = Time.frameCount;
                }
                return cachedMoveInput;
            }
        }
        public event Action<CombatInput> CombatInputPressed;
        public event Action<double> ChargeAttackPressed;
        public event Action<double> ChargeAttackReleased;
        public event Action CameraToggled;
        public event Action ResetPressed;

        private void Awake()
        {
            instance = Instantiate(actions);
            player = instance.FindActionMap("Player", true);
            move = player.FindAction("Move", true);
            skill = player.FindAction("Skill", true);
            attack = player.FindAction("Attack", true);
            dodge = player.FindAction("Dodge", true);
            charge = player.FindAction("ChargeAttack", true);
            cameraToggle = player.FindAction("Camera", true);
            reset = player.FindAction("Reset", true);
        }

        private void OnEnable()
        {
            moveInputFrame = -1;
            skill.performed += OnSkill;
            attack.performed += OnAttack;
            dodge.performed += OnDodge;
            charge.performed += OnCharge;
            charge.canceled += OnRelease;
            cameraToggle.performed += OnCamera;
            reset.performed += OnReset;
            player.Enable();
        }

        private void OnDisable()
        {
            skill.performed -= OnSkill;
            attack.performed -= OnAttack;
            dodge.performed -= OnDodge;
            charge.performed -= OnCharge;
            charge.canceled -= OnRelease;
            cameraToggle.performed -= OnCamera;
            reset.performed -= OnReset;
            player.Disable();
        }

        private void OnDestroy() => Destroy(instance);
        private void OnSkill(InputAction.CallbackContext _) => CombatInputPressed?.Invoke(CombatInput.Skill);
        private void OnAttack(InputAction.CallbackContext _) => CombatInputPressed?.Invoke(CombatInput.Attack);
        private void OnDodge(InputAction.CallbackContext _) => CombatInputPressed?.Invoke(CombatInput.Dodge);
        private void OnCharge(InputAction.CallbackContext _) => ChargeAttackPressed?.Invoke(Time.timeAsDouble);
        private void OnRelease(InputAction.CallbackContext _) => ChargeAttackReleased?.Invoke(Time.timeAsDouble);
        private void OnCamera(InputAction.CallbackContext _) => CameraToggled?.Invoke();
        private void OnReset(InputAction.CallbackContext _) => ResetPressed?.Invoke();

#if UNITY_EDITOR
        internal Vector2? TestMoveInput { get; set; }
        internal void TestCombat(CombatInput action) => CombatInputPressed?.Invoke(action);
        internal void TestCharge(double time) => ChargeAttackPressed?.Invoke(time);
        internal void TestRelease(double time) => ChargeAttackReleased?.Invoke(time);
        internal void TestCamera() => CameraToggled?.Invoke();
        internal void TestReset() => ResetPressed?.Invoke();
#endif
    }
}
