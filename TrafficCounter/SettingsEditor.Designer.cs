﻿namespace VTC
{
    partial class SettingsEditor
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
            this.settingsPropertyGrid = new System.Windows.Forms.PropertyGrid();
            this.saveButton = new System.Windows.Forms.Button();
            this.resetToDefaultButton = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // settingsPropertyGrid
            // 
            this.settingsPropertyGrid.Location = new System.Drawing.Point(12, 12);
            this.settingsPropertyGrid.Name = "settingsPropertyGrid";
            this.settingsPropertyGrid.Size = new System.Drawing.Size(495, 426);
            this.settingsPropertyGrid.TabIndex = 0;
            // 
            // saveButton
            // 
            this.saveButton.Location = new System.Drawing.Point(513, 191);
            this.saveButton.Name = "saveButton";
            this.saveButton.Size = new System.Drawing.Size(123, 23);
            this.saveButton.TabIndex = 1;
            this.saveButton.Text = "Save";
            this.saveButton.UseVisualStyleBackColor = true;
            this.saveButton.Click += new System.EventHandler(this.saveButton_Click);
            // 
            // resetToDefaultButton
            // 
            this.resetToDefaultButton.Location = new System.Drawing.Point(513, 220);
            this.resetToDefaultButton.Name = "resetToDefaultButton";
            this.resetToDefaultButton.Size = new System.Drawing.Size(123, 23);
            this.resetToDefaultButton.TabIndex = 2;
            this.resetToDefaultButton.Text = "Reset to default";
            this.resetToDefaultButton.UseVisualStyleBackColor = true;
            this.resetToDefaultButton.Click += new System.EventHandler(this.resetToDefaultButton_Click);
            // 
            // SettingsEditor
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(644, 450);
            this.Controls.Add(this.resetToDefaultButton);
            this.Controls.Add(this.saveButton);
            this.Controls.Add(this.settingsPropertyGrid);
            this.Name = "SettingsEditor";
            this.ShowIcon = false;
            this.Text = "Settings";
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.PropertyGrid settingsPropertyGrid;
        private System.Windows.Forms.Button saveButton;
        private System.Windows.Forms.Button resetToDefaultButton;
    }
}