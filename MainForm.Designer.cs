
namespace WinClip
{
    partial class MainForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        #region Windows Form Designer generated code
        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
            tvInfo = new Ephemera.NBagOfUis.TextViewer();
            btnDebug = new System.Windows.Forms.Button();
            btnClear = new System.Windows.Forms.Button();
            lblWin = new System.Windows.Forms.Label();
            lblLetter = new System.Windows.Forms.Label();
            txtCurrentWin = new System.Windows.Forms.TextBox();
            txtPreviousWin = new System.Windows.Forms.TextBox();
            label1 = new System.Windows.Forms.Label();
            label2 = new System.Windows.Forms.Label();
            lblAlt = new System.Windows.Forms.Label();
            lblCtrl = new System.Windows.Forms.Label();
            btnSettings = new System.Windows.Forms.Button();
            SuspendLayout();
            // 
            // tvInfo
            // 
            tvInfo.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            tvInfo.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            tvInfo.Location = new System.Drawing.Point(282, 147);
            tvInfo.MatchUseBackground = true;
            tvInfo.MaxText = 50000;
            tvInfo.Name = "tvInfo";
            tvInfo.Prompt = "";
            tvInfo.Size = new System.Drawing.Size(825, 420);
            tvInfo.TabIndex = 0;
            tvInfo.WordWrap = true;
            // 
            // btnDebug
            // 
            btnDebug.Location = new System.Drawing.Point(575, 112);
            btnDebug.Name = "btnDebug";
            btnDebug.Size = new System.Drawing.Size(94, 29);
            btnDebug.TabIndex = 2;
            btnDebug.Text = "Do Debug";
            btnDebug.UseVisualStyleBackColor = true;
            btnDebug.Click += Debug_Click;
            // 
            // btnClear
            // 
            btnClear.Location = new System.Drawing.Point(282, 112);
            btnClear.Name = "btnClear";
            btnClear.Size = new System.Drawing.Size(94, 29);
            btnClear.TabIndex = 3;
            btnClear.Text = "Clear";
            btnClear.UseVisualStyleBackColor = true;
            // 
            // lblWin
            // 
            lblWin.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            lblWin.Location = new System.Drawing.Point(344, 80);
            lblWin.Name = "lblWin";
            lblWin.Size = new System.Drawing.Size(25, 25);
            lblWin.TabIndex = 6;
            lblWin.Text = "W";
            // 
            // lblLetter
            // 
            lblLetter.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            lblLetter.Location = new System.Drawing.Point(375, 80);
            lblLetter.Name = "lblLetter";
            lblLetter.Size = new System.Drawing.Size(25, 25);
            lblLetter.TabIndex = 7;
            lblLetter.Text = "?";
            // 
            // txtCurrentWin
            // 
            txtCurrentWin.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            txtCurrentWin.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            txtCurrentWin.Location = new System.Drawing.Point(357, 12);
            txtCurrentWin.Name = "txtCurrentWin";
            txtCurrentWin.ReadOnly = true;
            txtCurrentWin.Size = new System.Drawing.Size(750, 26);
            txtCurrentWin.TabIndex = 9;
            // 
            // txtPreviousWin
            // 
            txtPreviousWin.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            txtPreviousWin.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            txtPreviousWin.Location = new System.Drawing.Point(357, 44);
            txtPreviousWin.Name = "txtPreviousWin";
            txtPreviousWin.ReadOnly = true;
            txtPreviousWin.Size = new System.Drawing.Size(750, 26);
            txtPreviousWin.TabIndex = 10;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new System.Drawing.Point(291, 16);
            label1.Name = "label1";
            label1.Size = new System.Drawing.Size(56, 19);
            label1.TabIndex = 11;
            label1.Text = "Current";
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new System.Drawing.Point(290, 46);
            label2.Name = "label2";
            label2.Size = new System.Drawing.Size(61, 19);
            label2.TabIndex = 12;
            label2.Text = "Previous";
            // 
            // lblAlt
            // 
            lblAlt.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            lblAlt.Location = new System.Drawing.Point(313, 80);
            lblAlt.Name = "lblAlt";
            lblAlt.Size = new System.Drawing.Size(25, 25);
            lblAlt.TabIndex = 13;
            lblAlt.Text = "A";
            // 
            // lblCtrl
            // 
            lblCtrl.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            lblCtrl.Location = new System.Drawing.Point(282, 80);
            lblCtrl.Name = "lblCtrl";
            lblCtrl.Size = new System.Drawing.Size(25, 25);
            lblCtrl.TabIndex = 14;
            lblCtrl.Text = "C";
            // 
            // btnSettings
            // 
            btnSettings.Location = new System.Drawing.Point(675, 112);
            btnSettings.Name = "btnSettings";
            btnSettings.Size = new System.Drawing.Size(94, 29);
            btnSettings.TabIndex = 15;
            btnSettings.Text = "Settings";
            btnSettings.UseVisualStyleBackColor = true;
            btnSettings.Click += Settings_Click;
            // 
            // MainForm
            // 
            ClientSize = new System.Drawing.Size(1120, 598);
            Controls.Add(btnSettings);
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
            FormBorderStyle = System.Windows.Forms.FormBorderStyle.SizableToolWindow;
            Icon = (System.Drawing.Icon)resources.GetObject("$this.Icon");
            Name = "MainForm";
            Text = "Hoo Haa";
            ResumeLayout(false);
            PerformLayout();
        }
        #endregion

        private Ephemera.NBagOfUis.TextViewer tvInfo;
        private System.Windows.Forms.Button btnDebug;
        private System.Windows.Forms.Button btnClear;
        private System.Windows.Forms.Label lblWin;
        private System.Windows.Forms.Label lblLetter;
        private System.Windows.Forms.TextBox txtCurrentWin;
        private System.Windows.Forms.TextBox txtPreviousWin;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label lblAlt;
        private System.Windows.Forms.Label lblCtrl;
        private System.Windows.Forms.Button btnSettings;
    }
}