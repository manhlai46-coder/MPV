using MPV.Models;
using MPV.Renderers;
using MPV.Service;
using MPV.Services;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace MPV
{
    public partial class Form1 : Form
    {

        private readonly RoiManager roiManager;
        private readonly BarcodeService barcodeService;
        private readonly RoiRenderer roiRenderer;
        private Bitmap _bitmap;
        private Rectangle _selectRectangle;
        private Point _startPoint;
        private bool _isSelecting = false;
        private bool _isUpdatingRoi = false;
        private bool _showRoiOnImage = false;
        private bool _drawMode = false;
        private int selectedRoiIndex = -1;
        private List<RoiRegion> roiList = new List<RoiRegion>();

        public Form1()
        {
            InitializeComponent();

            string roiPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "roi_regions.json");
            roiManager = new RoiManager(roiPath);
            barcodeService = new BarcodeService();
            roiRenderer = new RoiRenderer(pictureBox1);
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            try
            {
                LoadRoiToTreeView();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi khi load JSON: " + ex.Message);
                LoggerService.Error("Error loading JSON", ex);
            }
        }

        private void btn1_Click(object sender, EventArgs e)
        {
            using (var ofd = new OpenFileDialog())
            {
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    _bitmap = new Bitmap(ofd.FileName);
                    pictureBox1.Image = _bitmap;
                    pictureBox1.SizeMode = PictureBoxSizeMode.Zoom;
                    ResetSelections();
                    LoggerService.Info("Opened image file.");
                }
            }
        }

        private void ResetSelections()
        {
            _selectRectangle = Rectangle.Empty;
            selectedRoiIndex = -1;
            _showRoiOnImage = false;
            pictureBox1.Invalidate();
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
            if (_bitmap == null || !_drawMode) return;
            if (_selectRectangle.Width == 0 || _selectRectangle.Height == 0) return;

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

            var realRect = new Rectangle(realX, realY, realW, realH);
            var cropped = CropBitmap(_bitmap, realRect);

            string decodedText = barcodeService.Decode(cropped);

            if (!string.IsNullOrEmpty(decodedText))
            {
                MessageBox.Show("Mã đã quét: " + decodedText);
                LoggerService.Info("Detected barcode: " + decodedText);
            }
            else
            {
                MessageBox.Show("Không tìm thấy mã barcode trong vùng chọn.");
                LoggerService.Warn("No barcode detected in ROI.");
            }

            ptrb2.Image = cropped;
            ptrb2.SizeMode = PictureBoxSizeMode.Zoom;

            if (_isUpdatingRoi && selectedRoiIndex >= 0 && selectedRoiIndex < roiList.Count)
            {
                var roi = roiList[selectedRoiIndex];
                roi.X = realX;
                roi.Y = realY;
                roi.Width = realW;
                roi.Height = realH;
                roiManager.Save(roiList);
                MessageBox.Show($"Đã cập nhật ROI {selectedRoiIndex + 1}.");
                LoggerService.Info($"Updated ROI {selectedRoiIndex + 1}");
                _isUpdatingRoi = false;
            }
            _drawMode = false;
            btnDrawRoi.Text = "Vẽ ROI";
            btnDrawRoi.BackColor = SystemColors.Control;
            Cursor = Cursors.Default;


            LoadRoiToTreeView();
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
        private void btn2_Click(object sender, EventArgs e)
        {
            if (_selectRectangle.Width == 0 || _selectRectangle.Height == 0)
            {
                MessageBox.Show("Vui lòng chọn vùng hợp lệ.");
                return;
            }
            var roi = new RoiRegion
            {
                X = _selectRectangle.X,
                Y = _selectRectangle.Y,
                Width = _selectRectangle.Width,
                Height = _selectRectangle.Height
            };
            roiManager.Add(roi);
            MessageBox.Show("Đã lưu vùng chọn vào JSON.");
            LoggerService.Info("Saved new ROI to JSON.");
            LoadRoiToTreeView();
        }
        private void btnrun_Click(object sender, EventArgs e)
        {
            if (_bitmap == null)
            {
                MessageBox.Show("Vui lòng tải ảnh trước.");
                return;
            }

            roiList = roiManager.Load();
            if (roiList.Count == 0)
            {
                MessageBox.Show("Không có ROI nào được lưu.");
                return;
            }

            foreach (var roi in roiList)
            {
                var rect = new Rectangle(roi.X, roi.Y, roi.Width, roi.Height);
                using (var cropped = CropBitmap(_bitmap, rect))
                {
                    string text = barcodeService.Decode(cropped);
                    roi.IsDetected = !string.IsNullOrEmpty(text);
                    roi.BarcodeText = text ?? "Không có mã";
                    MessageBox.Show($"ROI [{roi.X},{roi.Y}] : {roi.BarcodeText}");
                }
            }

            _showRoiOnImage = true;
            roiManager.Save(roiList);
            pictureBox1.Invalidate();
        }

        private void pictureBox1_Paint(object sender, PaintEventArgs e)
        {
            if (_bitmap == null) return;

            if (_isSelecting)
                roiRenderer.DrawSelection(e.Graphics, _selectRectangle);

            roiRenderer.DrawRois(e.Graphics, roiList, _bitmap, selectedRoiIndex, _showRoiOnImage);
        }

        private void LoadRoiToTreeView()
        {
            trv1.Nodes.Clear();
            roiList = roiManager.Load();

            if (roiList.Count == 0)
            {
                trv1.Nodes.Add("Chưa có ROI nào được lưu.");
                return;
            }

            var root = new TreeNode("ROI Regions");
            for (int i = 0; i < roiList.Count; i++)
            {
                var roi = roiList[i];
                var node = new TreeNode($"ROI {i + 1}");
                node.Nodes.Add($"X: {roi.X}");
                node.Nodes.Add($"Y: {roi.Y}");
                node.Nodes.Add($"Width: {roi.Width}");
                node.Nodes.Add($"Height: {roi.Height}");
                root.Nodes.Add(node);
            }

            trv1.Nodes.Add(root);
            root.Expand();
        }

        private void trv1_AfterSelect(object sender, TreeViewEventArgs e)
        {
            if (e.Node.Text.StartsWith("ROI "))
            {
                int.TryParse(e.Node.Text.Replace("ROI ", ""), out int index);
                selectedRoiIndex = index - 1;
                pictureBox1.Invalidate();
            }
        }

        private void MenuDelete_Click(object sender, EventArgs e)
        {
            if (selectedRoiIndex < 0 || selectedRoiIndex >= roiList.Count)
                return;

            var confirm = MessageBox.Show($"Xóa ROI {selectedRoiIndex + 1}?", "Xác nhận", MessageBoxButtons.YesNo);
            if (confirm == DialogResult.Yes)
            {
                roiManager.DeleteAt(selectedRoiIndex);
                LoadRoiToTreeView();
                pictureBox1.Invalidate();
            }
        }
        private void btn_Update_Click(object sender, EventArgs e)
        {
            if (selectedRoiIndex < 0 || selectedRoiIndex >= roiList.Count)
            {
                MessageBox.Show("Vui lòng chọn ROI cần cập nhật.");
                return;
            }

            _isUpdatingRoi = true;
            MessageBox.Show("Hãy kéo chuột trên ảnh để vẽ vùng ROI mới.");
        }

        private void btn_Exit_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void btnDrawRoi_Click(object sender, EventArgs e)
        {
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
    }
}
