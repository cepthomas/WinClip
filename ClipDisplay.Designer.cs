
namespace ClipboardEx
{
    partial class ClipDisplay
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

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.rtbText = new System.Windows.Forms.RichTextBox();
            this.picImage = new System.Windows.Forms.PictureBox();
            ((System.ComponentModel.ISupportInitialize)(this.picImage)).BeginInit();
            this.SuspendLayout();
            // 
            // rtbText
            // 
            this.rtbText.BackColor = System.Drawing.SystemColors.Control;
            this.rtbText.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.rtbText.Location = new System.Drawing.Point(17, 27);
            this.rtbText.Name = "rtbText";
            this.rtbText.ReadOnly = true;
            this.rtbText.Size = new System.Drawing.Size(38, 30);
            this.rtbText.TabIndex = 0;
            this.rtbText.Text = "";
            // 
            // picImage
            // 
            this.picImage.Location = new System.Drawing.Point(102, 27);
            this.picImage.Name = "picImage";
            this.picImage.Size = new System.Drawing.Size(43, 31);
            this.picImage.TabIndex = 1;
            this.picImage.TabStop = false;
            // 
            // ClipDisplay
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.Controls.Add(this.picImage);
            this.Controls.Add(this.rtbText);
            this.Name = "ClipDisplay";
            this.Size = new System.Drawing.Size(255, 87);
            ((System.ComponentModel.ISupportInitialize)(this.picImage)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.RichTextBox rtbText;
        private System.Windows.Forms.PictureBox picImage;
    }
}
