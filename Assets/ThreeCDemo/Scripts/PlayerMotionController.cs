namespace ThreeCDemo
{
    public sealed class PlayerMotionController
    {
        private readonly PlayerMovement movement;
        private readonly PlayerRotation rotation;
        private readonly PlayerDodge dodge;

        public PlayerMotionController(
            IMoveInput input,
            IMover mover,
            IRotator rotator,
            float moveSpeed,
            float turnSharpness,
            float dodgeStartSpeed)
        {
            movement = new PlayerMovement(mover, input, moveSpeed);
            rotation = new PlayerRotation(rotator, input, turnSharpness);
            dodge = new PlayerDodge(input, mover, rotator, dodgeStartSpeed);
        }

        public void Tick(float deltaTime)
        {
            movement.Tick(deltaTime);
            rotation.Tick(deltaTime);
        }

        public void BeginDodge() => dodge.BeginDodge();

        public void TickDodge(float deltaTime, float normalizedTime) =>
            dodge.Tick(deltaTime, normalizedTime);
    }
}
