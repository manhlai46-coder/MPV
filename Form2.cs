using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using MPV.Models;
using MPV.Service;
using MPV.Services;
using OpenCvSharp;
using OpenCvSharp.Extensions;
using System.IO.Ports;

namespace MPV
{
    public partial class lb_pass : Form
    {
        private Form1 _form1;
        private int _numpass;
        private int _numfail;
        private List<FovRegion> _fovs = new List<FovRegion>();
        private FovManager _fovManager;
        private bool _lastAllPass; // track last overall result

        // services for evaluation
        private readonly BarcodeService _barcodeService;
        private readonly HsvService _hsvService;
        private readonly HsvAutoService _hsvAutoService = new HsvAutoService();
        private string _lastDecodedCode; // last scanned SN
        private const int ExpectedCodeLength = 22; // required SN length
        private readonly List<Bitmap> _capturedFrames = new List<Bitmap>();
        private readonly Dictionary<int, Bitmap> _liveFramesByFov = new Dictionary<int, Bitmap>();
        private bool _useLiveFramesForPreview;
        private bool _suppressSnEvent;

        public lb_pass(Form1 form1)
        {
            InitializeComponent();
            _form1 = form1;
            try
            {
                string fovPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "fov_data.json");
                _fovManager = new FovManager(fovPath);
            }
            catch { }

            _barcodeService = new BarcodeService();
            _hsvService = new HsvService();

            // Ensure the PTR image scales with the window
            try
            {
                if (ptr_image != null)
                {
                    ptr_image.SizeMode = PictureBoxSizeMode.Zoom;
                    ptr_image.Dock = DockStyle.Fill;
                }
                // Redraw PASS/FAIL text when panel resizes
                if (panel1 != null)
                {
                    panel1.Resize += (s, e) => panel1.Invalidate();
                    panel1.Paint += panel1_Paint;
                }
            }
            catch { }
        }
        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            ResetCapturedFrames();
            ResetLiveFrames();
            _form1.Show();  // khi Form2 tắt → hiện lại Form1
            base.OnFormClosed(e);
        }

        // Public API: cho phép gọi từ code khác với SN nhập tay
        public void RunBySn(string sn)
        {
            if (string.IsNullOrWhiteSpace(sn) || sn.Length != ExpectedCodeLength)
            {
                return; // không đủ 22 ký tự thì bỏ qua
            }

            _lastDecodedCode = sn;
            try
            {
                RunAllFovs(true);
                DrawVerticalSplits();
            }
            catch
            {
            }
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            // Reload data when the form becomes visible so preview matches latest setup
            try { _fovs = _fovManager != null ? _fovManager.Load() : new List<FovRegion>(); } catch { _fovs = new List<FovRegion>(); }
            try
            {
                DrawVerticalSplits();
            }
            catch { }
        }

        private void Form2_Load(object sender, EventArgs e)
        {
            
            try { _fovs = _fovManager != null ? _fovManager.Load() : new List<FovRegion>(); } catch { _fovs = new List<FovRegion>(); }

            // Ensure ptr_image still fills in case designer overrides
            try
            {
                if (ptr_image != null)
                {
                    ptr_image.SizeMode = PictureBoxSizeMode.Zoom;
                    ptr_image.Dock = DockStyle.Fill;
                }
            }
            catch { }

            // Only refresh the preview on load; wait for SN scans before running live tests
            try
            {
                DrawVerticalSplits();
            }
            catch { }
        }

        private void autoRunToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try { _fovs = _fovManager != null ? _fovManager.Load() : new List<FovRegion>(); } catch { _fovs = new List<FovRegion>(); }
            try
            {
                DrawVerticalSplits();
            }
            catch { }
        }

        private void resetToolStripMenuItem_Click(object sender, EventArgs e)
        {
            _numpass = 0;
            _numfail = 0;
            label1.Text = "Pass";
            lb_fail.Text = "Fail";
            _lastAllPass = false;
            panel1.Invalidate();

        }

        private void panel1_Paint(object sender, PaintEventArgs e)
        {
            // draw centered PASS/FAIL text on panel1
            string text = panel1.BackColor == Color.Green ? "PASS" : (panel1.BackColor == Color.Red ? "FAIL" : "");
            if (string.IsNullOrEmpty(text)) return;
            using (var f = new Font("Segoe UI", 16f, FontStyle.Bold))
            using (var b = new SolidBrush(Color.White))
            {
                var size = e.Graphics.MeasureString(text, f);
                float x = (panel1.ClientSize.Width - size.Width) / 2f;
                float y = (panel1.ClientSize.Height - size.Height) / 2f;
                e.Graphics.DrawString(text, f, b, x, y);
            }
        }

        private Bitmap LoadFovBitmap(FovRegion fov)
        {
            if (fov == null) return null;
            try
            {
                if (!string.IsNullOrEmpty(fov.ImagePath) && File.Exists(fov.ImagePath))
                {
                    return new Bitmap(fov.ImagePath);
                }
                if (!string.IsNullOrEmpty(fov.ImageBase64))
                {
                    return Base64ToBitmap(fov.ImageBase64);
                }
            }
            catch { }
            return null;
        }

        private Bitmap CaptureLiveFov(FovRegion fov)
        {
            if (_form1 == null) return null;
            try { return _form1.CaptureFovImage(fov); }
            catch { return null; }
        }

        private void ResetCapturedFrames()
        {
            if (_capturedFrames.Count == 0) return;
            foreach (var frame in _capturedFrames)
            {
                try { frame.Dispose(); }
                catch { }
            }
            _capturedFrames.Clear();
        }

        private void ResetLiveFrames()
        {
            if (_liveFramesByFov.Count == 0) return;
            foreach (var kvp in _liveFramesByFov)
            {
                try { kvp.Value?.Dispose(); }
                catch { }
            }
            _liveFramesByFov.Clear();
        }

        private void StoreLiveFrame(int fovIndex, Bitmap frame)
        {
            if (frame == null) return;
            if (_liveFramesByFov.TryGetValue(fovIndex, out var oldFrame))
            {
                try { oldFrame.Dispose(); }
                catch { }
            }
            _liveFramesByFov[fovIndex] = frame;
        }

        private Bitmap Base64ToBitmap(string base64)
        {
            try
            {
                var bytes = Convert.FromBase64String(base64);
                using (var ms = new MemoryStream(bytes))
                {
                    return new Bitmap(ms);
                }
            }
            catch { return null; }
        }

        private void DrawVerticalSplits()
        {
            int count = _fovs != null ? _fovs.Count : 0;
            if (count <= 0)
            {
                ptr_image.Image = null;
                return;
            }
            var bmp = new Bitmap(Math.Max(1, ptr_image.Width), Math.Max(1, ptr_image.Height));
            using (var g = Graphics.FromImage(bmp))
            {
                g.Clear(Color.Black);
                int sectionWidth = Math.Max(1, bmp.Width / count);
                for (int i = 0; i < count; i++)
                {
                    var rect = new Rectangle(i * sectionWidth, 0, i == count - 1 ? bmp.Width - i * sectionWidth : sectionWidth, bmp.Height);

                    // draw a background for the section
                    using (var brush = new SolidBrush(i % 2 == 0 ? Color.DimGray : Color.Gray))
                    { g.FillRectangle(brush, rect); }

                    // draw a border and label
                    using (var pen = new Pen(Color.White, 2)) { g.DrawRectangle(pen, rect); }
                    using (var font = new Font("Segoe UI", 10f))
                    using (var textBrush = new SolidBrush(Color.Yellow))
                    { g.DrawString($"FOV {i + 1}", font, textBrush, rect.X + 6, rect.Y + 6); }

                    Bitmap fovBmp = null;
                    bool disposeAfterDraw = false;
                    try
                    {
                        if (_useLiveFramesForPreview)
                        {
                            _liveFramesByFov.TryGetValue(i, out fovBmp);
                        }
                        else
                        {
                            try { fovBmp = LoadFovBitmap(_fovs[i]); }
                            catch { fovBmp = null; }
                            disposeAfterDraw = fovBmp != null;
                        }
                        Rectangle destRect = Rectangle.Empty;
                        if (fovBmp != null)
                        {
                            try
                            {
                                // compute fitted destination rect preserving aspect ratio, fill section area (no reserved top space)
                                float imgAspect = fovBmp.Width / (float)fovBmp.Height;
                                float rectAspect = rect.Width / (float)rect.Height;
                                int destW, destH;
                                if (imgAspect > rectAspect)
                                {
                                    destW = rect.Width - 4; // small padding
                                    destH = (int)(destW / imgAspect);
                                }
                                else
                                {
                                    destH = rect.Height - 4; // small padding
                                    destW = (int)(destH * imgAspect);
                                }
                                if (destW <= 0) destW = 1;
                                if (destH <= 0) destH = 1;
                                int destX = rect.X + (rect.Width - destW) / 2;
                                int destY = rect.Y + (rect.Height - destH) / 2;
                                destRect = new Rectangle(destX, destY, destW, destH);
                                g.DrawImage(fovBmp, destRect);

                                // draw label on top-left corner (over image)
                                using (var font = new Font("Segoe UI", 10f))
                                using (var textBrush = new SolidBrush(Color.Yellow))
                                {
                                    g.DrawString($"FOV {i + 1}", font, textBrush, rect.X + 6, rect.Y + 6);
                                }

                                // draw ROIs for this FOV over destRect
                                var rois = _fovs[i].Rois ?? new List<RoiRegion>();
                                float scaleX = destRect.Width / (float)fovBmp.Width;
                                float scaleY = destRect.Height / (float)fovBmp.Height;
                                for (int r = 0; r < rois.Count; r++)
                                {
                                    var roi = rois[r];
                                    if (roi.IsHidden) continue;
                                    // compute display rect
                                    var dr = new Rectangle(
                                        destRect.X + (int)(roi.X * scaleX),
                                        destRect.Y + (int)(roi.Y * scaleY),
                                        Math.Max(1, (int)(roi.Width * scaleX)),
                                        Math.Max(1, (int)(roi.Height * scaleY))
                                    );
                                    // determine pass/fail: use LastScore if present else treat as fail
                                    bool pass = false;
                                    try { pass = EvaluateScoreLocal(roi, roi.LastScore); } catch { pass = false; }

                                    using (var penR = new Pen(pass ? Color.LimeGreen : Color.Red, 2))
                                    {
                                        g.DrawRectangle(penR, dr);
                                    }
                                    using (var fnt = new Font("Arial", 9, FontStyle.Bold))
                                    using (var br = new SolidBrush(pass ? Color.LimeGreen : Color.Red))
                                    {
                                        g.DrawString($"{r + 1}", fnt, br, dr.X, Math.Max(rect.Y + 2, dr.Y - 16));
                                    }
                                }

                            }
                            catch { }
                        }
                        else
                        {
                            // draw placeholder text
                            using (var font = new Font("Segoe UI", 9f))
                            using (var tb = new SolidBrush(Color.White))
                            {
                                var msg = "(No image)";
                                var size = g.MeasureString(msg, font);
                                float x = rect.X + (rect.Width - size.Width) / 2;
                                float y = rect.Y + (rect.Height - size.Height) / 2;
                                g.DrawString(msg, font, tb, x, y);
                            }
                        }
                    }
                    finally
                    {
                        if (disposeAfterDraw && fovBmp != null)
                        {
                            fovBmp.Dispose();
                        }
                    }
                }
            }
            // replace existing image on picturebox
            var old = ptr_image.Image;
            ptr_image.Image = bmp;
            if (old != null) try { old.Dispose(); } catch { }
        }

        private bool EvaluateScoreLocal(RoiRegion roi, int score)
        {
            bool inRange = score >= roi.OkScoreLower && score <= roi.OkScoreUpper;
            return roi.ReverseSearch ? !inRange : inRange;
        }

        // Determine whether ROI mode is template matching
        private bool IsTemplateMode(string mode)
        {
            if (string.IsNullOrEmpty(mode)) return false;
            return string.Equals(mode, "Template Matching", StringComparison.OrdinalIgnoreCase)
                || string.Equals(mode, "TemplateMatching", StringComparison.OrdinalIgnoreCase);
        }

        private int RunTemplateMatchingLocal(RoiRegion roi, Bitmap fovBitmap, out Rectangle matchRect, out double matchScore)
        {
            matchRect = Rectangle.Empty;
            matchScore = 0;
            if (roi == null || roi.Template == null || fovBitmap == null) return 0;

            var searchRect = new Rectangle(roi.X, roi.Y, roi.Width, roi.Height);
            var fovRect = new Rectangle(0, 0, fovBitmap.Width, fovBitmap.Height);
            searchRect.Intersect(fovRect);
            if (searchRect.Width <= 0 || searchRect.Height <= 0) return 0;

            using (var searchBmp = new Bitmap(searchRect.Width, searchRect.Height))
            using (var g = Graphics.FromImage(searchBmp))
            {
                g.DrawImage(fovBitmap, new Rectangle(0, 0, searchRect.Width, searchRect.Height), searchRect, GraphicsUnit.Pixel);

                using (Mat img = BitmapConverter.ToMat(searchBmp))
                using (Mat templ = BitmapConverter.ToMat(roi.Template))
                using (Mat imgGray = new Mat())
                using (Mat templGray = new Mat())
                {
                    if (img.Empty() || templ.Empty()) return 0;

                    if (img.Channels() > 1)
                        Cv2.CvtColor(img, imgGray, ColorConversionCodes.BGR2GRAY);
                    else
                        img.CopyTo(imgGray);

                    if (templ.Channels() > 1)
                        Cv2.CvtColor(templ, templGray, ColorConversionCodes.BGR2GRAY);
                    else
                        templ.CopyTo(templGray);

                    if (imgGray.Width < templGray.Width || imgGray.Height < templGray.Height)
                        return 0;

                    using (Mat result = new Mat(imgGray.Rows - templGray.Rows + 1, imgGray.Cols - templGray.Cols + 1, MatType.CV_32FC1))
                    {
                        Cv2.MatchTemplate(imgGray, templGray, result, TemplateMatchModes.CCoeffNormed);
                        result.MinMaxLoc(out double _, out double maxVal, out _, out OpenCvSharp.Point maxLoc);

                        matchScore = maxVal;
                        matchRect = new Rectangle(searchRect.X + maxLoc.X, searchRect.Y + maxLoc.Y, templGray.Width, templGray.Height);
                        int scoreInt = (int)Math.Round(maxVal * 100.0);
                        return scoreInt;
                    }
                }
            }
        }



        private void RunAllFovs(bool captureFromCamera)
        {
            ResetLiveFrames();
            _useLiveFramesForPreview = captureFromCamera;
            if (captureFromCamera)
            {
                ResetCapturedFrames();
            }

            if (_fovs == null || _fovs.Count == 0)
            {
                panel1.BackColor = Color.Red;
                _lastAllPass = false;
                panel1.Invalidate();
                _numfail++;
                lb_fail.Text = $"Fail: {_numfail}";
                if (captureFromCamera)
                {
                    ResetCapturedFrames();
                }
                return;
            }

            bool allFovsPass = true;
            for (int i = 0; i < _fovs.Count; i++)
            {
                var f = _fovs[i];
                bool fovPass = true;
                Bitmap fovBmp = captureFromCamera ? CaptureLiveFov(f) : LoadFovBitmap(f);

                if (captureFromCamera && fovBmp != null)
                {
                    StoreLiveFrame(i, fovBmp);
                    if (_capturedFrames.Count < 2)
                    {
                        try { _capturedFrames.Add((Bitmap)fovBmp.Clone()); }
                        catch { }
                    }
                }

                if (fovBmp == null)
                {
                    fovPass = false;
                }
                else
                {
                    try
                    {
                        var rois = f.Rois ?? new List<RoiRegion>();
                        for (int r = 0; r < rois.Count; r++)
                        {
                            var roi = rois[r];
                            if (roi.IsHidden) continue;

                            int score = 0;
                            // ensure template loaded
                            if (roi.Template == null && !string.IsNullOrEmpty(roi.TemplateBase64))
                            {
                                try { roi.Template = Base64ToBitmap(roi.TemplateBase64); } catch { roi.Template = null; }
                            }

                            if (IsTemplateMode(roi.Mode) && roi.Template != null)
                            {
                                Rectangle mrect; double mscore;
                                score = RunTemplateMatchingLocal(roi, fovBmp, out mrect, out mscore);
                                roi.MatchScore = mscore; roi.MatchRect = mrect;
                            }
                            else if (string.Equals(roi.Mode, "HSV", StringComparison.OrdinalIgnoreCase))
                            {
                                Rectangle rect = new Rectangle(roi.X, roi.Y, roi.Width, roi.Height);
                                rect.Intersect(new Rectangle(0, 0, fovBmp.Width, fovBmp.Height));
                                if (rect.Width <= 0 || rect.Height <= 0) { score = 0; }
                                else
                                {
                                    using (var roiBmp = new Bitmap(rect.Width, rect.Height))
                                    using (var g = Graphics.FromImage(roiBmp))
                                    {
                                        g.DrawImage(fovBmp, new Rectangle(0, 0, rect.Width, rect.Height), rect, GraphicsUnit.Pixel);
                                        var (lowerAuto, upperAuto, _) = _hsvAutoService.Compute(roiBmp, 15, 10);
                                        if (roi.Lower == null || roi.Upper == null)
                                        {
                                            roi.Lower = lowerAuto; roi.Upper = upperAuto;
                                        }
                                        var lowerRange = new HsvRange(roi.Lower.H, roi.Lower.H, roi.Lower.S, roi.Lower.S, roi.Lower.V, roi.Lower.V);
                                        var upperRange = new HsvRange(roi.Upper.H, roi.Upper.H, roi.Upper.S, roi.Upper.S, roi.Upper.V, roi.Upper.V);
                                        double matchPct;
                                        _hsvService.DetectColor(roiBmp, lowerRange, upperRange, out matchPct);
                                        score = (int)Math.Round(matchPct);
                                    }
                                }
                            }
                            else
                            {
                                Rectangle rect = new Rectangle(roi.X, roi.Y, roi.Width, roi.Height);
                                rect.Intersect(new Rectangle(0, 0, fovBmp.Width, fovBmp.Height));
                                if (rect.Width <= 0 || rect.Height <= 0) { score = 0; }
                                else
                                {
                                    using (var roiBmp = new Bitmap(rect.Width, rect.Height))
                                    using (var g = Graphics.FromImage(roiBmp))
                                    {
                                        g.DrawImage(fovBmp, new Rectangle(0, 0, rect.Width, rect.Height), rect, GraphicsUnit.Pixel);
                                        var algorithm = roi.Algorithm ?? MPV.Enums.BarcodeAlgorithm.QRCode;
                                        string decoded = _barcodeService.Decode(roiBmp, algorithm);
                                        if (!string.IsNullOrWhiteSpace(decoded))
                                        {
                                            _lastDecodedCode = decoded; // track last scanned code
                                        }
                                        bool ok = !string.IsNullOrWhiteSpace(decoded) && decoded.Length == ExpectedCodeLength;
                                        score = ok ? 100 : 0;
                                    }
                                }
                            }

                            roi.LastScore = score;
                            bool roiPass = EvaluateScoreLocal(roi, score);
                            if (!roiPass) fovPass = false;
                        }
                    }
                    finally
                    {
                        if (!captureFromCamera)
                        {
                            fovBmp.Dispose();
                        }
                    }
                }

                if (!fovPass) allFovsPass = false;
            }

            _lastAllPass = allFovsPass;
            if (allFovsPass)
            {
                panel1.BackColor = Color.Green;
                label1.Text = $"Pass: {++_numpass}";
            }
            else
            {
                panel1.BackColor = Color.Red;
                lb_fail.Text = $"Fail: {++_numfail}";
            }
            panel1.Invalidate();

            bool haveValidSn = !string.IsNullOrWhiteSpace(_lastDecodedCode) && _lastDecodedCode.Length == ExpectedCodeLength;
            try
            {
                if (haveValidSn)
                {
                    SaveLogAndImages(_lastAllPass, _lastDecodedCode);
                }
            }
            catch
            {
            }
            finally
            {
                if (captureFromCamera)
                {
                    ResetCapturedFrames();
                }
            }

            // Save results back so DrawVerticalSplits can use LastScore
            try { _fovManager.Save(_fovs); } catch { }
        }

        private void SaveLogAndImages(bool allPass, string snCode)
        {
            try
            {
                string status = allPass ? "OK" : "FAIL";

                string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                string logsRoot = Path.Combine(baseDir, "logs");
                string dateFolder = DateTime.Now.ToString("yyyy-MM-dd");
                string dateDir = Path.Combine(logsRoot, dateFolder);

                string statusDir = Path.Combine(dateDir, status);

                string safeSn = MakeSafeFolderName(snCode);
                string snDir = Path.Combine(statusDir, safeSn);

                if (!Directory.Exists(snDir))
                {
                    Directory.CreateDirectory(snDir);
                }

                SaveFovImagesToFolder(snDir);
            }
            catch
            {
            }
        }

        private string MakeSafeFolderName(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return "UNKNOWN";
            }

            foreach (char c in Path.GetInvalidFileNameChars())
            {
                name = name.Replace(c, '_');
            }

            return name;
        }

        private void SaveFovImagesToFolder(string targetFolder)
        {
            if (_fovs == null || _fovs.Count == 0)
            {
                ResetCapturedFrames();
                return;
            }

            try
            {
                if (!Directory.Exists(targetFolder))
                {
                    Directory.CreateDirectory(targetFolder);
                }

                string timeStamp = DateTime.Now.ToString("HHmmss");

                for (int i = 0; i < _fovs.Count; i++)
                {
                    Bitmap frame = null;
                    if (_liveFramesByFov.TryGetValue(i, out var liveFrame) && liveFrame != null)
                    {
                        frame = liveFrame;
                    }
                    else if (i < _capturedFrames.Count)
                    {
                        frame = _capturedFrames[i];
                    }

                    if (frame == null)
                    {
                        continue;
                    }

                    using (var annotated = CreateAnnotatedImage(frame, _fovs[i]))
                    {
                        if (annotated == null)
                        {
                            continue;
                        }

                        string fileName = string.Format("{0}_FOV{1}.png", timeStamp, i + 1);
                        string path = Path.Combine(targetFolder, fileName);
                        annotated.Save(path, System.Drawing.Imaging.ImageFormat.Png);
                    }
                }
            }
            catch
            {
            }
            finally
            {
                ResetCapturedFrames();
            }
        }

        private void txt_sn_TextChanged(object sender, EventArgs e)
        {
            if (_suppressSnEvent)
            {
                return;
            }

            var text = txt_sn?.Text;
            if (string.IsNullOrWhiteSpace(text))
            {
                return;
            }

            // Normalize input in case scanner appends CR/LF or spaces
            text = text.Replace("\r", string.Empty)
                       .Replace("\n", string.Empty)
                       .Trim();

            if (text.Length != ExpectedCodeLength)
            {
                return;
            }

            string sn = text;

            try
            {
                _suppressSnEvent = true;
                txt_sn.Clear();
            }
            finally
            {
                _suppressSnEvent = false;
            }

            RunBySn(sn);
        }

        private Bitmap CreateAnnotatedImage(Bitmap source, FovRegion fov)
        {
            if (source == null || fov == null)
            {
                return null;
            }

            var annotated = new Bitmap(source.Width, source.Height);
            using (var g = Graphics.FromImage(annotated))
            {
                g.DrawImage(source, 0, 0, source.Width, source.Height);
                var rois = fov.Rois ?? new List<RoiRegion>();
                using (var font = new Font("Segoe UI", 9f, FontStyle.Bold))
                {
                    for (int r = 0; r < rois.Count; r++)
                    {
                        var roi = rois[r];
                        if (roi.IsHidden) continue;

                        var rect = new Rectangle(roi.X, roi.Y, roi.Width, roi.Height);
                        rect.Intersect(new Rectangle(0, 0, annotated.Width, annotated.Height));
                        if (rect.Width <= 0 || rect.Height <= 0) continue;

                        bool pass = EvaluateScoreLocal(roi, roi.LastScore);
                        using (var pen = new Pen(pass ? Color.LimeGreen : Color.Red, 2))
                        {
                            g.DrawRectangle(pen, rect);
                        }

                        string label = $"ROI {r + 1}";
                        var textSize = g.MeasureString(label, font);
                        float labelX = rect.X;
                        float labelY = Math.Max(0, rect.Y - textSize.Height - 2);
                        var textRect = new RectangleF(labelX, labelY, textSize.Width + 4, textSize.Height);
                        using (var bgBrush = new SolidBrush(Color.FromArgb(160, 0, 0, 0)))
                        {
                            g.FillRectangle(bgBrush, textRect);
                        }
                        using (var textBrush = new SolidBrush(pass ? Color.LimeGreen : Color.Red))
                        {
                            g.DrawString(label, font, textBrush, labelX + 2, labelY);
                        }
                    }
                }
            }

            return annotated;
        }




        // gửi data IT
        SerialPort _serialPort = new SerialPort("COM7");
        public void SendDataIT(string sn, bool isPass)
        {
            string snsent = txt_sn.Text.Substring(0, 12);
            string status = isPass ? "PASS" : "FAIL";
            string datareceive;
            if (!_serialPort.IsOpen)
            {
                _serialPort.Open();
            }
            if (status == "PASS")
            {

                string dataToSend = snsent + "             "+ "CHECK_CCD1++";
                _serialPort.WriteLine(dataToSend);
            }
            else
            {
                string dataToSend = snsent + "             " + "CHECK_CCD1++"+"FAIL";
                _serialPort.WriteLine(dataToSend);
            }
           
        }
    }
}
