using System;
using UnityEngine;

namespace ThreeCDemo
{
    public sealed class PlayerCombatController
    {
        private readonly ICombatInputSource input;
        private readonly IClock clock;
        private readonly ICombatState state;
        private readonly CombatActionBuffer comboBuffer;
        private readonly IChargeInterpreter press;
        private readonly IAttackExecutor executor;
        private bool dodgeRequested;
        private AttackKind? requestedAttack;

        public PlayerCombatController(
            ICombatInputSource input,
            IClock clock,
            ICombatState state,
            CombatActionBuffer comboBuffer,
            IChargeInterpreter press,
            IAttackExecutor executor)
        {
            this.input = input;
            this.clock = clock;
            this.state = state;
            this.comboBuffer = comboBuffer;
            this.press = press;
            this.executor = executor;
        }

        public event Action DodgeStarted;

        public int AttacksExecuted { get; private set; }
        public CombatState CurrentState => state.CurrentState;
        public int ComboStep => state.ComboStep;
        public ComboActionType CurrentAction => state.CurrentAction;
        public bool IsInvulnerable => state.IsInvulnerable(clock.Now);
        public string Feedback => executor.Feedback;
        public string ComboTrace => state.ComboTrace;
        public float ChargeProgress => press.GetChargeProgress(clock.Now);
        public float BufferProgress => comboBuffer.Progress(clock.Now);
        public float GetStateNormalizedTime(double now) => state.GetStateNormalizedTime(now);

        public void Enable()
        {
            input.CombatInputPressed += OnCombatInputPressed;
            input.ChargeAttackPressed += press.Press;
            input.ChargeAttackReleased += OnChargeReleased;
        }

        public void Disable()
        {
            input.CombatInputPressed -= OnCombatInputPressed;
            input.ChargeAttackPressed -= press.Press;
            input.ChargeAttackReleased -= OnChargeReleased;
            Clear();
        }

        public void Tick(double now, bool wantsMove)
        {
            state.Tick(now, wantsMove);

            // 闪避取消后摇，同帧到达的攻击输入也随连段取消。
            if (dodgeRequested)
            {
                dodgeRequested = false;
                TryDodge(now);
            }

            if (comboBuffer.Tick(now))
                ExecuteCombo(state.CurrentAction, state.ComboStep, now);

            if (requestedAttack.HasValue && state.TryStartAttack(now))
            {
                if (requestedAttack == AttackKind.Light)
                    Execute(new AttackRequest("Light attack", Element.Normal, 15, 2.2f, new Color(.3f, .9f, 1)), now);
                else
                    Execute(new AttackRequest("Element heavy attack", Element.Electric, 30, 2.8f, new Color(.95f, .91f, .27f)), now);
            }
            requestedAttack = null;
        }

        public void Reset()
        {
            Clear();
            AttacksExecuted = 0;
        }

        private void Clear()
        {
            state.Reset();
            comboBuffer.ClearPending();
            press.Clear();
            dodgeRequested = false;
            requestedAttack = null;
        }

        public void SetMessage(string message) => executor.SetMessage(message);

        private void OnCombatInputPressed(CombatInput action)
        {
            if (action == CombatInput.Dodge)
            {
                dodgeRequested = true;
                return;
            }

            ComboActionType kind = action == CombatInput.Attack
                ? ComboActionType.AttackA
                : ComboActionType.AttackE;
            comboBuffer.Request(kind, clock.Now);
        }

        private void TryDodge(double now)
        {
            CombatState before = state.CurrentState;
            if (!state.TryStartDodge(now)) return;

            comboBuffer.ClearPending();
            press.Clear();
            requestedAttack = null;
            DodgeStarted?.Invoke();
            executor.SetMessage(before == CombatState.Recovering
                ? "Dodge  /  Combo canceled"
                : "Dodge");
        }

        private void OnChargeReleased(double time) => requestedAttack = press.Release(time);

        private void ExecuteCombo(ComboActionType action, int step, double now)
        {
            switch (action)
            {
                case ComboActionType.AttackA:
                    Execute(ARequest(step), now);
                    break;
                case ComboActionType.AttackE:
                    Execute(ERequest(step), now);
                    break;
                case ComboActionType.DiDong_AEA:
                    Execute(new AttackRequest("Ground Quake  (A-E-A)", Element.Ground, 45, 3.2f,
                        new Color(.95f, .62f, .2f)), now);
                    break;
                case ComboActionType.ShanYao_EEA:
                    Execute(new AttackRequest("Mountain Shake  (E-E-A)", Element.Ground, 40, 3.6f,
                        new Color(1, .53f, .17f)), now);
                    break;
            }
        }

        private static AttackRequest ARequest(int step) => step switch
        {
            1 => new AttackRequest("A1 Slash", Element.Normal, 8, 2.2f, new Color(.3f, .9f, 1)),
            2 => new AttackRequest("A2 Rising Slash", Element.Normal, 10, 2.35f, new Color(.38f, .96f, .85f)),
            PlayerCombatStateMachine.ComboLength => new AttackRequest("A3 Finisher", Element.Normal, 14, 2.6f, new Color(.22f, .95f, .72f)),
            _ => throw new ArgumentOutOfRangeException(nameof(step)),
        };

        private static AttackRequest ERequest(int step) => step switch
        {
            1 => new AttackRequest("E1 Pulse", Element.Normal, 8, 2.3f, new Color(.72f, .55f, 1)),
            2 => new AttackRequest("E2 Pulse", Element.Normal, 10, 2.45f, new Color(.82f, .45f, 1)),
            PlayerCombatStateMachine.ComboLength => new AttackRequest("E3 Finisher Pulse", Element.Normal, 14, 2.7f, new Color(1, .5f, .85f)),
            _ => throw new ArgumentOutOfRangeException(nameof(step)),
        };

        private void Execute(AttackRequest request, double now)
        {
            AttacksExecuted++;
            executor.Execute(request, now);
        }
    }
}
