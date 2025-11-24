using System;
using System.Drawing;
using System.Windows.Forms;
using MPV.Models;
using System.Collections.Generic;
using MPV.Service;
using MPV.Enums;
using MPV.Services;

namespace MPV.Renderers
{
    public class RoiPropertyPanel
    {
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
                Padding = new Padding(10)
            };
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120F));
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

            // Mode selector
            var lblMode = CreateLabel("Mode:");
            var cboMode = new ComboBox
            {
                Dock = DockStyle.Fill,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cboMode.Items.AddRange(new object[] { "Barcode", "HSV" });
            cboMode.SelectedItem = roi.Mode ?? "Barcode";
            root.Controls.Add(lblMode, 0, 0);
            root.Controls.Add(cboMode, 1, 0);

            // Main content panel
            var panelContent = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true
            };
            root.Controls.Add(panelContent, 0, 1);
            root.SetColumnSpan(panelContent, 2);

            panelImage.Controls.Add(root);

            void RenderBarcode()
            {
                panelContent.Controls.Clear();
                var t = CreateInnerTable();
                AddReadOnlyRow(t, "X", roi.X.ToString());
                AddReadOnlyRow(t, "Y", roi.Y.ToString());
                AddReadOnlyRow(t, "Width", roi.Width.ToString());
                AddReadOnlyRow(t, "Height", roi.Height.ToString());

                // Algorithm combo
                var lblAlg = CreateLabel("Algorithm:");
                var cboAlg = new ComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList };
                cboAlg.Items.AddRange(new object[]
                {
                    BarcodeAlgorithm.QRCode,
                    BarcodeAlgorithm.Code128,
                    BarcodeAlgorithm.EAN13,
                    BarcodeAlgorithm.DataMatrix,
                    BarcodeAlgorithm.CODE_39,
                    BarcodeAlgorithm.EAN_8,
                    BarcodeAlgorithm.UPC_A,
                    BarcodeAlgorithm.PDF_417,
                    BarcodeAlgorithm.AZTEC
                });
                cboAlg.SelectedItem = roi.Algorithm ?? BarcodeAlgorithm.QRCode;
                cboAlg.SelectedIndexChanged += (s, e) =>
                {
                    roi.Algorithm = (BarcodeAlgorithm)cboAlg.SelectedItem;
                    SaveRoi();
                    pictureBox.Invalidate();
                };
                AddControlRow(t, lblAlg, cboAlg);

                // Hidden
                AddHiddenCheckbox(t, roi);
                panelContent.Controls.Add(t);
            }
            void RenderHsv()
            {
                panelContent.Controls.Clear();
                var t = CreateInnerTable();
                AddReadOnlyRow(t, "X", roi.X.ToString());
                AddReadOnlyRow(t, "Y", roi.Y.ToString());
                AddReadOnlyRow(t, "Width", roi.Width.ToString());
                AddReadOnlyRow(t, "Height", roi.Height.ToString());

                // Auto compute if missing
                if (roi.Lower == null || roi.Upper == null)
                    AutoComputeAndAssign(roi);

                // Show HSV as 3 rows: H / S / V
                AddReadOnlyRow(t, "H", $"{roi.Lower.H} - {roi.Upper.H}");
                AddReadOnlyRow(t, "S", $"{roi.Lower.S} - {roi.Upper.S}");
                AddReadOnlyRow(t, "V", $"{roi.Lower.V} - {roi.Upper.V}");

                // Recompute button
                var btnRecompute = new Button
                {
                    Text = "Recompute HSV",
                    Dock = DockStyle.Top,
                    Height = 30
                };
                btnRecompute.Click += (s, e) =>
                {
                    AutoComputeAndAssign(roi);
                    RenderHsv();
                    pictureBox.Invalidate();
                };
                t.Controls.Add(btnRecompute);
                t.SetColumnSpan(btnRecompute, 2);

                // Hidden checkbox
                AddHiddenCheckbox(t, roi);

                panelContent.Controls.Add(t);
            }



            cboMode.SelectedIndexChanged += (s, e) =>
            {
                roi.Mode = cboMode.SelectedItem.ToString();
                SaveRoi();
                if (roi.Mode == "HSV")
                    RenderHsv();
                else
                    RenderBarcode();
            };

            if (roi.Mode == "HSV") RenderHsv(); else RenderBarcode();
        }

        private void AutoComputeAndAssign(RoiRegion roi)
        {
            if (currentFovBitmap == null) return;
            var rect = new Rectangle(roi.X, roi.Y, roi.Width, roi.Height);
            rect.Intersect(new Rectangle(0, 0, currentFovBitmap.Width, currentFovBitmap.Height));
            if (rect.Width <= 0 || rect.Height <= 0) return;

            using (var roiBmp = new Bitmap(rect.Width, rect.Height))
            {
                using (var g = Graphics.FromImage(roiBmp))
                {
                    g.DrawImage(currentFovBitmap, new Rectangle(0, 0, rect.Width, rect.Height), rect, GraphicsUnit.Pixel);
                }

                var (lower, upper, _) = hsvAutoService.Compute(roiBmp, huePadding: 2, svPadding: 10);
                roi.Lower = lower;
                roi.Upper = upper;
                SaveRoi();
            }
        }

        private void SaveRoi()
        {
            if (selectedRoiIndex >= 0 && selectedRoiIndex < roiList.Count &&
                selectedFovIndex >= 0 && selectedFovIndex < fovList.Count)
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

        private Label CreateLabel(string text) =>
            new Label { Text = text, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, Font = new Font("Segoe UI", 9F, FontStyle.Bold) };

        private void AddReadOnlyRow(TableLayoutPanel t, string label, string value)
        {
            int row = t.RowCount;
            t.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            t.Controls.Add(CreateLabel(label + ":"), 0, row);
            t.Controls.Add(new TextBox
            {
                Text = value,
                ReadOnly = true,
                Dock = DockStyle.Fill
            }, 1, row);
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
            var chkHidden = new CheckBox
            {
                Text = "Hidden",
                Checked = roi.IsHidden,
                Dock = DockStyle.Fill
            };
            chkHidden.CheckedChanged += (s, e) =>
            {
                roi.IsHidden = chkHidden.Checked;
                SaveRoi();
                pictureBox.Invalidate();
            };
            AddControlRow(t, CreateLabel("IsHidden:"), chkHidden);
        }
    }
}
