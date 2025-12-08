using MPV.Enums;
using MPV.Models;
using MPV.Renderers;
using MPV.Service;
using MPV.Services;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using OpenCvSharp;
using OpenCvSharp.Extensions;

namespace MPV
{
    public partial class Form1 : Form
    {
        private readonly FovManager fovManager;
        private readonly BarcodeService barcodeService;
        private readonly HsvService hsvService;
        private readonly HsvAutoService hsvAutoService = new HsvAutoService();
        private readonly RoiRenderer roiRenderer;
        private ContextMenuStrip contextMenu;
        private Bitmap _bitmap;
        private Rectangle _selectRectangle;
        private System.Drawing.Point _startPoint; // disambiguate
        private bool _isSelecting = false;
        private bool _drawMode = false;
        private bool _isUpdatingRoi = false;
        private bool _showRoiOnImage = false;
        private int selectedFovIndex = -1;
        private int selectedRoiIndex = -1;
        private List<FovRegion> fovList = new List<FovRegion>();
        private List<RoiRegion> roiList = new List<RoiRegion>();      
        private readonly Dictionary<(int fov, int roi), bool> _lastTestResults = new Dictionary<(int fov, int roi), bool>();
        private bool _singleRoiMode = false;     
        private bool _showRunResults = false;
        private int _roiToUpdateIndex = -1;
        private float _zoomFactor = 1.0f;
        private const float _zoomStep = 0.1f;
        private const float _minZoom = 0.5f;
        private const float _maxZoom = 5.0f;

        // panel for HSV values on the left
        private Panel _hsvPanel;
        // replace labels with textboxes for lower/upper ranges
        private TextBox _txtHL; // H lower
        private TextBox _txtHU; // H upper
        private TextBox _txtSL; // S lower
        private TextBox _txtSU; // S upper
        private TextBox _txtVL; // V lower
        private TextBox _txtVU; // V upper

        // Barcode panel controls
        private Panel _barcodePanel;
        private ComboBox _cboBarcodeType;
        private TextBox _txtBarcodeLen;

        //AppDomain.CurrentDomain
        public Form1()
        {
            InitializeComponent();

            string fovPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "fov_data.json");
            fovManager = new FovManager(fovPath);
            barcodeService = new BarcodeService();
            hsvService = new HsvService();
            roiRenderer = new RoiRenderer(pictureBox1);
            pictureBox1.MouseWheel += pictureBox1_MouseWheel;

            // Ensure pictureBox is inside the scrollable panel and visible
            if (panelScroll != null && pictureBox1.Parent != panelScroll)
            {
                try
                {
                    panelScroll.Controls.Clear();
                    panelScroll.Controls.Add(pictureBox1);
                }
                catch { }
            }
            if (panelScroll != null)
            {
                panelScroll.AutoScroll = true;
                panelScroll.Visible = true;
                panelScroll.Resize += (s, e) =>
                {
                    if (_bitmap != null)
                    {
                        // Re-center when viewport changes
                        ApplyZoom();
                    }
                };
            }
            pictureBox1.Visible = true;

            // init HSV panel (hidden by default)
            InitHsvPanel();
            // init Barcode panel (hidden by default)
            InitBarcodePanel();
        }

        private void Form1_Shown(object sender, EventArgs e)
        {
            try
            {
                pictureBox1.BringToFront();
                if (_bitmap != null)
                {
                    ResetZoom();
                }
                else
                {
                    // ensure placeholder size so control is visible even without image
                    pictureBox1.Width = Math.Max(200, pictureBox1.Width);
                    pictureBox1.Height = Math.Max(150, pictureBox1.Height);
                }
            }
            catch { }
        }

        private void InitHsvPanel()
        {
            _hsvPanel = new Panel { Width = grpTemplate.Width - 6, Height = 160, Left = 12, Top = 19, Visible = false, BorderStyle = BorderStyle.None };

            // layout: three rows of L/U textboxes
            int labelW = 24;
            int boxW = (_hsvPanel.Width - 32 - labelW) / 2; // space for label + two columns
            int left = 8;
            int gapY = 28;
            int top = 8;

            // row labels
            var lblH = new Label { Left = left, Top = top + 3, Width = labelW, Text = "H:" };
            var lblS = new Label { Left = left, Top = top + gapY + 3, Width = labelW, Text = "S:" };
            var lblV = new Label { Left = left, Top = top + gapY * 2 + 3, Width = labelW, Text = "V:" };

            // textboxes positioned after labels
            _txtHL = new TextBox { Left = left + labelW, Top = top, Width = boxW, Text = "" };
            _txtHU = new TextBox { Left = left + labelW + boxW + 16, Top = top, Width = boxW, Text = "" };

            _txtSL = new TextBox { Left = left + labelW, Top = top + gapY, Width = boxW, Text = "" };
            _txtSU = new TextBox { Left = left + labelW + boxW + 16, Top = top + gapY, Width = boxW, Text = "" };

            _txtVL = new TextBox { Left = left + labelW, Top = top + gapY * 2, Width = boxW, Text = "" };
            _txtVU = new TextBox { Left = left + labelW + boxW + 16, Top = top + gapY * 2, Width = boxW, Text = "" };

            // wire events to persist changes
            _txtHL.TextChanged += (s, e) => UpdateHsvFromInputs();
            _txtHU.TextChanged += (s, e) => UpdateHsvFromInputs();
            _txtSL.TextChanged += (s, e) => UpdateHsvFromInputs();
            _txtSU.TextChanged += (s, e) => UpdateHsvFromInputs();
            _txtVL.TextChanged += (s, e) => UpdateHsvFromInputs();
            _txtVU.TextChanged += (s, e) => UpdateHsvFromInputs();

            // add controls
            _hsvPanel.Controls.Add(lblH);
            _hsvPanel.Controls.Add(lblS);
            _hsvPanel.Controls.Add(lblV);
            _hsvPanel.Controls.Add(_txtHL);
            _hsvPanel.Controls.Add(_txtHU);
            _hsvPanel.Controls.Add(_txtSL);
            _hsvPanel.Controls.Add(_txtSU);
            _hsvPanel.Controls.Add(_txtVL);
            _hsvPanel.Controls.Add(_txtVU);

            grpTemplate.Controls.Add(_hsvPanel);
        }

        private void InitBarcodePanel()
        {
            _barcodePanel = new Panel { Width = grpTemplate.Width - 6, Height = 120, Left = 12, Top = 19, Visible = false, BorderStyle = BorderStyle.None };

            int left = 8;
            int top = 8;
            int labelW = 80;
            int ctrlW = _barcodePanel.Width - left - labelW - 16;

            var lblType = new Label { Left = left, Top = top + 3, Width = labelW, Text = "Type:" };
            _cboBarcodeType = new ComboBox { Left = left + labelW, Top = top, Width = ctrlW, DropDownStyle = ComboBoxStyle.DropDownList };
            _cboBarcodeType.Items.AddRange(Enum.GetNames(typeof(BarcodeAlgorithm)));
            _cboBarcodeType.SelectedIndexChanged += (s, e) =>
            {
                if (selectedRoiIndex < 0 || selectedRoiIndex >= roiList.Count) return;
                if (Enum.TryParse<BarcodeAlgorithm>(_cboBarcodeType.SelectedItem?.ToString(), out var alg))
                {
                    roiList[selectedRoiIndex].Algorithm = alg;
                    fovManager.Save(fovList);
                }
            };

            var lblLen = new Label { Left = left, Top = top + 36 + 3, Width = labelW, Text = "Max Length:" };
            _txtBarcodeLen = new TextBox { Left = left + labelW, Top = top + 36, Width = ctrlW, Text = "" };
            _txtBarcodeLen.TextChanged += (s, e) =>
            {
                if (selectedRoiIndex < 0 || selectedRoiIndex >= roiList.Count) return;
                if (int.TryParse(_txtBarcodeLen.Text, out int v))
                {
                    v = Math.Max(0, v);
                    roiList[selectedRoiIndex].ExpectedLength = v;
                    fovManager.Save(fovList);
                }
            };

            _barcodePanel.Controls.Add(lblType);
            _barcodePanel.Controls.Add(_cboBarcodeType);
            _barcodePanel.Controls.Add(lblLen);
            _barcodePanel.Controls.Add(_txtBarcodeLen);

            grpTemplate.Controls.Add(_barcodePanel);
        }

        private void UpdateHsvPanelValues(RoiRegion roi)
        {
            if (roi == null)
            {
                if (_txtHL != null) { _txtHL.Text = ""; }
                if (_txtHU != null) { _txtHU.Text = ""; }
                if (_txtSL != null) { _txtSL.Text = ""; }
                if (_txtSU != null) { _txtSU.Text = ""; }
                if (_txtVL != null) { _txtVL.Text = ""; }
                if (_txtVU != null) { _txtVU.Text = ""; }
                return;
            }
            var lh = roi.Lower != null ? roi.Lower.H : 0;
            var ls = roi.Lower != null ? roi.Lower.S : 0;
            var lv = roi.Lower != null ? roi.Lower.V : 0;
            var uh = roi.Upper != null ? roi.Upper.H : 0;
            var us = roi.Upper != null ? roi.Upper.S : 0;
            var uv = roi.Upper != null ? roi.Upper.V : 0;

            if (_txtHL != null) _txtHL.Text = lh.ToString();
            if (_txtHU != null) _txtHU.Text = uh.ToString();
            if (_txtSL != null) _txtSL.Text = ls.ToString();
            if (_txtSU != null) _txtSU.Text = us.ToString();
            if (_txtVL != null) _txtVL.Text = lv.ToString();
            if (_txtVU != null) _txtVU.Text = uv.ToString();
        }

        private void UpdateBarcodePanelValues(RoiRegion roi)
        {
            if (roi == null)
            {
                if (_cboBarcodeType != null) _cboBarcodeType.SelectedIndex = -1;
                if (_txtBarcodeLen != null) _txtBarcodeLen.Text = "";
                return;
            }
            var current = (roi.Algorithm ?? BarcodeAlgorithm.QRCode).ToString();
            _cboBarcodeType.SelectedItem = current;
            _txtBarcodeLen.Text = roi.ExpectedLength.ToString();
        }

        // Persist HSV values from textboxes to current ROI
        private void UpdateHsvFromInputs()
        {
            if (selectedRoiIndex < 0 || selectedRoiIndex >= roiList.Count) return;
            var roi = roiList[selectedRoiIndex];
            int hl, hu, sl, su, vl, vu;
            if (!int.TryParse(_txtHL.Text, out hl)) hl = roi.Lower?.H ?? 0;
            if (!int.TryParse(_txtHU.Text, out hu)) hu = roi.Upper?.H ?? 0;
            if (!int.TryParse(_txtSL.Text, out sl)) sl = roi.Lower?.S ?? 0;
            if (!int.TryParse(_txtSU.Text, out su)) su = roi.Upper?.S ?? 0;
            if (!int.TryParse(_txtVL.Text, out vl)) vl = roi.Lower?.V ?? 0;
            if (!int.TryParse(_txtVU.Text, out vu)) vu = roi.Upper?.V ?? 0;

            hl = Math.Max(0, Math.Min(255, hl));
            hu = Math.Max(0, Math.Min(255, hu));
            sl = Math.Max(0, Math.Min(255, sl));
            su = Math.Max(0, Math.Min(255, su));
            vl = Math.Max(0, Math.Min(255, vl));
            vu = Math.Max(0, Math.Min(255, vu));

            roi.Lower = new HsvValue { H = hl, S = sl, V = vl };
            roi.Upper = new HsvValue { H = hu, S = su, V = vu };
            fovManager.Save(fovList);
        }

        private void InitializeContextMenu()
        {
            contextMenu = new ContextMenuStrip();
            var menuItemDrawRoi = new ToolStripMenuItem("Vẽ ROI",null, menuItemDrawRoi_Click);
            var menuItemResetRoi = new ToolStripMenuItem("Reset ROI",null, menuItemResetRoi_Click);
            var menuItemCancel = new ToolStripMenuItem("Hủy", null, (s, e) => { _drawMode = false; });
            var menuItemZoomIn = new ToolStripMenuItem("Phóng to (+)", null, (s, e) => ZoomIn());
            var menuItemZoomOut = new ToolStripMenuItem("Thu nhỏ (-)", null, (s, e) => ZoomOut());
            var menuItemZoomReset = new ToolStripMenuItem("Zoom gốc (100%)", null, (s, e) => ResetZoom());


            contextMenu.Items.Add(menuItemDrawRoi);
            contextMenu.Items.Add(menuItemResetRoi);
            contextMenu.Items.Add(new ToolStripSeparator());
            contextMenu.Items.Add(menuItemZoomIn);
            contextMenu.Items.Add(menuItemZoomOut);
            contextMenu.Items.Add(menuItemZoomReset);
            contextMenu.Items.Add(new ToolStripSeparator());
            contextMenu.Items.Add(menuItemCancel);
            pictureBox1.ContextMenuStrip = contextMenu;

            
            contextMenu.Opening += ContextMenu_Opening;
        }

        private void ContextMenu_Opening(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // Determine which ROI is selected in the TreeView
            _roiToUpdateIndex = -1;
            if (pn_property.SelectedNode != null && pn_property.SelectedNode.Text.StartsWith("ROI "))
            {
                if (int.TryParse(pn_property.SelectedNode.Text.Replace("ROI ", ""), out int roiIndex))
                {
                    _roiToUpdateIndex = roiIndex - 1;
                }
            }
        }

        private void menuItemResetRoi_Click(object sender, EventArgs e)
        {
            // Only allow resetting if a ROI node is selected
            if (pn_property.SelectedNode == null || !pn_property.SelectedNode.Text.StartsWith("ROI "))
            {
                MessageBox.Show("Vui lòng chọn ROI trên TreeView để reset.");
                return;
            }
            if (selectedFovIndex < 0 || selectedFovIndex >= fovList.Count)
            {
                MessageBox.Show("Vui lòng chọn FOV hợp lệ.");
                return;
            }
            if (_roiToUpdateIndex >= 0 && _roiToUpdateIndex < roiList.Count)
            {
                var roi = roiList[_roiToUpdateIndex];
                roi.X = 0;
                roi.Y = 0;
                roi.Width = 0;
                roi.Height = 0;
                fovManager.Save(fovList);
               // MessageBox.Show($"Đã reset ROI {(_roiToUpdateIndex + 1)}.");
                pictureBox1.Invalidate();
            }
        }
        private void menuItemDrawRoi_Click(object sender, EventArgs e)
        {
            // Only allow drawing if a ROI node is selected
            if (pn_property.SelectedNode == null || !pn_property.SelectedNode.Text.StartsWith("ROI "))
            {
                MessageBox.Show("Vui lòng chọn ROI trên TreeView để vẽ.");
                return;
            }

            if (selectedFovIndex < 0 || selectedFovIndex >= fovList.Count)
            {
                MessageBox.Show("Vui lòng chọn FOV hợp lệ.");
                return;
            }

            if (_roiToUpdateIndex >= 0 && _roiToUpdateIndex < roiList.Count)
            {
                var roi = roiList[_roiToUpdateIndex];
                if (roi.X == 0 && roi.Y == 0 && roi.Width == 0 && roi.Height == 0)
                {
                    _drawMode = true;
                    _isUpdatingRoi = true;
                    Cursor = Cursors.Cross;
                   // MessageBox.Show("Kéo chuột trên ảnh để vẽ vùng ROI mới.");
                    return;
                }
            }

            // Default: add new ROI (if not updating)
            _drawMode = true;
            _isUpdatingRoi = false;
            Cursor = Cursors.Cross;
          //  MessageBox.Show("Kéo chuột trên ảnh để vẽ vùng ROI mới.");
        }
        private void Form1_Load(object sender, EventArgs e)
        {
            InitializeContextMenu();
            this.KeyPreview = true;
            // giữ highlight node đang chọn khi TreeView mất focus
            try { pn_property.HideSelection = false; } catch { }
            ptr_template.Image = null;
            SyncTemplatePanel();
            try { LoadFovToTreeView(); } catch (Exception ex) { MessageBox.Show("Lỗi khi load JSON: " + ex.Message); LoggerService.Error("Error loading JSON", ex);}        }

        private void LoadFovToTreeView()
        {
            // Preserve current selection indices
            int prevFov = selectedFovIndex;
            int prevRoi = selectedRoiIndex;

            pn_property.Nodes.Clear();
            fovList = fovManager.Load();

            if (fovList.Count == 0)
            {
                pn_property.Nodes.Add("Chưa có FOV nào được lưu.");
                _bitmap = null;
                pictureBox1.Image = null;
                roiList.Clear();
                return;
            }

            var root = new TreeNode("FOV Regions");

            for (int i = 0; i < fovList.Count; i++)
            {
                var fov = fovList[i];
                var fovNode = new TreeNode($"FOV {i + 1}");
                // Remove image path display
                // fovNode.Nodes.Add($"Image: {Path.GetFileName(fov.ImagePath)}");

                for (int j = 0; j < fov.Rois.Count; j++)
                {
                    var roiNode = new TreeNode($"ROI {j + 1}");
                    // Remove ROI details display
                    // roiNode.Nodes.Add($"X: {roi.X}");
                    // roiNode.Nodes.Add($"Y: {roi.Y}");
                    // roiNode.Nodes.Add($"Width: {roi.Width}");
                    // roiNode.Nodes.Add($"Height: {roi.Height}");
                    fovNode.Nodes.Add(roiNode);
                }

                root.Nodes.Add(fovNode);
            }

            pn_property.Nodes.Add(root);
            root.Expand();

            // Restore previous selection if possible
            if (prevFov >= 0 && prevFov < root.Nodes.Count)
            {
                var fovNode = root.Nodes[prevFov];
                TreeNode nodeToSelect = fovNode;
                if (prevRoi >= 0 && prevRoi < fovNode.Nodes.Count)
                {
                    nodeToSelect = fovNode.Nodes[prevRoi];
                }
                pn_property.SelectedNode = nodeToSelect;
                nodeToSelect.EnsureVisible();
            }
        }

        private void trv1_AfterSelect(object sender, TreeViewEventArgs e)
        {

            if (e.Node == null) return;

            if (e.Node.Text.StartsWith("FOV "))
            {
                if (!int.TryParse(e.Node.Text.Replace("FOV ", ""), out int fovIndex)) return;
                selectedFovIndex = fovIndex - 1;
                selectedRoiIndex = -1;
                if (selectedFovIndex >= 0 && selectedFovIndex < fovList.Count)
                {
                    var fov = fovList[selectedFovIndex];
                    if (File.Exists(fov.ImagePath))
                    {
                        _bitmap = new Bitmap(fov.ImagePath);
                        pictureBox1.Image = _bitmap;
                        ResetZoom();
                        roiList = fov.Rois;
                        // Restore Template bitmap from persisted base64 for all ROIs
                        foreach (var roi in roiList)
                        {
                            if (roi.Template == null && !string.IsNullOrEmpty(roi.TemplateBase64))
                            {
                                try
                                {
                                    roi.Template = Base64ToBitmap(roi.TemplateBase64);
                                }
                                catch { }
                            }
                        }
                        // Ensure FOV image is also persisted as base64
                        try
                        {
                            fov.ImageBase64 = BitmapToBase64Png(_bitmap);
                            fovManager.Save(fovList);
                        }
                        catch { }
                    }
                    else if (!string.IsNullOrEmpty(fov.ImageBase64))
                    {
                        try
                        {
                            _bitmap = Base64ToBitmap(fov.ImageBase64);
                            pictureBox1.Image = _bitmap;
                            ResetZoom();
                            roiList = fov.Rois;
                        }
                        catch
                        {
                            pictureBox1.Image = null; _bitmap = null; roiList = new List<RoiRegion>();
                        }
                    }
                    else
                    {
                        pictureBox1.Image = null; _bitmap = null; roiList = new List<RoiRegion>();
                    }
                }
                selectedRoiIndex = -1;
                SyncTemplatePanel();
                panelImage.Controls.Clear();
                pictureBox1.Invalidate();
            }
            else if (e.Node.Text.StartsWith("ROI "))
            {
                if (!int.TryParse(e.Node.Text.Replace("ROI ", ""), out int roiIndex)) return;
                selectedRoiIndex = roiIndex - 1;
                if (selectedFovIndex >= 0 && selectedFovIndex < fovList.Count)
                    roiList = fovList[selectedFovIndex].Rois;
                // Restore template for selected ROI if needed
                if (selectedRoiIndex >= 0 && selectedRoiIndex < roiList.Count)
                {
                    var r = roiList[selectedRoiIndex];
                    if (r.Template == null && !string.IsNullOrEmpty(r.TemplateBase64))
                    {
                        try { r.Template = Base64ToBitmap(r.TemplateBase64); } catch { }
                    }
                }
                SyncTemplatePanel();

                // render ROI property panel into panelImage
                panelImage.Controls.Clear();
                if (selectedRoiIndex >= 0 && selectedRoiIndex < roiList.Count)
                {
                    var propertyPanel = new RoiPropertyPanel(
                        fovManager,
                        fovList,
                        pictureBox1,
                        _bitmap,
                        selectedFovIndex,
                        selectedRoiIndex,
                        roiList);
                    propertyPanel.RoiChanged += () => 
                    {
                        var currentNode = pn_property.SelectedNode; // nhớ node đang chọn
                        SyncTemplatePanel();
                        if (currentNode != null)
                        {
                            try
                            {
                                pn_property.SelectedNode = currentNode;
                                currentNode.EnsureVisible();
                                pn_property.Focus(); // ép TreeView lấy lại focus để không mất highlight
                            }
                            catch { }
                        }
                    }; // cập nhật panel trái khi thay đổi và giữ lựa chọn trên TreeView
                    propertyPanel.ShowRoiProperties(panelImage, roiList[selectedRoiIndex]);
                }
                pictureBox1.Invalidate();
            }
        }

        private void SyncTemplatePanel()
        {
            if (selectedRoiIndex < 0 || selectedRoiIndex >= roiList.Count)
            {
                txtOkLower.Text = ""; txtOkUpper.Text = ""; chkReverse.Checked = false; txtLastScore.Text = ""; txtCenterX.Text = ""; txtCenterY.Text = ""; ptr_template.Image = null; _hsvPanel.Visible = false; grpTemplate.Text = "Template"; btnUpdateTemplate.Enabled = false; btnUpdateTemplate.Text = "Update Template"; if (_barcodePanel != null) _barcodePanel.Visible = false; return;
            }
            var roi = roiList[selectedRoiIndex];
            txtOkLower.Text = roi.OkScoreLower.ToString();
            txtOkUpper.Text = roi.OkScoreUpper.ToString();
            chkReverse.Checked = roi.ReverseSearch;
            txtLastScore.Text = roi.LastScore.ToString();
            txtCenterX.Text = roi.X.ToString();
            txtCenterY.Text = roi.Y.ToString();

            var isTemplate = string.Equals(roi.Mode, "Template Matching", StringComparison.OrdinalIgnoreCase) && roi.Template != null;
            if (isTemplate)
            {
                grpTemplate.Text = "Template";
                ptr_template.Visible = true;
                ptr_template.Image = roi.Template;
                btnUpdateTemplate.Enabled = true;
                btnUpdateTemplate.Text = "Update Template";
                _hsvPanel.Visible = false;
                if (_barcodePanel != null) _barcodePanel.Visible = false;
            }
            else if (string.Equals(roi.Mode, "HSV", StringComparison.OrdinalIgnoreCase))
            {
                grpTemplate.Text = "HSV";
                ptr_template.Visible = false;
                ptr_template.Image = null;
                _hsvPanel.Visible = true;
                btnUpdateTemplate.Enabled = true; // use single button for HSV action
                btnUpdateTemplate.Text = "Get HSV";
                if (_barcodePanel != null) _barcodePanel.Visible = false;
                UpdateHsvPanelValues(roi);
            }
            else
            {
                // Barcode mode
                grpTemplate.Text = "Algorithm";
                ptr_template.Visible = false;
                ptr_template.Image = null;
                _hsvPanel.Visible = false;
                if (_barcodePanel != null)
                {
                    _barcodePanel.Visible = true;
                    UpdateBarcodePanelValues(roi);
                }
                btnUpdateTemplate.Enabled = false;
                btnUpdateTemplate.Text = "";
            }
        }

        private void btnUpdateTemplate_Click(object sender, EventArgs e)
        {
            if (selectedRoiIndex < 0 || selectedRoiIndex >= roiList.Count) return;
            var roi = roiList[selectedRoiIndex];

            if (string.Equals(roi.Mode, "HSV", StringComparison.OrdinalIgnoreCase))
            {
                if (_bitmap == null) return;
                Rectangle rect = new Rectangle(roi.X, roi.Y, roi.Width, roi.Height);
                rect.Intersect(new Rectangle(0,0,_bitmap.Width,_bitmap.Height));
                if (rect.Width <=0 || rect.Height <=0) return;
                using (var roiBmp = new Bitmap(rect.Width, rect.Height))
                using (var g = Graphics.FromImage(roiBmp))
                {
                    g.DrawImage(_bitmap, new Rectangle(0, 0, rect.Width, rect.Height), rect, GraphicsUnit.Pixel);
                    var (lower, upper, _) = hsvAutoService.Compute(roiBmp, 15, 10);
                    roi.Lower = lower; roi.Upper = upper; fovManager.Save(fovList);
                    UpdateHsvPanelValues(roi);
                }
                return;
            }

            // default: template update
            if (_bitmap == null) return;
            Rectangle rectT = new Rectangle(roi.X, roi.Y, roi.Width, roi.Height);
            rectT.Intersect(new Rectangle(0,0,_bitmap.Width,_bitmap.Height));
            if (rectT.Width <=0 || rectT.Height <=0) return;
            roi.Template?.Dispose();
            Bitmap bmp = new Bitmap(rectT.Width, rectT.Height);
            using (Graphics g = Graphics.FromImage(bmp))
                g.DrawImage(_bitmap, new Rectangle(0,0,rectT.Width,rectT.Height), rectT, GraphicsUnit.Pixel);
            roi.Template = bmp;
            // Persist template as base64 so it survives restarts
            roi.TemplateBase64 = BitmapToBase64Png(bmp);
            roi.MatchScore = 0; roi.LastScore = 0; roi.MatchRect = Rectangle.Empty;
            fovManager.Save(fovList);
            SyncTemplatePanel();
        }

        private string BitmapToBase64Png(Bitmap bmp)
        {
            using (var ms = new MemoryStream())
            {
                bmp.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                return Convert.ToBase64String(ms.ToArray());
            }
        }
        private Bitmap Base64ToBitmap(string base64)
        {
            var bytes = Convert.FromBase64String(base64);
            using (var ms = new MemoryStream(bytes))
            {
                return new Bitmap(ms);
            }
        }

        private void txtOkLower_TextChanged(object sender, EventArgs e)
        {
            if (selectedRoiIndex < 0 || selectedRoiIndex >= roiList.Count) return;
            if (int.TryParse(txtOkLower.Text, out int v))
            {
                v = Math.Max(0, Math.Min(100, v));
                roiList[selectedRoiIndex].OkScoreLower = v;
                fovManager.Save(fovList);
            }
        }
        private void txtOkUpper_TextChanged(object sender, EventArgs e)
        {
            if (selectedRoiIndex < 0 || selectedRoiIndex >= roiList.Count) return;
            if (int.TryParse(txtOkUpper.Text, out int v))
            {
                v = Math.Max(0, Math.Min(100, v));
                roiList[selectedRoiIndex].OkScoreUpper = v;
                fovManager.Save(fovList);
            }
        }
        private void chkReverse_CheckedChanged(object sender, EventArgs e)
        {
            if (selectedRoiIndex < 0 || selectedRoiIndex >= roiList.Count) return;
            roiList[selectedRoiIndex].ReverseSearch = chkReverse.Checked;
            fovManager.Save(fovList);
        }

        private bool EvaluateScore(RoiRegion roi, int score)
        {
            bool inRange = score >= roi.OkScoreLower && score <= roi.OkScoreUpper;
            return roi.ReverseSearch ? !inRange : inRange;
        }

        // Template Matching helper: crop to ROI, grayscale, CCoeffNormed
        private int RunTemplateMatching(RoiRegion roi, Bitmap fovBitmap, out Rectangle matchRect, out double matchScore)
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

        private void TestSelectedRoi()
        {
            if (selectedFovIndex < 0 || selectedFovIndex >= fovList.Count) { MessageBox.Show("Chưa chọn FOV hợp lệ."); return; }
            if (selectedRoiIndex < 0 || selectedRoiIndex >= roiList.Count) { MessageBox.Show("Vui lòng chọn ROI cần test."); return; }
            if (_bitmap == null)
            {
                var fov = fovList[selectedFovIndex];
                if (File.Exists(fov.ImagePath))
                {
                    _bitmap = new Bitmap(fov.ImagePath);
                    // Also persist to base64 when reading from file
                    try { fov.ImageBase64 = BitmapToBase64Png(_bitmap); fovManager.Save(fovList); } catch { }
                }
                else if (!string.IsNullOrEmpty(fov.ImageBase64))
                {
                    try { _bitmap = Base64ToBitmap(fov.ImageBase64); }
                    catch { MessageBox.Show("Không thể đọc ảnh FOV từ base64."); return; }
                }
                else
                {
                    MessageBox.Show("Không tìm thấy ảnh FOV.");
                    return;
                }
                pictureBox1.Image = _bitmap; pictureBox1.SizeMode = PictureBoxSizeMode.Zoom;
            }
            var roi = roiList[selectedRoiIndex];
            Rectangle rect = new Rectangle(roi.X, roi.Y, roi.Width, roi.Height);
            rect.Intersect(new Rectangle(0,0,_bitmap.Width,_bitmap.Height));
            if (rect.Width <=0 || rect.Height <=0) { MessageBox.Show("ROI nằm ngoài ảnh."); return; }
            int score = 0;
            using (var roiBmp = new Bitmap(rect.Width, rect.Height))
            using (var g = Graphics.FromImage(roiBmp))
            {
                g.DrawImage(_bitmap, new Rectangle(0,0,rect.Width,rect.Height), rect, GraphicsUnit.Pixel);
                if (roi.Mode == "Template Matching" && roi.Template != null)
                {
                    Rectangle mrect; double mscore;
                    score = RunTemplateMatching(roi, _bitmap, out mrect, out mscore);
                    roi.MatchScore = mscore; roi.MatchRect = mrect;
                }
                else if (roi.Mode == "HSV")
                {
                    // Use existing configured HSV ranges if available; otherwise compute once
                    if (roi.Lower == null || roi.Upper == null)
                    {
                        var (lowerAuto, upperAuto, _) = hsvAutoService.Compute(roiBmp, 15, 10);
                        roi.Lower = lowerAuto;
                        roi.Upper = upperAuto;
                    }

                    var lowerRange = new HsvRange(roi.Lower.H, roi.Lower.H, roi.Lower.S, roi.Lower.S, roi.Lower.V, roi.Lower.V);
                    var upperRange = new HsvRange(roi.Upper.H, roi.Upper.H, roi.Upper.S, roi.Upper.S, roi.Upper.V, roi.Upper.V);
                    double matchPct;
                    hsvService.DetectColor(roiBmp, lowerRange, upperRange, out matchPct);
                    score = (int)Math.Round(matchPct);
                }
                else
                {
                    var algorithm = roi.Algorithm ?? BarcodeAlgorithm.QRCode; string decoded = barcodeService.Decode(roiBmp, algorithm);
                    bool ok = !string.IsNullOrWhiteSpace(decoded) && (roi.ExpectedLength <=0 || decoded.Length == roi.ExpectedLength);
                    score = ok ? 100 : 0;
                }
            }
            roi.LastScore = score; fovManager.Save(fovList); SyncTemplatePanel();
            bool pass = EvaluateScore(roi, score);
            MessageBox.Show($"Score: {score} - {(pass ? "PASS" : "FAIL")}");
        }

      
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if ((keyData & Keys.KeyCode) == Keys.F5)
            {
                TestSelectedRoi();
                return true; // handled
            }
            // Add zoom shortcuts
            if (keyData == (Keys.Control | Keys.Add) || keyData == (Keys.Control | Keys.Oemplus))
            {
                ZoomIn();
                return true;
            }
            if (keyData == (Keys.Control | Keys.Subtract) || keyData == (Keys.Control | Keys.OemMinus))
            {
                ZoomOut();
                return true;
            }
            if (keyData == (Keys.Control | Keys.D0))
            {
                ResetZoom();
                return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }

      

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var result = MessageBox.Show("You want to Exit", "OK",
                MessageBoxButtons.OKCancel
                );
            if (result == DialogResult.OK)
            {
                Application.Exit();
            }
            else
            {

            }
        }

        private void ảutoRunToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (selectedFovIndex < 0 || selectedFovIndex >= fovList.Count)
            {
                MessageBox.Show("Hãy chọn FOV trước.");
                return;
            }

            var fov = fovList[selectedFovIndex];
            if (File.Exists(fov.ImagePath))
            {
                _bitmap = new Bitmap(fov.ImagePath);
                // Persist FOV image to base64 when loading from file
                try { fov.ImageBase64 = BitmapToBase64Png(_bitmap); fovManager.Save(fovList); } catch { }
            }
            else if (!string.IsNullOrEmpty(fov.ImageBase64))
            {
                try { _bitmap = Base64ToBitmap(fov.ImageBase64); }
                catch { MessageBox.Show("Không thể đọc ảnh FOV từ base64."); return; }
            }
            else
            {
                MessageBox.Show("Không tìm thấy ảnh FOV.");
                return;
            }

            pictureBox1.Image = _bitmap;
            pictureBox1.SizeMode = PictureBoxSizeMode.Zoom;
            roiList = fov.Rois;
            // ensure zoom applies to new image
            ResetZoom();

            _lastTestResults.Clear();

            for (int i = 0; i < roiList.Count; i++)
            {
                var roi = roiList[i];
                if (roi.IsHidden) continue;

                bool pass = false;
                Rectangle rect = new Rectangle(roi.X, roi.Y, roi.Width, roi.Height);
                rect.Intersect(new Rectangle(0, 0, _bitmap.Width, _bitmap.Height));
                if (rect.Width <= 0 || rect.Height <= 0)
                {
                    _lastTestResults[(selectedFovIndex, i)] = false;
                    continue;
                }

                // Handle Template Matching separately (use full FOV with ROI-limited search)
                if (string.Equals(roi.Mode, "Template Matching", StringComparison.OrdinalIgnoreCase) && roi.Template != null)
                {
                    Rectangle mrect; double mscore;
                    int tscore = RunTemplateMatching(roi, _bitmap, out mrect, out mscore);
                    roi.MatchRect = mrect; roi.MatchScore = mscore;
                    pass = EvaluateScore(roi, tscore);
                    _lastTestResults[(selectedFovIndex, i)] = pass;
                    continue;
                }

                using (var roiBmp = new Bitmap(rect.Width, rect.Height))
                using (var g = Graphics.FromImage(roiBmp))
                {
                    g.DrawImage(_bitmap, new Rectangle(0, 0, rect.Width, rect.Height), rect, GraphicsUnit.Pixel);
                    if (string.Equals(roi.Mode, "HSV", StringComparison.OrdinalIgnoreCase))
                    {
                        // Respect configured HSV ranges; compute only if missing
                        if (roi.Lower == null || roi.Upper == null)
                        {
                            var (lowerAuto, upperAuto, stats) = hsvAutoService.Compute(roiBmp, 15, 10);
                            roi.Lower = lowerAuto;
                            roi.Upper = upperAuto;
                        }
                        var lowerRange = new HsvRange(roi.Lower.H, roi.Lower.H, roi.Lower.S, roi.Lower.S, roi.Lower.V, roi.Lower.V);
                        var upperRange = new HsvRange(roi.Upper.H, roi.Upper.H, roi.Upper.S, roi.Upper.S, roi.Upper.V, roi.Upper.V);
                        double matchPct;
                        hsvService.DetectColor(roiBmp, lowerRange, upperRange, out matchPct);
                        int sc = (int)Math.Round(matchPct);
                        pass = EvaluateScore(roi, sc);
                    }
                    else
                    {
                        var algorithm = roi.Algorithm ?? BarcodeAlgorithm.QRCode;
                        string decoded = barcodeService.Decode(roiBmp, algorithm);

                        bool passLength = true;
                        if (roi.ExpectedLength > 0)
                        {
                            passLength = decoded?.Length == roi.ExpectedLength;
                        }

                        bool ok = !string.IsNullOrWhiteSpace(decoded) && passLength;
                        int sc = ok ? 100 : 0;
                        pass = EvaluateScore(roi, sc);
                    }
                }

                _lastTestResults[(selectedFovIndex, i)] = pass;
            }


            _showRunResults = true;
            _singleRoiMode = false;
            selectedRoiIndex = -1;
            pictureBox1.Invalidate();
        }

        private void btn_addfov_Click(object sender, EventArgs e)
        {
            using (var ofd = new OpenFileDialog())
            {
                ofd.Filter = "Image Files|*.png;*.jpg;*./jpeg;*.bmp";
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    var newFov = new FovRegion
                    {
                        ImagePath = ofd.FileName,
                        Rois = new List<RoiRegion>()
                    };

                    // Also store base64 so image is available across machines
                    try
                    {
                        var bytes = File.ReadAllBytes(ofd.FileName);
                        newFov.ImageBase64 = Convert.ToBase64String(bytes);
                    }
                    catch { }

                    fovManager.Add(newFov);
                    LoadFovToTreeView();

                    fovList = fovManager.Load();
                    selectedFovIndex = fovList.Count - 1;
                    roiList = fovList[selectedFovIndex].Rois;

                    if (File.Exists(ofd.FileName))
                    {
                        _bitmap = new Bitmap(ofd.FileName);
                        pictureBox1.Image = _bitmap;
                        pictureBox1.SizeMode = PictureBoxSizeMode.Zoom;
                        _showRoiOnImage = true;
                        ResetZoom();
                        pictureBox1.Invalidate();
                    }
                    else if (!string.IsNullOrEmpty(newFov.ImageBase64))
                    {
                        try
                        {
                            _bitmap = Base64ToBitmap(newFov.ImageBase64);
                            pictureBox1.Image = _bitmap;
                            pictureBox1.SizeMode = PictureBoxSizeMode.Zoom;
                            _showRoiOnImage = true;
                            ResetZoom();
                            pictureBox1.Invalidate();
                        }
                        catch { }
                    }

                    //MessageBox.Show("Đã thêm FOV mới với ảnh: " + Path.GetFileName(ofd.FileName));
                }
            }
            
        }

        private void btn_delete_Click(object sender, EventArgs e)
        {
            if (pn_property.SelectedNode == null) return;

            if (pn_property.SelectedNode.Text.StartsWith("ROI ") && selectedFovIndex >= 0 && selectedRoiIndex >= 0)
            {
                var confirm = MessageBox.Show($"Xóa ROI {selectedRoiIndex + 1} trong FOV {selectedFovIndex + 1}?", "Xác nhận", MessageBoxButtons.YesNo);
                if (confirm == DialogResult.Yes)
                {
                    fovList[selectedFovIndex].Rois.RemoveAt(selectedRoiIndex);
                    fovManager.Save(fovList);
                    roiList = fovList[selectedFovIndex].Rois;
                    selectedRoiIndex = -1;

                    LoadFovToTreeView();

                    if (selectedFovIndex >= 0 && selectedFovIndex < fovList.Count && File.Exists(fovList[selectedFovIndex].ImagePath))
                    {
                        _bitmap = new Bitmap(fovList[selectedFovIndex].ImagePath);
                        pictureBox1.Image = _bitmap;
                        _showRoiOnImage = true;
                        ResetZoom();
                        pictureBox1.Invalidate();
                    }

                    
                }
            }
            else if (pn_property.SelectedNode.Text.StartsWith("FOV ") && selectedFovIndex >= 0)
            {
                var confirm = MessageBox.Show($"Xóa FOV {selectedFovIndex + 1}?", "Xác nhận", MessageBoxButtons.YesNo);
                if (confirm == DialogResult.Yes)
                {
                    fovList.RemoveAt(selectedFovIndex);
                    fovManager.Save(fovList);

                    LoadFovToTreeView();

                    if (fovList.Count > 0)
                    {
                        selectedFovIndex = 0;
                        if (File.Exists(fovList[0].ImagePath))
                        {
                            _bitmap = new Bitmap(fovList[0].ImagePath);
                            pictureBox1.Image = _bitmap;
                            roiList = fovList[0].Rois;
                            _showRoiOnImage = true;
                            ResetZoom();
                        }
                        else
                        {
                            selectedFovIndex = -1;
                            selectedRoiIndex = -1;
                            pictureBox1.Image = null;
                            _bitmap = null;
                            roiList.Clear();
                        }

                        pictureBox1.Invalidate();
                    }
                }
            }
        }
        private void btnAddRoi_Click_1(object sender, EventArgs e)
        {
            if (selectedFovIndex < 0 || selectedFovIndex >= fovList.Count)
            {
                MessageBox.Show("Vui lòng chọn FOV trước khi thêm ROI.");
                return;
            }

            var newRoi = new RoiRegion
            {
                Id = fovList[selectedFovIndex].Rois.Count + 1,
                Name = "SMD_" + (fovList[selectedFovIndex].Rois.Count + 1).ToString("D3"),
                IsEnabled = true,
                Type = "Unknown",
                X = 0,
                Y = 0,
                Width = 0,
                Height = 0,
                IsHidden = false
            };

            fovList[selectedFovIndex].Rois.Add(newRoi);
            fovManager.Save(fovList);

            LoadFovToTreeView();

            foreach (TreeNode node in pn_property.Nodes)
            {
                foreach (TreeNode fovNode in node.Nodes)
                {
                    if (fovNode.Text == $"FOV {selectedFovIndex + 1}")
                    {
                        pn_property.SelectedNode = fovNode;
                        fovNode.Expand();
                        break;
                    }
                }
            }
        }
        
        
        private void pictureBox1_Paint(object sender, PaintEventArgs e)
        {
            if (_bitmap == null) return;
            if (_singleRoiMode && selectedRoiIndex >= 0 && selectedRoiIndex < roiList.Count)
            {
                var roi = roiList[selectedRoiIndex];
                if (!roi.IsHidden)
                    roiRenderer.DrawRois(e.Graphics, new List<RoiRegion> { roi }, _bitmap, 0, true);
            }
            else
            {
                var visibleRois = roiList.FindAll(r => !r.IsHidden);
                roiRenderer.DrawRois(e.Graphics, visibleRois, _bitmap, selectedRoiIndex, true);
            }
            if (_drawMode && _isSelecting && _selectRectangle != Rectangle.Empty)
            {
                using (Pen pen = new Pen(Color.Red, 2))
                {
                    pen.DashStyle = System.Drawing.Drawing2D.DashStyle.Dash;
                    e.Graphics.DrawRectangle(pen, _selectRectangle);
                }
            }
        }

        private void pictureBox1_MouseDown(object sender, MouseEventArgs e)
        {
            if (_bitmap == null || !_drawMode) return;
            _isSelecting = true;
            _startPoint = e.Location;
            _selectRectangle = new Rectangle(e.Location, new System.Drawing.Size(0, 0));
        }

        private void pictureBox1_MouseMove(object sender, MouseEventArgs e)
        {
            if (!_isSelecting || _bitmap == null || !_drawMode) return;
            int x = Math.Min(_startPoint.X, e.X);
            int y = Math.Min(_startPoint.Y, e.Y);
            int w = Math.Abs(_startPoint.X - e.X);
            int h = Math.Abs(_startPoint.Y - e.Y);
            _selectRectangle = new Rectangle(x, y, w, h);
            pictureBox1.Invalidate();
        }

        private void pictureBox1_MouseUp(object sender, MouseEventArgs e)
        {
            if (_bitmap == null || !_drawMode) return;
            if (_selectRectangle.Width == 0 || _selectRectangle.Height == 0) { _isSelecting = false; return; }
            _isSelecting = false;
            Rectangle imgRect = ImageHelper.GetDisplayedImageRectangle(pictureBox1);
            float scaleX = (float)_bitmap.Width / imgRect.Width;
            float scaleY = (float)_bitmap.Height / imgRect.Height;
            int realX = (int)((_selectRectangle.X - imgRect.X) * scaleX);
            int realY = (int)((_selectRectangle.Y - imgRect.Y) * scaleY);
            int realW = (int)(_selectRectangle.Width * scaleX);
            int realH = (int)(_selectRectangle.Height * scaleY);
            realX = Math.Max(0, realX);
            realY = Math.Max(0, realY);
            realW = Math.Min(realW, _bitmap.Width - realX);
            realH = Math.Min(realH, _bitmap.Height - realY);
            if (_isUpdatingRoi && _roiToUpdateIndex >= 0 && _roiToUpdateIndex < roiList.Count)
            {
                var roi = roiList[_roiToUpdateIndex];
                roi.X = realX; roi.Y = realY; roi.Width = realW; roi.Height = realH;
                fovManager.Save(fovList);
            }
            else if (selectedFovIndex >= 0 && selectedFovIndex < fovList.Count)
            {
                var newRoi = new RoiRegion { X = realX, Y = realY, Width = realW, Height = realH, IsHidden = false };
                fovList[selectedFovIndex].Rois.Add(newRoi);
                fovManager.Save(fovList);
                roiList = fovList[selectedFovIndex].Rois;
            }
            _drawMode = false; _isUpdatingRoi = false; Cursor = Cursors.Default; pictureBox1.Invalidate();
        }

        private void pictureBox1_MouseWheel(object sender, MouseEventArgs e)
        {
            if (_bitmap == null) return;

            if (e.Delta > 0)
            {
                ZoomIn();
            }
            else if (e.Delta < 0)
            {
                ZoomOut();
            }
        }

        private void ZoomIn()
        {
            if (_bitmap == null) return;
            _zoomFactor = Math.Min(_zoomFactor + _zoomStep, _maxZoom);
            ApplyZoom();
        }

        private void ZoomOut()
        {
            if (_bitmap == null) return;
            _zoomFactor = Math.Max(_zoomFactor - _zoomStep, _minZoom);
            ApplyZoom();
        }

        private void ResetZoom()
        {
            if (_bitmap == null) return;

            _zoomFactor = 1.0f;
            pictureBox1.SizeMode = PictureBoxSizeMode.Zoom;
            // place image in scroll panel at natural size
            pictureBox1.Dock = DockStyle.None;
            pictureBox1.Width = _bitmap.Width;
            pictureBox1.Height = _bitmap.Height;
            pictureBox1.Left = 0;
            pictureBox1.Top = 0;
            // center if smaller than viewport
            if (panelScroll != null)
            {
                int vw = panelScroll.ClientSize.Width;
                int vh = panelScroll.ClientSize.Height;
                if (pictureBox1.Width < vw)
                    pictureBox1.Left = (vw - pictureBox1.Width) / 2;
                if (pictureBox1.Height < vh)
                    pictureBox1.Top = (vh - pictureBox1.Height) / 2;
            }
            pictureBox1.Invalidate();
        }

        private void ApplyZoom()
        {
            if (_bitmap == null) return;

            pictureBox1.SizeMode = PictureBoxSizeMode.Zoom;
            pictureBox1.Dock = DockStyle.None;
            // compute new size based on bitmap and zoom factor
            int newWidth = (int)(_bitmap.Width * _zoomFactor);
            int newHeight = (int)(_bitmap.Height * _zoomFactor);
            pictureBox1.Width = Math.Max(1, newWidth);
            pictureBox1.Height = Math.Max(1, newHeight);

            // keep top-left at 0,0 so scrollbars appear when larger than viewport
            pictureBox1.Left = 0;
            pictureBox1.Top = 0;

            // center only if smaller than viewport (no scrollbars)
            if (panelScroll != null)
            {
                int vw = panelScroll.ClientSize.Width;
                int vh = panelScroll.ClientSize.Height;
                if (pictureBox1.Width < vw)
                    pictureBox1.Left = (vw - pictureBox1.Width) / 2;
                if (pictureBox1.Height < vh)
                    pictureBox1.Top = (vh - pictureBox1.Height) / 2;
            }

            pictureBox1.Invalidate();
        }
        
    }
}
