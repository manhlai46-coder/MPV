using MPV.Camera;
using MPV.Config;

namespace MPV.Services
{
    public static class CameraManager
    {
        public static ICamera Cam1 { get; private set; }
        public static ICamera Cam2 { get; private set; }
        public static ICamera Cam3 { get; private set; }
        public static CameraMode Mode { get; private set; }

        public static void Initialize()
        {
            var cfg = CameraConfigLoader.LoadOrCreateDefault();
            Mode = MapMode(cfg.CameraMode);

            Cam1 = CreateCamera(Mode);
            Cam2 = CreateCamera(Mode);
            Cam3 = CreateCamera(Mode);

            SafeOpenAndStart(Cam1, cfg.Cam1Id);
            SafeOpenAndStart(Cam2, cfg.Cam2Id);
            SafeOpenAndStart(Cam3, cfg.Cam3Id);
        }

        private static CameraMode MapMode(CameraModeType t)
        {
            switch (t)
            {
                case CameraModeType.Hik: return CameraMode.Hikvision;
                case CameraModeType.Mind: return CameraMode.MindVision;
                case CameraModeType.Usb: return CameraMode.USB;
                default: return CameraMode.MindVision;
            }
        }

        private static ICamera CreateCamera(CameraMode mode)
        {
            switch (mode)
            {
                case CameraMode.Hikvision: return new HikvisionCamera();
                case CameraMode.MindVision: return new MindVisionCameraAdapter();
                case CameraMode.USB: return new UsbOpenCvCamera();
                default: return new MindVisionCameraAdapter();
            }
        }

        private static void SafeOpenAndStart(ICamera cam, string id)
        {
            if (cam == null) return;
            try { cam.DeviceListAcq(); } catch { }
            try { if (!string.IsNullOrWhiteSpace(id)) cam.Open(id); } catch { }
            try { if (cam.IsConnected) cam.StartLive(); } catch { }
        }
    }
}
