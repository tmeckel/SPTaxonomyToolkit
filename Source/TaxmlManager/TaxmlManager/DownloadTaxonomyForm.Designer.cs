namespace TaxonomyToolkit.TaxmlManager
{
    partial class DownloadTaxonomyForm
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
            this.btnOk = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.radDepthGroups = new System.Windows.Forms.RadioButton();
            this.radDepthTermSets = new System.Windows.Forms.RadioButton();
            this.radDepthTerms = new System.Windows.Forms.RadioButton();
            this.chkIncludeSystemGroup = new System.Windows.Forms.CheckBox();
            this.chkIncludeSiteCollectionGroup = new System.Windows.Forms.CheckBox();
            this.chkIncludeGlobalGroups = new System.Windows.Forms.CheckBox();
            this.label2 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.txtTermStoreName = new System.Windows.Forms.TextBox();
            this.SuspendLayout();
            // 
            // btnOk
            // 
            this.btnOk.Location = new System.Drawing.Point(222, 160);
            this.btnOk.Name = "btnOk";
            this.btnOk.Size = new System.Drawing.Size(76, 23);
            this.btnOk.TabIndex = 0;
            this.btnOk.Text = "&Ok";
            this.btnOk.UseVisualStyleBackColor = true;
            this.btnOk.Click += new System.EventHandler(this.btnOk_Click);
            // 
            // btnCancel
            // 
            this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnCancel.Location = new System.Drawing.Point(304, 160);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(76, 23);
            this.btnCancel.TabIndex = 1;
            this.btnCancel.Text = "&Cancel";
            this.btnCancel.UseVisualStyleBackColor = true;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(20, 47);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(64, 13);
            this.label1.TabIndex = 2;
            this.label1.Text = "Tree Depth:";
            // 
            // radDepthGroups
            // 
            this.radDepthGroups.AutoSize = true;
            this.radDepthGroups.Location = new System.Drawing.Point(146, 45);
            this.radDepthGroups.Name = "radDepthGroups";
            this.radDepthGroups.Size = new System.Drawing.Size(59, 17);
            this.radDepthGroups.TabIndex = 3;
            this.radDepthGroups.Text = "Groups";
            this.radDepthGroups.UseVisualStyleBackColor = true;
            // 
            // radDepthTermSets
            // 
            this.radDepthTermSets.AutoSize = true;
            this.radDepthTermSets.Checked = true;
            this.radDepthTermSets.Location = new System.Drawing.Point(222, 45);
            this.radDepthTermSets.Name = "radDepthTermSets";
            this.radDepthTermSets.Size = new System.Drawing.Size(73, 17);
            this.radDepthTermSets.TabIndex = 4;
            this.radDepthTermSets.TabStop = true;
            this.radDepthTermSets.Text = "Term Sets";
            this.radDepthTermSets.UseVisualStyleBackColor = true;
            // 
            // radDepthTerms
            // 
            this.radDepthTerms.AutoSize = true;
            this.radDepthTerms.Location = new System.Drawing.Point(310, 45);
            this.radDepthTerms.Name = "radDepthTerms";
            this.radDepthTerms.Size = new System.Drawing.Size(54, 17);
            this.radDepthTerms.TabIndex = 5;
            this.radDepthTerms.Text = "Terms";
            this.radDepthTerms.UseVisualStyleBackColor = true;
            // 
            // chkIncludeSystemGroup
            // 
            this.chkIncludeSystemGroup.AutoSize = true;
            this.chkIncludeSystemGroup.Enabled = false;
            this.chkIncludeSystemGroup.Location = new System.Drawing.Point(146, 97);
            this.chkIncludeSystemGroup.Name = "chkIncludeSystemGroup";
            this.chkIncludeSystemGroup.Size = new System.Drawing.Size(92, 17);
            this.chkIncludeSystemGroup.TabIndex = 7;
            this.chkIncludeSystemGroup.Text = "System Group";
            this.chkIncludeSystemGroup.UseVisualStyleBackColor = true;
            // 
            // chkIncludeSiteCollectionGroup
            // 
            this.chkIncludeSiteCollectionGroup.AutoSize = true;
            this.chkIncludeSiteCollectionGroup.Enabled = false;
            this.chkIncludeSiteCollectionGroup.Location = new System.Drawing.Point(146, 120);
            this.chkIncludeSiteCollectionGroup.Name = "chkIncludeSiteCollectionGroup";
            this.chkIncludeSiteCollectionGroup.Size = new System.Drawing.Size(125, 17);
            this.chkIncludeSiteCollectionGroup.TabIndex = 8;
            this.chkIncludeSiteCollectionGroup.Text = "Site Collection Group";
            this.chkIncludeSiteCollectionGroup.UseVisualStyleBackColor = true;
            // 
            // chkIncludeGlobalGroups
            // 
            this.chkIncludeGlobalGroups.AutoSize = true;
            this.chkIncludeGlobalGroups.Checked = true;
            this.chkIncludeGlobalGroups.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkIncludeGlobalGroups.Enabled = false;
            this.chkIncludeGlobalGroups.Location = new System.Drawing.Point(146, 74);
            this.chkIncludeGlobalGroups.Name = "chkIncludeGlobalGroups";
            this.chkIncludeGlobalGroups.Size = new System.Drawing.Size(93, 17);
            this.chkIncludeGlobalGroups.TabIndex = 9;
            this.chkIncludeGlobalGroups.Text = "Global Groups";
            this.chkIncludeGlobalGroups.UseVisualStyleBackColor = true;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(20, 74);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(98, 13);
            this.label2.TabIndex = 12;
            this.label2.Text = "Groups To Include:";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(20, 22);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(64, 13);
            this.label3.TabIndex = 13;
            this.label3.Text = "Tree Depth:";
            // 
            // txtTermStoreName
            // 
            this.txtTermStoreName.Location = new System.Drawing.Point(146, 19);
            this.txtTermStoreName.Name = "txtTermStoreName";
            this.txtTermStoreName.ReadOnly = true;
            this.txtTermStoreName.Size = new System.Drawing.Size(234, 20);
            this.txtTermStoreName.TabIndex = 14;
            // 
            // DownloadTaxonomyForm
            // 
            this.AcceptButton = this.btnOk;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.btnCancel;
            this.ClientSize = new System.Drawing.Size(398, 197);
            this.Controls.Add(this.txtTermStoreName);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.chkIncludeGlobalGroups);
            this.Controls.Add(this.chkIncludeSystemGroup);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.chkIncludeSiteCollectionGroup);
            this.Controls.Add(this.radDepthGroups);
            this.Controls.Add(this.radDepthTermSets);
            this.Controls.Add(this.radDepthTerms);
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.btnOk);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "DownloadTaxonomyForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Download Taxonomy Content";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button btnOk;
        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.RadioButton radDepthGroups;
        private System.Windows.Forms.RadioButton radDepthTermSets;
        private System.Windows.Forms.RadioButton radDepthTerms;
        private System.Windows.Forms.CheckBox chkIncludeSystemGroup;
        private System.Windows.Forms.CheckBox chkIncludeSiteCollectionGroup;
        private System.Windows.Forms.CheckBox chkIncludeGlobalGroups;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.TextBox txtTermStoreName;
    }
}