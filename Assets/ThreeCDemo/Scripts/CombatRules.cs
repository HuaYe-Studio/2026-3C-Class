using System;
using System.Collections.Generic;

namespace ThreeCDemo
{
    public enum Element { Normal, Electric, Ground }
    public enum CombatInput { Skill, Attack, Dodge }
    public enum AttackKind { Light, Heavy }
    public enum CombatState { Idle, Moving, Dodging, Attacking, Recovering }
    public enum ComboActionType { None, AttackA, AttackE, DiDong_AEA, ShanYao_EEA }

    public interface ICombatState
    {
        CombatState CurrentState { get; }
        double StateEndsAt { get; }
        int ComboStep { get; }
        ComboActionType CurrentAction { get; }
        string ComboTrace { get; }
        bool IsInvulnerable(double now);
        bool TryStartAttack(double now);
        bool TryStartCombo(ComboActionType action, double now);
        bool IsFinisherRecovery(double now);
        bool TryChainFinisher(ComboActionType first, ComboActionType second, ComboActionType third, double now);
        bool TryStartDodge(double now);
        float GetStateNormalizedTime(double now);
        void Tick(double now, bool wantsMove);
        void Reset();
    }

    public interface IChargeInterpreter
    {
        void Press(double now);
        AttackKind? Release(double now);
        float GetChargeProgress(double now);
        void Clear();
    }

    public sealed class CharacterAttributes
    {
        public Element Type { get; }
        public int MaxHp { get; }

        public CharacterAttributes(Element type, int maxHp)
        {
            if (maxHp <= 0) throw new ArgumentOutOfRangeException(nameof(maxHp));
            Type = type;
            MaxHp = maxHp;
        }
    }

    public sealed class Health
    {
        private readonly CharacterAttributes attributes;

        public Health(CharacterAttributes attributes)
        {
            this.attributes = attributes;
            Hp = attributes.MaxHp;
        }

        public Element Type => attributes.Type;
        public int MaxHp => attributes.MaxHp;
        public int Hp { get; private set; }

        public int TakeDamage(int damage)
        {
            int applied = Math.Min(Hp, Math.Max(0, damage));
            Hp -= applied;
            return applied;
        }

        public void Reset() => Hp = MaxHp;
    }

    public static class DamageRules
    {
        public static int ApplyMove(Element move, int damage, Health target)
        {
            return move == Element.Electric && target.Type == Element.Ground
                ? 0 : target.TakeDamage(damage);
        }

        public static int ApplySkill(
            Element skillType, int damage, Health target, ICombatState targetState, double now)
        {
            return targetState.IsInvulnerable(now) ? 0 : ApplyMove(skillType, damage, target);
        }
    }

    public sealed class PlayerHealth
    {
        private readonly ICombatState state;
        private readonly Health health = new Health(new CharacterAttributes(Element.Normal, 100));

        public PlayerHealth(ICombatState state) => this.state = state;

        public event Action PerfectDodge;

        public int Hp => health.Hp;
        public int MaxHp => health.MaxHp;

        public int TakeDamage(Element element, int damage, double now)
        {
            bool perfect = state.IsInvulnerable(now);
            int applied = DamageRules.ApplySkill(element, damage, health, state, now);
            if (perfect) PerfectDodge?.Invoke();
            return applied;
        }

        public void Reset() => health.Reset();
    }

    public sealed class PlayerCombatStateMachine : ICombatState
    {
        public const int ComboLength = 3;

        private readonly double attackSeconds;
        private readonly double recoverySeconds;
        private readonly double finisherRecoverySeconds;
        private readonly double dodgeSeconds;
        private readonly double iFrameSeconds;
        private readonly List<ComboActionType> comboHistory = new List<ComboActionType>(ComboLength);
        private double stateStartedAt;

        public CombatState CurrentState { get; private set; }
        public double StateEndsAt { get; private set; }
        public int ComboStep { get; private set; }
        public ComboActionType CurrentAction { get; private set; }
        public string ComboTrace => string.Join(" ", comboHistory);

        public bool IsInvulnerable(double now) =>
            CurrentState == CombatState.Dodging && now >= stateStartedAt
            && now < Math.Min(StateEndsAt, stateStartedAt + iFrameSeconds);

        public PlayerCombatStateMachine(
            double attackSeconds, double recoverySeconds, double dodgeSeconds, double iFrameSeconds,
            double finisherRecoverySeconds)
        {
            this.attackSeconds = attackSeconds;
            this.recoverySeconds = recoverySeconds;
            this.finisherRecoverySeconds = finisherRecoverySeconds;
            this.dodgeSeconds = dodgeSeconds;
            this.iFrameSeconds = iFrameSeconds;
        }

        public bool TryStartAttack(double now)
        {
            if (!CanStartAction()) return false;
            ResetCombo();
            BeginAttack(now, ComboActionType.None);
            return true;
        }

        // A / E 共用同一个连段步数；第三段输入 A 时由最近两招决定是否派生强化技。
        public bool TryStartCombo(ComboActionType action, double now)
        {
            if (action != ComboActionType.AttackA && action != ComboActionType.AttackE)
                return false;
            if (!CanStartAction()) return false;

            bool continuing = CurrentState == CombatState.Recovering
                && ComboStep > 0 && ComboStep < ComboLength;

            if (continuing)
            {
                ComboStep++;
            }
            else
            {
                ComboStep = 1;
                comboHistory.Clear();
            }

            ComboActionType resolved = ResolveAction(action);
            comboHistory.Add(action);
            BeginAttack(now, resolved);
            return true;
        }

        private ComboActionType ResolveAction(ComboActionType input)
        {
            if (ComboStep != ComboLength) return input;

            ComboActionType finisher = ResolveFinisher(comboHistory[0], comboHistory[1], input);
            return finisher == ComboActionType.None ? input : finisher;
        }

        public bool IsFinisherRecovery(double now)
        {
            if (!IsFinisher) return false;

            // 输入回调可能先于本帧的 Tick 到达。
            return CurrentState switch
            {
                CombatState.Attacking => now >= StateEndsAt && now < StateEndsAt + finisherRecoverySeconds,
                CombatState.Recovering => now >= stateStartedAt && now < StateEndsAt,
                _ => false,
            };
        }

        private bool IsFinisher => CurrentAction == ComboActionType.ShanYao_EEA
            || CurrentAction == ComboActionType.DiDong_AEA;

        public bool TryChainFinisher(ComboActionType first, ComboActionType second, ComboActionType third, double now)
        {
            if (CurrentState != CombatState.Recovering || !IsFinisherRecovery(now)) return false;

            ComboActionType finisher = ResolveFinisher(first, second, third);
            if (finisher == ComboActionType.None) return false;

            ComboStep = ComboLength;
            comboHistory.Clear();
            comboHistory.Add(first);
            comboHistory.Add(second);
            comboHistory.Add(third);
            BeginAttack(now, finisher);
            return true;
        }

        private static ComboActionType ResolveFinisher(
            ComboActionType first, ComboActionType second, ComboActionType third) =>
            (first, second, third) switch
            {
                (ComboActionType.AttackE, ComboActionType.AttackE, ComboActionType.AttackA) => ComboActionType.ShanYao_EEA,
                (ComboActionType.AttackA, ComboActionType.AttackE, ComboActionType.AttackA) => ComboActionType.DiDong_AEA,
                _ => ComboActionType.None,
            };

        public bool TryStartDodge(double now)
        {
            if (CurrentState == CombatState.Attacking || CurrentState == CombatState.Dodging) return false;
            ResetCombo();
            CurrentState = CombatState.Dodging;
            stateStartedAt = now;
            StateEndsAt = now + dodgeSeconds;
            return true;
        }

        public float GetStateNormalizedTime(double now)
        {
            double duration = StateEndsAt - stateStartedAt;
            if (duration <= 0) return 1f;
            return (float)Math.Clamp((now - stateStartedAt) / duration, 0.0, 1.0);
        }

        public void Tick(double now, bool wantsMove)
        {
            if (CurrentState == CombatState.Attacking && now >= StateEndsAt)
            {
                CurrentState = CombatState.Recovering;
                stateStartedAt = StateEndsAt;
                StateEndsAt += IsFinisher ? finisherRecoverySeconds : recoverySeconds;
            }

            bool canChooseMovement = CurrentState == CombatState.Idle || CurrentState == CombatState.Moving;
            bool recoveryFinished = CurrentState == CombatState.Recovering && now >= StateEndsAt;
            bool dodgeFinished = CurrentState == CombatState.Dodging && now >= StateEndsAt;

            if (recoveryFinished)
                ResetCombo();

            if (canChooseMovement || recoveryFinished || dodgeFinished)
                CurrentState = wantsMove ? CombatState.Moving : CombatState.Idle;
        }

        public void Reset()
        {
            CurrentState = CombatState.Idle;
            stateStartedAt = 0;
            StateEndsAt = 0;
            ResetCombo();
        }

        private bool CanStartAction() =>
            CurrentState != CombatState.Attacking && CurrentState != CombatState.Dodging
            && (CurrentState != CombatState.Recovering || ComboStep < ComboLength);

        private void BeginAttack(double now, ComboActionType action)
        {
            CurrentAction = action;
            CurrentState = CombatState.Attacking;
            stateStartedAt = now;
            StateEndsAt = now + attackSeconds;
        }

        private void ResetCombo()
        {
            ComboStep = 0;
            CurrentAction = ComboActionType.None;
            comboHistory.Clear();
        }
    }

    // 普通预输入从后摇开始计时。强化技接续收集同一次强化技后摇内的按键。
    public sealed class CombatActionBuffer
    {
        private readonly ICombatState combat;
        private readonly double duration;
        private readonly List<ComboActionType> finisherInputs = new List<ComboActionType>(PlayerCombatStateMachine.ComboLength);
        private ComboActionType pending = ComboActionType.None;
        private double expiresAt;
        private bool waitingForRecovery;

        public CombatActionBuffer(ICombatState combat, double duration)
        {
            this.combat = combat;
            this.duration = duration;
        }

        public void Request(ComboActionType action, double now)
        {
            if (combat.IsFinisherRecovery(now))
            {
                ClearBufferedAction();
                finisherInputs.Add(action);
                return;
            }

            finisherInputs.Clear();
            pending = action;
            waitingForRecovery = combat.CurrentState == CombatState.Attacking && now < combat.StateEndsAt;
            double windowStartsAt = waitingForRecovery ? combat.StateEndsAt : now;
            expiresAt = windowStartsAt + duration;
        }

        public bool Tick(double now)
        {
            if (finisherInputs.Count > 0) return TryConsumeFinisher(now);
            if (pending == ComboActionType.None) return false;

            if (now >= expiresAt || (waitingForRecovery && combat.CurrentState != CombatState.Attacking
                && combat.CurrentState != CombatState.Recovering))
            {
                ClearPending();
                return false;
            }

            if (!combat.TryStartCombo(pending, now)) return false;

            ClearPending();
            return true;
        }

        private bool TryConsumeFinisher(double now)
        {
            if (!combat.IsFinisherRecovery(now))
            {
                finisherInputs.Clear();
                return false;
            }

            int first = 0;
            while (first + PlayerCombatStateMachine.ComboLength <= finisherInputs.Count)
            {
                if (combat.TryChainFinisher(finisherInputs[first], finisherInputs[first + 1], finisherInputs[first + 2], now))
                {
                    ClearPending();
                    return true;
                }
                first++;
            }

            // 后续输入还可能与最后两键组成有效指令。
            finisherInputs.RemoveRange(0, first);
            return false;
        }

        public float Progress(double now)
        {
            if (finisherInputs.Count > 0 && combat.IsFinisherRecovery(now))
                return 1f - combat.GetStateNormalizedTime(now);

            return pending == ComboActionType.None || duration <= 0
                ? 0f
                : (float)Math.Clamp((expiresAt - now) / duration, 0, 1);
        }

        public void ClearPending()
        {
            finisherInputs.Clear();
            ClearBufferedAction();
        }

        private void ClearBufferedAction()
        {
            pending = ComboActionType.None;
            waitingForRecovery = false;
        }

    }

    public sealed class AttackPressInterpreter : IChargeInterpreter
    {
        private readonly double threshold;
        private double? pressedAt;

        public AttackPressInterpreter(double threshold) => this.threshold = threshold;
        public void Press(double now) => pressedAt = now;
        public AttackKind? Release(double now)
        {
            if (!pressedAt.HasValue) return null;
            AttackKind kind = now - pressedAt.Value >= threshold ? AttackKind.Heavy : AttackKind.Light;
            Clear();
            return kind;
        }

        public float GetChargeProgress(double now) =>
            pressedAt.HasValue ? (float)Math.Min(1, Math.Max(0, (now - pressedAt.Value) / threshold)) : 0;
        public void Clear() => pressedAt = null;
    }
}
