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
            btnDebug = new System.Windows.Forms.Button();
            btnSettings = new System.Windows.Forms.Button();
            SuspendLayout();
            // 
            // btnDebug
            // 
            btnDebug.Location = new System.Drawing.Point(1, 2);
            btnDebug.Name = "btnDebug";
            btnDebug.Size = new System.Drawing.Size(94, 29);
            btnDebug.TabIndex = 2;
            btnDebug.Text = "Do Debug";
            btnDebug.UseVisualStyleBackColor = true;
            btnDebug.Click += Debug_Click;
            // 
            // btnSettings
            // 
            btnSettings.Location = new System.Drawing.Point(101, 2);
            btnSettings.Name = "btnSettings";
            btnSettings.Size = new System.Drawing.Size(94, 29);
            btnSettings.TabIndex = 15;
            btnSettings.Text = "Settings";
            btnSettings.UseVisualStyleBackColor = true;
            btnSettings.Click += Settings_Click;
            // 
            // MainForm
            // 
            AutoScroll = true;
            ClientSize = new System.Drawing.Size(434, 598);
            Controls.Add(btnSettings);
            Controls.Add(btnDebug);
            FormBorderStyle = System.Windows.Forms.FormBorderStyle.SizableToolWindow;
            Icon = (System.Drawing.Icon)resources.GetObject("$this.Icon");
            Name = "MainForm";
            StartPosition = System.Windows.Forms.FormStartPosition.Manual;
            Text = "Hoo Haa";
            WindowState = System.Windows.Forms.FormWindowState.Minimized;
            ResumeLayout(false);
        }
        #endregion

        private System.Windows.Forms.Button btnDebug;
        private System.Windows.Forms.Button btnSettings;
    }
}