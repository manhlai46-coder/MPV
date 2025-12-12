using System;
using System.Drawing;
using System.Windows.Forms;
using MPV.Models;
using System.Collections.Generic;
using MPV.Service;
using MPV.Enums;
using MPV.Services;
using OpenCvSharp;
using System.Linq;
using System.IO;
using System.Drawing.Imaging;
using OpenCvSharp.Extensions;

namespace MPV.Renderers
{
    public class RoiPropertyPanel
    {
        public event Action RoiChanged; // sự kiện để Form1 lắng nghe
        private readonly FovManager fovManager;
        private readonly List<FovRegion> fovList;
        private readonly PictureBox pictureBox;
        private readonly Bitmap currentFovBitmap;
        private int selectedRoiIndex;
        private int selectedFovIndex;
        private readonly List<RoiRegion> roiList;
        private readonly HsvAutoService hsvAutoService = new HsvAutoService();

        public RoiPropertyPanel(
            FovManager fovManager,
            List<FovRegion> fovList,
            PictureBox pictureBox,
            Bitmap currentFovBitmap,
            int selectedFovIndex,
            int selectedRoiIndex,
            List<RoiRegion> roiList)
        {
            this.fovManager = fovManager;
            this.fovList = fovList;
            this.pictureBox = pictureBox;
            this.currentFovBitmap = currentFovBitmap;
            this.selectedFovIndex = selectedFovIndex;
            this.selectedRoiIndex = selectedRoiIndex;
            this.roiList = roiList;
        }

        public void ShowRoiProperties(Panel panelImage, RoiRegion roi)
        {
            panelImage.Controls.Clear();
            if (roi == null) return;

            var root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                AutoSize = true,
                Padding = new Padding(5)
            };
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90F));
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

            AddReadOnlyRow(root, "ID", roi.Id.ToString());
            var txtName = new TextBox { Text = roi.Name, Dock = DockStyle.Fill,ReadOnly = true };
            txtName.TextChanged += (s, e) => { roi.Name = txtName.Text; SaveRoi(); };
            AddControlRow(root, CreateLabel("Name"), txtName);

            var chkEnabled = new CheckBox { Checked = roi.IsEnabled, Dock = DockStyle.Left, Text = "" };
            chkEnabled.CheckedChanged += (s, e) => { roi.IsEnabled = chkEnabled.Checked; SaveRoi(); };
            AddControlRow(root, CreateLabel("Is Enabled"), chkEnabled);

            var roiGroup = new TableLayoutPanel { ColumnCount = 4, RowCount = 2, Dock = DockStyle.Top, AutoSize = true };
            for (int i = 0; i < 4; i++) roiGroup.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            roiGroup.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            roiGroup.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            roiGroup.Controls.Add(CreateMiniLabel("X"), 0, 0);
            roiGroup.Controls.Add(CreateValueBox(roi.X), 1, 0);
            roiGroup.Controls.Add(CreateMiniLabel("Y"), 2, 0);
            roiGroup.Controls.Add(CreateValueBox(roi.Y), 3, 0);

            roiGroup.Controls.Add(CreateMiniLabel("W"), 0, 1);
            roiGroup.Controls.Add(CreateValueBox(roi.Width), 1, 1);
            roiGroup.Controls.Add(CreateMiniLabel("H"), 2, 1);
            roiGroup.Controls.Add(CreateValueBox(roi.Height), 3, 1);

            AddControlRow(root, CreateLabel("ROI"), roiGroup);

            // type combobox
            var cboType = new ComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList };
            cboType.Items.AddRange(new object[] { "Unknown", "Component", "Marking" });
            cboType.SelectedItem = roi.Type ?? "Unknown";
            cboType.SelectedIndexChanged += (s, e) =>
            {
                roi.Type = cboType.SelectedItem.ToString();
                // Nếu là Marking: khởi tạo base và áp dụng bù lệch ngay
                if (string.Equals(roi.Type, "Marking", StringComparison.OrdinalIgnoreCase))
                {
                    if (roi.BaseX == 0 && roi.BaseY == 0)
                    {
                        roi.BaseX = roi.X;
                        roi.BaseY = roi.Y;
                    }
                    ApplyMarkingOffset(roi);
                }
                SaveRoi();
            };
            AddControlRow(root, CreateLabel("Type"), cboType);

            var cboAlg = new ComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList };
            cboAlg.Items.AddRange(new object[] { "Barcode", "HSV", "TemplateMatching" });
            string currentMode = roi.Mode;
            if (currentMode == "Template Matching") currentMode = "TemplateMatching";
            cboAlg.SelectedItem = currentMode ?? "Barcode";
            cboAlg.SelectedIndexChanged += (s, e) =>
            {
                string sel = cboAlg.SelectedItem.ToString();
                roi.Mode = sel == "TemplateMatching" ? "Template Matching" : sel;
                SaveRoi();
                pictureBox.Invalidate();
                RoiChanged?.Invoke();
            };
            AddControlRow(root, CreateLabel("Algorithm"), cboAlg);

            panelImage.Controls.Add(root);
        }

        private void RenderBarcodePanel(Panel panel, RoiRegion roi)
        {
            panel.Controls.Clear();
            var t = CreateInnerTable();
            AddReadOnlyRow(t, "X", roi.X.ToString());
            AddReadOnlyRow(t, "Y", roi.Y.ToString());
            AddReadOnlyRow(t, "Width", roi.Width.ToString());
            AddReadOnlyRow(t, "Height", roi.Height.ToString());

            // Barcode Algorithm selection
            var lblAlg = CreateLabel("Type:");
            var cbo = new ComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList };
            cbo.Items.AddRange(Enum.GetNames(typeof(BarcodeAlgorithm)));
            var current = roi.Algorithm.HasValue ? roi.Algorithm.Value.ToString() : BarcodeAlgorithm.QRCode.ToString();
            cbo.SelectedItem = current;
            cbo.SelectedIndexChanged += (s, e) =>
            {
                if (Enum.TryParse<BarcodeAlgorithm>(cbo.SelectedItem.ToString(), out var alg))
                {
                    roi.Algorithm = alg;
                    SaveRoi();
                    pictureBox.Invalidate();
                }
            };
            AddControlRow(t, lblAlg, cbo);

            // Expected length textbox
            var lblLen = CreateLabel("Max Length:");
            var txtLen = new TextBox { Text = roi.ExpectedLength.ToString(), Dock = DockStyle.Fill };
            txtLen.TextChanged += (s, e) =>
            {
                if (int.TryParse(txtLen.Text, out var len))
                {
                    roi.ExpectedLength = len;
                    SaveRoi();
                }
            };
            AddControlRow(t, lblLen, txtLen);

            AddHiddenCheckbox(t, roi);
            panel.Controls.Add(t);
        }

        private void RenderHsvPanel(Panel panel, RoiRegion roi)
        {
            panel.Controls.Clear();
            var t = CreateInnerTable();
            AddReadOnlyRow(t, "X", roi.X.ToString());
            AddReadOnlyRow(t, "Y", roi.Y.ToString());
            AddReadOnlyRow(t, "Width", roi.Width.ToString());
            AddReadOnlyRow(t, "Height", roi.Height.ToString());
            AutoComputeHSVIfNeeded(roi);
            AddReadOnlyRow(t, "H", $"{roi.Lower.H} - {roi.Upper.H}");
            AddReadOnlyRow(t, "S", $"{roi.Lower.S} - {roi.Upper.S}");
            AddReadOnlyRow(t, "V", $"{roi.Lower.V} - {roi.Upper.V}");
            var btn = new Button { Text = "Get HSV", Height = 30, Dock = DockStyle.Top };
            btn.Click += (s, e) =>
            {
                AutoComputeHSVIfNeeded(roi, true);
                RenderHsvPanel(panel, roi);
                pictureBox.Invalidate();
            };
            t.Controls.Add(btn);
            t.SetColumnSpan(btn, 2);
            AddHiddenCheckbox(t, roi);
            panel.Controls.Add(t);
        }

        private void RenderTemplatePanel(Panel panel, RoiRegion roi)
        {
            panel.Controls.Clear();
            var t = CreateInnerTable();
            AddReadOnlyRow(t, "X", roi.X.ToString());
            AddReadOnlyRow(t, "Y", roi.Y.ToString());
            AddReadOnlyRow(t, "Width", roi.Width.ToString());
            AddReadOnlyRow(t, "Height", roi.Height.ToString());
            var btn = new Button { Text = "Get Template", Height = 30, Dock = DockStyle.Top };
            btn.Click += (s, e) => { GetTemplateFromRoi(roi); RenderTemplatePanel(panel, roi); };
            t.Controls.Add(btn);
            t.SetColumnSpan(btn, 2);
            if (roi.Template != null)
            {
                AddReadOnlyRow(t, "Template Size", $"{roi.Template.Width} x {roi.Template.Height}");
                AddReadOnlyRow(t, "Match Score", roi.MatchScore.ToString("F3"));
            }
            AddHiddenCheckbox(t, roi);
            panel.Controls.Add(t);
        }

        private void GetTemplateFromRoi(RoiRegion roi)
        {
            if (currentFovBitmap == null) return;
            Rectangle rect = new Rectangle(roi.X, roi.Y, roi.Width, roi.Height);
            rect.Intersect(new Rectangle(0, 0, currentFovBitmap.Width, currentFovBitmap.Height));
            if (rect.Width <= 0 || rect.Height <= 0) return;
            roi.Template?.Dispose();
            roi.Template = null;
            Bitmap bmp = new Bitmap(rect.Width, rect.Height);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.DrawImage(currentFovBitmap, new Rectangle(0, 0, rect.Width, rect.Height), rect, GraphicsUnit.Pixel);
            }
            roi.Template = bmp;
            roi.TemplateBase64 = ImageToBase64(bmp);
            roi.MatchScore = 0;
            roi.MatchRect = Rectangle.Empty;
            SaveRoi();
        }

        private string ImageToBase64(Bitmap bmp)
        {
            using (var ms = new MemoryStream())
            {
                bmp.Save(ms, ImageFormat.Png);
                return Convert.ToBase64String(ms.ToArray());
            }
        }

        private void AutoComputeHSVIfNeeded(RoiRegion roi, bool force = false)
        {
            if (!force && roi.Lower != null && roi.Upper != null) return;
            if (currentFovBitmap == null) return;
            Rectangle rect = new Rectangle(roi.X, roi.Y, roi.Width, roi.Height);
            rect.Intersect(new Rectangle(0, 0, currentFovBitmap.Width, currentFovBitmap.Height));
            if (rect.Width <= 0 || rect.Height <= 0) return;
            var bmp = new Bitmap(rect.Width, rect.Height);
            using (Graphics g = Graphics.FromImage(bmp))
                g.DrawImage(currentFovBitmap, new Rectangle(0, 0, rect.Width, rect.Height), rect, GraphicsUnit.Pixel);
            var (lower, upper, _) = hsvAutoService.Compute(bmp, 15, 10);
            roi.Lower = lower;
            roi.Upper = upper;
            SaveRoi();
        }

        public void RunTemplateMatching(RoiRegion roi)
        {
            if (roi == null || roi.Template == null || currentFovBitmap == null) return;
            Mat img = BitmapConverter.ToMat(currentFovBitmap);
            Mat templ = BitmapConverter.ToMat(roi.Template);
            if (img.Width < templ.Width || img.Height < templ.Height)
            {
                roi.MatchScore = 0;
                roi.MatchRect = Rectangle.Empty;
                SaveRoi();
                return;
            }
            Mat result = new Mat(img.Rows - templ.Rows + 1, img.Cols - templ.Cols + 1, MatType.CV_32FC1);
            Cv2.MatchTemplate(img, templ, result, TemplateMatchModes.CCoeffNormed);
            result.MinMaxLoc(out double _, out double maxVal, out _, out OpenCvSharp.Point maxLoc);
            roi.MatchScore = maxVal;
            roi.MatchRect = new Rectangle(maxLoc.X, maxLoc.Y, templ.Width, templ.Height);

            // Nếu là Marking, cập nhật bù lệch cho các ROI khác
            if (string.Equals(roi.Type, "Marking", StringComparison.OrdinalIgnoreCase))
            {
                // Cập nhật base nếu chưa có
                if (roi.BaseX == 0 && roi.BaseY == 0)
                {
                    roi.BaseX = roi.X;
                    roi.BaseY = roi.Y;
                }
                ApplyMarkingOffset(roi);
            }

            SaveRoi();
        }

        private void SaveRoi()
        {
            if (selectedRoiIndex >= 0 && selectedRoiIndex < roiList.Count && selectedFovIndex >= 0 && selectedFovIndex < fovList.Count)
            {
                fovList[selectedFovIndex].Rois = roiList;
                fovManager.Save(fovList);
            }
        }

        private TableLayoutPanel CreateInnerTable()
        {
            var t = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                ColumnCount = 2,
                AutoSize = true,
                Padding = new Padding(5)
            };
            t.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120F));
            t.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            return t;
        }

        private Label CreateLabel(string text) => new Label { Text = text, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft };

        private void AddReadOnlyRow(TableLayoutPanel t, string label, string value)
        {
            int row = t.RowCount;
            t.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            t.Controls.Add(CreateLabel(label + ":"), 0, row);
            t.Controls.Add(new TextBox { Text = value, ReadOnly = true, Dock = DockStyle.Fill }, 1, row);
            t.RowCount++;
        }

        private void AddControlRow(TableLayoutPanel t, Control left, Control right)
        {
            int row = t.RowCount;
            t.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            t.Controls.Add(left, 0, row);
            t.Controls.Add(right, 1, row);
            t.RowCount++;
        }

        private void AddHiddenCheckbox(TableLayoutPanel t, RoiRegion roi)
        {
            var chk = new CheckBox { Text = "Hidden", Checked = roi.IsHidden, Dock = DockStyle.Fill };
            chk.CheckedChanged += (s, e) =>
            {
                roi.IsHidden = chk.Checked;
                SaveRoi();
                pictureBox.Invalidate();
            };
            AddControlRow(t, CreateLabel("IsHidden:"), chk);
        }

        private Label CreateMiniLabel(string text) => new Label { Text = text, AutoSize = true, Padding = new Padding(3, 3, 3, 3) };
        private TextBox CreateValueBox(int v) => new TextBox { Text = v.ToString(), ReadOnly = true, Width = 60 };

        public void RenderModePanel(Panel panel, RoiRegion roi)
        {
            if (roi == null) { panel.Controls.Clear(); return; }
            var mode = roi.Mode ?? "Barcode";
            if (string.Equals(mode, "Template Matching", StringComparison.OrdinalIgnoreCase) || string.Equals(mode, "TemplateMatching", StringComparison.OrdinalIgnoreCase))
            {
                RenderTemplatePanel(panel, roi);
            }
            else if (string.Equals(mode, "HSV", StringComparison.OrdinalIgnoreCase))
            {
                RenderHsvPanel(panel, roi);
            }
            else // Barcode default
            {
                RenderBarcodePanel(panel, roi);
            }
        }

        // Áp dụng bù lệch cho các ROI khác dựa trên Marking
        private void ApplyMarkingOffset(RoiRegion marker)
        {
            int markerPosX = marker.MatchRect != Rectangle.Empty ? marker.MatchRect.X : marker.X;
            int markerPosY = marker.MatchRect != Rectangle.Empty ? marker.MatchRect.Y : marker.Y;
            int dx = markerPosX - marker.BaseX;
            int dy = markerPosY - marker.BaseY;

            foreach (var r in roiList)
            {
                if (ReferenceEquals(r, marker)) continue;
                if (string.Equals(r.Type, "Marking", StringComparison.OrdinalIgnoreCase)) continue;

                if (r.BaseX == 0 && r.BaseY == 0)
                {
                    r.BaseX = r.X;
                    r.BaseY = r.Y;
                }

                r.X = r.BaseX + dx;
                r.Y = r.BaseY + dy;
            }

            SaveRoi();
            pictureBox.Invalidate();
            RoiChanged?.Invoke();
        }
    }
}
