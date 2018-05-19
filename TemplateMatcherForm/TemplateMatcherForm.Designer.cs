namespace TemplateMatcherForm
{
    partial class TemplateMatcherForm
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
            this.templatesPathTextbox = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.loadTemplatesButton = new System.Windows.Forms.Button();
            this.reconstructionPicturebox = new System.Windows.Forms.PictureBox();
            this.label2 = new System.Windows.Forms.Label();
            this.findBestReconstructionButton = new System.Windows.Forms.Button();
            this.generateReconstructionButton = new System.Windows.Forms.Button();
            this.reconstructionDataGridview = new System.Windows.Forms.DataGridView();
            this.Template = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.XShift = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.YShift = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.label3 = new System.Windows.Forms.Label();
            this.inputImagePathTextbox = new System.Windows.Forms.TextBox();
            this.label4 = new System.Windows.Forms.Label();
            this.label5 = new System.Windows.Forms.Label();
            this.reconstructionErrorPicturebox = new System.Windows.Forms.PictureBox();
            this.reconstructionErrorTextbox = new System.Windows.Forms.TextBox();
            this.label6 = new System.Windows.Forms.Label();
            this.label7 = new System.Windows.Forms.Label();
            this.inputImageBox = new System.Windows.Forms.PictureBox();
            ((System.ComponentModel.ISupportInitialize)(this.reconstructionPicturebox)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.reconstructionDataGridview)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.reconstructionErrorPicturebox)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.inputImageBox)).BeginInit();
            this.SuspendLayout();
            // 
            // templatesPathTextbox
            // 
            this.templatesPathTextbox.Location = new System.Drawing.Point(12, 12);
            this.templatesPathTextbox.Name = "templatesPathTextbox";
            this.templatesPathTextbox.Size = new System.Drawing.Size(558, 20);
            this.templatesPathTextbox.TabIndex = 0;
            this.templatesPathTextbox.Text = "C:\\Emgu\\emgucv-windows-universal 3.0.0.2157\\Emgu.CV.Example\\vtc_eugene3\\bin\\templ" +
    "ates";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(587, 15);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(80, 13);
            this.label1.TabIndex = 1;
            this.label1.Text = "Templates path";
            // 
            // loadTemplatesButton
            // 
            this.loadTemplatesButton.Location = new System.Drawing.Point(737, 12);
            this.loadTemplatesButton.Name = "loadTemplatesButton";
            this.loadTemplatesButton.Size = new System.Drawing.Size(136, 23);
            this.loadTemplatesButton.TabIndex = 2;
            this.loadTemplatesButton.Text = "Load Templates";
            this.loadTemplatesButton.UseVisualStyleBackColor = true;
            this.loadTemplatesButton.Click += new System.EventHandler(this.loadTemplatesButton_Click);
            // 
            // reconstructionPicturebox
            // 
            this.reconstructionPicturebox.BackColor = System.Drawing.Color.Black;
            this.reconstructionPicturebox.Location = new System.Drawing.Point(9, 84);
            this.reconstructionPicturebox.Name = "reconstructionPicturebox";
            this.reconstructionPicturebox.Size = new System.Drawing.Size(320, 240);
            this.reconstructionPicturebox.TabIndex = 3;
            this.reconstructionPicturebox.TabStop = false;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(9, 68);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(106, 13);
            this.label2.TabIndex = 4;
            this.label2.Text = "Image reconstruction";
            // 
            // findBestReconstructionButton
            // 
            this.findBestReconstructionButton.Location = new System.Drawing.Point(737, 41);
            this.findBestReconstructionButton.Name = "findBestReconstructionButton";
            this.findBestReconstructionButton.Size = new System.Drawing.Size(136, 23);
            this.findBestReconstructionButton.TabIndex = 5;
            this.findBestReconstructionButton.Text = "Find best reconstruction";
            this.findBestReconstructionButton.UseVisualStyleBackColor = true;
            this.findBestReconstructionButton.Click += new System.EventHandler(this.findBestReconstructionButton_Click);
            // 
            // generateReconstructionButton
            // 
            this.generateReconstructionButton.Location = new System.Drawing.Point(737, 70);
            this.generateReconstructionButton.Name = "generateReconstructionButton";
            this.generateReconstructionButton.Size = new System.Drawing.Size(136, 23);
            this.generateReconstructionButton.TabIndex = 6;
            this.generateReconstructionButton.Text = "Generate reconstruction";
            this.generateReconstructionButton.UseVisualStyleBackColor = true;
            this.generateReconstructionButton.Click += new System.EventHandler(this.generateReconstructionButton_Click);
            // 
            // reconstructionDataGridview
            // 
            this.reconstructionDataGridview.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.reconstructionDataGridview.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.Template,
            this.XShift,
            this.YShift});
            this.reconstructionDataGridview.Location = new System.Drawing.Point(737, 125);
            this.reconstructionDataGridview.Name = "reconstructionDataGridview";
            this.reconstructionDataGridview.Size = new System.Drawing.Size(344, 328);
            this.reconstructionDataGridview.TabIndex = 7;
            // 
            // Template
            // 
            this.Template.HeaderText = "Template";
            this.Template.Name = "Template";
            // 
            // XShift
            // 
            this.XShift.HeaderText = "XShift";
            this.XShift.Name = "XShift";
            // 
            // YShift
            // 
            this.YShift.HeaderText = "YShift";
            this.YShift.Name = "YShift";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(734, 109);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(103, 13);
            this.label3.TabIndex = 8;
            this.label3.Text = "Reconstruction data";
            // 
            // inputImagePathTextbox
            // 
            this.inputImagePathTextbox.Location = new System.Drawing.Point(12, 37);
            this.inputImagePathTextbox.Name = "inputImagePathTextbox";
            this.inputImagePathTextbox.Size = new System.Drawing.Size(558, 20);
            this.inputImagePathTextbox.TabIndex = 9;
            this.inputImagePathTextbox.Text = "C:\\Emgu\\emgucv-windows-universal 3.0.0.2157\\Emgu.CV.Example\\vtc_eugene3\\bin\\templ" +
    "ateInput";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(587, 40);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(86, 13);
            this.label4.TabIndex = 10;
            this.label4.Text = "Input image path";
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(6, 337);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(130, 13);
            this.label5.TabIndex = 12;
            this.label5.Text = "Image reconstruction error";
            // 
            // reconstructionErrorPicturebox
            // 
            this.reconstructionErrorPicturebox.BackColor = System.Drawing.Color.Black;
            this.reconstructionErrorPicturebox.Location = new System.Drawing.Point(9, 353);
            this.reconstructionErrorPicturebox.Name = "reconstructionErrorPicturebox";
            this.reconstructionErrorPicturebox.Size = new System.Drawing.Size(320, 240);
            this.reconstructionErrorPicturebox.TabIndex = 11;
            this.reconstructionErrorPicturebox.TabStop = false;
            // 
            // reconstructionErrorTextbox
            // 
            this.reconstructionErrorTextbox.Location = new System.Drawing.Point(123, 612);
            this.reconstructionErrorTextbox.Name = "reconstructionErrorTextbox";
            this.reconstructionErrorTextbox.Size = new System.Drawing.Size(206, 20);
            this.reconstructionErrorTextbox.TabIndex = 13;
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(204, 596);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(125, 13);
            this.label6.TabIndex = 14;
            this.label6.Text = "Reconstruction error sum";
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(353, 68);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(62, 13);
            this.label7.TabIndex = 16;
            this.label7.Text = "Input image";
            // 
            // inputImageBox
            // 
            this.inputImageBox.BackColor = System.Drawing.Color.Black;
            this.inputImageBox.Location = new System.Drawing.Point(353, 84);
            this.inputImageBox.Name = "inputImageBox";
            this.inputImageBox.Size = new System.Drawing.Size(320, 240);
            this.inputImageBox.TabIndex = 15;
            this.inputImageBox.TabStop = false;
            // 
            // TemplateMatcherForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1136, 642);
            this.Controls.Add(this.label7);
            this.Controls.Add(this.inputImageBox);
            this.Controls.Add(this.label6);
            this.Controls.Add(this.reconstructionErrorTextbox);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.reconstructionErrorPicturebox);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.inputImagePathTextbox);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.reconstructionDataGridview);
            this.Controls.Add(this.generateReconstructionButton);
            this.Controls.Add(this.findBestReconstructionButton);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.reconstructionPicturebox);
            this.Controls.Add(this.loadTemplatesButton);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.templatesPathTextbox);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Name = "TemplateMatcherForm";
            this.Text = "Template Matching";
            ((System.ComponentModel.ISupportInitialize)(this.reconstructionPicturebox)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.reconstructionDataGridview)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.reconstructionErrorPicturebox)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.inputImageBox)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox templatesPathTextbox;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Button loadTemplatesButton;
        private System.Windows.Forms.PictureBox reconstructionPicturebox;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Button findBestReconstructionButton;
        private System.Windows.Forms.Button generateReconstructionButton;
        private System.Windows.Forms.DataGridView reconstructionDataGridview;
        private System.Windows.Forms.DataGridViewTextBoxColumn Template;
        private System.Windows.Forms.DataGridViewTextBoxColumn XShift;
        private System.Windows.Forms.DataGridViewTextBoxColumn YShift;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.TextBox inputImagePathTextbox;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.PictureBox reconstructionErrorPicturebox;
        private System.Windows.Forms.TextBox reconstructionErrorTextbox;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.PictureBox inputImageBox;
    }
}

