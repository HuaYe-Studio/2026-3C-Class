using Cinemachine;

namespace ThreeCDemo
{
    public sealed class PlayerCameraRig
    {
        private readonly CinemachineVirtualCamera overview;

        public PlayerCameraRig(CinemachineVirtualCamera overview)
        {
            this.overview = overview;
        }

        public bool Overview { get; private set; }

        public void Toggle()
        {
            Overview = !Overview;
            overview.Priority = Overview ? 20 : 5;
        }
    }
}
