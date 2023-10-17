namespace POCOMySQL
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
            this.cbTableName = new System.Windows.Forms.ComboBox();
            this.label1 = new System.Windows.Forms.Label();
            this.cmdGenerateClass = new System.Windows.Forms.Button();
            this.txtClass = new System.Windows.Forms.TextBox();
            this.cmdConnectDatabase = new System.Windows.Forms.Button();
            this.cmdGenerateInsert = new System.Windows.Forms.Button();
            this.cmdGenerateSQLUpdate = new System.Windows.Forms.Button();
            this.cmdGenerateClassModel = new System.Windows.Forms.Button();
            this.cmdGenerateService = new System.Windows.Forms.Button();
            this.cmdGenerateMySQLCommand = new System.Windows.Forms.Button();
            this.openFileDialog1 = new System.Windows.Forms.OpenFileDialog();
            this.cmdConvertImageToBase64 = new System.Windows.Forms.Button();
            this.cmdResizeImage = new System.Windows.Forms.Button();
            this.btnExportFile = new System.Windows.Forms.Button();
            this.txtNameSpace = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // cbTableName
            // 
            this.cbTableName.FormattingEnabled = true;
            this.cbTableName.Location = new System.Drawing.Point(170, 13);
            this.cbTableName.Name = "cbTableName";
            this.cbTableName.Size = new System.Drawing.Size(235, 21);
            this.cbTableName.TabIndex = 0;
            this.cbTableName.TextChanged += new System.EventHandler(this.cbTableName_TextChanged);
            this.cbTableName.KeyUp += new System.Windows.Forms.KeyEventHandler(this.cbTableName_KeyUp);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(13, 13);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(67, 13);
            this.label1.TabIndex = 1;
            this.label1.Text = "List Of Table";
            // 
            // cmdGenerateClass
            // 
            this.cmdGenerateClass.Location = new System.Drawing.Point(16, 40);
            this.cmdGenerateClass.Name = "cmdGenerateClass";
            this.cmdGenerateClass.Size = new System.Drawing.Size(143, 23);
            this.cmdGenerateClass.TabIndex = 2;
            this.cmdGenerateClass.Text = "Generate Class";
            this.cmdGenerateClass.UseVisualStyleBackColor = true;
            this.cmdGenerateClass.Click += new System.EventHandler(this.cmdGenerateClass_Click);
            // 
            // txtClass
            // 
            this.txtClass.AcceptsReturn = true;
            this.txtClass.Font = new System.Drawing.Font("Microsoft Sans Serif", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.txtClass.Location = new System.Drawing.Point(12, 98);
            this.txtClass.Multiline = true;
            this.txtClass.Name = "txtClass";
            this.txtClass.Size = new System.Drawing.Size(720, 554);
            this.txtClass.TabIndex = 3;
            // 
            // cmdConnectDatabase
            // 
            this.cmdConnectDatabase.Location = new System.Drawing.Point(428, 13);
            this.cmdConnectDatabase.Name = "cmdConnectDatabase";
            this.cmdConnectDatabase.Size = new System.Drawing.Size(111, 23);
            this.cmdConnectDatabase.TabIndex = 4;
            this.cmdConnectDatabase.Text = "Connect Database";
            this.cmdConnectDatabase.UseVisualStyleBackColor = true;
            this.cmdConnectDatabase.Click += new System.EventHandler(this.cmdConnectDatabase_Click);
            // 
            // cmdGenerateInsert
            // 
            this.cmdGenerateInsert.Location = new System.Drawing.Point(170, 40);
            this.cmdGenerateInsert.Name = "cmdGenerateInsert";
            this.cmdGenerateInsert.Size = new System.Drawing.Size(148, 23);
            this.cmdGenerateInsert.TabIndex = 5;
            this.cmdGenerateInsert.Text = "Generate SQL Insert";
            this.cmdGenerateInsert.UseVisualStyleBackColor = true;
            this.cmdGenerateInsert.Click += new System.EventHandler(this.cmdGenerateInsert_Click);
            // 
            // cmdGenerateSQLUpdate
            // 
            this.cmdGenerateSQLUpdate.Location = new System.Drawing.Point(170, 69);
            this.cmdGenerateSQLUpdate.Name = "cmdGenerateSQLUpdate";
            this.cmdGenerateSQLUpdate.Size = new System.Drawing.Size(146, 23);
            this.cmdGenerateSQLUpdate.TabIndex = 6;
            this.cmdGenerateSQLUpdate.Text = "Generate SQL Update";
            this.cmdGenerateSQLUpdate.UseVisualStyleBackColor = true;
            this.cmdGenerateSQLUpdate.Click += new System.EventHandler(this.cmdGenerateSQLUpdate_Click);
            // 
            // cmdGenerateClassModel
            // 
            this.cmdGenerateClassModel.Location = new System.Drawing.Point(16, 69);
            this.cmdGenerateClassModel.Name = "cmdGenerateClassModel";
            this.cmdGenerateClassModel.Size = new System.Drawing.Size(143, 23);
            this.cmdGenerateClassModel.TabIndex = 7;
            this.cmdGenerateClassModel.Text = "Generate Class Model";
            this.cmdGenerateClassModel.UseVisualStyleBackColor = true;
            this.cmdGenerateClassModel.Click += new System.EventHandler(this.cmdGenerateClassModel_Click);
            // 
            // cmdGenerateService
            // 
            this.cmdGenerateService.Location = new System.Drawing.Point(324, 40);
            this.cmdGenerateService.Name = "cmdGenerateService";
            this.cmdGenerateService.Size = new System.Drawing.Size(154, 23);
            this.cmdGenerateService.TabIndex = 8;
            this.cmdGenerateService.Text = "Generate Service";
            this.cmdGenerateService.UseVisualStyleBackColor = true;
            this.cmdGenerateService.Click += new System.EventHandler(this.cmdGenerateService_Click);
            // 
            // cmdGenerateMySQLCommand
            // 
            this.cmdGenerateMySQLCommand.Location = new System.Drawing.Point(324, 69);
            this.cmdGenerateMySQLCommand.Name = "cmdGenerateMySQLCommand";
            this.cmdGenerateMySQLCommand.Size = new System.Drawing.Size(154, 23);
            this.cmdGenerateMySQLCommand.TabIndex = 9;
            this.cmdGenerateMySQLCommand.Text = "Generate MySQL Command";
            this.cmdGenerateMySQLCommand.UseVisualStyleBackColor = true;
            this.cmdGenerateMySQLCommand.Click += new System.EventHandler(this.cmdGenerateMySQLCommand_Click);
            // 
            // openFileDialog1
            // 
            this.openFileDialog1.FileName = "openFileDialog1";
            // 
            // cmdConvertImageToBase64
            // 
            this.cmdConvertImageToBase64.Location = new System.Drawing.Point(484, 40);
            this.cmdConvertImageToBase64.Name = "cmdConvertImageToBase64";
            this.cmdConvertImageToBase64.Size = new System.Drawing.Size(148, 23);
            this.cmdConvertImageToBase64.TabIndex = 10;
            this.cmdConvertImageToBase64.Text = "Convert Image To Base64";
            this.cmdConvertImageToBase64.UseVisualStyleBackColor = true;
            this.cmdConvertImageToBase64.Click += new System.EventHandler(this.cmdConvertImageToBase64_Click);
            // 
            // cmdResizeImage
            // 
            this.cmdResizeImage.Location = new System.Drawing.Point(484, 69);
            this.cmdResizeImage.Name = "cmdResizeImage";
            this.cmdResizeImage.Size = new System.Drawing.Size(148, 23);
            this.cmdResizeImage.TabIndex = 11;
            this.cmdResizeImage.Text = "Resize Image";
            this.cmdResizeImage.UseVisualStyleBackColor = true;
            this.cmdResizeImage.Click += new System.EventHandler(this.cmdResizeImage_Click);
            // 
            // btnExportFile
            // 
            this.btnExportFile.Location = new System.Drawing.Point(741, 135);
            this.btnExportFile.Name = "btnExportFile";
            this.btnExportFile.Size = new System.Drawing.Size(176, 23);
            this.btnExportFile.TabIndex = 12;
            this.btnExportFile.Text = "Export All File";
            this.btnExportFile.UseVisualStyleBackColor = true;
            this.btnExportFile.Click += new System.EventHandler(this.btnExportFile_Click);
            // 
            // txtNameSpace
            // 
            this.txtNameSpace.Location = new System.Drawing.Point(813, 98);
            this.txtNameSpace.Name = "txtNameSpace";
            this.txtNameSpace.Size = new System.Drawing.Size(280, 20);
            this.txtNameSpace.TabIndex = 13;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(738, 101);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(69, 13);
            this.label2.TabIndex = 1;
            this.label2.Text = "Name Space";
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1105, 664);
            this.Controls.Add(this.txtNameSpace);
            this.Controls.Add(this.btnExportFile);
            this.Controls.Add(this.cmdResizeImage);
            this.Controls.Add(this.cmdConvertImageToBase64);
            this.Controls.Add(this.cmdGenerateMySQLCommand);
            this.Controls.Add(this.cmdGenerateService);
            this.Controls.Add(this.cmdGenerateClassModel);
            this.Controls.Add(this.cmdGenerateSQLUpdate);
            this.Controls.Add(this.cmdGenerateInsert);
            this.Controls.Add(this.cmdConnectDatabase);
            this.Controls.Add(this.txtClass);
            this.Controls.Add(this.cmdGenerateClass);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.cbTableName);
            this.Name = "MainForm";
            this.Text = "Form1";
            this.Load += new System.EventHandler(this.MainForm_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ComboBox cbTableName;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Button cmdGenerateClass;
        private System.Windows.Forms.TextBox txtClass;
        private System.Windows.Forms.Button cmdConnectDatabase;
        private System.Windows.Forms.Button cmdGenerateInsert;
        private System.Windows.Forms.Button cmdGenerateSQLUpdate;
        private System.Windows.Forms.Button cmdGenerateClassModel;
        private System.Windows.Forms.Button cmdGenerateService;
        private System.Windows.Forms.Button cmdGenerateMySQLCommand;
        private System.Windows.Forms.OpenFileDialog openFileDialog1;
        private System.Windows.Forms.Button cmdConvertImageToBase64;
        private System.Windows.Forms.Button cmdResizeImage;
        private System.Windows.Forms.Button btnExportFile;
        private System.Windows.Forms.TextBox txtNameSpace;
        private System.Windows.Forms.Label label2;
    }
}

