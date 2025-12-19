using System.Drawing;
using Cong1;

namespace MPV.Camera
{
    public sealed class MindVisionCameraAdapter : ICamera
    {
        private readonly MindVisionCamera _inner = new MindVisionCamera();
        public bool IsConnected => _inner.IsConnected;
        public int Open(string identifier) => _inner.Open(identifier);
        public void StartLive() => _inner.StartLive();
        public Bitmap GrabFrame(int timeoutMs) => _inner.GrabFrame(timeoutMs);
        public void SetExposureTime(int exposureUs) => _inner.SetExposureTime(exposureUs);
        public int GetExposureTime(out int exposureUs) => _inner.GetExposureTime(out exposureUs);
        public void DeviceListAcq() => _inner.DeviceListAcq();
        public void Close() { try { _inner.Close(); } catch { }
        }
    }
}
