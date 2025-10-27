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
            this.txt1 = new System.Windows.Forms.TextBox();
            this.btnrun = new System.Windows.Forms.Button();
            this.trv1 = new System.Windows.Forms.TreeView();
            this.contextMenuTree = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.menuDelete = new System.Windows.Forms.ToolStripMenuItem();
            this.btn_Exit = new System.Windows.Forms.Button();
            this.btn_Update = new System.Windows.Forms.Button();
            this.ptrb2 = new System.Windows.Forms.PictureBox();
            this.btnDrawRoi = new System.Windows.Forms.Button();
            this.cb_hide = new System.Windows.Forms.CheckBox();
            this.btnAddFov = new System.Windows.Forms.Button();
            this.cbb_tt = new System.Windows.Forms.ComboBox();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
            this.contextMenuTree.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.ptrb2)).BeginInit();
            this.SuspendLayout();
            // 
            // pictureBox1
            // 
            this.pictureBox1.Location = new System.Drawing.Point(29, 22);
            this.pictureBox1.Name = "pictureBox1";
            this.pictureBox1.Size = new System.Drawing.Size(519, 285);
            this.pictureBox1.TabIndex = 0;
            this.pictureBox1.TabStop = false;
            this.pictureBox1.Paint += new System.Windows.Forms.PaintEventHandler(this.pictureBox1_Paint);
            this.pictureBox1.MouseDown += new System.Windows.Forms.MouseEventHandler(this.pictureBox1_MouseDown);
            this.pictureBox1.MouseMove += new System.Windows.Forms.MouseEventHandler(this.pictureBox1_MouseMove);
            this.pictureBox1.MouseUp += new System.Windows.Forms.MouseEventHandler(this.pictureBox1_MouseUp);
            // 
            // txt1
            // 
            this.txt1.Location = new System.Drawing.Point(12, 347);
            this.txt1.Name = "txt1";
            this.txt1.Size = new System.Drawing.Size(100, 20);
            this.txt1.TabIndex = 3;
            // 
            // btnrun
            // 
            this.btnrun.Location = new System.Drawing.Point(564, 22);
            this.btnrun.Name = "btnrun";
            this.btnrun.Size = new System.Drawing.Size(75, 23);
            this.btnrun.TabIndex = 4;
            this.btnrun.Text = "Run";
            this.btnrun.UseVisualStyleBackColor = true;
            this.btnrun.Click += new System.EventHandler(this.btnrun_Click_1);
            // 
            // trv1
            // 
            this.trv1.Location = new System.Drawing.Point(601, 170);
            this.trv1.Name = "trv1";
            this.trv1.Size = new System.Drawing.Size(165, 238);
            this.trv1.TabIndex = 5;
            this.trv1.AfterSelect += new System.Windows.Forms.TreeViewEventHandler(this.trv1_AfterSelect);
            // 
            // contextMenuTree
            // 
            this.contextMenuTree.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.menuDelete});
            this.contextMenuTree.Name = "contextMenuStrip1";
            this.contextMenuTree.Size = new System.Drawing.Size(108, 26);
            // 
            // menuDelete
            // 
            this.menuDelete.Name = "menuDelete";
            this.menuDelete.Size = new System.Drawing.Size(107, 22);
            this.menuDelete.Text = "Delete";
            this.menuDelete.Click += new System.EventHandler(this.MenuDelete_Click);
            // 
            // btn_Exit
            // 
            this.btn_Exit.Location = new System.Drawing.Point(730, 414);
            this.btn_Exit.Name = "btn_Exit";
            this.btn_Exit.Size = new System.Drawing.Size(75, 23);
            this.btn_Exit.TabIndex = 6;
            this.btn_Exit.Text = "Exit";
            this.btn_Exit.UseVisualStyleBackColor = true;
            this.btn_Exit.Click += new System.EventHandler(this.btn_Exit_Click);
            // 
            // btn_Update
            // 
            this.btn_Update.Location = new System.Drawing.Point(705, 22);
            this.btn_Update.Name = "btn_Update";
            this.btn_Update.Size = new System.Drawing.Size(75, 23);
            this.btn_Update.TabIndex = 7;
            this.btn_Update.Text = "Update Roi";
            this.btn_Update.UseVisualStyleBackColor = true;
            this.btn_Update.Click += new System.EventHandler(this.btn_Update_Click);
            // 
            // ptrb2
            // 
            this.ptrb2.Location = new System.Drawing.Point(228, 334);
            this.ptrb2.Name = "ptrb2";
            this.ptrb2.Size = new System.Drawing.Size(242, 103);
            this.ptrb2.TabIndex = 8;
            this.ptrb2.TabStop = false;
            // 
            // btnDrawRoi
            // 
            this.btnDrawRoi.Location = new System.Drawing.Point(564, 70);
            this.btnDrawRoi.Name = "btnDrawRoi";
            this.btnDrawRoi.Size = new System.Drawing.Size(75, 23);
            this.btnDrawRoi.TabIndex = 9;
            this.btnDrawRoi.Text = "Vẽ Roi";
            this.btnDrawRoi.UseVisualStyleBackColor = true;
            this.btnDrawRoi.Click += new System.EventHandler(this.btnDrawRoi_Click);
            // 
            // cb_hide
            // 
            this.cb_hide.AutoSize = true;
            this.cb_hide.Location = new System.Drawing.Point(772, 170);
            this.cb_hide.Name = "cb_hide";
            this.cb_hide.Size = new System.Drawing.Size(48, 17);
            this.cb_hide.TabIndex = 10;
            this.cb_hide.Text = "Hide";
            this.cb_hide.UseVisualStyleBackColor = true;
            this.cb_hide.CheckedChanged += new System.EventHandler(this.cb_hide_CheckedChanged);
            // 
            // btnAddFov
            // 
            this.btnAddFov.Location = new System.Drawing.Point(705, 70);
            this.btnAddFov.Name = "btnAddFov";
            this.btnAddFov.Size = new System.Drawing.Size(75, 23);
            this.btnAddFov.TabIndex = 11;
            this.btnAddFov.Text = "Add FOV";
            this.btnAddFov.UseVisualStyleBackColor = true;
            this.btnAddFov.Click += new System.EventHandler(this.btnAddFov_Click_1);
            // 
            // cbb_tt
            // 
            this.cbb_tt.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbb_tt.FormattingEnabled = true;
            this.cbb_tt.Location = new System.Drawing.Point(564, 115);
            this.cbb_tt.Name = "cbb_tt";
            this.cbb_tt.Size = new System.Drawing.Size(121, 21);
            this.cbb_tt.TabIndex = 12;
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(832, 450);
            this.Controls.Add(this.cbb_tt);
            this.Controls.Add(this.btnAddFov);
            this.Controls.Add(this.cb_hide);
            this.Controls.Add(this.btnDrawRoi);
            this.Controls.Add(this.ptrb2);
            this.Controls.Add(this.btn_Update);
            this.Controls.Add(this.btn_Exit);
            this.Controls.Add(this.trv1);
            this.Controls.Add(this.btnrun);
            this.Controls.Add(this.txt1);
            this.Controls.Add(this.pictureBox1);
            this.Name = "Form1";
            this.Text = "Form1";
            this.Load += new System.EventHandler(this.Form1_Load);
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
            this.contextMenuTree.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.ptrb2)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.PictureBox pictureBox1;
        private System.Windows.Forms.TextBox txt1;
        private System.Windows.Forms.Button btnrun;
        private System.Windows.Forms.TreeView trv1;
        private System.Windows.Forms.ContextMenuStrip contextMenuTree;
        private System.Windows.Forms.ToolStripMenuItem menuDelete;
        private System.Windows.Forms.Button btn_Exit;
        private System.Windows.Forms.Button btn_Update;
        private System.Windows.Forms.PictureBox ptrb2;
        private System.Windows.Forms.Button btnDrawRoi;
        private System.Windows.Forms.CheckBox cb_hide;
        private System.Windows.Forms.Button btnAddFov;
        private System.Windows.Forms.ComboBox cbb_tt;
    }
}

