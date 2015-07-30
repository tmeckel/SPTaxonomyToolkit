namespace TaxonomyToolkit.TaxmlManager
{
    partial class ServerPane
    {
        /// <summary> 
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.label1 = new System.Windows.Forms.Label();
            this.lblTermStoreManager = new System.Windows.Forms.LinkLabel();
            this.txtTermStore = new System.Windows.Forms.ComboBox();
            this.label2 = new System.Windows.Forms.Label();
            this.btnConnectDisconnect = new System.Windows.Forms.Button();
            this.txtInterface = new System.Windows.Forms.ComboBox();
            this.label3 = new System.Windows.Forms.Label();
            this.btnUpload = new System.Windows.Forms.Button();
            this.btnDownload = new System.Windows.Forms.Button();
            this.chkSkipLoginPrompt = new System.Windows.Forms.CheckBox();
            this.txtServerUrl = new System.Windows.Forms.TextBox();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(4, 13);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(83, 13);
            this.label1.TabIndex = 0;
            this.label1.Text = "SharePoint Site:";
            // 
            // lblTermStoreManager
            // 
            this.lblTermStoreManager.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.lblTermStoreManager.AutoSize = true;
            this.lblTermStoreManager.Location = new System.Drawing.Point(497, 40);
            this.lblTermStoreManager.Name = "lblTermStoreManager";
            this.lblTermStoreManager.Size = new System.Drawing.Size(113, 13);
            this.lblTermStoreManager.TabIndex = 7;
            this.lblTermStoreManager.TabStop = true;
            this.lblTermStoreManager.Text = "Term Store Manager...";
            this.lblTermStoreManager.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.lblTermStoreManager_LinkClicked);
            // 
            // txtTermStore
            // 
            this.txtTermStore.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtTermStore.DisplayMember = "Name";
            this.txtTermStore.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.txtTermStore.FormattingEnabled = true;
            this.txtTermStore.Location = new System.Drawing.Point(93, 37);
            this.txtTermStore.Name = "txtTermStore";
            this.txtTermStore.Size = new System.Drawing.Size(398, 21);
            this.txtTermStore.TabIndex = 6;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(25, 40);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(62, 13);
            this.label2.TabIndex = 5;
            this.label2.Text = "Term Store:";
            // 
            // btnConnectDisconnect
            // 
            this.btnConnectDisconnect.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnConnectDisconnect.Location = new System.Drawing.Point(520, 8);
            this.btnConnectDisconnect.Name = "btnConnectDisconnect";
            this.btnConnectDisconnect.Size = new System.Drawing.Size(90, 24);
            this.btnConnectDisconnect.TabIndex = 4;
            this.btnConnectDisconnect.Text = "(connect)";
            this.btnConnectDisconnect.UseVisualStyleBackColor = true;
            this.btnConnectDisconnect.Click += new System.EventHandler(this.btnConnectDisconnect_Click);
            // 
            // txtInterface
            // 
            this.txtInterface.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.txtInterface.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.txtInterface.FormattingEnabled = true;
            this.txtInterface.Items.AddRange(new object[] {
            "Client OM 15"});
            this.txtInterface.Location = new System.Drawing.Point(280, 10);
            this.txtInterface.Name = "txtInterface";
            this.txtInterface.Size = new System.Drawing.Size(110, 21);
            this.txtInterface.TabIndex = 3;
            // 
            // label3
            // 
            this.label3.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(249, 14);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(27, 13);
            this.label3.TabIndex = 2;
            this.label3.Text = "API:";
            // 
            // btnUpload
            // 
            this.btnUpload.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnUpload.Location = new System.Drawing.Point(520, 64);
            this.btnUpload.Name = "btnUpload";
            this.btnUpload.Size = new System.Drawing.Size(89, 24);
            this.btnUpload.TabIndex = 9;
            this.btnUpload.Text = "&Upload...";
            this.btnUpload.UseVisualStyleBackColor = true;
            this.btnUpload.Click += new System.EventHandler(this.btnUpload_Click);
            // 
            // btnDownload
            // 
            this.btnDownload.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnDownload.Location = new System.Drawing.Point(425, 64);
            this.btnDownload.Name = "btnDownload";
            this.btnDownload.Size = new System.Drawing.Size(89, 24);
            this.btnDownload.TabIndex = 8;
            this.btnDownload.Text = "&Download...";
            this.btnDownload.UseVisualStyleBackColor = true;
            this.btnDownload.Click += new System.EventHandler(this.btnDownload_Click);
            // 
            // chkSkipLoginPrompt
            // 
            this.chkSkipLoginPrompt.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.chkSkipLoginPrompt.Location = new System.Drawing.Point(398, 12);
            this.chkSkipLoginPrompt.Name = "chkSkipLoginPrompt";
            this.chkSkipLoginPrompt.Size = new System.Drawing.Size(121, 17);
            this.chkSkipLoginPrompt.TabIndex = 10;
            this.chkSkipLoginPrompt.Text = "Skip Login Prompt";
            this.chkSkipLoginPrompt.UseVisualStyleBackColor = true;
            // 
            // txtServerUrl
            // 
            this.txtServerUrl.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtServerUrl.Location = new System.Drawing.Point(93, 11);
            this.txtServerUrl.Name = "txtServerUrl";
            this.txtServerUrl.Size = new System.Drawing.Size(152, 20);
            this.txtServerUrl.TabIndex = 11;
            // 
            // ServerPane
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.txtServerUrl);
            this.Controls.Add(this.btnDownload);
            this.Controls.Add(this.btnUpload);
            this.Controls.Add(this.txtInterface);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.btnConnectDisconnect);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.txtTermStore);
            this.Controls.Add(this.lblTermStoreManager);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.chkSkipLoginPrompt);
            this.Name = "ServerPane";
            this.Size = new System.Drawing.Size(616, 96);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.LinkLabel lblTermStoreManager;
        private System.Windows.Forms.ComboBox txtTermStore;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Button btnConnectDisconnect;
        private System.Windows.Forms.ComboBox txtInterface;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Button btnUpload;
        private System.Windows.Forms.Button btnDownload;
        private System.Windows.Forms.CheckBox chkSkipLoginPrompt;
        private System.Windows.Forms.TextBox txtServerUrl;
    }
}
