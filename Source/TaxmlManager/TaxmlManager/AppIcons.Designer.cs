namespace TaxonomyToolkit.TaxmlManager
{
    partial class AppIcons
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(AppIcons));
            this.ctlIcons = new System.Windows.Forms.ImageList(this.components);
            // 
            // ctlIcons
            // 
            this.ctlIcons.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("ctlIcons.ImageStream")));
            this.ctlIcons.TransparentColor = System.Drawing.Color.Transparent;
            this.ctlIcons.Images.SetKeyName(0, "Error");
            this.ctlIcons.Images.SetKeyName(1, "TermStore");
            this.ctlIcons.Images.SetKeyName(2, "TermGroup");
            this.ctlIcons.Images.SetKeyName(3, "TermSet");
            this.ctlIcons.Images.SetKeyName(4, "TermSetSpecial");
            this.ctlIcons.Images.SetKeyName(5, "Term");
            this.ctlIcons.Images.SetKeyName(6, "TermReused");

        }

        #endregion

        private System.Windows.Forms.ImageList ctlIcons;

    }
}
