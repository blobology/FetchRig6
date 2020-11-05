namespace FetchRig6
{
    partial class ChessboardCalibration
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
            this.components = new System.ComponentModel.Container();
            this.calibration_imageBox1 = new Emgu.CV.UI.ImageBox();
            this.calibration_imageBox2 = new Emgu.CV.UI.ImageBox();
            this.calibration_imageNumUpDown = new System.Windows.Forms.NumericUpDown();
            this.showChessboardButton = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.calibration_imageBox1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.calibration_imageBox2)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.calibration_imageNumUpDown)).BeginInit();
            this.SuspendLayout();
            // 
            // calibration_imageBox1
            // 
            this.calibration_imageBox1.FunctionalMode = Emgu.CV.UI.ImageBox.FunctionalModeOption.PanAndZoom;
            this.calibration_imageBox1.Location = new System.Drawing.Point(12, 12);
            this.calibration_imageBox1.Name = "calibration_imageBox1";
            this.calibration_imageBox1.Size = new System.Drawing.Size(802, 550);
            this.calibration_imageBox1.TabIndex = 2;
            this.calibration_imageBox1.TabStop = false;
            // 
            // calibration_imageBox2
            // 
            this.calibration_imageBox2.FunctionalMode = Emgu.CV.UI.ImageBox.FunctionalModeOption.PanAndZoom;
            this.calibration_imageBox2.Location = new System.Drawing.Point(12, 568);
            this.calibration_imageBox2.Name = "calibration_imageBox2";
            this.calibration_imageBox2.Size = new System.Drawing.Size(802, 550);
            this.calibration_imageBox2.TabIndex = 3;
            this.calibration_imageBox2.TabStop = false;
            // 
            // calibration_imageNumUpDown
            // 
            this.calibration_imageNumUpDown.Location = new System.Drawing.Point(820, 12);
            this.calibration_imageNumUpDown.Name = "calibration_imageNumUpDown";
            this.calibration_imageNumUpDown.Size = new System.Drawing.Size(120, 20);
            this.calibration_imageNumUpDown.TabIndex = 4;
            // 
            // showChessboardButton
            // 
            this.showChessboardButton.Location = new System.Drawing.Point(820, 38);
            this.showChessboardButton.Name = "showChessboardButton";
            this.showChessboardButton.Size = new System.Drawing.Size(120, 42);
            this.showChessboardButton.TabIndex = 5;
            this.showChessboardButton.Text = "Show Calibration Images";
            this.showChessboardButton.UseVisualStyleBackColor = true;
            this.showChessboardButton.Click += new System.EventHandler(this.showChessboardButton_Click);
            // 
            // ChessboardCalibration
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1041, 1151);
            this.Controls.Add(this.showChessboardButton);
            this.Controls.Add(this.calibration_imageNumUpDown);
            this.Controls.Add(this.calibration_imageBox2);
            this.Controls.Add(this.calibration_imageBox1);
            this.Name = "ChessboardCalibration";
            this.Text = "ChessboardCalibration";
            ((System.ComponentModel.ISupportInitialize)(this.calibration_imageBox1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.calibration_imageBox2)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.calibration_imageNumUpDown)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private Emgu.CV.UI.ImageBox calibration_imageBox1;
        private Emgu.CV.UI.ImageBox calibration_imageBox2;
        private System.Windows.Forms.NumericUpDown calibration_imageNumUpDown;
        private System.Windows.Forms.Button showChessboardButton;
    }
}