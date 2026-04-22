
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
            lblMatch = new System.Windows.Forms.Label();
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
            tvInfo.Size = new System.Drawing.Size(511, 420);
            tvInfo.TabIndex = 0;
            tvInfo.WordWrap = true;
            // 
            // btnDebug
            // 
            btnDebug.Location = new System.Drawing.Point(281, 8);
            btnDebug.Name = "btnDebug";
            btnDebug.Size = new System.Drawing.Size(94, 29);
            btnDebug.TabIndex = 2;
            btnDebug.Text = "Do Debug";
            btnDebug.UseVisualStyleBackColor = true;
            btnDebug.Click += Debug_Click;
            // 
            // btnClear
            // 
            btnClear.Location = new System.Drawing.Point(281, 43);
            btnClear.Name = "btnClear";
            btnClear.Size = new System.Drawing.Size(94, 29);
            btnClear.TabIndex = 3;
            btnClear.Text = "Clear";
            btnClear.UseVisualStyleBackColor = true;
            // 
            // lblWin
            // 
            lblWin.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            lblWin.Location = new System.Drawing.Point(282, 84);
            lblWin.Name = "lblWin";
            lblWin.Size = new System.Drawing.Size(25, 25);
            lblWin.TabIndex = 6;
            lblWin.Text = "W";
            // 
            // lblLetter
            // 
            lblLetter.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            lblLetter.Location = new System.Drawing.Point(313, 84);
            lblLetter.Name = "lblLetter";
            lblLetter.Size = new System.Drawing.Size(25, 25);
            lblLetter.TabIndex = 7;
            lblLetter.Text = "?";
            // 
            // lblMatch
            // 
            lblMatch.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            lblMatch.Location = new System.Drawing.Point(344, 84);
            lblMatch.Name = "lblMatch";
            lblMatch.Size = new System.Drawing.Size(25, 25);
            lblMatch.TabIndex = 8;
            lblMatch.Text = "!!";
            // 
            // MainForm
            // 
            ClientSize = new System.Drawing.Size(806, 598);
            Controls.Add(lblMatch);
            Controls.Add(lblLetter);
            Controls.Add(lblWin);
            Controls.Add(btnClear);
            Controls.Add(btnDebug);
            Controls.Add(tvInfo);
            FormBorderStyle = System.Windows.Forms.FormBorderStyle.SizableToolWindow;
            Icon = (System.Drawing.Icon)resources.GetObject("$this.Icon");
            Name = "MainForm";
            StartPosition = System.Windows.Forms.FormStartPosition.Manual;
            Text = "Hoo Haa";
            ResumeLayout(false);
        }
        #endregion

        private Ephemera.NBagOfUis.TextViewer tvInfo;
        private System.Windows.Forms.Button btnDebug;
        private System.Windows.Forms.Button btnClear;
        private System.Windows.Forms.Label lblWin;
        private System.Windows.Forms.Label lblLetter;
        private System.Windows.Forms.Label lblMatch;
    }
}