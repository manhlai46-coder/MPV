using System.Drawing;

namespace MPV.Camera
{
    public sealed class HikvisionCamera : ICamera
    {
        public bool IsConnected { get; private set; }
        public int Open(string identifier) { IsConnected = false; return 0; }
        public void StartLive() { }
        public Bitmap GrabFrame(int timeoutMs) => null;
        public void SetExposureTime(int exposureUs) { }
        public int GetExposureTime(out int exposureUs) { exposureUs = 0; return 0; }
        public void DeviceListAcq() { }
        public void Close() { }
    }
}
