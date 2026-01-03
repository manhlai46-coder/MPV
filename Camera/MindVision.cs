using System;
using System.Collections.Generic;
using System.Text;
using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using MVSDK;
using MvApi = MVSDK.MvApi;
using CameraHandle = System.Int32;
namespace Cong1
{
    public class MindVisionCamera
    {
        // ===== Trạng thái nội bộ =====
        private CameraHandle m_hCamera = 0;
        private IntPtr m_ImageBuffer = IntPtr.Zero;          // buffer dùng cho live / display
        private IntPtr m_ImageBufferSnapshot = IntPtr.Zero;  // buffer dùng cho GrabFrame (Bitmap)
        private int m_ImageBufferSnapshotSize = 0;
        private tSdkCameraCapbility tCameraCapability;

        // KHAI BÁO BIẾN HANDLE CHO PICTUREBOX
        private IntPtr m_hDisplayWnd = IntPtr.Zero; // Handle của PictureBox

        private Thread m_tCaptureThread;
        private volatile bool m_bExitCaptureThread = false;

        private readonly object _lockObj = new object();

        private string _userDefinedName = string.Empty;
        private bool _isConnected = false;
        private bool _isGrabbing = false;

        public bool IsConnected => _isConnected;
        public bool IsGrabbing => _isGrabbing;

        // ===== Helpers =====
        private static string ReadCString(byte[] src)
        {
            if (src == null) return string.Empty;
            int n = Array.IndexOf(src, (byte)0);
            if (n < 0) n = src.Length;
            return Encoding.UTF8.GetString(src, 0, n).Trim();
        }

        private void EnsureSnapshotBufferAllocated(in tSdkFrameHead head)
        {
            int bpp = head.uiMediaType == (uint)emImageFormat.CAMERA_MEDIA_TYPE_MONO8 ? 1 : 3;
            int need = head.iWidth * head.iHeight * bpp;
            if (m_ImageBufferSnapshot == IntPtr.Zero || need > m_ImageBufferSnapshotSize)
            {
                if (m_ImageBufferSnapshot != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(m_ImageBufferSnapshot);
                    m_ImageBufferSnapshot = IntPtr.Zero;
                }
                m_ImageBufferSnapshot = Marshal.AllocHGlobal(need);
                m_ImageBufferSnapshotSize = need;
            }
        }

        private static void SetGrayPaletteIfNeeded(Bitmap bmp)
        {
            if (bmp.PixelFormat != PixelFormat.Format8bppIndexed) return;
            var pal = bmp.Palette;
            for (int i = 0; i < 256; i++) pal.Entries[i] = Color.FromArgb(i, i, i);
            bmp.Palette = pal;
        }

        // ===== Public API =====

        /// <summary>
        /// Liệt kê camera MindVision (log ra Output console).
        /// </summary>
        public void DeviceListAcq()
        {
            GC.Collect();
            var status = MvApi.CameraEnumerateDevice(out tSdkCameraDevInfo[] devs);
            if (status != CameraSdkStatus.CAMERA_STATUS_SUCCESS || devs == null || devs.Length == 0)
            {
                Console.WriteLine("No camera found (try admin privileges).");
                return;
            }

            Console.WriteLine("=== MindVision Cameras ===");
            foreach (var d in devs)
            {
                Console.WriteLine(
                    $"SN: {ReadCString(d.acSn)} | Friendly: {ReadCString(d.acFriendlyName)} | Model: {ReadCString(d.acProductName)}");
            }
        }

        /// <summary>
        /// Mở camera. Nếu truyền userDefinedName = "" thì mở camera đầu tiên.
        /// Có thể truyền SN hoặc FriendlyName để chọn đúng camera.
        /// </summary>
        public int Open(string userDefinedName = "")
        {
            try
            {
                if (!string.IsNullOrEmpty(userDefinedName))
                    _userDefinedName = userDefinedName;

                if (_isConnected) return 1;

                m_hCamera = FindAndInitDevice(_userDefinedName);
                if (m_hCamera <= 0)
                {
                    _isConnected = false;
                    return 0;
                }

                // Lấy capability
                MvApi.CameraGetCapability(m_hCamera, out tCameraCapability);

                int maxW = tCameraCapability.sResolutionRange.iWidthMax;
                int maxH = tCameraCapability.sResolutionRange.iHeightMax;
                int maxRGB = maxW * maxH * 3 + 1024;

                // Buffer cho live
                m_ImageBuffer = Marshal.AllocHGlobal(maxRGB);

                // Buffer cho snapshot
                EnsureSnapshotBufferAllocated(new tSdkFrameHead
                {
                    iWidth = maxW,
                    iHeight = maxH,
                    uiMediaType = (uint)emImageFormat.CAMERA_MEDIA_TYPE_BGR8
                });



                // Set format output (BGR8 / MONO8) cho GDI+
                if (tCameraCapability.sIspCapacity.bMonoSensor != 0)
                    MvApi.CameraSetIspOutFormat(m_hCamera, (uint)emImageFormat.CAMERA_MEDIA_TYPE_MONO8);
                else
                    MvApi.CameraSetIspOutFormat(m_hCamera, (uint)emImageFormat.CAMERA_MEDIA_TYPE_BGR8);

                // Đặt độ phân giải snapshot = max
                tSdkImageResolution tRes = new tSdkImageResolution
                {
                    uSkipMode = 0,
                    uBinAverageMode = 0,
                    uBinSumMode = 0,
                    uResampleMask = 0,
                    iVOffsetFOV = 0,
                    iHOffsetFOV = 0,
                    iWidthFOV = maxW,
                    iHeightFOV = maxH,
                    iWidth = maxW,
                    iHeight = maxH,
                    iIndex = 0xFF,
                    acDescription = new byte[32],
                    iWidthZoomHd = 0,
                    iHeightZoomHd = 0,
                    iWidthZoomSw = 0,
                    iHeightZoomSw = 0
                };
                MvApi.CameraSetResolutionForSnap(m_hCamera, ref tRes);

                // Mirror/Rotate default
                MvApi.CameraSetMirror(m_hCamera, 0, 0);
                MvApi.CameraSetMirror(m_hCamera, 1, 0);
                MvApi.CameraSetRotate(m_hCamera, 0);

                // Auto exposure / white balance (nếu có hỗ trợ)
                try
                {
                    MvApi.CameraSetAeState(m_hCamera, 1);
                    MvApi.CameraSetWbMode(m_hCamera, 1);
                    MvApi.CameraSetOnceWB(m_hCamera);
                }
                catch { }

                _isConnected = true;
                return 1;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                return -1;
            }
        }

        /// <summary>
        /// Đóng camera, giải phóng buffer.
        /// </summary>
        public int Close()
        {
            try
            {
                if (m_hCamera > 0)
                {
                    if (_isGrabbing) StopLive();

                    var st = MvApi.CameraUnInit(m_hCamera);
                    if (st == CameraSdkStatus.CAMERA_STATUS_SUCCESS)
                        _isConnected = false;

                    if (m_ImageBuffer != IntPtr.Zero)
                    {
                        Marshal.FreeHGlobal(m_ImageBuffer);
                        m_ImageBuffer = IntPtr.Zero;
                    }
                    if (m_ImageBufferSnapshot != IntPtr.Zero)
                    {
                        Marshal.FreeHGlobal(m_ImageBufferSnapshot);
                        m_ImageBufferSnapshot = IntPtr.Zero;
                        m_ImageBufferSnapshotSize = 0;
                    }

                    m_hCamera = 0;
                }
                _isConnected = false;
                return 1;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                return -1;
            }
        }

        /// <summary>
        /// Bắt đầu live (CameraPlay) + thread hiển thị (dùng DisplayRGB trực tiếp).
        /// Nếu bạn chỉ cần GrabFrame thì có thể không dùng thread này.
        /// </summary>
        public int StartLive()
        {
            if (_isGrabbing) return 1;
            if (m_hCamera <= 0) return -1;

            var st = MvApi.CameraPlay(m_hCamera);
            if (st != CameraSdkStatus.CAMERA_STATUS_SUCCESS)
            {
                Debug.WriteLine("StartLive: CameraPlay failed.");
                return -1;
            }

            m_bExitCaptureThread = false;
            m_tCaptureThread = new Thread(CaptureThreadProc) { IsBackground = true };
            m_tCaptureThread.Start();

            _isGrabbing = true;
            return 1;
        }

        /// <summary>
        /// Dừng live.
        /// </summary>
        public int StopLive()
        {
            if (!_isGrabbing) return 1;
            if (m_hCamera <= 0) return -1;

            m_bExitCaptureThread = true;
            if (m_tCaptureThread != null && m_tCaptureThread.IsAlive)
            {
                try { m_tCaptureThread.Join(500); } catch { }
                m_tCaptureThread = null;
            }

            var st = MvApi.CameraPause(m_hCamera);
            if (st != CameraSdkStatus.CAMERA_STATUS_SUCCESS)
            {
                Debug.WriteLine("StopLive: CameraPause failed.");
                return -1;
            }

            _isGrabbing = false;
            return 1;
        }

        /// <summary>
        /// Lấy 1 frame ra Bitmap (snapshot). Có thể gọi mà không cần StartLive,
        /// SDK sẽ tự kéo frame.
        /// </summary>
        public Bitmap GrabFrame(int timeout = 1000)
        {
            try
            {
                lock (_lockObj)
                {
                    if (m_hCamera <= 0) return null;

                    var st = MvApi.CameraGetImageBuffer(m_hCamera, out tSdkFrameHead head, out IntPtr pRaw, (uint)timeout);
                    if (st != CameraSdkStatus.CAMERA_STATUS_SUCCESS || pRaw == IntPtr.Zero)
                        return null;

                    try
                    {
                        EnsureSnapshotBufferAllocated(head);

                        var st2 = MvApi.CameraImageProcess(m_hCamera, pRaw, m_ImageBufferSnapshot, ref head);
                        if (st2 != CameraSdkStatus.CAMERA_STATUS_SUCCESS)
                            return null;

                        bool mono = head.uiMediaType == (uint)emImageFormat.CAMERA_MEDIA_TYPE_MONO8;
                        int stride = mono ? head.iWidth : head.iWidth * 3;

                        var bmp = new Bitmap(
                            head.iWidth,
                            head.iHeight,
                            stride,
                            mono ? PixelFormat.Format8bppIndexed : PixelFormat.Format24bppRgb,
                            m_ImageBufferSnapshot);

                        if (mono) SetGrayPaletteIfNeeded(bmp);
                        return (Bitmap)bmp.Clone();  // clone cho an toàn
                    }
                    finally
                    {
                        MvApi.CameraReleaseImageBuffer(m_hCamera, pRaw);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                return null;
            }
        }

        public int SetExposureTime(int microseconds)
        {
            try
            {
                if (m_hCamera <= 0) return -1;
                if (microseconds < 0) microseconds = 0;
                // Turn off auto exposure to allow manual control
                try { MvApi.CameraSetAeState(m_hCamera, 0); } catch { }
                var st = MvApi.CameraSetExposureTime(m_hCamera, (double)microseconds);
                return st == CameraSdkStatus.CAMERA_STATUS_SUCCESS ? 1 : 0;
            }
            catch { return -1; }
        }

        public int GetExposureTime(out int microseconds)
        {
            microseconds = 0;
            try
            {
                if (m_hCamera <= 0) return -1;
                double us = 0;
                var st = MvApi.CameraGetExposureTime(m_hCamera, ref us);
                if (st == CameraSdkStatus.CAMERA_STATUS_SUCCESS)
                {
                    microseconds = (int)Math.Max(0, Math.Min(int.MaxValue, us));
                    return 1;
                }
                return 0;
            }
            catch { return -1; }
        }

        // ===== Nội bộ =====

        private CameraHandle FindAndInitDevice(string userDefinedName)
        {
            var st = MvApi.CameraEnumerateDevice(out tSdkCameraDevInfo[] devs);
            if (st != CameraSdkStatus.CAMERA_STATUS_SUCCESS || devs == null || devs.Length == 0)
            {
                Console.WriteLine("No camera found (try admin privileges).");
                return 0;
            }

            for (int i = 0; i < devs.Length; i++)
            {
                string sn = ReadCString(devs[i].acSn);
                string fn = ReadCString(devs[i].acFriendlyName);

                bool match =
                    string.IsNullOrEmpty(userDefinedName) ||
                    sn.Equals(userDefinedName, StringComparison.OrdinalIgnoreCase) ||
                    fn.Equals(userDefinedName, StringComparison.OrdinalIgnoreCase);

                if (!match) continue;

                var stInit = MvApi.CameraInit(
                    ref devs[i],
                    (int)emSdkParameterMode.PARAM_MODE_BY_SN,
                    (int)emSdkParameterTeam.PARAMETER_TEAM_A,
                    ref m_hCamera);

                if (stInit == CameraSdkStatus.CAMERA_STATUS_SUCCESS)
                    return m_hCamera;

                m_hCamera = 0;
                Console.WriteLine("Camera init error: " + MvApi.CameraGetErrorString(stInit));
            }

            return 0;
        }

        private void CaptureThreadProc()
        {
            while (!m_bExitCaptureThread)
            {
                var st = MvApi.CameraGetImageBuffer(m_hCamera, out tSdkFrameHead head, out IntPtr pRaw, 500);
                if (st != CameraSdkStatus.CAMERA_STATUS_SUCCESS) continue;

                try
                {
                    // RAW -> RGB
                    MvApi.CameraImageProcess(m_hCamera, pRaw, m_ImageBuffer, ref head);
                    // overlay OSD nếu có
                    MvApi.CameraImageOverlay(m_hCamera, m_ImageBuffer, ref head);
                    // hiển thị qua driver (nếu bạn có gọi CameraDisplayInit somewhere)
                    MvApi.CameraDisplayRGB24(m_hCamera, m_ImageBuffer, ref head);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex);
                }
                finally
                {
                    MvApi.CameraReleaseImageBuffer(m_hCamera, pRaw);
                }
            }
        }
    }
}