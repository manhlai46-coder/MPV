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
        private readonly Dictionary<(int fov, int roi), bool> _lastTestResults = new Dictionary<(int fov, int roi), bool>();
        private bool _singleRoiMode = false;     
        private bool _showRunResults = false;

       //AppDomain.CurrentDomain
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
            
            var menuItemTest = new ToolStripMenuItem("Vẽ roi", null, DrawRoi_Click);
            var menuItemDelete = new ToolStripMenuItem("Reset", null, ResetRoi_Click);
           
            contextMenu.Items.Add(menuItemTest);
            contextMenu.Items.Add(menuItemDelete);
            pictureBox1.ContextMenuStrip = contextMenu;

        }
        private void ResetRoi_Click(object sender, EventArgs e)
        {
            if (selectedRoiIndex >= 0 && selectedRoiIndex < roiList.Count)
            {
                roiList[selectedRoiIndex].IsHidden = true;
                selectedRoiIndex = -1; 
                _singleRoiMode = false; 
                panelImage.Controls.Clear(); 
                pictureBox1.Invalidate();
            }
            else
            {
                MessageBox.Show("Vui lòng chọn một ROI hợp lệ để reset.");
            }
        }
        private void DrawRoi_Click(object sender, EventArgs e)
        {
            if (selectedFovIndex < 0 || selectedFovIndex >= fovList.Count)
            {
                MessageBox.Show("Vui lòng chọn một FOV trước khi vẽ ROI.");
                return;
            }

            // Bật chế độ vẽ
            _drawMode = true;
            _isUpdatingRoi = true; // Không phải cập nhật ROI cũ
            _isSelecting = false;   // Chưa bắt đầu vẽ
            _selectRectangle = Rectangle.Empty; // Reset vùng chọn

            // Cập nhật giao diện
            Cursor = Cursors.Cross;
            MessageBox.Show("Chế độ vẽ ROI đã được bật. Hãy kéo chuột trên PictureBox để vẽ.");
        }





        private void Form1_Load(object sender, EventArgs e)
        {
            InitializeContextMenu();

           
            this.KeyPreview = true;

           
            ptr_template.Enabled = false;

            try
            {
                LoadFovToTreeView();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi khi load JSON: " + ex.Message);
                LoggerService.Error("Error loading JSON", ex);
            }
            ToolTip toolTip = new ToolTip();

        }

        private void LoadFovToTreeView()
        {
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

            pn_property.Nodes.Add(root);
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
                _showRunResults = false; 

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
                    _showRunResults = false; 
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

        


void pictureBox1_MouseDown(object sender, MouseEventArgs e)
        {
            if (_bitmap == null || !_drawMode) return;

            _isSelecting = true;
            _startPoint = e.Location;
            _selectRectangle = new Rectangle(e.Location, new Size(0, 0));
        }

        // MouseMove
        private void pictureBox1_MouseMove(object sender, MouseEventArgs e)
        {
            if (!_isSelecting || _bitmap == null || !_drawMode) return;

            int x = Math.Min(_startPoint.X, e.X);
            int y = Math.Min(_startPoint.Y, e.Y);
            int w = Math.Abs(_startPoint.X - e.X);
            int h = Math.Abs(_startPoint.Y - e.Y);
            _selectRectangle = new Rectangle(x, y, w, h);

            pictureBox1.Invalidate(); // Vẽ lại PictureBox để hiển thị ROI
        }

        // MouseUp
        private void pictureBox1_MouseUp(object sender, MouseEventArgs e)
        {
            if (_bitmap == null || !_drawMode) return;

            if (_selectRectangle.Width == 0 || _selectRectangle.Height == 0)
            {
                MessageBox.Show("Vui lòng kéo chuột để vẽ ROI hợp lệ.");
                return;
            }

            _isSelecting = false;

            // Chuyển tọa độ từ vùng hiển thị sang tọa độ ảnh gốc
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

            // Thêm ROI mới vào danh sách
            var newRoi = new RoiRegion
            {
                X = realX,
                Y = realY,
                Width = realW,
                Height = realH,
                IsHidden = false
            };
            fovList[selectedFovIndex].Rois.Add(newRoi);
            fovManager.Save(fovList);

            MessageBox.Show($"Đã thêm ROI vào FOV {selectedFovIndex + 1}.");

            // Tắt chế độ vẽ
            _drawMode = false;
            Cursor = Cursors.Default;

            // Cập nhật TreeView và PictureBox
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
            // Hiển thị các ROI đã lưu (bỏ qua các ROI bị ẩn)
            var visibleRois = roiList.FindAll(r => !r.IsHidden);
            roiRenderer.DrawRois(e.Graphics, visibleRois, _bitmap, selectedRoiIndex, _showRoiOnImage);

            // Hiển thị ROI đang được vẽ (nếu có)
            if (_drawMode && _selectRectangle != Rectangle.Empty)
            {
                using (Pen pen = new Pen(Color.Red, 2))
                {
                    pen.DashStyle = System.Drawing.Drawing2D.DashStyle.Dash;
                    e.Graphics.DrawRectangle(pen, _selectRectangle);
                }
            }
        }

        

        //private void MenuDelete_Click(object sender, EventArgs e)
        //{
        //    if (pn_property.SelectedNode == null) return;
        //    if (pn_property.SelectedNode.Text.StartsWith("ROI ") && selectedFovIndex >= 0 && selectedRoiIndex >= 0)
        //    {
        //        var confirm = MessageBox.Show($"Xóa ROI {selectedRoiIndex + 1} trong FOV {selectedFovIndex + 1}?", "Xác nhận", MessageBoxButtons.YesNo);
        //        if (confirm == DialogResult.Yes)
        //        {
        //            fovList[selectedFovIndex].Rois.RemoveAt(selectedRoiIndex);
        //            fovManager.Save(fovList);
        //            roiList = fovList[selectedFovIndex].Rois;
        //            selectedRoiIndex = -1;

        //            LoadFovToTreeView();

        //            if (selectedFovIndex >= 0 && selectedFovIndex < fovList.Count && File.Exists(fovList[selectedFovIndex].ImagePath))
        //            {
        //                _bitmap = new Bitmap(fovList[selectedFovIndex].ImagePath);
        //                pictureBox1.Image = _bitmap;
        //                _showRoiOnImage = true;
        //                pictureBox1.Invalidate();
        //            }

        //            panelImage.Controls.Clear();
        //        }
        //    }
        //    else if (pn_property.SelectedNode.Text.StartsWith("FOV ") && selectedFovIndex >= 0)
        //    {
        //        var confirm = MessageBox.Show($"Xóa FOV {selectedFovIndex + 1}?", "Xác nhận", MessageBoxButtons.YesNo);
        //        if (confirm == DialogResult.Yes)
        //        {
        //            fovList.RemoveAt(selectedFovIndex);
        //            fovManager.Save(fovList);

        //            LoadFovToTreeView();

        //            if (fovList.Count > 0)
        //            {
        //                selectedFovIndex = 0;
        //                if (File.Exists(fovList[0].ImagePath))
        //                {
        //                    _bitmap = new Bitmap(fovList[0].ImagePath);
        //                    pictureBox1.Image = _bitmap;
        //                    roiList = fovList[0].Rois;
        //                    _showRoiOnImage = true;
        //                }
        //            }
        //            else
        //            {
        //                selectedFovIndex = -1;
        //                selectedRoiIndex = -1;
        //                pictureBox1.Image = null;
        //                _bitmap = null;
        //                roiList.Clear();
        //            }

        //            pictureBox1.Invalidate();
        //            panelImage.Controls.Clear();
        //        }
        //    }

        //}

    

        

        // Extracted from btn_test_Click so F5 can reuse it
        private void TestSelectedRoi()
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
                    var (lower, upper, _) = hsvAutoService.Compute(roiBmp, 2, 10);
                    bool inRange = true;
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

        private void btn_test_Click(object sender, EventArgs e)
        {
            
            TestSelectedRoi();
        }

      
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if ((keyData & Keys.KeyCode) == Keys.F5)
            {
                TestSelectedRoi();
                return true; // handled
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

        private void btn_addfov_Click(object sender, EventArgs e)
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
                        pictureBox1.Invalidate();
                    }

                    panelImage.Controls.Clear();
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

        private void btnDrawRoi_Click(object sender, EventArgs e)
        {

        }
    }
}
