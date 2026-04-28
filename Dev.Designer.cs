namespace WinClip
{
    partial class Dev
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
            lblCtrl = new System.Windows.Forms.Label();
            lblAlt = new System.Windows.Forms.Label();
            label2 = new System.Windows.Forms.Label();
            label1 = new System.Windows.Forms.Label();
            txtPreviousWin = new System.Windows.Forms.TextBox();
            txtCurrentWin = new System.Windows.Forms.TextBox();
            lblLetter = new System.Windows.Forms.Label();
            lblWin = new System.Windows.Forms.Label();
            btnClear = new System.Windows.Forms.Button();
            btnDebug = new System.Windows.Forms.Button();
            tvInfo = new Ephemera.NBagOfUis.TextViewer();
            SuspendLayout();
            // 
            // lblCtrl
            // 
            lblCtrl.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            lblCtrl.Location = new System.Drawing.Point(54, 101);
            lblCtrl.Name = "lblCtrl";
            lblCtrl.Size = new System.Drawing.Size(25, 25);
            lblCtrl.TabIndex = 26;
            lblCtrl.Text = "C";
            // 
            // lblAlt
            // 
            lblAlt.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            lblAlt.Location = new System.Drawing.Point(85, 101);
            lblAlt.Name = "lblAlt";
            lblAlt.Size = new System.Drawing.Size(25, 25);
            lblAlt.TabIndex = 25;
            lblAlt.Text = "A";
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new System.Drawing.Point(308, 35);
            label2.Name = "label2";
            label2.Size = new System.Drawing.Size(61, 19);
            label2.TabIndex = 24;
            label2.Text = "Previous";
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new System.Drawing.Point(63, 37);
            label1.Name = "label1";
            label1.Size = new System.Drawing.Size(56, 19);
            label1.TabIndex = 23;
            label1.Text = "Current";
            // 
            // txtPreviousWin
            // 
            txtPreviousWin.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            txtPreviousWin.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            txtPreviousWin.Location = new System.Drawing.Point(375, 33);
            txtPreviousWin.Name = "txtPreviousWin";
            txtPreviousWin.ReadOnly = true;
            txtPreviousWin.Size = new System.Drawing.Size(145, 26);
            txtPreviousWin.TabIndex = 22;
            // 
            // txtCurrentWin
            // 
            txtCurrentWin.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            txtCurrentWin.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            txtCurrentWin.Location = new System.Drawing.Point(129, 33);
            txtCurrentWin.Name = "txtCurrentWin";
            txtCurrentWin.ReadOnly = true;
            txtCurrentWin.Size = new System.Drawing.Size(145, 26);
            txtCurrentWin.TabIndex = 21;
            // 
            // lblLetter
            // 
            lblLetter.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            lblLetter.Location = new System.Drawing.Point(147, 101);
            lblLetter.Name = "lblLetter";
            lblLetter.Size = new System.Drawing.Size(25, 25);
            lblLetter.TabIndex = 20;
            lblLetter.Text = "?";
            // 
            // lblWin
            // 
            lblWin.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            lblWin.Location = new System.Drawing.Point(116, 101);
            lblWin.Name = "lblWin";
            lblWin.Size = new System.Drawing.Size(25, 25);
            lblWin.TabIndex = 19;
            lblWin.Text = "W";
            // 
            // btnClear
            // 
            btnClear.Location = new System.Drawing.Point(54, 133);
            btnClear.Name = "btnClear";
            btnClear.Size = new System.Drawing.Size(94, 29);
            btnClear.TabIndex = 18;
            btnClear.Text = "Clear";
            btnClear.UseVisualStyleBackColor = true;
            // 
            // btnDebug
            // 
            btnDebug.Location = new System.Drawing.Point(347, 133);
            btnDebug.Name = "btnDebug";
            btnDebug.Size = new System.Drawing.Size(94, 29);
            btnDebug.TabIndex = 17;
            btnDebug.Text = "Do Debug";
            btnDebug.UseVisualStyleBackColor = true;
            // 
            // tvInfo
            // 
            tvInfo.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            tvInfo.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            tvInfo.Location = new System.Drawing.Point(54, 168);
            tvInfo.MatchUseBackground = true;
            tvInfo.MaxText = 50000;
            tvInfo.Name = "tvInfo";
            tvInfo.Prompt = "";
            tvInfo.Size = new System.Drawing.Size(825, 420);
            tvInfo.TabIndex = 16;
            tvInfo.WordWrap = true;
            // 
            // Dev
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(8F, 19F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            ClientSize = new System.Drawing.Size(933, 620);
            Controls.Add(lblCtrl);
            Controls.Add(lblAlt);
            Controls.Add(label2);
            Controls.Add(label1);
            Controls.Add(txtPreviousWin);
            Controls.Add(txtCurrentWin);
            Controls.Add(lblLetter);
            Controls.Add(lblWin);
            Controls.Add(btnClear);
            Controls.Add(btnDebug);
            Controls.Add(tvInfo);
            Name = "Dev";
            Text = "Dev";
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private System.Windows.Forms.Label lblCtrl;
        private System.Windows.Forms.Label lblAlt;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox txtPreviousWin;
        private System.Windows.Forms.TextBox txtCurrentWin;
        private System.Windows.Forms.Label lblLetter;
        private System.Windows.Forms.Label lblWin;
        private System.Windows.Forms.Button btnClear;
        private System.Windows.Forms.Button btnDebug;
        private Ephemera.NBagOfUis.TextViewer tvInfo;
    }
}