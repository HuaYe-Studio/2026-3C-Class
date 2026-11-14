#if UNITY_EDITOR
using System;
using System.Collections;
using Cinemachine;
using UnityEditor;
using UnityEngine;

namespace ThreeCDemo
{
    public sealed class TrainingLabSmokeCheck : MonoBehaviour
    {
        private PlayerInputReader input;
        private PlayerController player;
        private int perfectDodges;

        [MenuItem("Tools/Gameplay 3C/Verify Training Lab")]
        private static void StartCheck()
        {
            if (EditorApplication.isPlaying && FindObjectOfType<PlayerController>() != null)
                new GameObject("3C Smoke Check").AddComponent<TrainingLabSmokeCheck>();
        }

        private IEnumerator Start()
        {
            CheckCombatTiming();
            CheckFinisherChains();

            player = FindObjectOfType<PlayerController>();
            var dummy = FindObjectOfType<TrainingDummy>();
            int targetMaxHp = dummy.MaxHp;
            var brain = FindObjectOfType<CinemachineBrain>();
            input = player.GetComponent<PlayerInputReader>();
            Vector3 spawn = player.transform.position;

            input.TestMoveInput = Vector2.up;
            yield return new WaitForSeconds(.95f);
            input.TestMoveInput = Vector2.zero;
            Expect(player.transform.position.z > spawn.z + 3.8f, "Forward movement");

            input.TestCharge(0);
            input.TestRelease(.08);
            yield return new WaitForSeconds(.09f);
            Expect(dummy.Hp == targetMaxHp - 15, "Q tap deals 15 damage");

            yield return new WaitForSeconds(.25f);
            Vector3 beforeDodge = player.transform.position;
            player.transform.rotation = Quaternion.LookRotation(Vector3.left);
            player.PerfectDodge += CountPerfectDodge;
            input.TestMoveInput = Vector2.up;
            Press(CombatInput.Dodge);
            yield return new WaitForSeconds(.02f);
            input.TestMoveInput = Vector2.zero;
            Expect(player.CurrentState == CombatState.Dodging && player.IsInvulnerable,
                "Shift cancels recovery and grants i-frames");
            Expect(player.TakeDamage(Element.Normal, 10) == 0 && player.Hp == 100,
                "Dodging blocks damage during i-frames");
            Expect(perfectDodges == 1, "Perfect dodge event fires");
            yield return new WaitForSeconds(.23f);
            Expect(player.CurrentState == CombatState.Dodging && !player.IsInvulnerable,
                "i-frames expire while still dodging");
            Expect(player.TakeDamage(Element.Normal, 10) == 10 && player.Hp == 90,
                "Damage applies after i-frames");
            yield return new WaitForSeconds(.22f);
            Expect(player.CurrentState != CombatState.Dodging && !player.IsInvulnerable, "Dodge ends");
            Expect(player.transform.position.z > beforeDodge.z + .5f, "Dodge moves along input direction");
            Expect(player.TakeDamage(Element.Normal, 10) == 10 && player.Hp == 80,
                "Damage applies after dodge");
            yield return new WaitForSeconds(.78f);

            int attacksBeforeHeavy = player.AttacksExecuted;
            input.TestCharge(1);
            input.TestRelease(1.4);
            yield return new WaitForSeconds(.09f);
            Expect(player.AttacksExecuted == attacksBeforeHeavy + 1 && dummy.Hp == targetMaxHp - 15,
                "Q hold executes a heavy attack and the Ground target takes no damage");

            input.TestReset();
            yield return null;
            yield return Approach();

            Press(CombatInput.Attack);
            yield return null;
            Vector3 attackPosition = player.transform.position;
            input.TestMoveInput = Vector2.left;
            yield return new WaitForSeconds(.3f);
            Expect(Vector3.Distance(attackPosition, player.transform.position) < .02f,
                "Attack and recovery lock movement");
            input.TestMoveInput = Vector2.zero;
            Press(CombatInput.Attack);
            yield return new WaitForSeconds(.3f);
            Press(CombatInput.Attack);
            yield return new WaitForSeconds(.3f);
            Expect(dummy.Hp == targetMaxHp - 32, "LMB chain A1-A3 deals 32 damage");
            Expect(player.ComboStep == 3 && player.CurrentAction == ComboActionType.AttackA,
                "A chain reached step 3");
            Expect(player.AttacksExecuted == 3, "A chain executed 3 hits");
            yield return new WaitForSeconds(.55f);

            Press(CombatInput.Skill);
            yield return new WaitForSeconds(.3f);
            Press(CombatInput.Skill);
            yield return new WaitForSeconds(.3f);
            Press(CombatInput.Skill);
            yield return new WaitForSeconds(.3f);
            Expect(dummy.Hp == targetMaxHp - 64, "E chain E1-E3 deals 32 damage");
            Expect(player.ComboStep == 3 && player.CurrentAction == ComboActionType.AttackE,
                "E chain reached step 3");
            Expect(player.AttacksExecuted == 6, "E chain executed 3 hits");
            yield return new WaitForSeconds(.55f);

            input.TestReset();
            yield return null;
            yield return Approach();

            Press(CombatInput.Attack);
            yield return new WaitForSeconds(.3f);
            Press(CombatInput.Skill);
            yield return new WaitForSeconds(.3f);
            Press(CombatInput.Attack);
            yield return new WaitForSeconds(.3f);
            Expect(dummy.Hp == targetMaxHp - 63, "A-E-A derives Ground Quake (63 damage)");
            Expect(player.CurrentAction == ComboActionType.DiDong_AEA && player.ComboStep == 3,
                "Ground Quake derived at step 3");

            Press(CombatInput.Attack);
            yield return null;
            Press(CombatInput.Skill);
            yield return new WaitForSeconds(.65f);
            Expect(dummy.Hp == targetMaxHp - 63 && player.AttacksExecuted == 3, "AEA prefix skips both basic attacks");
            Press(CombatInput.Attack);
            yield return new WaitForSeconds(.3f);
            Expect(dummy.Hp == Math.Max(0, targetMaxHp - 108) && player.AttacksExecuted == 4
                && player.CurrentAction == ComboActionType.DiDong_AEA,
                "AEA past the regular recovery duration hits the target with one chained Ground Quake");

            input.TestReset();
            yield return null;
            yield return Approach();

            Press(CombatInput.Skill);
            yield return new WaitForSeconds(.3f);
            Press(CombatInput.Skill);
            yield return new WaitForSeconds(.3f);
            Press(CombatInput.Attack);
            yield return new WaitForSeconds(.3f);
            Expect(dummy.Hp == targetMaxHp - 58, "E-E-A derives Mountain Shake (58 damage total)");
            Expect(player.CurrentAction == ComboActionType.ShanYao_EEA && player.ComboStep == 3,
                "Mountain Shake derived at step 3");

            Press(CombatInput.Skill);
            yield return null;
            Press(CombatInput.Skill);
            yield return null;
            Expect(dummy.Hp == targetMaxHp - 58 && player.AttacksExecuted == 3, "EEA prefix skips both basic attacks");
            Press(CombatInput.Attack);
            yield return null;
            Expect(dummy.Hp == targetMaxHp - 98 && player.AttacksExecuted == 4
                && player.CurrentAction == ComboActionType.ShanYao_EEA,
                "EEA during recovery hits the target with one chained Mountain Shake");

            input.TestReset();
            yield return null;
            yield return Approach();

            Press(CombatInput.Attack);
            yield return null;
            Press(CombatInput.Attack);
            yield return new WaitForSeconds(.3f);
            Expect(dummy.Hp == targetMaxHp - 18, "Pre-input during attack chains A2");
            Expect(player.ComboStep == 2 && player.AttacksExecuted == 2, "Combo advanced to step 2");
            yield return new WaitForSeconds(.4f);

            Press(CombatInput.Dodge);
            yield return new WaitForSeconds(.02f);
            Expect(player.CurrentState == CombatState.Dodging && player.ComboStep == 0,
                "Dodge resets the combo chain");
            yield return new WaitForSeconds(.45f);
            Press(CombatInput.Attack);
            yield return new WaitForSeconds(.3f);
            Expect(dummy.Hp == targetMaxHp - 26, "New chain restarts at A1 after dodge");
            Expect(player.ComboStep == 1 && player.CurrentAction == ComboActionType.AttackA,
                "Combo restarts at step 1");

            input.TestCamera();
            yield return new WaitForSeconds(.8f);
            Expect(brain.ActiveVirtualCamera.Name == "VCam - Overview", "C blends to overview");
            input.TestReset();
            yield return null;
            Expect(dummy.Hp == targetMaxHp && Vector3.Distance(spawn, player.transform.position) < .02f,
                "R resets health, combo and position");
            input.TestCamera();
            yield return new WaitForSeconds(.8f);
            Expect(brain.ActiveVirtualCamera.Name == "VCam - Follow", "C blends back to follow");

            Debug.Log("3C smoke check passed: combat timing boundaries, movement, Q light/heavy + immunity, shared-step A/E chains, EEA Mountain Shake, AEA Ground Quake, EEA/AEA finisher chaining, pre-input buffer, dodge i-frames + combo reset, damage gating, recovery lock, cameras, reset.");
            Destroy(gameObject);
        }

        private IEnumerator Approach()
        {
            input.TestMoveInput = Vector2.up;
            yield return new WaitForSeconds(.95f);
            input.TestMoveInput = Vector2.zero;
        }

        private static void CheckCombatTiming()
        {
            var state = new PlayerCombatStateMachine(.25, .55, .4, .2, .9);
            var buffer = new CombatActionBuffer(state, .2);
            var driver = new CombatTestDriver();
            var combat = new PlayerCombatController(driver, driver, state, buffer,
                new AttackPressInterpreter(.3), driver);
            combat.Enable();

            driver.Press(CombatInput.Attack);
            combat.Tick(driver.Now, false);
            driver.Now = .01;
            driver.Press(CombatInput.Attack);
            driver.Now = .3;
            combat.Tick(driver.Now, false);
            Expect(combat.ComboStep == 2 && combat.AttacksExecuted == 2,
                "Early A/E pre-input survives a frame that crosses into recovery");

            driver.Now = .4;
            driver.Press(CombatInput.Attack);
            driver.Now = .75;
            combat.Tick(driver.Now, false);
            Expect(combat.ComboStep == 2 && combat.AttacksExecuted == 2,
                "A/E pre-input expires .2 seconds after recovery starts");

            combat.Reset();
            driver.Now = 1;
            driver.Press(CombatInput.Attack);
            combat.Tick(driver.Now, false);
            driver.Now = 1.01;
            driver.Press(CombatInput.Attack);
            driver.Now = 1.25;
            driver.Press(CombatInput.Dodge);
            combat.Tick(driver.Now, false);
            Expect(combat.CurrentState == CombatState.Dodging && combat.ComboStep == 0 && combat.AttacksExecuted == 1,
                "Shift at the attack boundary cancels recovery and buffered A/E");
            Expect(state.IsInvulnerable(1.449) && !state.IsInvulnerable(1.45),
                "Invulnerability ends at the configured boundary");

            combat.Reset();
            driver.Now = 2;
            driver.Press(CombatInput.Attack);
            combat.Tick(driver.Now, false);
            driver.Now = 2.1;
            driver.Press(CombatInput.Dodge);
            combat.Tick(driver.Now, false);
            driver.Now = 2.25;
            combat.Tick(driver.Now, false);
            Expect(combat.CurrentState == CombatState.Recovering, "Shift during attack is discarded");

            combat.Reset();
            driver.Now = 3;
            driver.Charge(3, 3.4);
            combat.Tick(driver.Now, false);
            Expect(combat.AttacksExecuted == 1 && driver.LastAttack.Element == Element.Electric
                && driver.LastAttack.Damage == 30, "Q hold executes the Electric heavy attack");

            state.Reset();
            Expect(state.TryStartCombo(ComboActionType.AttackE, 4), "Finisher setup: E1 starts");
            state.Tick(4.25, false);
            Expect(state.TryStartCombo(ComboActionType.AttackE, 4.25), "Finisher setup: E2 starts");
            state.Tick(4.5, false);
            Expect(state.TryStartCombo(ComboActionType.AttackA, 4.5)
                && state.CurrentAction == ComboActionType.ShanYao_EEA, "Finisher setup: EEA derives Mountain Shake");
            state.Tick(4.75, false);
            Expect(!state.TryStartCombo(ComboActionType.AttackA, 4.75) && !state.TryStartAttack(4.75),
                "Finisher recovery rejects A/E and Q attacks");
            Expect(state.TryStartDodge(4.75) && state.ComboStep == 0,
                "Dodge cancels finisher recovery and clears the combo");
            combat.Disable();
        }

        private static void CheckFinisherChains()
        {
            const CombatInput A = CombatInput.Attack;
            const CombatInput E = CombatInput.Skill;
            var state = new PlayerCombatStateMachine(.25, .55, .4, .2, .9);
            var buffer = new CombatActionBuffer(state, .2);
            var driver = new CombatTestDriver();
            var combat = new PlayerCombatController(driver, driver, state, buffer,
                new AttackPressInterpreter(.3), driver);
            combat.Enable();

            void PressAt(double time, params CombatInput[] inputs)
            {
                driver.Now = time;
                foreach (CombatInput input in inputs) driver.Press(input);
                combat.Tick(time, false);
            }

            PressAt(1, E);
            PressAt(1.25, E);
            PressAt(1.5, A);
            PressAt(1.6, A, E, A);
            PressAt(1.75, E);
            PressAt(1.99, E);
            Expect(combat.CurrentState == CombatState.Recovering && combat.AttacksExecuted == 3,
                "Attack-phase inputs are excluded and the EEA prefix executes no attacks");
            PressAt(2.5, A, E);
            Expect(combat.CurrentAction == ComboActionType.ShanYao_EEA && combat.ComboStep == 3
                && combat.AttacksExecuted == 4 && driver.LastAttack.Damage == 40,
                "EEA past the regular recovery duration executes exactly one Mountain Shake");

            PressAt(2.8, A, E, A);
            Expect(combat.CurrentAction == ComboActionType.DiDong_AEA && combat.ComboStep == 3
                && combat.AttacksExecuted == 5 && driver.LastAttack.Damage == 45,
                "AEA on a single frame executes exactly one Ground Quake");

            PressAt(3.1, A, A, E);
            Expect(combat.CurrentState == CombatState.Recovering && combat.AttacksExecuted == 5,
                "AAE is not a finisher command");
            PressAt(3.96, A);
            Expect(combat.CurrentAction == ComboActionType.AttackA && combat.ComboStep == 1
                && combat.AttacksExecuted == 6, "Finisher prefix expires at the end of recovery");

            combat.Reset();
            PressAt(4, A);
            PressAt(4.25, E);
            PressAt(4.5, A);
            PressAt(4.75, E, E);
            PressAt(4.8, A, CombatInput.Dodge);
            Expect(combat.CurrentState == CombatState.Dodging && combat.AttacksExecuted == 3,
                "Dodge takes priority over a complete finisher command");
            PressAt(5.25, A);
            Expect(combat.CurrentAction == ComboActionType.AttackA && combat.ComboStep == 1
                && combat.AttacksExecuted == 4, "Dodge clears the finisher inputs");
            combat.Disable();
        }

        private sealed class CombatTestDriver : ICombatInputSource, IClock, IAttackExecutor
        {
            public event Action<CombatInput> CombatInputPressed;
            public event Action<double> ChargeAttackPressed;
            public event Action<double> ChargeAttackReleased;

            public double Now { get; set; }
            public double Realtime => Now;
            public float DeltaTime => 0;
            public string Feedback { get; private set; }
            public AttackRequest LastAttack { get; private set; }

            public void Press(CombatInput action) => CombatInputPressed?.Invoke(action);

            public void Charge(double start, double end)
            {
                ChargeAttackPressed?.Invoke(start);
                ChargeAttackReleased?.Invoke(end);
            }

            public void Execute(AttackRequest request, double now) => LastAttack = request;
            public void SetMessage(string message) => Feedback = message;
        }

        private void Press(CombatInput action) => input.TestCombat(action);

        private void CountPerfectDodge() => perfectDodges++;

        private void OnDestroy()
        {
            if (input != null) input.TestMoveInput = null;
            if (player != null) player.PerfectDodge -= CountPerfectDodge;
        }

        private static void Expect(bool condition, string check)
        {
            if (!condition) throw new InvalidOperationException("3C smoke check failed: " + check);
        }
    }
}
#endif
