namespace TaxonomyToolkit.TaxmlManager
{
    partial class TermSetListView
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
            System.Windows.Forms.ListViewGroup listViewGroup1 = new System.Windows.Forms.ListViewGroup("Group 1", System.Windows.Forms.HorizontalAlignment.Left);
            System.Windows.Forms.ListViewGroup listViewGroup2 = new System.Windows.Forms.ListViewGroup("Group 2", System.Windows.Forms.HorizontalAlignment.Left);
            System.Windows.Forms.ListViewItem listViewItem1 = new System.Windows.Forms.ListViewItem(new string[] {
            "Test Item",
            "G"}, "(none)");
            System.Windows.Forms.ListViewItem listViewItem2 = new System.Windows.Forms.ListViewItem("Test Item #2");
            System.Windows.Forms.ListViewItem listViewItem3 = new System.Windows.Forms.ListViewItem("Test Item #3");
            this.ctlListView = new System.Windows.Forms.ListView();
            this.columnHeader1 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader2 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.ctlAppIcons = new TaxonomyToolkit.TaxmlManager.AppIcons(this.components);
            this.SuspendLayout();
            // 
            // ctlListView
            // 
            this.ctlListView.CheckBoxes = true;
            this.ctlListView.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.columnHeader1,
            this.columnHeader2});
            this.ctlListView.Dock = System.Windows.Forms.DockStyle.Fill;
            listViewGroup1.Header = "Group 1";
            listViewGroup1.Name = "listViewGroup1";
            listViewGroup2.Header = "Group 2";
            listViewGroup2.Name = "listViewGroup2";
            this.ctlListView.Groups.AddRange(new System.Windows.Forms.ListViewGroup[] {
            listViewGroup1,
            listViewGroup2});
            this.ctlListView.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.Nonclickable;
            listViewItem1.StateImageIndex = 0;
            listViewItem2.StateImageIndex = 0;
            listViewItem3.StateImageIndex = 0;
            this.ctlListView.Items.AddRange(new System.Windows.Forms.ListViewItem[] {
            listViewItem1,
            listViewItem2,
            listViewItem3});
            this.ctlListView.Location = new System.Drawing.Point(0, 0);
            this.ctlListView.Name = "ctlListView";
            this.ctlListView.ShowGroups = false;
            this.ctlListView.Size = new System.Drawing.Size(439, 479);
            this.ctlListView.TabIndex = 0;
            this.ctlListView.UseCompatibleStateImageBehavior = false;
            this.ctlListView.View = System.Windows.Forms.View.Details;
            // 
            // columnHeader1
            // 
            this.columnHeader1.Text = "Term Set";
            this.columnHeader1.Width = 115;
            // 
            // columnHeader2
            // 
            this.columnHeader2.Text = "Group";
            this.columnHeader2.Width = 103;
            // 
            // TermSetListView
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.ctlListView);
            this.Name = "TermSetListView";
            this.Size = new System.Drawing.Size(439, 479);
            this.ResumeLayout(false);

        }

        #endregion

        private TaxonomyToolkit.TaxmlManager.AppIcons ctlAppIcons;
        private System.Windows.Forms.ListView ctlListView;
        private System.Windows.Forms.ColumnHeader columnHeader1;
        private System.Windows.Forms.ColumnHeader columnHeader2;

    }
}
