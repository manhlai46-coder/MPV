using MPV.Enums;
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
        private readonly FovManager fovManager;
        private readonly BarcodeService barcodeService;
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
        
        public Form1()
        {
            InitializeComponent();

            string fovPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "fov_data.json");
            fovManager = new FovManager(fovPath);
            barcodeService = new BarcodeService();
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
            var propertyPanel = new RoiPropertyPanel(fovManager, fovList, pictureBox1, selectedFovIndex, selectedRoiIndex, roiList);
           

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
                        pictureBox1.SizeMode = PictureBoxSizeMode.Zoom;
                        roiList = fov.Rois;
                        _showRoiOnImage = true;
                        pictureBox1.Invalidate();
                    }
                    
                   
                }
            }
            else if (e.Node.Text.StartsWith("ROI "))
            {
                if (!int.TryParse(e.Node.Text.Replace("ROI ", ""), out int roiIndex)) return;
                selectedRoiIndex = roiIndex - 1;

                if (selectedRoiIndex >= 0 && selectedRoiIndex < roiList.Count)
                {
                   
                   
                    pictureBox1.Invalidate();


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
            var propertyPanel = new RoiPropertyPanel(fovManager, fovList, pictureBox1, selectedFovIndex, selectedRoiIndex, roiList);
           

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

                            propertyPanel.ShowRoiProperties(panelImage, roiList[selectedRoiIndex]);
                            pictureBox1.Invalidate();
                        }
                        else
                        {
                           
                            selectedRoiIndex = -1;
                            panelImage.Controls.Clear();
                            pictureBox1.Invalidate();
                        }
                    }
                }

          
                if (e.Button == MouseButtons.Right)
                {
                    TreeNode node = trv1.GetNodeAt(e.X, e.Y);
                    if (node != null)
                    {
                        trv1.SelectedNode = node;
                    }
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

            var realRect = new Rectangle(realX, realY, realW, realH);
            var cropped = CropBitmap(_bitmap, realRect);

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
            if (e.Button == MouseButtons.Right)
            {
                TreeNode node = trv1.GetNodeAt(e.X, e.Y);
                if (node != null)
                {
                    trv1.SelectedNode = node;
                }
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
            var visibleRois = roiList.FindAll(r => !r.IsHidden);
            if (selectedFovIndex >= 0 && fovList[selectedFovIndex].IsHidden)
                visibleRois.Clear();

            roiRenderer.DrawRois(e.Graphics, visibleRois, _bitmap, selectedRoiIndex, _showRoiOnImage);

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

                    if (File.Exists(fovList[selectedFovIndex].ImagePath))
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
                    selectedFovIndex = -1;
                    selectedRoiIndex = -1;
                    roiList.Clear();
                  

                    LoadFovToTreeView();

                    if (fovList.Count > 0)
                    {
                        selectedFovIndex = 0;
                        _bitmap = new Bitmap(fovList[0].ImagePath);
                        pictureBox1.Image = _bitmap;
                        roiList = fovList[0].Rois;
                        _showRoiOnImage = true;
                    }
                    else
                    {
                        pictureBox1.Image = null;
                        _bitmap = null;
                    }

                    pictureBox1.Invalidate();

               
                    panelImage.Controls.Clear();
                }
            }
        }





        private void btn_Exit_Click(object sender, EventArgs e)
        {
            Application.Exit();
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
            fovList = fovManager.Load();

            if (fovList.Count == 0)
            {
                MessageBox.Show("Không có FOV nào được lưu.");
                return;
            }

            string detectedBarcodeContent = ""; // Store the detected barcode content here.
            bool anyBarcodeDetected = false; // Flag to check if any barcode is detected.

            foreach (var fov in fovList)
            {
                if (fov.IsHidden) continue;
                if (!File.Exists(fov.ImagePath))
                {
                    MessageBox.Show($"Không tìm thấy ảnh: {fov.ImagePath}");
                    continue;
                }

                _bitmap = new Bitmap(fov.ImagePath);
                roiList = fov.Rois;

                foreach (var roi in roiList)
                {
                    if (roi.IsHidden) continue;

                    Rectangle rect = new Rectangle(roi.X, roi.Y, roi.Width, roi.Height);
                    using (var cropped = CropBitmap(_bitmap, rect))
                    {
                        string text = barcodeService.Decode(cropped, roi.Algorithm);
                        roi.IsDetected = !string.IsNullOrEmpty(text);

                        if (roi.IsDetected)
                        {
                            detectedBarcodeContent = text; // Store the detected barcode content
                            anyBarcodeDetected = true; // Mark that a barcode has been detected
                            LoggerService.Info($"ROI detected barcode: {text}");
                        }
                    }
                }

                pictureBox1.Image = _bitmap;
                pictureBox1.SizeMode = PictureBoxSizeMode.Zoom;
                _showRoiOnImage = true;
                pictureBox1.Invalidate();

                Application.DoEvents();
                System.Threading.Thread.Sleep(500);
            }
            fovManager.Save(fovList);

            if (anyBarcodeDetected)
            {
                MessageBox.Show(detectedBarcodeContent);
            }
            else
            {
                MessageBox.Show("không đọc được barcode");
            }
            pictureBox1.Invalidate();
            fovList = fovManager.Load();
            

        }



      


        private void btn_Exit_Click_1(object sender, EventArgs e)
        {
            Application.Exit();
        }

        

       
       

        private void AddPropertyRow(TableLayoutPanel table, string label, string value)
        {
            var lblProperty = new Label
            {
                Text = label,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            };

            var txtValue = new TextBox
            {
                Text = value,
                Dock = DockStyle.Fill,
                ReadOnly = true,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Segoe UI", 9F)
            };

            table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            table.Controls.Add(lblProperty, 0, table.RowCount - 1);
            table.Controls.Add(txtValue, 1, table.RowCount - 1);
        }
    }

}
