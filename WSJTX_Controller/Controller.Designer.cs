namespace WSJTX_Controller
{
    partial class Controller
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Controller));
            this.statusText = new System.Windows.Forms.TextBox();
            this.callText = new System.Windows.Forms.Label();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.loggedLabel = new System.Windows.Forms.Label();
            this.callLabel = new System.Windows.Forms.Label();
            this.loggedText = new System.Windows.Forms.Label();
            this.verLabel = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.groupBox4 = new System.Windows.Forms.GroupBox();
            this.advTextBox1 = new System.Windows.Forms.TextBox();
            this.advButton = new System.Windows.Forms.Button();
            this.advTextBox2 = new System.Windows.Forms.TextBox();
            this.label4 = new System.Windows.Forms.Label();
            this.altPauseButton = new System.Windows.Forms.Button();
            this.altClearButton = new System.Windows.Forms.Button();
            this.altListBox = new System.Windows.Forms.ListBox();
            this.label5 = new System.Windows.Forms.Label();
            this.label6 = new System.Windows.Forms.Label();
            this.label7 = new System.Windows.Forms.Label();
            this.label8 = new System.Windows.Forms.Label();
            this.label9 = new System.Windows.Forms.Label();
            this.label10 = new System.Windows.Forms.Label();
            this.label11 = new System.Windows.Forms.Label();
            this.label12 = new System.Windows.Forms.Label();
            this.label13 = new System.Windows.Forms.Label();
            this.label14 = new System.Windows.Forms.Label();
            this.label15 = new System.Windows.Forms.Label();
            this.label16 = new System.Windows.Forms.Label();
            this.label17 = new System.Windows.Forms.Label();
            this.verLabel2 = new System.Windows.Forms.Label();
            this.groupBox3 = new System.Windows.Forms.GroupBox();
            this.useRR73CheckBox = new System.Windows.Forms.CheckBox();
            this.skipGridCheckBox = new System.Windows.Forms.CheckBox();
            this.logEarlyCheckBox = new System.Windows.Forms.CheckBox();
            this.timeoutLabel = new System.Windows.Forms.Label();
            this.directedTextBox = new System.Windows.Forms.TextBox();
            this.directedCheckBox = new System.Windows.Forms.CheckBox();
            this.loggedCheckBox = new System.Windows.Forms.CheckBox();
            this.alertCheckBox = new System.Windows.Forms.CheckBox();
            this.alertTextBox = new System.Windows.Forms.TextBox();
            this.mycallCheckBox = new System.Windows.Forms.CheckBox();
            this.timeoutNumUpDown = new System.Windows.Forms.NumericUpDown();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.groupBox4.SuspendLayout();
            this.groupBox3.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.timeoutNumUpDown)).BeginInit();
            this.SuspendLayout();
            // 
            // statusText
            // 
            this.statusText.BackColor = System.Drawing.Color.Red;
            this.statusText.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.statusText.ForeColor = System.Drawing.Color.White;
            this.statusText.Location = new System.Drawing.Point(16, 519);
            this.statusText.Name = "statusText";
            this.statusText.ReadOnly = true;
            this.statusText.Size = new System.Drawing.Size(267, 20);
            this.statusText.TabIndex = 3;
            this.statusText.TabStop = false;
            this.statusText.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // callText
            // 
            this.callText.AutoSize = true;
            this.callText.BackColor = System.Drawing.SystemColors.Control;
            this.callText.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.callText.Location = new System.Drawing.Point(24, 36);
            this.callText.Name = "callText";
            this.callText.Size = new System.Drawing.Size(39, 13);
            this.callText.TabIndex = 6;
            this.callText.Text = "[None]";
            // 
            // groupBox1
            // 
            this.groupBox1.BackColor = System.Drawing.SystemColors.Control;
            this.groupBox1.Location = new System.Drawing.Point(16, 26);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(158, 130);
            this.groupBox1.TabIndex = 10;
            this.groupBox1.TabStop = false;
            // 
            // groupBox2
            // 
            this.groupBox2.BackColor = System.Drawing.SystemColors.Control;
            this.groupBox2.Location = new System.Drawing.Point(180, 26);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(103, 130);
            this.groupBox2.TabIndex = 11;
            this.groupBox2.TabStop = false;
            // 
            // loggedLabel
            // 
            this.loggedLabel.AutoSize = true;
            this.loggedLabel.Location = new System.Drawing.Point(188, 10);
            this.loggedLabel.Name = "loggedLabel";
            this.loggedLabel.Size = new System.Drawing.Size(67, 13);
            this.loggedLabel.TabIndex = 8;
            this.loggedLabel.Text = "Calls logged:";
            // 
            // callLabel
            // 
            this.callLabel.AutoSize = true;
            this.callLabel.Location = new System.Drawing.Point(24, 10);
            this.callLabel.Name = "callLabel";
            this.callLabel.Size = new System.Drawing.Size(108, 13);
            this.callLabel.TabIndex = 5;
            this.callLabel.Text = "Calls waiting for reply:";
            // 
            // loggedText
            // 
            this.loggedText.AutoSize = true;
            this.loggedText.BackColor = System.Drawing.SystemColors.Control;
            this.loggedText.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.loggedText.Location = new System.Drawing.Point(189, 36);
            this.loggedText.Name = "loggedText";
            this.loggedText.Size = new System.Drawing.Size(39, 13);
            this.loggedText.TabIndex = 9;
            this.loggedText.Text = "[None]";
            // 
            // verLabel
            // 
            this.verLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.verLabel.Location = new System.Drawing.Point(16, 542);
            this.verLabel.Name = "verLabel";
            this.verLabel.Size = new System.Drawing.Size(268, 13);
            this.verLabel.TabIndex = 0;
            this.verLabel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.verLabel.DoubleClick += new System.EventHandler(this.verLabel_DoubleClick);
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(28, 165);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(245, 13);
            this.label3.TabIndex = 17;
            this.label3.Text = "Optional: double-click a message to queue a reply.";
            this.label3.Visible = false;
            // 
            // groupBox4
            // 
            this.groupBox4.Controls.Add(this.advTextBox1);
            this.groupBox4.Controls.Add(this.advButton);
            this.groupBox4.Controls.Add(this.advTextBox2);
            this.groupBox4.Controls.Add(this.label4);
            this.groupBox4.Controls.Add(this.altPauseButton);
            this.groupBox4.Controls.Add(this.altClearButton);
            this.groupBox4.Controls.Add(this.altListBox);
            this.groupBox4.Location = new System.Drawing.Point(16, 154);
            this.groupBox4.Name = "groupBox4";
            this.groupBox4.Size = new System.Drawing.Size(267, 211);
            this.groupBox4.TabIndex = 18;
            this.groupBox4.TabStop = false;
            // 
            // advTextBox1
            // 
            this.advTextBox1.BackColor = System.Drawing.SystemColors.Control;
            this.advTextBox1.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.advTextBox1.Location = new System.Drawing.Point(41, 47);
            this.advTextBox1.Multiline = true;
            this.advTextBox1.Name = "advTextBox1";
            this.advTextBox1.Size = new System.Drawing.Size(183, 48);
            this.advTextBox1.TabIndex = 6;
            this.advTextBox1.Text = "This program can be completely automatic, you don\'t need to do anything for conti" +
    "nuous CQs. ";
            // 
            // advButton
            // 
            this.advButton.Location = new System.Drawing.Point(41, 156);
            this.advButton.Name = "advButton";
            this.advButton.Size = new System.Drawing.Size(183, 27);
            this.advButton.TabIndex = 5;
            this.advButton.Text = "Show more options";
            this.advButton.UseVisualStyleBackColor = true;
            this.advButton.Click += new System.EventHandler(this.advButton_Click);
            // 
            // advTextBox2
            // 
            this.advTextBox2.BackColor = System.Drawing.SystemColors.Control;
            this.advTextBox2.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.advTextBox2.Location = new System.Drawing.Point(41, 100);
            this.advTextBox2.Multiline = true;
            this.advTextBox2.Name = "advTextBox2";
            this.advTextBox2.Size = new System.Drawing.Size(183, 50);
            this.advTextBox2.TabIndex = 4;
            this.advTextBox2.Text = "After you\'re familiar with the basic automatic operation, you might be interested" +
    " in more options.";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(12, 25);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(249, 13);
            this.label4.TabIndex = 3;
            this.label4.Text = "Shift/double-click to reply to a \"to\" call immediately.";
            this.label4.Visible = false;
            // 
            // altPauseButton
            // 
            this.altPauseButton.Location = new System.Drawing.Point(12, 182);
            this.altPauseButton.Name = "altPauseButton";
            this.altPauseButton.Size = new System.Drawing.Size(110, 23);
            this.altPauseButton.TabIndex = 2;
            this.altPauseButton.Text = "Pause list";
            this.altPauseButton.UseVisualStyleBackColor = true;
            this.altPauseButton.Visible = false;
            this.altPauseButton.Click += new System.EventHandler(this.altPauseButton_Click);
            // 
            // altClearButton
            // 
            this.altClearButton.Location = new System.Drawing.Point(142, 182);
            this.altClearButton.Name = "altClearButton";
            this.altClearButton.Size = new System.Drawing.Size(110, 23);
            this.altClearButton.TabIndex = 1;
            this.altClearButton.Text = "Clear list";
            this.altClearButton.UseVisualStyleBackColor = true;
            this.altClearButton.Visible = false;
            this.altClearButton.Click += new System.EventHandler(this.altClearButton_Click);
            // 
            // altListBox
            // 
            this.altListBox.Font = new System.Drawing.Font("Consolas", 10F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.altListBox.FormattingEnabled = true;
            this.altListBox.ItemHeight = 15;
            this.altListBox.Location = new System.Drawing.Point(13, 41);
            this.altListBox.Name = "altListBox";
            this.altListBox.Size = new System.Drawing.Size(238, 139);
            this.altListBox.TabIndex = 0;
            this.altListBox.Visible = false;
            this.altListBox.Click += new System.EventHandler(this.altListBox_Click);
            this.altListBox.DoubleClick += new System.EventHandler(this.altListBox_DoubleClick);
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label5.Location = new System.Drawing.Point(15, 574);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(45, 16);
            this.label5.TabIndex = 21;
            this.label5.Text = "label5";
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label6.Location = new System.Drawing.Point(69, 574);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(45, 16);
            this.label6.TabIndex = 22;
            this.label6.Text = "label6";
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label7.Location = new System.Drawing.Point(155, 574);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(45, 16);
            this.label7.TabIndex = 23;
            this.label7.Text = "label7";
            // 
            // label8
            // 
            this.label8.AutoSize = true;
            this.label8.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label8.Location = new System.Drawing.Point(15, 590);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(45, 16);
            this.label8.TabIndex = 24;
            this.label8.Text = "label8";
            // 
            // label9
            // 
            this.label9.AutoSize = true;
            this.label9.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label9.Location = new System.Drawing.Point(155, 590);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size(45, 16);
            this.label9.TabIndex = 25;
            this.label9.Text = "label9";
            // 
            // label10
            // 
            this.label10.AutoSize = true;
            this.label10.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label10.Location = new System.Drawing.Point(15, 638);
            this.label10.Name = "label10";
            this.label10.Size = new System.Drawing.Size(52, 16);
            this.label10.TabIndex = 26;
            this.label10.Text = "label10";
            // 
            // label11
            // 
            this.label11.AutoSize = true;
            this.label11.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label11.Location = new System.Drawing.Point(71, 638);
            this.label11.Name = "label11";
            this.label11.Size = new System.Drawing.Size(52, 16);
            this.label11.TabIndex = 27;
            this.label11.Text = "label11";
            // 
            // label12
            // 
            this.label12.AutoSize = true;
            this.label12.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label12.Location = new System.Drawing.Point(15, 606);
            this.label12.Name = "label12";
            this.label12.Size = new System.Drawing.Size(52, 16);
            this.label12.TabIndex = 28;
            this.label12.Text = "label12";
            // 
            // label13
            // 
            this.label13.AutoSize = true;
            this.label13.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label13.Location = new System.Drawing.Point(156, 606);
            this.label13.Name = "label13";
            this.label13.Size = new System.Drawing.Size(52, 16);
            this.label13.TabIndex = 29;
            this.label13.Text = "label13";
            // 
            // label14
            // 
            this.label14.AutoSize = true;
            this.label14.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label14.Location = new System.Drawing.Point(15, 622);
            this.label14.Name = "label14";
            this.label14.Size = new System.Drawing.Size(52, 16);
            this.label14.TabIndex = 30;
            this.label14.Text = "label14";
            // 
            // label15
            // 
            this.label15.AutoSize = true;
            this.label15.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label15.Location = new System.Drawing.Point(155, 622);
            this.label15.Name = "label15";
            this.label15.Size = new System.Drawing.Size(52, 16);
            this.label15.TabIndex = 31;
            this.label15.Text = "label15";
            // 
            // label16
            // 
            this.label16.AutoSize = true;
            this.label16.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label16.Location = new System.Drawing.Point(156, 637);
            this.label16.Name = "label16";
            this.label16.Size = new System.Drawing.Size(52, 16);
            this.label16.TabIndex = 32;
            this.label16.Text = "label16";
            // 
            // label17
            // 
            this.label17.AutoSize = true;
            this.label17.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label17.Location = new System.Drawing.Point(15, 654);
            this.label17.Name = "label17";
            this.label17.Size = new System.Drawing.Size(52, 16);
            this.label17.TabIndex = 33;
            this.label17.Text = "label17";
            // 
            // verLabel2
            // 
            this.verLabel2.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.verLabel2.Location = new System.Drawing.Point(15, 556);
            this.verLabel2.Name = "verLabel2";
            this.verLabel2.Size = new System.Drawing.Size(268, 13);
            this.verLabel2.TabIndex = 34;
            this.verLabel2.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // groupBox3
            // 
            this.groupBox3.Controls.Add(this.useRR73CheckBox);
            this.groupBox3.Controls.Add(this.skipGridCheckBox);
            this.groupBox3.Controls.Add(this.logEarlyCheckBox);
            this.groupBox3.Controls.Add(this.timeoutLabel);
            this.groupBox3.Controls.Add(this.directedTextBox);
            this.groupBox3.Controls.Add(this.directedCheckBox);
            this.groupBox3.Controls.Add(this.loggedCheckBox);
            this.groupBox3.Location = new System.Drawing.Point(16, 365);
            this.groupBox3.Name = "groupBox3";
            this.groupBox3.Size = new System.Drawing.Size(267, 148);
            this.groupBox3.TabIndex = 15;
            this.groupBox3.TabStop = false;
            // 
            // useRR73CheckBox
            // 
            this.useRR73CheckBox.AutoSize = true;
            this.useRR73CheckBox.Location = new System.Drawing.Point(138, 12);
            this.useRR73CheckBox.Name = "useRR73CheckBox";
            this.useRR73CheckBox.Size = new System.Drawing.Size(98, 17);
            this.useRR73CheckBox.TabIndex = 27;
            this.useRR73CheckBox.Text = "Use RR73 msg";
            this.useRR73CheckBox.UseVisualStyleBackColor = true;
            this.useRR73CheckBox.Visible = false;
            // 
            // skipGridCheckBox
            // 
            this.skipGridCheckBox.AutoSize = true;
            this.skipGridCheckBox.Location = new System.Drawing.Point(13, 12);
            this.skipGridCheckBox.Name = "skipGridCheckBox";
            this.skipGridCheckBox.Size = new System.Drawing.Size(89, 17);
            this.skipGridCheckBox.TabIndex = 26;
            this.skipGridCheckBox.Text = "Skip grid msg";
            this.skipGridCheckBox.UseVisualStyleBackColor = true;
            this.skipGridCheckBox.Visible = false;
            // 
            // logEarlyCheckBox
            // 
            this.logEarlyCheckBox.AutoSize = true;
            this.logEarlyCheckBox.Location = new System.Drawing.Point(13, 35);
            this.logEarlyCheckBox.Name = "logEarlyCheckBox";
            this.logEarlyCheckBox.Size = new System.Drawing.Size(247, 17);
            this.logEarlyCheckBox.TabIndex = 25;
            this.logEarlyCheckBox.Text = "Log early, when sending RRR (recommended!)";
            this.logEarlyCheckBox.UseVisualStyleBackColor = true;
            this.logEarlyCheckBox.Visible = false;
            // 
            // timeoutLabel
            // 
            this.timeoutLabel.AutoSize = true;
            this.timeoutLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.timeoutLabel.Location = new System.Drawing.Point(207, 57);
            this.timeoutLabel.Name = "timeoutLabel";
            this.timeoutLabel.Size = new System.Drawing.Size(53, 13);
            this.timeoutLabel.TabIndex = 24;
            this.timeoutLabel.Text = "(now: 0)";
            // 
            // directedTextBox
            // 
            this.directedTextBox.ForeColor = System.Drawing.Color.Gray;
            this.directedTextBox.Location = new System.Drawing.Point(143, 77);
            this.directedTextBox.Name = "directedTextBox";
            this.directedTextBox.Size = new System.Drawing.Size(110, 20);
            this.directedTextBox.TabIndex = 23;
            this.directedTextBox.Text = "(separate by spaces)";
            this.directedTextBox.Visible = false;
            this.directedTextBox.Click += new System.EventHandler(this.directedTextBox_Click);
            // 
            // directedCheckBox
            // 
            this.directedCheckBox.AutoSize = true;
            this.directedCheckBox.Location = new System.Drawing.Point(13, 79);
            this.directedCheckBox.Name = "directedCheckBox";
            this.directedCheckBox.Size = new System.Drawing.Size(124, 17);
            this.directedCheckBox.TabIndex = 22;
            this.directedCheckBox.Text = "Use CQs directed to:";
            this.directedCheckBox.UseVisualStyleBackColor = true;
            this.directedCheckBox.Visible = false;
            this.directedCheckBox.CheckedChanged += new System.EventHandler(this.directedCheckBox_CheckedChanged);
            // 
            // loggedCheckBox
            // 
            this.loggedCheckBox.AutoSize = true;
            this.loggedCheckBox.Location = new System.Drawing.Point(140, 103);
            this.loggedCheckBox.Name = "loggedCheckBox";
            this.loggedCheckBox.Size = new System.Drawing.Size(117, 17);
            this.loggedCheckBox.TabIndex = 21;
            this.loggedCheckBox.Text = "Play \'logged\' sound";
            this.loggedCheckBox.UseVisualStyleBackColor = true;
            this.loggedCheckBox.CheckedChanged += new System.EventHandler(this.loggedCheckBox_CheckedChanged);
            // 
            // alertCheckBox
            // 
            this.alertCheckBox.AutoSize = true;
            this.alertCheckBox.Location = new System.Drawing.Point(29, 492);
            this.alertCheckBox.Name = "alertCheckBox";
            this.alertCheckBox.Size = new System.Drawing.Size(126, 17);
            this.alertCheckBox.TabIndex = 19;
            this.alertCheckBox.Text = "Alert CQs directed to:";
            this.alertCheckBox.UseVisualStyleBackColor = true;
            this.alertCheckBox.Visible = false;
            this.alertCheckBox.CheckedChanged += new System.EventHandler(this.alertCheckBox_CheckedChanged);
            // 
            // alertTextBox
            // 
            this.alertTextBox.ForeColor = System.Drawing.Color.Gray;
            this.alertTextBox.Location = new System.Drawing.Point(159, 490);
            this.alertTextBox.Name = "alertTextBox";
            this.alertTextBox.Size = new System.Drawing.Size(110, 20);
            this.alertTextBox.TabIndex = 20;
            this.alertTextBox.Text = "(separate by spaces)";
            this.alertTextBox.Visible = false;
            this.alertTextBox.Click += new System.EventHandler(this.alertTextBox_Click);
            // 
            // mycallCheckBox
            // 
            this.mycallCheckBox.AutoSize = true;
            this.mycallCheckBox.Location = new System.Drawing.Point(29, 468);
            this.mycallCheckBox.Name = "mycallCheckBox";
            this.mycallCheckBox.Size = new System.Drawing.Size(117, 17);
            this.mycallCheckBox.TabIndex = 7;
            this.mycallCheckBox.Text = "Play \'my call\' sound";
            this.mycallCheckBox.UseVisualStyleBackColor = true;
            this.mycallCheckBox.CheckedChanged += new System.EventHandler(this.mycallCheckBox_CheckedChanged);
            // 
            // timeoutNumUpDown
            // 
            this.timeoutNumUpDown.Location = new System.Drawing.Point(98, 420);
            this.timeoutNumUpDown.Name = "timeoutNumUpDown";
            this.timeoutNumUpDown.Size = new System.Drawing.Size(34, 20);
            this.timeoutNumUpDown.TabIndex = 12;
            this.timeoutNumUpDown.ValueChanged += new System.EventHandler(this.timeoutNumUpDown_ValueChanged);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(26, 422);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(71, 13);
            this.label1.TabIndex = 13;
            this.label1.Text = "Skip call after";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(134, 422);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(91, 13);
            this.label2.TabIndex = 14;
            this.label2.Text = "TXs w/o progress";
            // 
            // Controller
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(295, 678);
            this.Controls.Add(this.verLabel2);
            this.Controls.Add(this.label17);
            this.Controls.Add(this.label16);
            this.Controls.Add(this.label15);
            this.Controls.Add(this.label14);
            this.Controls.Add(this.label13);
            this.Controls.Add(this.label12);
            this.Controls.Add(this.label11);
            this.Controls.Add(this.label10);
            this.Controls.Add(this.label9);
            this.Controls.Add(this.label8);
            this.Controls.Add(this.label7);
            this.Controls.Add(this.label6);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.alertTextBox);
            this.Controls.Add(this.alertCheckBox);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.timeoutNumUpDown);
            this.Controls.Add(this.loggedText);
            this.Controls.Add(this.loggedLabel);
            this.Controls.Add(this.mycallCheckBox);
            this.Controls.Add(this.callText);
            this.Controls.Add(this.callLabel);
            this.Controls.Add(this.statusText);
            this.Controls.Add(this.groupBox2);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.groupBox3);
            this.Controls.Add(this.verLabel);
            this.Controls.Add(this.groupBox4);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.MaximumSize = new System.Drawing.Size(311, 717);
            this.MinimumSize = new System.Drawing.Size(311, 611);
            this.Name = "Controller";
            this.Text = "WSJT-X Controller";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.Controller_FormClosing);
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.Controller_FormClosed);
            this.Load += new System.EventHandler(this.Form_Load);
            this.groupBox4.ResumeLayout(false);
            this.groupBox4.PerformLayout();
            this.groupBox3.ResumeLayout(false);
            this.groupBox3.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.timeoutNumUpDown)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion


        public System.Windows.Forms.TextBox statusText;
        public System.Windows.Forms.Label callText;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.Label loggedLabel;
        private System.Windows.Forms.Label callLabel;
        public System.Windows.Forms.Label loggedText;
        public System.Windows.Forms.Label verLabel;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.GroupBox groupBox4;
        private System.Windows.Forms.Button altClearButton;
        public System.Windows.Forms.ListBox altListBox;
        public System.Windows.Forms.Button altPauseButton;
        private System.Windows.Forms.Label label4;
        public System.Windows.Forms.Label label5;
        public System.Windows.Forms.Label label6;
        public System.Windows.Forms.Label label7;
        public System.Windows.Forms.Label label8;
        public System.Windows.Forms.Label label9;
        public System.Windows.Forms.Label label10;
        public System.Windows.Forms.Label label11;
        public System.Windows.Forms.Label label12;
        public System.Windows.Forms.Label label13;
        public System.Windows.Forms.Label label14;
        public System.Windows.Forms.Label label15;
        public System.Windows.Forms.Label label16;
        public System.Windows.Forms.Label label17;
        public System.Windows.Forms.Label verLabel2;
        private System.Windows.Forms.Button advButton;
        private System.Windows.Forms.TextBox advTextBox2;
        private System.Windows.Forms.TextBox advTextBox1;
        private System.Windows.Forms.GroupBox groupBox3;
        public System.Windows.Forms.CheckBox useRR73CheckBox;
        public System.Windows.Forms.CheckBox skipGridCheckBox;
        public System.Windows.Forms.CheckBox logEarlyCheckBox;
        public System.Windows.Forms.Label timeoutLabel;
        public System.Windows.Forms.TextBox directedTextBox;
        public System.Windows.Forms.CheckBox directedCheckBox;
        public System.Windows.Forms.CheckBox loggedCheckBox;
        public System.Windows.Forms.CheckBox alertCheckBox;
        public System.Windows.Forms.TextBox alertTextBox;
        public System.Windows.Forms.CheckBox mycallCheckBox;
        public System.Windows.Forms.NumericUpDown timeoutNumUpDown;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
    }
}

