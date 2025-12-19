namespace MPV.Config
{
    public enum CameraModeType
    {
        Hik = 0,
        Mind = 1,
        Usb = 2
    }

    public sealed class CameraConfig
    {
        // 0=Hik, 1=Mind, 2=USB
        public CameraModeType CameraMode { get; set; } = CameraModeType.Usb;
        // IDs for up to 3 cameras
        public string Cam1Id { get; set; } = "";
        public string Cam2Id { get; set; } = "";
        public string Cam3Id { get; set; } = "";
    }
}
