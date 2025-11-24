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
        private Point _startPoint;
        private bool _isSelecting = false;
        private bool _drawMode = false;
        private bool _isUpdatingRoi = false;
        private bool _showRoiOnImage = false;

        private int selectedFovIndex = -1;
        private int selectedRoiIndex = -1;

        private List<FovRegion> fovList = new List<FovRegion>();
        private List<RoiRegion> roiList = new List<RoiRegion>();

        // Last pass/fail results (true=PASS, false=FAIL)
        private readonly Dictionary<(int fov, int roi), bool> _lastTestResults = new Dictionary<(int fov, int roi), bool>();

        // Show only selected ROI
        private bool _singleRoiMode = false;

        // Show colored results for ALL ROIs after Run
        private bool _showRunResults = false;

        public Form1()
        {
            InitializeComponent();

            string fovPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "fov_data.json");
            fovManager = new FovManager(fovPath);
            barcodeService = new BarcodeService();
            hsvService = new HsvService();
            roiRenderer = new RoiRenderer(pictureBox1);
        }

        private void InitializeContextMenu()
        {
            contextMenu = new ContextMenuStrip();
            var deleteItem = new ToolStripMenuItem("Xóa");
            deleteItem.Click += MenuDelete_Click;
            contextMenu.Items.Add(deleteItem);

            trv1.ContextMenuStrip = contextMenu;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            InitializeContextMenu();
            try
            {
                LoadFovToTreeView();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi khi load JSON: " + ex.Message);
                LoggerService.Error("Error loading JSON", ex);
            }
        }

        private void LoadFovToTreeView()
        {
            trv1.Nodes.Clear();
            fovList = fovManager.Load();

            if (fovList.Count == 0)
            {
                trv1.Nodes.Add("Chưa có FOV nào được lưu.");
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
                fovNode.Nodes.Add($"Image: {Path.GetFileName(fov.ImagePath)}");

                for (int j = 0; j < fov.Rois.Count; j++)
                {
                    var roi = fov.Rois[j];
                    var roiNode = new TreeNode($"ROI {j + 1}");
                    roiNode.Nodes.Add($"X: {roi.X}");
                    roiNode.Nodes.Add($"Y: {roi.Y}");
                    roiNode.Nodes.Add($"Width: {roi.Width}");
                    roiNode.Nodes.Add($"Height: {roi.Height}");
                    fovNode.Nodes.Add(roiNode);
                }

                root.Nodes.Add(fovNode);
            }

            trv1.Nodes.Add(root);
            root.Expand();
        }

        private void trv1_AfterSelect(object sender, TreeViewEventArgs e)
        {
            if (e.Node == null) return;

            if (e.Node.Text.StartsWith("FOV "))
            {
                if (!int.TryParse(e.Node.Text.Replace("FOV ", ""), out int fovIndex)) return;
                selectedFovIndex = fovIndex - 1;
                selectedRoiIndex = -1;
                _singleRoiMode = false;
                _showRunResults = false; // leaving run visualization when changing FOV

                if (selectedFovIndex >= 0 && selectedFovIndex < fovList.Count)
                {
                    var fov = fovList[selectedFovIndex];
                    if (File.Exists(fov.ImagePath))
                    {
                        _bitmap = new Bitmap(fov.ImagePath);
                        pictureBox1.Image = _bitmap;
                        pictureBox1.SizeMode = PictureBoxSizeMode.Zoom;
                        roiList = fov.Rois;
                        _showRoiOnImage = true;
                        pictureBox1.Invalidate();
                    }
                    else
                    {
                        pictureBox1.Image = null;
                        _bitmap = null;
                        roiList = new List<RoiRegion>();
                        _showRoiOnImage = false;
                        pictureBox1.Invalidate();
                    }
                }
                panelImage.Controls.Clear();
            }
            else if (e.Node.Text.StartsWith("ROI "))
            {
                if (!int.TryParse(e.Node.Text.Replace("ROI ", ""), out int roiIndex)) return;
                selectedRoiIndex = roiIndex - 1;

                if (selectedFovIndex >= 0 && selectedFovIndex < fovList.Count)
                    roiList = fovList[selectedFovIndex].Rois;

                if (selectedRoiIndex >= 0 && selectedRoiIndex < roiList.Count)
                {
                    _singleRoiMode = true;
                    _showRunResults = false; // focus only on selected ROI
                    pictureBox1.Invalidate();

                    var propertyPanel = new RoiPropertyPanel(
                        fovManager,
                        fovList,
                        pictureBox1,
                        _bitmap,
                        selectedFovIndex,
                        selectedRoiIndex,
                        roiList);
                    propertyPanel.ShowRoiProperties(panelImage, roiList[selectedRoiIndex]);
                }
            }
        }

        private void btnDrawRoi_Click(object sender, EventArgs e)
        {
            if (selectedFovIndex < 0)
            {
                MessageBox.Show("Hãy chọn FOV trước khi vẽ ROI.");
                return;
            }

            _drawMode = !_drawMode;

            if (_drawMode)
            {
                btnDrawRoi.Text = "Đang Vẽ ROI...";
                btnDrawRoi.BackColor = Color.LightGreen;
                Cursor = Cursors.Cross;
                LoggerService.Info("Vẽ ROI: Bật chế độ vẽ.");
            }
            else
            {
                btnDrawRoi.Text = "Vẽ ROI";
                btnDrawRoi.BackColor = SystemColors.Control;
                Cursor = Cursors.Default;
                _isSelecting = false;
                _selectRectangle = Rectangle.Empty;
                pictureBox1.Invalidate();
                LoggerService.Info("Vẽ ROI: Tắt chế độ vẽ.");
            }
        }

        private void pictureBox1_MouseDown(object sender, MouseEventArgs e)
        {
            if (_bitmap == null || !_drawMode) return;

            _isSelecting = true;
            _startPoint = e.Location;
            _selectRectangle = new Rectangle(e.Location, new Size(0, 0));
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
            if (_bitmap == null) return;

            if (!_drawMode)
            {
                if (e.Button == MouseButtons.Left)
                {
                    Rectangle imgRect = ImageHelper.GetDisplayedImageRectangle(pictureBox1);
                    if (imgRect.Contains(e.Location))
                    {
                        float scaleX = (float)_bitmap.Width / imgRect.Width;
                        float scaleY = (float)_bitmap.Height / imgRect.Height;
                        int imgX = (int)((e.X - imgRect.X) * scaleX);
                        int imgY = (int)((e.Y - imgRect.Y) * scaleY);

                        int foundIndex = roiList.FindIndex(r => new Rectangle(r.X, r.Y, r.Width, r.Height).Contains(imgX, imgY));
                        if (foundIndex >= 0)
                        {
                            selectedRoiIndex = foundIndex;
                            _singleRoiMode = true;
                            _showRunResults = false;

                            var propertyPanel = new RoiPropertyPanel(
                                fovManager,
                                fovList,
                                pictureBox1,
                                _bitmap,
                                selectedFovIndex,
                                selectedRoiIndex,
                                roiList);
                            propertyPanel.ShowRoiProperties(panelImage, roiList[selectedRoiIndex]);

                            pictureBox1.Invalidate();
                        }
                        else
                        {
                            selectedRoiIndex = -1;
                            _singleRoiMode = false;
                            _showRunResults = false;
                            panelImage.Controls.Clear();
                            pictureBox1.Invalidate();
                        }
                    }
                }

                if (e.Button == MouseButtons.Right)
                {
                    TreeNode node = trv1.GetNodeAt(e.X, e.Y);
                    if (node != null)
                        trv1.SelectedNode = node;
                }
                return;
            }

            if (_selectRectangle.Width == 0 || _selectRectangle.Height == 0) return;

            _isSelecting = false;

            Rectangle imgRect2 = ImageHelper.GetDisplayedImageRectangle(pictureBox1);
            float scaleX2 = (float)_bitmap.Width / imgRect2.Width;
            float scaleY2 = (float)_bitmap.Height / imgRect2.Height;

            int realX = (int)((_selectRectangle.X - imgRect2.X) * scaleX2);
            int realY = (int)((_selectRectangle.Y - imgRect2.Y) * scaleY2);
            int realW = (int)(_selectRectangle.Width * scaleX2);
            int realH = (int)(_selectRectangle.Height * scaleY2);

            realX = Math.Max(0, realX);
            realY = Math.Max(0, realY);
            realW = Math.Min(realW, _bitmap.Width - realX);
            realH = Math.Min(realH, _bitmap.Height - realY);

            if (_isUpdatingRoi && selectedRoiIndex >= 0)
            {
                var roi = roiList[selectedRoiIndex];
                roi.X = realX;
                roi.Y = realY;
                roi.Width = realW;
                roi.Height = realH;

                fovList[selectedFovIndex].Rois = roiList;
                fovManager.Save(fovList);

                MessageBox.Show($"Đã cập nhật ROI {selectedRoiIndex + 1} trong FOV {selectedFovIndex + 1}.");
                _isUpdatingRoi = false;
            }
            else if (selectedFovIndex >= 0)
            {
                var roi = new RoiRegion
                {
                    X = realX,
                    Y = realY,
                    Width = realW,
                    Height = realH
                };
                fovList[selectedFovIndex].Rois.Add(roi);
                fovManager.Save(fovList);
                MessageBox.Show($"Đã thêm ROI vào FOV {selectedFovIndex + 1}");
            }

            _drawMode = false;
            btnDrawRoi.Text = "Vẽ ROI";
            btnDrawRoi.BackColor = SystemColors.Control;
            Cursor = Cursors.Default;
            LoadFovToTreeView();
            pictureBox1.Invalidate();
        }

        private Bitmap CropBitmap(Bitmap source, Rectangle area)
        {
            Bitmap cropped = new Bitmap(area.Width, area.Height);
            using (Graphics g = Graphics.FromImage(cropped))
            {
                g.DrawImage(source, new Rectangle(0, 0, cropped.Width, cropped.Height),
                            area, GraphicsUnit.Pixel);
            }
            return cropped;
        }

        private void pictureBox1_Paint(object sender, PaintEventArgs e)
        {
            // Single selected ROI mode (after Test button)
            if (_singleRoiMode && !_showRunResults && selectedRoiIndex >= 0 && selectedRoiIndex < roiList.Count && _bitmap != null)
            {
                var roi = roiList[selectedRoiIndex];
                Rectangle imgRect = ImageHelper.GetDisplayedImageRectangle(pictureBox1);
                if (imgRect.Width > 0 && imgRect.Height > 0)
                {
                    float sx = (float)imgRect.Width / _bitmap.Width;
                    float sy = (float)imgRect.Height / _bitmap.Height;

                    Rectangle dispRect = new Rectangle(
                        imgRect.X + (int)Math.Round(roi.X * sx),
                        imgRect.Y + (int)Math.Round(roi.Y * sy),
                        (int)Math.Round(roi.Width * sx),
                        (int)Math.Round(roi.Height * sy)
                    );

                    bool pass;
                    Color borderColor;
                    if (_lastTestResults.TryGetValue((selectedFovIndex, selectedRoiIndex), out pass))
                        borderColor = pass ? Color.LimeGreen : Color.Red; // PASS=green, FAIL=red
                    else
                        borderColor = Color.DodgerBlue; // not tested

                    using (var pen = new Pen(borderColor, 3))
                        e.Graphics.DrawRectangle(pen, dispRect);
                }

                if (_drawMode && _selectRectangle != Rectangle.Empty)
                {
                    using (Pen pen = new Pen(Color.Red, 2))
                    {
                        pen.DashStyle = System.Drawing.Drawing2D.DashStyle.Dash;
                        e.Graphics.DrawRectangle(pen, _selectRectangle);
                    }
                }
                return;
            }

            // After Run: show pass/fail colors for ALL ROIs
            if (_showRunResults && _bitmap != null)
            {
                var visibleRois = roiList.FindAll(r => !r.IsHidden);
                if (selectedFovIndex >= 0 && selectedFovIndex < fovList.Count && fovList[selectedFovIndex].IsHidden)
                    visibleRois.Clear();

                Rectangle imgRect = ImageHelper.GetDisplayedImageRectangle(pictureBox1);
                if (imgRect.Width > 0 && imgRect.Height > 0)
                {
                    float sx = (float)imgRect.Width / _bitmap.Width;
                    float sy = (float)imgRect.Height / _bitmap.Height;

                    for (int i = 0; i < roiList.Count; i++)
                    {
                        var r = roiList[i];
                        if (r.IsHidden) continue;

                        bool pass;
                        Color borderColor;
                        if (_lastTestResults.TryGetValue((selectedFovIndex, i), out pass))
                            borderColor = pass ? Color.LimeGreen : Color.Red; // PASS=green, FAIL=red
                        else
                            continue; // skip ROIs that weren't tested

                        Rectangle dispRect = new Rectangle(
                            imgRect.X + (int)Math.Round(r.X * sx),
                            imgRect.Y + (int)Math.Round(r.Y * sy),
                            (int)Math.Round(r.Width * sx),
                            (int)Math.Round(r.Height * sy)
                        );
                        using (var pen = new Pen(borderColor, 3))
                        {
                            e.Graphics.DrawRectangle(pen, dispRect);
                        }
                    }
                }

                if (_drawMode && _selectRectangle != Rectangle.Empty)
                {
                    using (Pen pen = new Pen(Color.Red, 2))
                    {
                        pen.DashStyle = System.Drawing.Drawing2D.DashStyle.Dash;
                        e.Graphics.DrawRectangle(pen, _selectRectangle);
                    }
                }
                return;
            }

            // Normal mode: draw base ROIs using roiRenderer
            var normalVisibleRois = roiList.FindAll(r => !r.IsHidden);
            if (selectedFovIndex >= 0 && selectedFovIndex < fovList.Count && fovList[selectedFovIndex].IsHidden)
                normalVisibleRois.Clear();

            roiRenderer.DrawRois(e.Graphics, normalVisibleRois, _bitmap, selectedRoiIndex, _showRoiOnImage);

            if (_drawMode && _selectRectangle != Rectangle.Empty)
            {
                using (Pen pen = new Pen(Color.Red, 2))
                {
                    pen.DashStyle = System.Drawing.Drawing2D.DashStyle.Dash;
                    e.Graphics.DrawRectangle(pen, _selectRectangle);
                }
            }
        }

        private void btn_Update_Click(object sender, EventArgs e)
        {
            if (selectedFovIndex < 0)
            {
                MessageBox.Show("Vui lòng chọn FOV trước khi cập nhật ROI.");
                return;
            }

            if (selectedRoiIndex < 0)
            {
                MessageBox.Show("Vui lòng chọn ROI cần cập nhật.");
                return;
            }

            _isUpdatingRoi = true;
            _drawMode = true;
            _isSelecting = false;
            _selectRectangle = Rectangle.Empty;

            btnDrawRoi.Text = "Đang cập nhật ROI...";
            btnDrawRoi.BackColor = Color.Orange;
            Cursor = Cursors.Cross;

            MessageBox.Show("Kéo chuột trên ảnh để vẽ vùng ROI mới thay thế ROI cũ.");
        }

        private void MenuDelete_Click(object sender, EventArgs e)
        {
            if (trv1.SelectedNode == null) return;

            if (trv1.SelectedNode.Text.StartsWith("ROI ") && selectedFovIndex >= 0 && selectedRoiIndex >= 0)
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
                        pictureBox1.Invalidate();
                    }

                    panelImage.Controls.Clear();
                }
            }
            else if (trv1.SelectedNode.Text.StartsWith("FOV ") && selectedFovIndex >= 0)
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
                        }
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
                    panelImage.Controls.Clear();
                }
            }
        }

        private void btnAddFov_Click_1(object sender, EventArgs e)
        {
            using (var ofd = new OpenFileDialog())
            {
                ofd.Filter = "Image Files|*.png;*.jpg;*.jpeg;*.bmp";
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    var newFov = new FovRegion
                    {
                        ImagePath = ofd.FileName,
                        Rois = new List<RoiRegion>()
                    };

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
                        pictureBox1.Invalidate();
                    }

                    MessageBox.Show("Đã thêm FOV mới với ảnh: " + Path.GetFileName(ofd.FileName));
                }
            }
        }

        private void btnrun_Click_1(object sender, EventArgs e)
        {
          
            if (selectedFovIndex < 0 || selectedFovIndex >= fovList.Count)
            {
                MessageBox.Show("Hãy chọn FOV trước.");
                return;
            }
            if (!File.Exists(fovList[selectedFovIndex].ImagePath))
            {
                MessageBox.Show("Không tìm thấy ảnh FOV.");
                return;
            }

            var fov = fovList[selectedFovIndex];
            _bitmap = new Bitmap(fov.ImagePath);
            pictureBox1.Image = _bitmap;
            pictureBox1.SizeMode = PictureBoxSizeMode.Zoom;
            roiList = fov.Rois;

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

                using (var roiBmp = new Bitmap(rect.Width, rect.Height))
                using (var g = Graphics.FromImage(roiBmp))
                {
                    g.DrawImage(_bitmap, new Rectangle(0, 0, rect.Width, rect.Height), rect, GraphicsUnit.Pixel);
                    if (string.Equals(roi.Mode, "HSV", StringComparison.OrdinalIgnoreCase))
                    {
                        var (lower, upper, stats) = hsvAutoService.Compute(roiBmp, 2, 10);
                        roi.Lower = lower;
                        roi.Upper = upper;
                        var lowerRange = new HsvRange(lower.H, lower.H, lower.S, lower.S, lower.V, lower.V);
                        var upperRange = new HsvRange(upper.H, upper.H, upper.S, upper.S, upper.V, upper.V);
                        double matchPct;
                        pass = hsvService.DetectColor(roiBmp, lowerRange, upperRange, out matchPct);
                    }
                    else
                    {
                        var algorithm = roi.Algorithm ?? BarcodeAlgorithm.QRCode;
                        string decoded = barcodeService.Decode(roiBmp, algorithm);
                        txt1.Text = decoded;

                        bool passLength = true;
                        if (roi.ExpectedLength > 0)
                        {
                            passLength = decoded?.Length == roi.ExpectedLength;
                        }

                        pass = !string.IsNullOrWhiteSpace(decoded) && passLength;
                    }
                }

                _lastTestResults[(selectedFovIndex, i)] = pass;
            }

            
            _showRunResults = true;
            _singleRoiMode = false;
            selectedRoiIndex = -1;
            pictureBox1.Invalidate();
        }

        private void btn_test_Click(object sender, EventArgs e)
        {
            if (selectedFovIndex < 0 || selectedFovIndex >= fovList.Count)
            {
                MessageBox.Show("Chưa chọn FOV hợp lệ.");
                return;
            }
            if (selectedRoiIndex < 0 || selectedRoiIndex >= roiList.Count)
            {
                MessageBox.Show("Vui lòng chọn ROI cần test.");
                return;
            }
            if (_bitmap == null)
            {
                var fov = fovList[selectedFovIndex];
                if (!File.Exists(fov.ImagePath))
                {
                    MessageBox.Show("Không tìm thấy ảnh FOV.");
                    return;
                }
                _bitmap = new Bitmap(fov.ImagePath);
                pictureBox1.Image = _bitmap;
                pictureBox1.SizeMode = PictureBoxSizeMode.Zoom;
            }

            var roi = roiList[selectedRoiIndex];
            Rectangle rect = new Rectangle(roi.X, roi.Y, roi.Width, roi.Height);
            rect.Intersect(new Rectangle(0, 0, _bitmap.Width, _bitmap.Height));
            if (rect.Width <= 0 || rect.Height <= 0)
            {
                MessageBox.Show("ROI nằm ngoài ảnh.");
                return;
            }

            bool pass = false;
            using (var roiBmp = new Bitmap(rect.Width, rect.Height))
            using (var g = Graphics.FromImage(roiBmp))
            {
                g.DrawImage(_bitmap, new Rectangle(0, 0, rect.Width, rect.Height), rect, GraphicsUnit.Pixel);
                if (string.Equals(roi.Mode, "HSV", StringComparison.OrdinalIgnoreCase))
                {
                    // Tính HSV của ảnh roiBmp
                    var (lower, upper, _) = hsvAutoService.Compute(roiBmp, 2, 10);

                    // So sánh giá trị HSV này với ngưỡng Lower và Upper trong roi hiện tại
                    bool inRange = true;

                    // Giả sử hsvAutoService.Compute trả về lower và upper như roi đã lưu
                    // Kiểm tra từng kênh H, S, V xem có nằm trong ngưỡng roi.Lower - roi.Upper không
                    if (lower.H < roi.Lower.H || upper.H > roi.Upper.H ||
                        lower.S < roi.Lower.S || upper.S > roi.Upper.S ||
                        lower.V < roi.Lower.V || upper.V > roi.Upper.V)
                    {
                        inRange = false;
                    }

                    pass = inRange;
                }

                else
                {
                    var algorithm = roi.Algorithm ?? BarcodeAlgorithm.QRCode;
                    string decoded = barcodeService.Decode(roiBmp, algorithm);
                    txt1.Text = decoded;

                    bool passLength = true;
                    if (roi.ExpectedLength > 0)  
                    {
                        passLength = decoded?.Length == roi.ExpectedLength;
                    }

                    pass = !string.IsNullOrWhiteSpace(decoded) && passLength;
                }

            }


            _lastTestResults[(selectedFovIndex, selectedRoiIndex)] = pass;
            _singleRoiMode = true;
            _showRunResults = false;
            pictureBox1.Invalidate();

            MessageBox.Show(pass ? "PASS" : "FAIL");
        }
    }
}
