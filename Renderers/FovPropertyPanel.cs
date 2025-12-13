using System;
using System.Drawing;
using System.Windows.Forms;
using MPV.Models;
using MPV.Service;
using System.Collections.Generic;

namespace MPV.Renderers
{
    public class FovPropertyPanel
    {
        public event Action<string> CaptureRequested; // camera mode
        private readonly FovManager fovManager;
        private readonly List<FovRegion> fovList;
        private readonly int selectedFovIndex;

        public FovPropertyPanel(FovManager fovManager, List<FovRegion> fovList, int selectedFovIndex)
        {
            this.fovManager = fovManager;
            this.fovList = fovList;
            this.selectedFovIndex = selectedFovIndex;
        }

        public void ShowFovProperties(Panel panelImage, FovRegion fov)
        {
            panelImage.Controls.Clear();
            if (fov == null) return;

            var root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                AutoSize = true,
                Padding = new Padding(5)
            };
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90F));
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

            AddReadOnlyRow(root, "ID", fov.Id.ToString());

            var txtName = new TextBox { Text = string.IsNullOrEmpty(fov.Name) ? $"FOV_{selectedFovIndex + 1:D3}" : fov.Name, Dock = DockStyle.Fill };
            txtName.TextChanged += (s, e) => { fov.Name = txtName.Text; SaveFov(); };
            AddControlRow(root, CreateLabel("Name"), txtName);

            var chkEnabled = new CheckBox { Checked = fov.IsEnabled, Dock = DockStyle.Left, Text = "" };
            chkEnabled.CheckedChanged += (s, e) => { fov.IsEnabled = chkEnabled.Checked; SaveFov(); };
            AddControlRow(root, CreateLabel("Is Enabled"), chkEnabled);

            var cboCam = new ComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList };
            AddControlRow(root, CreateLabel("Camera Mode"), cboCam);
            cboCam.Items.AddRange(new object[] { "Camera1", "Camera2" });
            cboCam.SelectedItem = string.IsNullOrEmpty(fov.CameraMode) ? "Camera1" : fov.CameraMode;
            cboCam.SelectedIndex = cboCam.SelectedIndex >= 0 ? cboCam.SelectedIndex : 0;
            cboCam.SelectedIndexChanged += (s, e) => { fov.CameraMode = cboCam.SelectedItem.ToString(); SaveFov(); };

            var txtExposure = new TextBox { Text = fov.ExposureTime.ToString(), Dock = DockStyle.Fill };
            txtExposure.TextChanged += (s, e) =>
            {
                if (int.TryParse(txtExposure.Text, out int v))
                {
                    if (v < 0) v = 0;
                    fov.ExposureTime = v;
                    SaveFov();
                }
            };
            AddControlRow(root, CreateLabel("Exposure Time"), txtExposure);

            var btnCapture = new Button { Text = "Capture", Dock = DockStyle.Top, Height = 28 };
            btnCapture.Click += (s, e) => { CaptureRequested?.Invoke(fov.CameraMode); };
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.Controls.Add(new Label { Text = "", AutoSize = true }, 0, root.RowCount);
            root.Controls.Add(btnCapture, 1, root.RowCount);
            root.RowCount++;

            panelImage.Controls.Add(root);
        }

        private void SaveFov()
        {
            if (selectedFovIndex >= 0 && selectedFovIndex < fovList.Count)
            {
                fovManager.Save(fovList);
            }
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
    }
}
