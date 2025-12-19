using System.Drawing;

namespace MPV.Camera
{
    public interface ICamera
    {
        bool IsConnected { get; }
        int Open(string identifier);
        void StartLive();
        Bitmap GrabFrame(int timeoutMs);
        void SetExposureTime(int exposureUs);
        int GetExposureTime(out int exposureUs);
        void DeviceListAcq();
        void Close();
    }
}
