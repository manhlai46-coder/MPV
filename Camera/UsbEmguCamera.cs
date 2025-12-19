using System.Diagnostics;
using System.Drawing;
using OpenCvSharp;
using OpenCvSharp.Extensions;

namespace MPV.Camera
{
    // USB: dùng OpenCvSharp, chỉ chụp, bỏ exposure
    public sealed class UsbOpenCvCamera : ICamera
    {
        private VideoCapture _cap;
        public bool IsConnected => _cap != null && _cap.IsOpened();

        public int Open(string identifier)
        {
            if (!int.TryParse(identifier, out int index)) return 0;
            _cap = new VideoCapture(index, VideoCaptureAPIs.ANY);
            return _cap != null && _cap.IsOpened() ? 1 : 0;
        }

        public void StartLive() { /* OpenCvSharp không cần start riêng */ }

        public Bitmap GrabFrame(int timeoutMs)
        {
            if (!IsConnected) return null;
            var sw = Stopwatch.StartNew();
            using (var mat = new Mat())
            {
                while (sw.ElapsedMilliseconds < timeoutMs)
                {
                    try
                    {
                        if (_cap.Read(mat) && !mat.Empty())
                        {
                            return BitmapConverter.ToBitmap(mat);
                        }
                    }
                    catch { }
                }
            }
            return null;
        }

        public void SetExposureTime(int exposureUs) { /* USB không hỗ trợ, bỏ qua */ }
        public int GetExposureTime(out int exposureUs) { exposureUs = 0; return 0; }
        public void DeviceListAcq() { }
        public void Close() { try { _cap?.Release(); _cap?.Dispose(); } catch { } _cap = null; }
    }
}
