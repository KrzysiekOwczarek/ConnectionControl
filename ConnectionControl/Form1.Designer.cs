namespace ConnectionControl
{
    partial class ConnectionControl
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
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.cloudIPTextBox = new System.Windows.Forms.TextBox();
            this.cloudPortTextBox = new System.Windows.Forms.TextBox();
            this.conToCloudButton = new System.Windows.Forms.Button();
            this.selectedClientBox = new System.Windows.Forms.ComboBox();
            this.label4 = new System.Windows.Forms.Label();
            this.selectedTransportBox = new System.Windows.Forms.ComboBox();
            this.log = new System.Windows.Forms.RichTextBox();
            this.label5 = new System.Windows.Forms.Label();
            this.label6 = new System.Windows.Forms.Label();
            this.label7 = new System.Windows.Forms.Label();
            this.networkNumberTextBox = new System.Windows.Forms.TextBox();
            this.subnetTextBox = new System.Windows.Forms.TextBox();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(12, 9);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(108, 13);
            this.label1.TabIndex = 0;
            this.label1.Text = "IP chmury sterowania";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(155, 9);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(117, 13);
            this.label2.TabIndex = 1;
            this.label2.Text = "Port chmury sterowania";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(12, 104);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(38, 13);
            this.label3.TabIndex = 2;
            this.label3.Text = "Klienci";
            // 
            // cloudIPTextBox
            // 
            this.cloudIPTextBox.Location = new System.Drawing.Point(11, 25);
            this.cloudIPTextBox.Name = "cloudIPTextBox";
            this.cloudIPTextBox.Size = new System.Drawing.Size(128, 20);
            this.cloudIPTextBox.TabIndex = 3;
            this.cloudIPTextBox.Text = "127.0.0.1";
            // 
            // cloudPortTextBox
            // 
            this.cloudPortTextBox.Location = new System.Drawing.Point(158, 25);
            this.cloudPortTextBox.Name = "cloudPortTextBox";
            this.cloudPortTextBox.Size = new System.Drawing.Size(134, 20);
            this.cloudPortTextBox.TabIndex = 4;
            this.cloudPortTextBox.Text = "13000";
            // 
            // conToCloudButton
            // 
            this.conToCloudButton.Location = new System.Drawing.Point(316, 22);
            this.conToCloudButton.Name = "conToCloudButton";
            this.conToCloudButton.Size = new System.Drawing.Size(75, 23);
            this.conToCloudButton.TabIndex = 5;
            this.conToCloudButton.Text = "POŁĄCZ";
            this.conToCloudButton.UseVisualStyleBackColor = true;
            this.conToCloudButton.Click += new System.EventHandler(this.conToCloudButton_Click);
            // 
            // selectedClientBox
            // 
            this.selectedClientBox.FormattingEnabled = true;
            this.selectedClientBox.Location = new System.Drawing.Point(15, 120);
            this.selectedClientBox.Name = "selectedClientBox";
            this.selectedClientBox.Size = new System.Drawing.Size(124, 21);
            this.selectedClientBox.TabIndex = 6;
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(155, 104);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(38, 13);
            this.label4.TabIndex = 7;
            this.label4.Text = "Węzły";
            // 
            // selectedTransportBox
            // 
            this.selectedTransportBox.FormattingEnabled = true;
            this.selectedTransportBox.Location = new System.Drawing.Point(158, 120);
            this.selectedTransportBox.Name = "selectedTransportBox";
            this.selectedTransportBox.Size = new System.Drawing.Size(134, 21);
            this.selectedTransportBox.TabIndex = 8;
            // 
            // log
            // 
            this.log.Location = new System.Drawing.Point(15, 217);
            this.log.Name = "log";
            this.log.Size = new System.Drawing.Size(376, 96);
            this.log.TabIndex = 9;
            this.log.Text = "";
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(15, 201);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(25, 13);
            this.label5.TabIndex = 10;
            this.label5.Text = "Log";
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(12, 48);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(62, 13);
            this.label6.TabIndex = 11;
            this.label6.Text = "Numer sieci";
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(155, 48);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(80, 13);
            this.label7.TabIndex = 12;
            this.label7.Text = "Numer podsieci";
            // 
            // networkNumberTextBox
            // 
            this.networkNumberTextBox.Location = new System.Drawing.Point(11, 69);
            this.networkNumberTextBox.Name = "networkNumberTextBox";
            this.networkNumberTextBox.Size = new System.Drawing.Size(128, 20);
            this.networkNumberTextBox.TabIndex = 13;
            // 
            // subnetTextBox
            // 
            this.subnetTextBox.Location = new System.Drawing.Point(158, 69);
            this.subnetTextBox.Name = "subnetTextBox";
            this.subnetTextBox.Size = new System.Drawing.Size(134, 20);
            this.subnetTextBox.TabIndex = 14;
            // 
            // ConnectionControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(403, 325);
            this.Controls.Add(this.subnetTextBox);
            this.Controls.Add(this.networkNumberTextBox);
            this.Controls.Add(this.label7);
            this.Controls.Add(this.label6);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.log);
            this.Controls.Add(this.selectedTransportBox);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.selectedClientBox);
            this.Controls.Add(this.conToCloudButton);
            this.Controls.Add(this.cloudPortTextBox);
            this.Controls.Add(this.cloudIPTextBox);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Name = "ConnectionControl";
            this.Text = "ConnectionControl";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.TextBox cloudIPTextBox;
        private System.Windows.Forms.TextBox cloudPortTextBox;
        private System.Windows.Forms.Button conToCloudButton;
        private System.Windows.Forms.ComboBox selectedClientBox;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.ComboBox selectedTransportBox;
        private System.Windows.Forms.RichTextBox log;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.TextBox networkNumberTextBox;
        private System.Windows.Forms.TextBox subnetTextBox;
    }
}

