using UnityEngine;

namespace ThreeCDemo
{
    public interface IMover
    {
        void Move(Vector3 delta);
    }

    public interface IRotator
    {
        Quaternion Rotation { get; set; }
    }

    public interface IPositionSource
    {
        Vector3 Position { get; }
    }

    public sealed class PlayerMovement
    {
        private readonly IMover body;
        private readonly IMoveInput input;
        private readonly float speed;

        public PlayerMovement(IMover body, IMoveInput input, float speed)
        {
            this.body = body;
            this.input = input;
            this.speed = speed;
        }

        public void Tick(float deltaTime)
        {
            Vector2 direction = Vector2.ClampMagnitude(input.MoveInput, 1);
            body.Move(new Vector3(direction.x, 0, direction.y) * (speed * deltaTime));
        }
    }

    public sealed class PlayerRotation
    {
        private readonly IRotator body;
        private readonly IMoveInput input;
        private readonly float sharpness;

        public PlayerRotation(IRotator body, IMoveInput input, float sharpness)
        {
            this.body = body;
            this.input = input;
            this.sharpness = sharpness;
        }

        public void Tick(float deltaTime)
        {
            Vector2 direction = input.MoveInput;
            if (direction.sqrMagnitude <= .01f) return;
            Quaternion target = Quaternion.LookRotation(new Vector3(direction.x, 0, direction.y));
            body.Rotation = Quaternion.Slerp(body.Rotation, target, 1 - Mathf.Exp(-sharpness * deltaTime));
        }
    }

    public sealed class PlayerDodge
    {
        private readonly IMoveInput input;
        private readonly IMover mover;
        private readonly IRotator rotator;
        private readonly float startSpeed;
        private Vector3 lockedDirection;

        public PlayerDodge(IMoveInput input, IMover mover, IRotator rotator, float startSpeed)
        {
            this.input = input;
            this.mover = mover;
            this.rotator = rotator;
            this.startSpeed = startSpeed;
        }

        public void BeginDodge()
        {
            Vector2 move = input.MoveInput;
            Vector3 direction = new Vector3(move.x, 0f, move.y);
            if (direction.sqrMagnitude > .01f)
            {
                lockedDirection = direction.normalized;
                rotator.Rotation = Quaternion.LookRotation(lockedDirection);
            }
            else
            {
                lockedDirection = rotator.Rotation * Vector3.forward;
            }
        }

        public void Tick(float deltaTime, float normalizedTime)
        {
            float speed = startSpeed * Mathf.Clamp01(1f - normalizedTime);
            mover.Move(lockedDirection * (speed * deltaTime));
        }
    }

    public sealed class CharacterControllerMover : IMover
    {
        private readonly CharacterController body;
        public CharacterControllerMover(CharacterController body) => this.body = body;
        public void Move(Vector3 delta) => body.Move(delta);
    }

    public sealed class TransformRotator : IRotator
    {
        private readonly Transform body;
        public TransformRotator(Transform body) => this.body = body;

        public Quaternion Rotation
        {
            get => body.rotation;
            set => body.rotation = value;
        }
    }

    public sealed class TransformPositionSource : IPositionSource
    {
        private readonly Transform body;
        public TransformPositionSource(Transform body) => this.body = body;
        public Vector3 Position => body.position;
    }
}
