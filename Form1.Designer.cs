namespace MPV
{
    partial class Form1
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.pictureBox1 = new System.Windows.Forms.PictureBox();
            this.pn_property = new System.Windows.Forms.TreeView();
            this.contextMenuTree = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.btnAddRoi = new System.Windows.Forms.Button();
            this.menuStrip1 = new System.Windows.Forms.MenuStrip();
            this.fileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.openToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.saveToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.runToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.ảutoRunToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.exitToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            this.splitContainer2 = new System.Windows.Forms.SplitContainer();
            this.pn_left = new System.Windows.Forms.Panel();
            this.grpTemplate = new System.Windows.Forms.GroupBox();
            this.txtCenterY = new System.Windows.Forms.TextBox();
            this.txtCenterX = new System.Windows.Forms.TextBox();
            this.lblCenter = new System.Windows.Forms.Label();
            this.txtLastScore = new System.Windows.Forms.TextBox();
            this.lblScore = new System.Windows.Forms.Label();
            this.chkReverse = new System.Windows.Forms.CheckBox();
            this.txtOkUpper = new System.Windows.Forms.TextBox();
            this.txtOkLower = new System.Windows.Forms.TextBox();
            this.lblKRange = new System.Windows.Forms.Label();
            this.ptr_template = new System.Windows.Forms.PictureBox();
            this.btnUpdateTemplate = new System.Windows.Forms.Button();
            this.panelScroll = new System.Windows.Forms.Panel();
            this.splitContainer3 = new System.Windows.Forms.SplitContainer();
            this.btn_delete = new System.Windows.Forms.Button();
            this.btn_addfov = new System.Windows.Forms.Button();
            this.panelImage = new System.Windows.Forms.Panel();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
            this.menuStrip1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).BeginInit();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer2)).BeginInit();
            this.splitContainer2.Panel1.SuspendLayout();
            this.splitContainer2.Panel2.SuspendLayout();
            this.splitContainer2.SuspendLayout();
            this.pn_left.SuspendLayout();
            this.grpTemplate.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.ptr_template)).BeginInit();
            this.panelScroll.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer3)).BeginInit();
            this.splitContainer3.Panel1.SuspendLayout();
            this.splitContainer3.Panel2.SuspendLayout();
            this.splitContainer3.SuspendLayout();
            this.SuspendLayout();
            // 
            // pictureBox1
            // 
            this.pictureBox1.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.pictureBox1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pictureBox1.Location = new System.Drawing.Point(0, 0);
            this.pictureBox1.Name = "pictureBox1";
            this.pictureBox1.Size = new System.Drawing.Size(552, 493);
            this.pictureBox1.TabIndex = 0;
            this.pictureBox1.TabStop = false;
            this.pictureBox1.Paint += new System.Windows.Forms.PaintEventHandler(this.pictureBox1_Paint);
            this.pictureBox1.MouseDown += new System.Windows.Forms.MouseEventHandler(this.pictureBox1_MouseDown);
            this.pictureBox1.MouseMove += new System.Windows.Forms.MouseEventHandler(this.pictureBox1_MouseMove);
            this.pictureBox1.MouseUp += new System.Windows.Forms.MouseEventHandler(this.pictureBox1_MouseUp);
            // 
            // pn_property
            // 
            this.pn_property.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pn_property.Location = new System.Drawing.Point(0, 0);
            this.pn_property.Name = "pn_property";
            this.pn_property.Size = new System.Drawing.Size(199, 230);
            this.pn_property.TabIndex = 5;
            this.pn_property.AfterSelect += new System.Windows.Forms.TreeViewEventHandler(this.trv1_AfterSelect);
            // 
            // contextMenuTree
            // 
            this.contextMenuTree.Name = "contextMenuStrip1";
            this.contextMenuTree.Size = new System.Drawing.Size(61, 4);
            // 
            // btnAddRoi
            // 
            this.btnAddRoi.Anchor = System.Windows.Forms.AnchorStyles.Bottom;
            this.btnAddRoi.Location = new System.Drawing.Point(58, 204);
            this.btnAddRoi.Name = "btnAddRoi";
            this.btnAddRoi.Size = new System.Drawing.Size(75, 23);
            this.btnAddRoi.TabIndex = 9;
            this.btnAddRoi.Text = "MOV";
            this.btnAddRoi.UseVisualStyleBackColor = true;
            this.btnAddRoi.Click += new System.EventHandler(this.btnAddRoi_Click_1);
            // 
            // menuStrip1
            // 
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.runToolStripMenuItem,
            this.exitToolStripMenuItem,
            this.fileToolStripMenuItem});
            this.menuStrip1.Location = new System.Drawing.Point(0, 0);
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.Size = new System.Drawing.Size(949, 24);
            this.menuStrip1.TabIndex = 14;
            this.menuStrip1.Text = "menuStrip1";
            // 
            // fileToolStripMenuItem
            // 
            this.fileToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.openToolStripMenuItem,
            this.saveToolStripMenuItem});
            this.fileToolStripMenuItem.Name = "fileToolStripMenuItem";
            this.fileToolStripMenuItem.Size = new System.Drawing.Size(37, 20);
            this.fileToolStripMenuItem.Text = "File";
            // 
            // openToolStripMenuItem
            // 
            this.openToolStripMenuItem.Name = "openToolStripMenuItem";
            this.openToolStripMenuItem.Size = new System.Drawing.Size(103, 22);
            this.openToolStripMenuItem.Text = "Open";
            // 
            // saveToolStripMenuItem
            // 
            this.saveToolStripMenuItem.Name = "saveToolStripMenuItem";
            this.saveToolStripMenuItem.Size = new System.Drawing.Size(103, 22);
            this.saveToolStripMenuItem.Text = "Save";
            this.saveToolStripMenuItem.Click += new System.EventHandler(this.saveToolStripMenuItem_Click);
            // 
            // runToolStripMenuItem
            // 
            this.runToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.ảutoRunToolStripMenuItem});
            this.runToolStripMenuItem.Name = "runToolStripMenuItem";
            this.runToolStripMenuItem.Size = new System.Drawing.Size(40, 20);
            this.runToolStripMenuItem.Text = "Run";
            // 
            // ảutoRunToolStripMenuItem
            // 
            this.ảutoRunToolStripMenuItem.Name = "ảutoRunToolStripMenuItem";
            this.ảutoRunToolStripMenuItem.Size = new System.Drawing.Size(124, 22);
            this.ảutoRunToolStripMenuItem.Text = "Ảuto Run";
            this.ảutoRunToolStripMenuItem.Click += new System.EventHandler(this.ảutoRunToolStripMenuItem_Click);
            // 
            // exitToolStripMenuItem
            // 
            this.exitToolStripMenuItem.Name = "exitToolStripMenuItem";
            this.exitToolStripMenuItem.Size = new System.Drawing.Size(38, 20);
            this.exitToolStripMenuItem.Text = "Exit";
            this.exitToolStripMenuItem.Click += new System.EventHandler(this.exitToolStripMenuItem_Click);
            // 
            // splitContainer1
            // 
            this.splitContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer1.IsSplitterFixed = true;
            this.splitContainer1.Location = new System.Drawing.Point(0, 24);
            this.splitContainer1.Name = "splitContainer1";
            // 
            // splitContainer1.Panel1
            // 
            this.splitContainer1.Panel1.Controls.Add(this.splitContainer2);
            // 
            // splitContainer1.Panel2
            // 
            this.splitContainer1.Panel2.Controls.Add(this.splitContainer3);
            this.splitContainer1.Size = new System.Drawing.Size(949, 493);
            this.splitContainer1.SplitterDistance = 746;
            this.splitContainer1.TabIndex = 15;
            // 
            // splitContainer2
            // 
            this.splitContainer2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer2.FixedPanel = System.Windows.Forms.FixedPanel.Panel1;
            this.splitContainer2.Location = new System.Drawing.Point(0, 0);
            this.splitContainer2.Name = "splitContainer2";
            // 
            // splitContainer2.Panel1
            // 
            this.splitContainer2.Panel1.Controls.Add(this.pn_left);
            this.splitContainer2.Panel1MinSize = 192;
            // 
            // splitContainer2.Panel2
            // 
            this.splitContainer2.Panel2.Controls.Add(this.panelScroll);
            this.splitContainer2.Size = new System.Drawing.Size(746, 493);
            this.splitContainer2.SplitterDistance = 192;
            this.splitContainer2.SplitterWidth = 2;
            this.splitContainer2.TabIndex = 0;
            // 
            // pn_left
            // 
            this.pn_left.Controls.Add(this.grpTemplate);
            this.pn_left.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pn_left.Location = new System.Drawing.Point(0, 0);
            this.pn_left.Name = "pn_left";
            this.pn_left.Size = new System.Drawing.Size(192, 493);
            this.pn_left.TabIndex = 0;
            // 
            // grpTemplate
            // 
            this.grpTemplate.Controls.Add(this.txtCenterY);
            this.grpTemplate.Controls.Add(this.txtCenterX);
            this.grpTemplate.Controls.Add(this.lblCenter);
            this.grpTemplate.Controls.Add(this.txtLastScore);
            this.grpTemplate.Controls.Add(this.lblScore);
            this.grpTemplate.Controls.Add(this.chkReverse);
            this.grpTemplate.Controls.Add(this.txtOkUpper);
            this.grpTemplate.Controls.Add(this.txtOkLower);
            this.grpTemplate.Controls.Add(this.lblKRange);
            this.grpTemplate.Controls.Add(this.ptr_template);
            this.grpTemplate.Controls.Add(this.btnUpdateTemplate);
            this.grpTemplate.Location = new System.Drawing.Point(0, 0);
            this.grpTemplate.Name = "grpTemplate";
            this.grpTemplate.Size = new System.Drawing.Size(197, 406);
            this.grpTemplate.TabIndex = 0;
            this.grpTemplate.TabStop = false;
            this.grpTemplate.Text = "Template";
            // 
            // txtCenterY
            // 
            this.txtCenterY.Location = new System.Drawing.Point(84, 316);
            this.txtCenterY.Name = "txtCenterY";
            this.txtCenterY.ReadOnly = true;
            this.txtCenterY.Size = new System.Drawing.Size(60, 20);
            this.txtCenterY.TabIndex = 5;
            // 
            // txtCenterX
            // 
            this.txtCenterX.Location = new System.Drawing.Point(12, 316);
            this.txtCenterX.Name = "txtCenterX";
            this.txtCenterX.ReadOnly = true;
            this.txtCenterX.Size = new System.Drawing.Size(60, 20);
            this.txtCenterX.TabIndex = 4;
            // 
            // lblCenter
            // 
            this.lblCenter.AutoSize = true;
            this.lblCenter.Location = new System.Drawing.Point(9, 300);
            this.lblCenter.Name = "lblCenter";
            this.lblCenter.Size = new System.Drawing.Size(60, 13);
            this.lblCenter.TabIndex = 9;
            this.lblCenter.Text = "Center X/Y";
            // 
            // txtLastScore
            // 
            this.txtLastScore.Location = new System.Drawing.Point(60, 264);
            this.txtLastScore.Name = "txtLastScore";
            this.txtLastScore.ReadOnly = true;
            this.txtLastScore.Size = new System.Drawing.Size(60, 20);
            this.txtLastScore.TabIndex = 8;
            // 
            // lblScore
            // 
            this.lblScore.AutoSize = true;
            this.lblScore.Location = new System.Drawing.Point(9, 267);
            this.lblScore.Name = "lblScore";
            this.lblScore.Size = new System.Drawing.Size(38, 13);
            this.lblScore.TabIndex = 7;
            this.lblScore.Text = "Score:";
            // 
            // chkReverse
            // 
            this.chkReverse.AutoSize = true;
            this.chkReverse.Location = new System.Drawing.Point(12, 235);
            this.chkReverse.Name = "chkReverse";
            this.chkReverse.Size = new System.Drawing.Size(103, 17);
            this.chkReverse.TabIndex = 3;
            this.chkReverse.Text = "Reverse Search";
            this.chkReverse.UseVisualStyleBackColor = true;
            this.chkReverse.CheckedChanged += new System.EventHandler(this.chkReverse_CheckedChanged);
            // 
            // txtOkUpper
            // 
            this.txtOkUpper.Location = new System.Drawing.Point(84, 209);
            this.txtOkUpper.Name = "txtOkUpper";
            this.txtOkUpper.Size = new System.Drawing.Size(60, 20);
            this.txtOkUpper.TabIndex = 2;
            this.txtOkUpper.TextChanged += new System.EventHandler(this.txtOkUpper_TextChanged);
            // 
            // txtOkLower
            // 
            this.txtOkLower.Location = new System.Drawing.Point(12, 209);
            this.txtOkLower.Name = "txtOkLower";
            this.txtOkLower.Size = new System.Drawing.Size(60, 20);
            this.txtOkLower.TabIndex = 1;
            this.txtOkLower.TextChanged += new System.EventHandler(this.txtOkLower_TextChanged);
            // 
            // lblKRange
            // 
            this.lblKRange.AutoSize = true;
            this.lblKRange.Location = new System.Drawing.Point(9, 193);
            this.lblKRange.Name = "lblKRange";
            this.lblKRange.Size = new System.Drawing.Size(71, 13);
            this.lblKRange.TabIndex = 11;
            this.lblKRange.Text = "K Range L/U";
            // 
            // ptr_template
            // 
            this.ptr_template.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.ptr_template.Location = new System.Drawing.Point(12, 19);
            this.ptr_template.Name = "ptr_template";
            this.ptr_template.Size = new System.Drawing.Size(168, 160);
            this.ptr_template.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.ptr_template.TabIndex = 10;
            this.ptr_template.TabStop = false;
            // 
            // btnUpdateTemplate
            // 
            this.btnUpdateTemplate.Location = new System.Drawing.Point(12, 337);
            this.btnUpdateTemplate.Name = "btnUpdateTemplate";
            this.btnUpdateTemplate.Size = new System.Drawing.Size(168, 23);
            this.btnUpdateTemplate.TabIndex = 9;
            this.btnUpdateTemplate.Text = "Update Template";
            this.btnUpdateTemplate.UseVisualStyleBackColor = true;
            this.btnUpdateTemplate.Click += new System.EventHandler(this.btnUpdateTemplate_Click);
            // 
            // panelScroll
            // 
            this.panelScroll.AutoScroll = true;
            this.panelScroll.Controls.Add(this.pictureBox1);
            this.panelScroll.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panelScroll.Location = new System.Drawing.Point(0, 0);
            this.panelScroll.Name = "panelScroll";
            this.panelScroll.Size = new System.Drawing.Size(552, 493);
            this.panelScroll.TabIndex = 200;
            // 
            // splitContainer3
            // 
            this.splitContainer3.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer3.Location = new System.Drawing.Point(0, 0);
            this.splitContainer3.Name = "splitContainer3";
            this.splitContainer3.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // splitContainer3.Panel1
            // 
            this.splitContainer3.Panel1.Controls.Add(this.btn_delete);
            this.splitContainer3.Panel1.Controls.Add(this.btnAddRoi);
            this.splitContainer3.Panel1.Controls.Add(this.btn_addfov);
            this.splitContainer3.Panel1.Controls.Add(this.pn_property);
            // 
            // splitContainer3.Panel2
            // 
            this.splitContainer3.Panel2.Controls.Add(this.panelImage);
            this.splitContainer3.Size = new System.Drawing.Size(199, 493);
            this.splitContainer3.SplitterDistance = 230;
            this.splitContainer3.TabIndex = 0;
            // 
            // btn_delete
            // 
            this.btn_delete.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btn_delete.Location = new System.Drawing.Point(121, 204);
            this.btn_delete.Name = "btn_delete";
            this.btn_delete.Size = new System.Drawing.Size(75, 23);
            this.btn_delete.TabIndex = 10;
            this.btn_delete.Text = "Delete";
            this.btn_delete.UseVisualStyleBackColor = true;
            this.btn_delete.Click += new System.EventHandler(this.btn_delete_Click);
            // 
            // btn_addfov
            // 
            this.btn_addfov.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.btn_addfov.Location = new System.Drawing.Point(0, 204);
            this.btn_addfov.Name = "btn_addfov";
            this.btn_addfov.Size = new System.Drawing.Size(75, 23);
            this.btn_addfov.TabIndex = 1;
            this.btn_addfov.Text = "FOV";
            this.btn_addfov.UseVisualStyleBackColor = true;
            this.btn_addfov.Click += new System.EventHandler(this.btn_addfov_Click);
            // 
            // panelImage
            // 
            this.panelImage.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.panelImage.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panelImage.Location = new System.Drawing.Point(0, 0);
            this.panelImage.Name = "panelImage";
            this.panelImage.Size = new System.Drawing.Size(199, 259);
            this.panelImage.TabIndex = 100;
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(949, 517);
            this.Controls.Add(this.splitContainer1);
            this.Controls.Add(this.menuStrip1);
            this.MainMenuStrip = this.menuStrip1;
            this.Name = "Form1";
            this.Text = " ";
            this.Load += new System.EventHandler(this.Form1_Load);
            this.Shown += new System.EventHandler(this.Form1_Shown);
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            this.splitContainer1.Panel1.ResumeLayout(false);
            this.splitContainer1.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).EndInit();
            this.splitContainer1.ResumeLayout(false);
            this.splitContainer2.Panel1.ResumeLayout(false);
            this.splitContainer2.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer2)).EndInit();
            this.splitContainer2.ResumeLayout(false);
            this.pn_left.ResumeLayout(false);
            this.grpTemplate.ResumeLayout(false);
            this.grpTemplate.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.ptr_template)).EndInit();
            this.panelScroll.ResumeLayout(false);
            this.splitContainer3.Panel1.ResumeLayout(false);
            this.splitContainer3.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer3)).EndInit();
            this.splitContainer3.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.PictureBox pictureBox1;
        private System.Windows.Forms.TreeView pn_property;
        private System.Windows.Forms.ContextMenuStrip contextMenuTree;
        private System.Windows.Forms.Button btnAddRoi;
        private System.Windows.Forms.MenuStrip menuStrip1;
        private System.Windows.Forms.ToolStripMenuItem runToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem ảutoRunToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem exitToolStripMenuItem;
        private System.Windows.Forms.SplitContainer splitContainer1;
        private System.Windows.Forms.SplitContainer splitContainer2;
        private System.Windows.Forms.Panel pn_left;
        private System.Windows.Forms.SplitContainer splitContainer3;
        private System.Windows.Forms.Button btn_addfov;
        private System.Windows.Forms.Button btn_delete;
        private System.Windows.Forms.GroupBox grpTemplate;
        private System.Windows.Forms.Button btnUpdateTemplate;
        private System.Windows.Forms.PictureBox ptr_template;
        private System.Windows.Forms.Label lblKRange;
        private System.Windows.Forms.TextBox txtOkLower;
        private System.Windows.Forms.TextBox txtOkUpper;
        private System.Windows.Forms.CheckBox chkReverse;
        private System.Windows.Forms.Label lblScore;
        private System.Windows.Forms.TextBox txtLastScore;
        private System.Windows.Forms.Label lblCenter;
        private System.Windows.Forms.TextBox txtCenterX;
        private System.Windows.Forms.TextBox txtCenterY;
        private System.Windows.Forms.Panel panelImage;
        private System.Windows.Forms.Panel panelScroll;
        private System.Windows.Forms.ToolStripMenuItem fileToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem openToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem saveToolStripMenuItem;
    }
}

