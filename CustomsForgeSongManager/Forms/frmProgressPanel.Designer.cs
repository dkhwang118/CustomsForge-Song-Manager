namespace CustomsForgeSongManager.Forms
{
    partial class frmProgressPanel
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
            this.tableLayoutPanel_BackgroundPanel = new System.Windows.Forms.TableLayoutPanel();
            this.groupBox_GlobalLog = new System.Windows.Forms.GroupBox();
            this.listBox_GlobalLog = new System.Windows.Forms.ListBox();
            this.groupBox_ProcessBox = new System.Windows.Forms.GroupBox();
            this.tableLayoutPanel_ProcessesPanel = new System.Windows.Forms.TableLayoutPanel();
            this.groupBox_ActiveProcesses = new System.Windows.Forms.GroupBox();
            this.listBox_ActiveProcesses = new System.Windows.Forms.ListBox();
            this.groupBox_CurrentProcessProgressBar = new System.Windows.Forms.GroupBox();
            this.progressBar_CurrentProcess = new System.Windows.Forms.ProgressBar();
            this.groupBox_TotalProgress = new System.Windows.Forms.GroupBox();
            this.progressBar_TotalProgress = new System.Windows.Forms.ProgressBar();
            this.tableLayoutPanel_CurrentProcessSelection = new System.Windows.Forms.TableLayoutPanel();
            this.label_CurrentProcess = new System.Windows.Forms.Label();
            this.comboBox_CurrentProcessSelectionBox = new System.Windows.Forms.ComboBox();
            this.abortableBackgroundWorker1 = new CustomsForgeSongManager.LocalTools.AbortableBackgroundWorker();
            this.tableLayoutPanel_BackgroundPanel.SuspendLayout();
            this.groupBox_GlobalLog.SuspendLayout();
            this.groupBox_ProcessBox.SuspendLayout();
            this.tableLayoutPanel_ProcessesPanel.SuspendLayout();
            this.groupBox_ActiveProcesses.SuspendLayout();
            this.groupBox_CurrentProcessProgressBar.SuspendLayout();
            this.groupBox_TotalProgress.SuspendLayout();
            this.tableLayoutPanel_CurrentProcessSelection.SuspendLayout();
            this.SuspendLayout();
            // 
            // tableLayoutPanel_BackgroundPanel
            // 
            this.tableLayoutPanel_BackgroundPanel.ColumnCount = 1;
            this.tableLayoutPanel_BackgroundPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel_BackgroundPanel.Controls.Add(this.groupBox_GlobalLog, 0, 1);
            this.tableLayoutPanel_BackgroundPanel.Controls.Add(this.groupBox_ProcessBox, 0, 0);
            this.tableLayoutPanel_BackgroundPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel_BackgroundPanel.Location = new System.Drawing.Point(0, 0);
            this.tableLayoutPanel_BackgroundPanel.Name = "tableLayoutPanel_BackgroundPanel";
            this.tableLayoutPanel_BackgroundPanel.RowCount = 2;
            this.tableLayoutPanel_BackgroundPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 55.55556F));
            this.tableLayoutPanel_BackgroundPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 44.44444F));
            this.tableLayoutPanel_BackgroundPanel.Size = new System.Drawing.Size(940, 783);
            this.tableLayoutPanel_BackgroundPanel.TabIndex = 2;
            // 
            // groupBox_GlobalLog
            // 
            this.groupBox_GlobalLog.Controls.Add(this.listBox_GlobalLog);
            this.groupBox_GlobalLog.Dock = System.Windows.Forms.DockStyle.Fill;
            this.groupBox_GlobalLog.Location = new System.Drawing.Point(3, 438);
            this.groupBox_GlobalLog.Name = "groupBox_GlobalLog";
            this.groupBox_GlobalLog.Size = new System.Drawing.Size(934, 342);
            this.groupBox_GlobalLog.TabIndex = 2;
            this.groupBox_GlobalLog.TabStop = false;
            this.groupBox_GlobalLog.Text = "Global Log";
            // 
            // listBox_GlobalLog
            // 
            this.listBox_GlobalLog.Dock = System.Windows.Forms.DockStyle.Fill;
            this.listBox_GlobalLog.FormattingEnabled = true;
            this.listBox_GlobalLog.ItemHeight = 25;
            this.listBox_GlobalLog.Location = new System.Drawing.Point(3, 27);
            this.listBox_GlobalLog.Name = "listBox_GlobalLog";
            this.listBox_GlobalLog.Size = new System.Drawing.Size(928, 312);
            this.listBox_GlobalLog.TabIndex = 0;
            // 
            // groupBox_ProcessBox
            // 
            this.groupBox_ProcessBox.Controls.Add(this.tableLayoutPanel_ProcessesPanel);
            this.groupBox_ProcessBox.Dock = System.Windows.Forms.DockStyle.Fill;
            this.groupBox_ProcessBox.Location = new System.Drawing.Point(3, 3);
            this.groupBox_ProcessBox.Name = "groupBox_ProcessBox";
            this.groupBox_ProcessBox.Size = new System.Drawing.Size(934, 429);
            this.groupBox_ProcessBox.TabIndex = 3;
            this.groupBox_ProcessBox.TabStop = false;
            this.groupBox_ProcessBox.Text = "Processes";
            // 
            // tableLayoutPanel_ProcessesPanel
            // 
            this.tableLayoutPanel_ProcessesPanel.ColumnCount = 2;
            this.tableLayoutPanel_ProcessesPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel_ProcessesPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel_ProcessesPanel.Controls.Add(this.groupBox_ActiveProcesses, 1, 0);
            this.tableLayoutPanel_ProcessesPanel.Controls.Add(this.groupBox_CurrentProcessProgressBar, 0, 1);
            this.tableLayoutPanel_ProcessesPanel.Controls.Add(this.groupBox_TotalProgress, 0, 2);
            this.tableLayoutPanel_ProcessesPanel.Controls.Add(this.tableLayoutPanel_CurrentProcessSelection, 0, 0);
            this.tableLayoutPanel_ProcessesPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel_ProcessesPanel.Location = new System.Drawing.Point(3, 27);
            this.tableLayoutPanel_ProcessesPanel.Name = "tableLayoutPanel_ProcessesPanel";
            this.tableLayoutPanel_ProcessesPanel.RowCount = 3;
            this.tableLayoutPanel_ProcessesPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel_ProcessesPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 80F));
            this.tableLayoutPanel_ProcessesPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 80F));
            this.tableLayoutPanel_ProcessesPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.tableLayoutPanel_ProcessesPanel.Size = new System.Drawing.Size(928, 399);
            this.tableLayoutPanel_ProcessesPanel.TabIndex = 0;
            // 
            // groupBox_ActiveProcesses
            // 
            this.groupBox_ActiveProcesses.Controls.Add(this.listBox_ActiveProcesses);
            this.groupBox_ActiveProcesses.Dock = System.Windows.Forms.DockStyle.Fill;
            this.groupBox_ActiveProcesses.Location = new System.Drawing.Point(467, 3);
            this.groupBox_ActiveProcesses.Name = "groupBox_ActiveProcesses";
            this.groupBox_ActiveProcesses.Size = new System.Drawing.Size(458, 233);
            this.groupBox_ActiveProcesses.TabIndex = 0;
            this.groupBox_ActiveProcesses.TabStop = false;
            this.groupBox_ActiveProcesses.Text = "Active Processes";
            // 
            // listBox_ActiveProcesses
            // 
            this.listBox_ActiveProcesses.Dock = System.Windows.Forms.DockStyle.Fill;
            this.listBox_ActiveProcesses.FormattingEnabled = true;
            this.listBox_ActiveProcesses.ItemHeight = 25;
            this.listBox_ActiveProcesses.Location = new System.Drawing.Point(3, 27);
            this.listBox_ActiveProcesses.Name = "listBox_ActiveProcesses";
            this.listBox_ActiveProcesses.Size = new System.Drawing.Size(452, 203);
            this.listBox_ActiveProcesses.TabIndex = 0;
            // 
            // groupBox_CurrentProcessProgressBar
            // 
            this.tableLayoutPanel_ProcessesPanel.SetColumnSpan(this.groupBox_CurrentProcessProgressBar, 2);
            this.groupBox_CurrentProcessProgressBar.Controls.Add(this.progressBar_CurrentProcess);
            this.groupBox_CurrentProcessProgressBar.Dock = System.Windows.Forms.DockStyle.Fill;
            this.groupBox_CurrentProcessProgressBar.Location = new System.Drawing.Point(3, 242);
            this.groupBox_CurrentProcessProgressBar.Name = "groupBox_CurrentProcessProgressBar";
            this.groupBox_CurrentProcessProgressBar.Size = new System.Drawing.Size(922, 74);
            this.groupBox_CurrentProcessProgressBar.TabIndex = 1;
            this.groupBox_CurrentProcessProgressBar.TabStop = false;
            this.groupBox_CurrentProcessProgressBar.Text = "Current Process";
            // 
            // progressBar_CurrentProcess
            // 
            this.progressBar_CurrentProcess.Dock = System.Windows.Forms.DockStyle.Fill;
            this.progressBar_CurrentProcess.Location = new System.Drawing.Point(3, 27);
            this.progressBar_CurrentProcess.Name = "progressBar_CurrentProcess";
            this.progressBar_CurrentProcess.Size = new System.Drawing.Size(916, 44);
            this.progressBar_CurrentProcess.TabIndex = 0;
            // 
            // groupBox_TotalProgress
            // 
            this.tableLayoutPanel_ProcessesPanel.SetColumnSpan(this.groupBox_TotalProgress, 2);
            this.groupBox_TotalProgress.Controls.Add(this.progressBar_TotalProgress);
            this.groupBox_TotalProgress.Dock = System.Windows.Forms.DockStyle.Fill;
            this.groupBox_TotalProgress.Location = new System.Drawing.Point(3, 322);
            this.groupBox_TotalProgress.Name = "groupBox_TotalProgress";
            this.groupBox_TotalProgress.Size = new System.Drawing.Size(922, 74);
            this.groupBox_TotalProgress.TabIndex = 2;
            this.groupBox_TotalProgress.TabStop = false;
            this.groupBox_TotalProgress.Text = "Total Progress";
            // 
            // progressBar_TotalProgress
            // 
            this.progressBar_TotalProgress.Dock = System.Windows.Forms.DockStyle.Fill;
            this.progressBar_TotalProgress.Location = new System.Drawing.Point(3, 27);
            this.progressBar_TotalProgress.Name = "progressBar_TotalProgress";
            this.progressBar_TotalProgress.Size = new System.Drawing.Size(916, 44);
            this.progressBar_TotalProgress.TabIndex = 0;
            // 
            // tableLayoutPanel_CurrentProcessSelection
            // 
            this.tableLayoutPanel_CurrentProcessSelection.ColumnCount = 1;
            this.tableLayoutPanel_CurrentProcessSelection.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel_CurrentProcessSelection.Controls.Add(this.label_CurrentProcess, 0, 0);
            this.tableLayoutPanel_CurrentProcessSelection.Controls.Add(this.comboBox_CurrentProcessSelectionBox, 0, 1);
            this.tableLayoutPanel_CurrentProcessSelection.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel_CurrentProcessSelection.Location = new System.Drawing.Point(3, 3);
            this.tableLayoutPanel_CurrentProcessSelection.Name = "tableLayoutPanel_CurrentProcessSelection";
            this.tableLayoutPanel_CurrentProcessSelection.RowCount = 2;
            this.tableLayoutPanel_CurrentProcessSelection.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 50F));
            this.tableLayoutPanel_CurrentProcessSelection.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel_CurrentProcessSelection.Size = new System.Drawing.Size(458, 233);
            this.tableLayoutPanel_CurrentProcessSelection.TabIndex = 3;
            // 
            // label_CurrentProcess
            // 
            this.label_CurrentProcess.AutoSize = true;
            this.label_CurrentProcess.Dock = System.Windows.Forms.DockStyle.Fill;
            this.label_CurrentProcess.Location = new System.Drawing.Point(3, 0);
            this.label_CurrentProcess.Name = "label_CurrentProcess";
            this.label_CurrentProcess.Size = new System.Drawing.Size(452, 50);
            this.label_CurrentProcess.TabIndex = 0;
            this.label_CurrentProcess.Text = "Current Process:";
            this.label_CurrentProcess.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // comboBox_CurrentProcessSelectionBox
            // 
            this.comboBox_CurrentProcessSelectionBox.Dock = System.Windows.Forms.DockStyle.Fill;
            this.comboBox_CurrentProcessSelectionBox.FormattingEnabled = true;
            this.comboBox_CurrentProcessSelectionBox.Location = new System.Drawing.Point(3, 53);
            this.comboBox_CurrentProcessSelectionBox.Name = "comboBox_CurrentProcessSelectionBox";
            this.comboBox_CurrentProcessSelectionBox.Size = new System.Drawing.Size(452, 33);
            this.comboBox_CurrentProcessSelectionBox.TabIndex = 1;
            // 
            // frmProgressPanel
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(12F, 25F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(940, 783);
            this.Controls.Add(this.tableLayoutPanel_BackgroundPanel);
            this.Name = "frmProgressPanel";
            this.Text = "frmProgressPanel";
            this.tableLayoutPanel_BackgroundPanel.ResumeLayout(false);
            this.groupBox_GlobalLog.ResumeLayout(false);
            this.groupBox_ProcessBox.ResumeLayout(false);
            this.tableLayoutPanel_ProcessesPanel.ResumeLayout(false);
            this.groupBox_ActiveProcesses.ResumeLayout(false);
            this.groupBox_CurrentProcessProgressBar.ResumeLayout(false);
            this.groupBox_TotalProgress.ResumeLayout(false);
            this.tableLayoutPanel_CurrentProcessSelection.ResumeLayout(false);
            this.tableLayoutPanel_CurrentProcessSelection.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel_BackgroundPanel;
        private System.Windows.Forms.GroupBox groupBox_GlobalLog;
        private System.Windows.Forms.ListBox listBox_GlobalLog;
        private System.Windows.Forms.GroupBox groupBox_ProcessBox;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel_ProcessesPanel;
        private System.Windows.Forms.GroupBox groupBox_ActiveProcesses;
        private System.Windows.Forms.ListBox listBox_ActiveProcesses;
        private LocalTools.AbortableBackgroundWorker abortableBackgroundWorker1;
        private System.Windows.Forms.GroupBox groupBox_CurrentProcessProgressBar;
        private System.Windows.Forms.ProgressBar progressBar_CurrentProcess;
        private System.Windows.Forms.GroupBox groupBox_TotalProgress;
        private System.Windows.Forms.ProgressBar progressBar_TotalProgress;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel_CurrentProcessSelection;
        private System.Windows.Forms.Label label_CurrentProcess;
        private System.Windows.Forms.ComboBox comboBox_CurrentProcessSelectionBox;
    }
}