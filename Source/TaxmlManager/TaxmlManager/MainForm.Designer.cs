namespace TaxonomyToolkit.TaxmlManager
{
    partial class MainForm
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
            this.ctlMenuStrip = new System.Windows.Forms.MenuStrip();
            this.mnuFile = new System.Windows.Forms.ToolStripMenuItem();
            this.mnuFileNew = new System.Windows.Forms.ToolStripMenuItem();
            this.mnuFileOpen = new System.Windows.Forms.ToolStripMenuItem();
            this.mnuFileSave = new System.Windows.Forms.ToolStripMenuItem();
            this.mnuFileSaveAs = new System.Windows.Forms.ToolStripMenuItem();
            this.mnuFileSep1 = new System.Windows.Forms.ToolStripSeparator();
            this.mnuFileExit = new System.Windows.Forms.ToolStripMenuItem();
            this.mnuHelp = new System.Windows.Forms.ToolStripMenuItem();
            this.mnuHelpAbout = new System.Windows.Forms.ToolStripMenuItem();
            this.ctlStatusBar = new System.Windows.Forms.StatusStrip();
            this.ctlTabControl = new System.Windows.Forms.TabControl();
            this.tabPage1 = new System.Windows.Forms.TabPage();
            this.tabPage2 = new System.Windows.Forms.TabPage();
            this.ctlOpenFileDialog = new System.Windows.Forms.OpenFileDialog();
            this.ctlSaveFileDialog = new System.Windows.Forms.SaveFileDialog();
            this.btnCloseTab = new System.Windows.Forms.Button();
            this.ctlSeverPane = new TaxonomyToolkit.TaxmlManager.ServerPane();
            this.mnuDebug = new System.Windows.Forms.ToolStripMenuItem();
            this.mnuDebugTest = new System.Windows.Forms.ToolStripMenuItem();
            this.ctlMenuStrip.SuspendLayout();
            this.ctlTabControl.SuspendLayout();
            this.SuspendLayout();
            // 
            // ctlMenuStrip
            // 
            this.ctlMenuStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.mnuFile,
            this.mnuHelp,
            this.mnuDebug});
            this.ctlMenuStrip.Location = new System.Drawing.Point(0, 0);
            this.ctlMenuStrip.Name = "ctlMenuStrip";
            this.ctlMenuStrip.Size = new System.Drawing.Size(650, 24);
            this.ctlMenuStrip.TabIndex = 5;
            this.ctlMenuStrip.Text = "menuStrip1";
            // 
            // mnuFile
            // 
            this.mnuFile.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.mnuFileNew,
            this.mnuFileOpen,
            this.mnuFileSave,
            this.mnuFileSaveAs,
            this.mnuFileSep1,
            this.mnuFileExit});
            this.mnuFile.Name = "mnuFile";
            this.mnuFile.Size = new System.Drawing.Size(37, 20);
            this.mnuFile.Text = "&File";
            // 
            // mnuFileNew
            // 
            this.mnuFileNew.Name = "mnuFileNew";
            this.mnuFileNew.Size = new System.Drawing.Size(155, 22);
            this.mnuFileNew.Text = "&New";
            this.mnuFileNew.Click += new System.EventHandler(this.mnuFileNew_Click);
            // 
            // mnuFileOpen
            // 
            this.mnuFileOpen.Name = "mnuFileOpen";
            this.mnuFileOpen.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.O)));
            this.mnuFileOpen.Size = new System.Drawing.Size(155, 22);
            this.mnuFileOpen.Text = "&Open...";
            this.mnuFileOpen.Click += new System.EventHandler(this.mnuFileOpen_Click);
            // 
            // mnuFileSave
            // 
            this.mnuFileSave.Name = "mnuFileSave";
            this.mnuFileSave.Size = new System.Drawing.Size(155, 22);
            this.mnuFileSave.Text = "&Save";
            this.mnuFileSave.Click += new System.EventHandler(this.mnuFileSave_Click);
            // 
            // mnuFileSaveAs
            // 
            this.mnuFileSaveAs.Name = "mnuFileSaveAs";
            this.mnuFileSaveAs.Size = new System.Drawing.Size(155, 22);
            this.mnuFileSaveAs.Text = "&Save As...";
            this.mnuFileSaveAs.Click += new System.EventHandler(this.mnuFileSaveAs_Click);
            // 
            // mnuFileSep1
            // 
            this.mnuFileSep1.Name = "mnuFileSep1";
            this.mnuFileSep1.Size = new System.Drawing.Size(152, 6);
            // 
            // mnuFileExit
            // 
            this.mnuFileExit.Name = "mnuFileExit";
            this.mnuFileExit.Size = new System.Drawing.Size(155, 22);
            this.mnuFileExit.Text = "E&xit";
            this.mnuFileExit.Click += new System.EventHandler(this.mnuFileExit_Click);
            // 
            // mnuHelp
            // 
            this.mnuHelp.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.mnuHelpAbout});
            this.mnuHelp.Name = "mnuHelp";
            this.mnuHelp.Size = new System.Drawing.Size(44, 20);
            this.mnuHelp.Text = "&Help";
            // 
            // mnuHelpAbout
            // 
            this.mnuHelpAbout.Name = "mnuHelpAbout";
            this.mnuHelpAbout.Size = new System.Drawing.Size(152, 22);
            this.mnuHelpAbout.Text = "&About...";
            // 
            // ctlStatusBar
            // 
            this.ctlStatusBar.Location = new System.Drawing.Point(0, 701);
            this.ctlStatusBar.Name = "ctlStatusBar";
            this.ctlStatusBar.Size = new System.Drawing.Size(650, 22);
            this.ctlStatusBar.TabIndex = 1;
            this.ctlStatusBar.Text = "statusStrip1";
            // 
            // ctlTabControl
            // 
            this.ctlTabControl.Controls.Add(this.tabPage1);
            this.ctlTabControl.Controls.Add(this.tabPage2);
            this.ctlTabControl.Dock = System.Windows.Forms.DockStyle.Fill;
            this.ctlTabControl.Location = new System.Drawing.Point(0, 120);
            this.ctlTabControl.Name = "ctlTabControl";
            this.ctlTabControl.SelectedIndex = 0;
            this.ctlTabControl.ShowToolTips = true;
            this.ctlTabControl.Size = new System.Drawing.Size(650, 581);
            this.ctlTabControl.TabIndex = 4;
            // 
            // tabPage1
            // 
            this.tabPage1.Location = new System.Drawing.Point(4, 22);
            this.tabPage1.Name = "tabPage1";
            this.tabPage1.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage1.Size = new System.Drawing.Size(642, 555);
            this.tabPage1.TabIndex = 0;
            this.tabPage1.Text = "tabPage1";
            this.tabPage1.UseVisualStyleBackColor = true;
            // 
            // tabPage2
            // 
            this.tabPage2.Location = new System.Drawing.Point(4, 22);
            this.tabPage2.Name = "tabPage2";
            this.tabPage2.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage2.Size = new System.Drawing.Size(642, 555);
            this.tabPage2.TabIndex = 1;
            this.tabPage2.Text = "tabPage2";
            this.tabPage2.UseVisualStyleBackColor = true;
            // 
            // ctlOpenFileDialog
            // 
            this.ctlOpenFileDialog.DefaultExt = "taxml";
            this.ctlOpenFileDialog.Filter = "TAXML Files (*.taxml)|*.taxml|XML Files (*.xml)|*.xml|All Files (*.*)|*.*";
            // 
            // ctlSaveFileDialog
            // 
            this.ctlSaveFileDialog.DefaultExt = "taxml";
            this.ctlSaveFileDialog.Filter = "TAXML Files (*.taxml)|*.taxml|XML Files (*.xml)|*.xml|All Files (*.*)|*.*";
            // 
            // btnCloseTab
            // 
            this.btnCloseTab.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnCloseTab.FlatStyle = System.Windows.Forms.FlatStyle.Popup;
            this.btnCloseTab.Font = new System.Drawing.Font("Microsoft Sans Serif", 6.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnCloseTab.Location = new System.Drawing.Point(628, 118);
            this.btnCloseTab.Margin = new System.Windows.Forms.Padding(0);
            this.btnCloseTab.Name = "btnCloseTab";
            this.btnCloseTab.Size = new System.Drawing.Size(18, 18);
            this.btnCloseTab.TabIndex = 6;
            this.btnCloseTab.Text = "X";
            this.btnCloseTab.UseVisualStyleBackColor = true;
            this.btnCloseTab.Click += new System.EventHandler(this.btnCloseTab_Click);
            // 
            // ctlSeverPane
            // 
            this.ctlSeverPane.Dock = System.Windows.Forms.DockStyle.Top;
            this.ctlSeverPane.Location = new System.Drawing.Point(0, 24);
            this.ctlSeverPane.Name = "ctlSeverPane";
            this.ctlSeverPane.Size = new System.Drawing.Size(650, 96);
            this.ctlSeverPane.TabIndex = 0;
            // 
            // mnuDebug
            // 
            this.mnuDebug.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.mnuDebugTest});
            this.mnuDebug.Name = "mnuDebug";
            this.mnuDebug.Size = new System.Drawing.Size(54, 20);
            this.mnuDebug.Text = "&Debug";
            this.mnuDebug.Visible = false;
            // 
            // mnuDebugTest
            // 
            this.mnuDebugTest.Name = "mnuDebugTest";
            this.mnuDebugTest.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.T)));
            this.mnuDebugTest.Size = new System.Drawing.Size(152, 22);
            this.mnuDebugTest.Text = "&Test";
            this.mnuDebugTest.Click += new System.EventHandler(this.mnuDebugTest_Click);
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(650, 723);
            this.Controls.Add(this.btnCloseTab);
            this.Controls.Add(this.ctlTabControl);
            this.Controls.Add(this.ctlStatusBar);
            this.Controls.Add(this.ctlSeverPane);
            this.Controls.Add(this.ctlMenuStrip);
            this.MainMenuStrip = this.ctlMenuStrip;
            this.Name = "MainForm";
            this.Text = "Taxml Manager";
            this.ctlMenuStrip.ResumeLayout(false);
            this.ctlMenuStrip.PerformLayout();
            this.ctlTabControl.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.MenuStrip ctlMenuStrip;
        private System.Windows.Forms.ToolStripMenuItem mnuFile;
        private System.Windows.Forms.ToolStripMenuItem mnuFileExit;
        private ServerPane ctlSeverPane;
        private System.Windows.Forms.ToolStripMenuItem mnuHelp;
        private System.Windows.Forms.ToolStripMenuItem mnuHelpAbout;
        private System.Windows.Forms.ToolStripMenuItem mnuFileNew;
        private System.Windows.Forms.ToolStripMenuItem mnuFileOpen;
        private System.Windows.Forms.ToolStripMenuItem mnuFileSave;
        private System.Windows.Forms.ToolStripMenuItem mnuFileSaveAs;
        private System.Windows.Forms.ToolStripSeparator mnuFileSep1;
        private System.Windows.Forms.StatusStrip ctlStatusBar;
        private System.Windows.Forms.TabControl ctlTabControl;
        private System.Windows.Forms.TabPage tabPage1;
        private System.Windows.Forms.TabPage tabPage2;
        private System.Windows.Forms.OpenFileDialog ctlOpenFileDialog;
        private System.Windows.Forms.SaveFileDialog ctlSaveFileDialog;
        private System.Windows.Forms.Button btnCloseTab;
        private System.Windows.Forms.ToolStripMenuItem mnuDebug;
        private System.Windows.Forms.ToolStripMenuItem mnuDebugTest;
    }
}

