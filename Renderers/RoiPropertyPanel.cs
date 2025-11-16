using System;
using System.Drawing;
using System.Windows.Forms;
using MPV.Models;
using MPV.Services;
using MPV.Enums;
using System.Collections.Generic;
using MPV.Service;

namespace MPV.Renderers
{
    public class RoiPropertyPanel
    {
        private readonly FovManager fovManager;
        private readonly List<FovRegion> fovList;
        private readonly PictureBox pictureBox;
        private int selectedRoiIndex;
        private int selectedFovIndex;
        private List<RoiRegion> roiList;

        public RoiPropertyPanel(FovManager fovManager, List<FovRegion> fovList, PictureBox pictureBox,
                                int selectedFovIndex, int selectedRoiIndex, List<RoiRegion> roiList)
        {
            this.fovManager = fovManager;
            this.fovList = fovList;
            this.pictureBox = pictureBox;
            this.selectedFovIndex = selectedFovIndex;
            this.selectedRoiIndex = selectedRoiIndex;
            this.roiList = roiList;
        }

        public void ShowRoiProperties(Panel panelImage, RoiRegion roi)
        {
            panelImage.Controls.Clear();

            if (roi == null) return;

            var tableLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                AutoSize = true,
                Padding = new Padding(10),
                RowCount = 2
            };

            tableLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120F));
            tableLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tableLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            tableLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            // --- Combobox chọn Mode ---
            var lblMode = new Label
            {
                Text = "Mode:",
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            };

            var cboMode = new ComboBox
            {
                Dock = DockStyle.Fill,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 9F)
            };
            cboMode.Items.AddRange(new object[] { "Barcode", "HSV" });
            cboMode.SelectedIndex = 0;

            tableLayout.Controls.Add(lblMode, 0, 0);
            tableLayout.Controls.Add(cboMode, 1, 0);

            // --- Panel nội dung ---
            var panelModeContent = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                BackColor = Color.White
            };

            tableLayout.Controls.Add(panelModeContent, 0, 1);
            tableLayout.SetColumnSpan(panelModeContent, 2);

            panelImage.Controls.Add(tableLayout);

            // --- Hiển thị Barcode Info ---
            void ShowBarcodeInfo()
            {
                panelModeContent.Controls.Clear();

                var innerTable = new TableLayoutPanel
                {
                    Dock = DockStyle.Top,
                    ColumnCount = 2,
                    AutoSize = true,
                    Padding = new Padding(5),
                    BackColor = Color.White
                };

                innerTable.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120F));
                innerTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

                AddPropertyRow(innerTable, "X:", roi.X.ToString());
                AddPropertyRow(innerTable, "Y:", roi.Y.ToString());
                AddPropertyRow(innerTable, "Width:", roi.Width.ToString());
                AddPropertyRow(innerTable, "Height:", roi.Height.ToString());
                AddPropertyRow(innerTable, "IsDetected:", roi.IsDetected.ToString());

                // Algorithm
                var lblAlgorithm = new Label
                {
                    Text = "Algorithm:",
                    Dock = DockStyle.Fill,
                    TextAlign = ContentAlignment.MiddleLeft,
                    Font = new Font("Segoe UI", 9F, FontStyle.Bold)
                };

                var cboAlgorithm = new ComboBox
                {
                    Dock = DockStyle.Fill,
                    DropDownStyle = ComboBoxStyle.DropDownList,
                    Font = new Font("Segoe UI", 9F)
                };

                cboAlgorithm.Items.AddRange(new object[]
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

                cboAlgorithm.SelectedItem = roi.Algorithm;
                cboAlgorithm.SelectedIndexChanged += (s, e) =>
                {
                    if (selectedRoiIndex >= 0 && selectedRoiIndex < roiList.Count)
                    {
                        roiList[selectedRoiIndex].Algorithm = (BarcodeAlgorithm)cboAlgorithm.SelectedItem;
                        fovList[selectedFovIndex].Rois = roiList;
                        fovManager.Save(fovList);

                        var newFovList = fovManager.Load();
                        fovList.Clear();
                        fovList.AddRange(newFovList);

                        LoggerService.Info($"Đã thay đổi thuật toán ROI {selectedRoiIndex + 1} thành {cboAlgorithm.SelectedItem}");
                    }
                };

                int algoRow = innerTable.RowCount;
                innerTable.RowStyles.Add(new RowStyle(SizeType.AutoSize));
                innerTable.Controls.Add(lblAlgorithm, 0, algoRow);
                innerTable.Controls.Add(cboAlgorithm, 1, algoRow);
                innerTable.RowCount++;

                // IsHidden checkbox
                var chkHidden = new CheckBox
                {
                    Text = "Hidden",
                    Checked = roi.IsHidden,
                    Dock = DockStyle.Fill,
                    Font = new Font("Segoe UI", 9F)
                };

                chkHidden.CheckedChanged += (s, e) =>
                {
                    roi.IsHidden = chkHidden.Checked;
                    roiList[selectedRoiIndex] = roi;
                    fovList[selectedFovIndex].Rois = roiList;
                    fovManager.Save(fovList);
                    pictureBox.Invalidate();
                };

                int hiddenRow = innerTable.RowCount;
                innerTable.RowStyles.Add(new RowStyle(SizeType.AutoSize));
                innerTable.Controls.Add(new Label
                {
                    Text = "IsHidden:",
                    Dock = DockStyle.Fill,
                    TextAlign = ContentAlignment.MiddleLeft,
                    Font = new Font("Segoe UI", 9F, FontStyle.Bold)
                }, 0, hiddenRow);
                innerTable.Controls.Add(chkHidden, 1, hiddenRow);
                innerTable.RowCount++;

                // QUAN TRỌNG: Add innerTable vào panelModeContent
                panelModeContent.Controls.Add(innerTable);
            }

            // --- Placeholder HSV ---
            void ShowHSVInfo()
            {
                panelModeContent.Controls.Clear();
                var lbl = new Label
                {
                    Text = "Chế độ HSV (chưa triển khai).",
                    Dock = DockStyle.Top,
                    AutoSize = true,
                    Font = new Font("Segoe UI", 10F, FontStyle.Italic),
                    ForeColor = Color.Gray,
                    Padding = new Padding(5)
                };
                panelModeContent.Controls.Add(lbl);
            }

            // --- Sự kiện đổi chế độ ---
            cboMode.SelectedIndexChanged += (s, e) =>
            {
                if (cboMode.SelectedItem.ToString() == "Barcode")
                    ShowBarcodeInfo();
                else
                    ShowHSVInfo();
            };

            ShowBarcodeInfo();
        }

        // Helper: tạo dòng Label + Text
        private void AddPropertyRow(TableLayoutPanel table, string label, string value)
        {
            int row = table.RowCount;

            var lbl = new Label
            {
                Text = label,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            };

            var txt = new TextBox
            {
                Text = value,
                Dock = DockStyle.Fill,
                ReadOnly = true,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Segoe UI", 9F)
            };

            table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            table.Controls.Add(lbl, 0, row);
            table.Controls.Add(txt, 1, row);
            table.RowCount++;
        }
    }
}
