namespace TaxonomyToolkit.TaxmlManager
{
    partial class DocumentView
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
            this.components = new System.ComponentModel.Container();
            this.ctlListView = new TaxonomyToolkit.TaxmlManager.TermSetListView();
            this.mnuContext = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.mnuContextTest = new System.Windows.Forms.ToolStripMenuItem();
            this.mnuContext.SuspendLayout();
            this.SuspendLayout();
            // 
            // ctlListView
            // 
            this.ctlListView.ContextMenuStrip = this.mnuContext;
            this.ctlListView.Dock = System.Windows.Forms.DockStyle.Fill;
            this.ctlListView.Location = new System.Drawing.Point(0, 0);
            this.ctlListView.Name = "ctlListView";
            this.ctlListView.Size = new System.Drawing.Size(597, 555);
            this.ctlListView.TabIndex = 0;
            this.ctlListView.TermStore = null;
            // 
            // mnuContext
            // 
            this.mnuContext.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.mnuContextTest});
            this.mnuContext.Name = "mnuContext";
            this.mnuContext.Size = new System.Drawing.Size(153, 48);
            // 
            // mnuContextTest
            // 
            this.mnuContextTest.Name = "mnuContextTest";
            this.mnuContextTest.Size = new System.Drawing.Size(152, 22);
            this.mnuContextTest.Text = "&Test";
            this.mnuContextTest.Click += new System.EventHandler(this.mnuContextTest_Click);
            // 
            // DocumentView
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.ctlListView);
            this.Name = "DocumentView";
            this.Size = new System.Drawing.Size(597, 555);
            this.mnuContext.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private TermSetListView ctlListView;
        private System.Windows.Forms.ContextMenuStrip mnuContext;
        private System.Windows.Forms.ToolStripMenuItem mnuContextTest;
    }
}
