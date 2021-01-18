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
            this.exceptCheckBox = new System.Windows.Forms.CheckBox();
            this.exceptTextBox = new System.Windows.Forms.TextBox();
            this.replyCqCheckBox = new System.Windows.Forms.CheckBox();
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
            this.setupButton = new System.Windows.Forms.Button();
            this.addCallLabel = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.label18 = new System.Windows.Forms.Label();
            this.label19 = new System.Windows.Forms.Label();
            this.label20 = new System.Windows.Forms.Label();
            this.label21 = new System.Windows.Forms.Label();
            this.label22 = new System.Windows.Forms.Label();
            this.label23 = new System.Windows.Forms.Label();
            this.UseDirectedHelpLabel = new System.Windows.Forms.Label();
            this.AlertDirectedHelpLabel = new System.Windows.Forms.Label();
            this.LogEarlyHelpLabel = new System.Windows.Forms.Label();
            this.verLabel3 = new System.Windows.Forms.Label();
            this.AutoReplyHelpLabel = new System.Windows.Forms.Label();
            this.ExcludeHelpLabel = new System.Windows.Forms.Label();
            this.groupBox3.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.timeoutNumUpDown)).BeginInit();
            this.SuspendLayout();
            // 
            // statusText
            // 
            this.statusText.BackColor = System.Drawing.Color.Red;
            this.statusText.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.statusText.ForeColor = System.Drawing.Color.White;
            this.statusText.Location = new System.Drawing.Point(14, 334);
            this.statusText.Name = "statusText";
            this.statusText.ReadOnly = true;
            this.statusText.Size = new System.Drawing.Size(270, 20);
            this.statusText.TabIndex = 3;
            this.statusText.TabStop = false;
            this.statusText.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // callText
            // 
            this.callText.AutoSize = true;
            this.callText.BackColor = System.Drawing.SystemColors.Control;
            this.callText.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.callText.Location = new System.Drawing.Point(22, 36);
            this.callText.Name = "callText";
            this.callText.Size = new System.Drawing.Size(39, 13);
            this.callText.TabIndex = 6;
            this.callText.Text = "[None]";
            this.callText.MouseUp += new System.Windows.Forms.MouseEventHandler(this.callText_MouseUp);
            // 
            // groupBox1
            // 
            this.groupBox1.BackColor = System.Drawing.SystemColors.Control;
            this.groupBox1.Location = new System.Drawing.Point(14, 26);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(158, 130);
            this.groupBox1.TabIndex = 10;
            this.groupBox1.TabStop = false;
            // 
            // groupBox2
            // 
            this.groupBox2.BackColor = System.Drawing.SystemColors.Control;
            this.groupBox2.Location = new System.Drawing.Point(178, 26);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(106, 130);
            this.groupBox2.TabIndex = 11;
            this.groupBox2.TabStop = false;
            // 
            // loggedLabel
            // 
            this.loggedLabel.AutoSize = true;
            this.loggedLabel.Location = new System.Drawing.Point(186, 11);
            this.loggedLabel.Name = "loggedLabel";
            this.loggedLabel.Size = new System.Drawing.Size(67, 13);
            this.loggedLabel.TabIndex = 8;
            this.loggedLabel.Text = "Calls logged:";
            // 
            // callLabel
            // 
            this.callLabel.AutoSize = true;
            this.callLabel.Location = new System.Drawing.Point(22, 10);
            this.callLabel.Name = "callLabel";
            this.callLabel.Size = new System.Drawing.Size(93, 13);
            this.callLabel.TabIndex = 5;
            this.callLabel.Text = "Calls waiting reply:";
            // 
            // loggedText
            // 
            this.loggedText.AutoSize = true;
            this.loggedText.BackColor = System.Drawing.SystemColors.Control;
            this.loggedText.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.loggedText.Location = new System.Drawing.Point(187, 36);
            this.loggedText.Name = "loggedText";
            this.loggedText.Size = new System.Drawing.Size(39, 13);
            this.loggedText.TabIndex = 9;
            this.loggedText.Text = "[None]";
            // 
            // verLabel
            // 
            this.verLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.verLabel.Location = new System.Drawing.Point(14, 357);
            this.verLabel.Name = "verLabel";
            this.verLabel.Size = new System.Drawing.Size(204, 13);
            this.verLabel.TabIndex = 0;
            this.verLabel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.verLabel.DoubleClick += new System.EventHandler(this.verLabel_DoubleClick);
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label5.Location = new System.Drawing.Point(13, 389);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(41, 13);
            this.label5.TabIndex = 21;
            this.label5.Text = "label5";
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label6.Location = new System.Drawing.Point(67, 389);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(41, 13);
            this.label6.TabIndex = 22;
            this.label6.Text = "label6";
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label7.Location = new System.Drawing.Point(153, 389);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(41, 13);
            this.label7.TabIndex = 23;
            this.label7.Text = "label7";
            // 
            // label8
            // 
            this.label8.AutoSize = true;
            this.label8.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label8.Location = new System.Drawing.Point(13, 405);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(41, 13);
            this.label8.TabIndex = 24;
            this.label8.Text = "label8";
            // 
            // label9
            // 
            this.label9.AutoSize = true;
            this.label9.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label9.Location = new System.Drawing.Point(153, 405);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size(41, 13);
            this.label9.TabIndex = 25;
            this.label9.Text = "label9";
            // 
            // label10
            // 
            this.label10.AutoSize = true;
            this.label10.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label10.Location = new System.Drawing.Point(13, 453);
            this.label10.Name = "label10";
            this.label10.Size = new System.Drawing.Size(48, 13);
            this.label10.TabIndex = 26;
            this.label10.Text = "label10";
            // 
            // label11
            // 
            this.label11.AutoSize = true;
            this.label11.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label11.Location = new System.Drawing.Point(69, 453);
            this.label11.Name = "label11";
            this.label11.Size = new System.Drawing.Size(48, 13);
            this.label11.TabIndex = 27;
            this.label11.Text = "label11";
            // 
            // label12
            // 
            this.label12.AutoSize = true;
            this.label12.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label12.Location = new System.Drawing.Point(13, 421);
            this.label12.Name = "label12";
            this.label12.Size = new System.Drawing.Size(48, 13);
            this.label12.TabIndex = 28;
            this.label12.Text = "label12";
            // 
            // label13
            // 
            this.label13.AutoSize = true;
            this.label13.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label13.Location = new System.Drawing.Point(153, 421);
            this.label13.Name = "label13";
            this.label13.Size = new System.Drawing.Size(48, 13);
            this.label13.TabIndex = 29;
            this.label13.Text = "label13";
            // 
            // label14
            // 
            this.label14.AutoSize = true;
            this.label14.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label14.Location = new System.Drawing.Point(13, 437);
            this.label14.Name = "label14";
            this.label14.Size = new System.Drawing.Size(48, 13);
            this.label14.TabIndex = 30;
            this.label14.Text = "label14";
            // 
            // label15
            // 
            this.label15.AutoSize = true;
            this.label15.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label15.Location = new System.Drawing.Point(153, 437);
            this.label15.Name = "label15";
            this.label15.Size = new System.Drawing.Size(48, 13);
            this.label15.TabIndex = 31;
            this.label15.Text = "label15";
            // 
            // label16
            // 
            this.label16.AutoSize = true;
            this.label16.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label16.Location = new System.Drawing.Point(153, 453);
            this.label16.Name = "label16";
            this.label16.Size = new System.Drawing.Size(48, 13);
            this.label16.TabIndex = 32;
            this.label16.Text = "label16";
            // 
            // label17
            // 
            this.label17.AutoSize = true;
            this.label17.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label17.Location = new System.Drawing.Point(13, 517);
            this.label17.Name = "label17";
            this.label17.Size = new System.Drawing.Size(48, 13);
            this.label17.TabIndex = 33;
            this.label17.Text = "label17";
            // 
            // verLabel2
            // 
            this.verLabel2.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Underline, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.verLabel2.ForeColor = System.Drawing.Color.Blue;
            this.verLabel2.Location = new System.Drawing.Point(99, 371);
            this.verLabel2.Name = "verLabel2";
            this.verLabel2.Size = new System.Drawing.Size(122, 13);
            this.verLabel2.TabIndex = 34;
            this.verLabel2.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.verLabel2.Click += new System.EventHandler(this.verLabel2_Click);
            // 
            // groupBox3
            // 
            this.groupBox3.Controls.Add(this.exceptCheckBox);
            this.groupBox3.Controls.Add(this.exceptTextBox);
            this.groupBox3.Controls.Add(this.replyCqCheckBox);
            this.groupBox3.Controls.Add(this.useRR73CheckBox);
            this.groupBox3.Controls.Add(this.skipGridCheckBox);
            this.groupBox3.Controls.Add(this.logEarlyCheckBox);
            this.groupBox3.Controls.Add(this.timeoutLabel);
            this.groupBox3.Controls.Add(this.directedTextBox);
            this.groupBox3.Controls.Add(this.directedCheckBox);
            this.groupBox3.Controls.Add(this.loggedCheckBox);
            this.groupBox3.Location = new System.Drawing.Point(14, 155);
            this.groupBox3.Name = "groupBox3";
            this.groupBox3.Size = new System.Drawing.Size(270, 175);
            this.groupBox3.TabIndex = 15;
            this.groupBox3.TabStop = false;
            // 
            // exceptCheckBox
            // 
            this.exceptCheckBox.AutoSize = true;
            this.exceptCheckBox.Location = new System.Drawing.Point(151, 151);
            this.exceptCheckBox.Name = "exceptCheckBox";
            this.exceptCheckBox.Size = new System.Drawing.Size(49, 17);
            this.exceptCheckBox.TabIndex = 30;
            this.exceptCheckBox.Text = "Excl:";
            this.exceptCheckBox.UseVisualStyleBackColor = true;
            this.exceptCheckBox.Visible = false;
            this.exceptCheckBox.Click += new System.EventHandler(this.exceptCheckBox_Click);
            // 
            // exceptTextBox
            // 
            this.exceptTextBox.Location = new System.Drawing.Point(201, 150);
            this.exceptTextBox.Name = "exceptTextBox";
            this.exceptTextBox.Size = new System.Drawing.Size(50, 20);
            this.exceptTextBox.TabIndex = 29;
            this.exceptTextBox.Visible = false;
            // 
            // replyCqCheckBox
            // 
            this.replyCqCheckBox.AutoSize = true;
            this.replyCqCheckBox.Location = new System.Drawing.Point(10, 151);
            this.replyCqCheckBox.Name = "replyCqCheckBox";
            this.replyCqCheckBox.Size = new System.Drawing.Size(108, 17);
            this.replyCqCheckBox.TabIndex = 28;
            this.replyCqCheckBox.Text = "Auto-reply to CQs";
            this.replyCqCheckBox.UseVisualStyleBackColor = true;
            this.replyCqCheckBox.Visible = false;
            this.replyCqCheckBox.Click += new System.EventHandler(this.replyCqCheckBox_Click);
            // 
            // useRR73CheckBox
            // 
            this.useRR73CheckBox.AutoSize = true;
            this.useRR73CheckBox.Location = new System.Drawing.Point(148, 12);
            this.useRR73CheckBox.Name = "useRR73CheckBox";
            this.useRR73CheckBox.Size = new System.Drawing.Size(98, 17);
            this.useRR73CheckBox.TabIndex = 27;
            this.useRR73CheckBox.Text = "Use RR73 msg";
            this.useRR73CheckBox.UseVisualStyleBackColor = true;
            this.useRR73CheckBox.Visible = false;
            this.useRR73CheckBox.CheckedChanged += new System.EventHandler(this.useRR73CheckBox_CheckedChanged);
            // 
            // skipGridCheckBox
            // 
            this.skipGridCheckBox.AutoSize = true;
            this.skipGridCheckBox.Location = new System.Drawing.Point(9, 12);
            this.skipGridCheckBox.Name = "skipGridCheckBox";
            this.skipGridCheckBox.Size = new System.Drawing.Size(89, 17);
            this.skipGridCheckBox.TabIndex = 26;
            this.skipGridCheckBox.Text = "Skip grid msg";
            this.skipGridCheckBox.UseVisualStyleBackColor = true;
            this.skipGridCheckBox.Visible = false;
            this.skipGridCheckBox.CheckedChanged += new System.EventHandler(this.skipGridCheckBox_CheckedChanged);
            // 
            // logEarlyCheckBox
            // 
            this.logEarlyCheckBox.AutoSize = true;
            this.logEarlyCheckBox.Location = new System.Drawing.Point(9, 35);
            this.logEarlyCheckBox.Name = "logEarlyCheckBox";
            this.logEarlyCheckBox.Size = new System.Drawing.Size(230, 17);
            this.logEarlyCheckBox.TabIndex = 25;
            this.logEarlyCheckBox.Text = "Log early, at sending RRR (recommended!)";
            this.logEarlyCheckBox.UseVisualStyleBackColor = true;
            this.logEarlyCheckBox.Visible = false;
            // 
            // timeoutLabel
            // 
            this.timeoutLabel.AutoSize = true;
            this.timeoutLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.timeoutLabel.Location = new System.Drawing.Point(205, 57);
            this.timeoutLabel.Name = "timeoutLabel";
            this.timeoutLabel.Size = new System.Drawing.Size(53, 13);
            this.timeoutLabel.TabIndex = 24;
            this.timeoutLabel.Text = "(now: 0)";
            // 
            // directedTextBox
            // 
            this.directedTextBox.ForeColor = System.Drawing.Color.Gray;
            this.directedTextBox.Location = new System.Drawing.Point(150, 77);
            this.directedTextBox.Name = "directedTextBox";
            this.directedTextBox.Size = new System.Drawing.Size(100, 20);
            this.directedTextBox.TabIndex = 23;
            this.directedTextBox.Text = "(separate by spaces)";
            this.directedTextBox.Visible = false;
            this.directedTextBox.Click += new System.EventHandler(this.directedTextBox_Click);
            // 
            // directedCheckBox
            // 
            this.directedCheckBox.AutoSize = true;
            this.directedCheckBox.Location = new System.Drawing.Point(9, 79);
            this.directedCheckBox.Name = "directedCheckBox";
            this.directedCheckBox.Size = new System.Drawing.Size(108, 17);
            this.directedCheckBox.TabIndex = 22;
            this.directedCheckBox.Text = "Direct my CQs to:";
            this.directedCheckBox.UseVisualStyleBackColor = true;
            this.directedCheckBox.Visible = false;
            this.directedCheckBox.CheckedChanged += new System.EventHandler(this.directedCheckBox_CheckedChanged);
            // 
            // loggedCheckBox
            // 
            this.loggedCheckBox.AutoSize = true;
            this.loggedCheckBox.Location = new System.Drawing.Point(151, 103);
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
            this.alertCheckBox.Location = new System.Drawing.Point(24, 282);
            this.alertCheckBox.Name = "alertCheckBox";
            this.alertCheckBox.Size = new System.Drawing.Size(136, 17);
            this.alertCheckBox.TabIndex = 19;
            this.alertCheckBox.Text = "Alert for CQ directed to:";
            this.alertCheckBox.UseVisualStyleBackColor = true;
            this.alertCheckBox.Visible = false;
            this.alertCheckBox.CheckedChanged += new System.EventHandler(this.alertCheckBox_CheckedChanged);
            // 
            // alertTextBox
            // 
            this.alertTextBox.ForeColor = System.Drawing.Color.Gray;
            this.alertTextBox.Location = new System.Drawing.Point(165, 280);
            this.alertTextBox.Name = "alertTextBox";
            this.alertTextBox.Size = new System.Drawing.Size(100, 20);
            this.alertTextBox.TabIndex = 20;
            this.alertTextBox.Text = "(separate by spaces)";
            this.alertTextBox.Visible = false;
            this.alertTextBox.Click += new System.EventHandler(this.alertTextBox_Click);
            // 
            // mycallCheckBox
            // 
            this.mycallCheckBox.AutoSize = true;
            this.mycallCheckBox.Location = new System.Drawing.Point(24, 258);
            this.mycallCheckBox.Name = "mycallCheckBox";
            this.mycallCheckBox.Size = new System.Drawing.Size(117, 17);
            this.mycallCheckBox.TabIndex = 7;
            this.mycallCheckBox.Text = "Play \'my call\' sound";
            this.mycallCheckBox.UseVisualStyleBackColor = true;
            this.mycallCheckBox.CheckedChanged += new System.EventHandler(this.mycallCheckBox_CheckedChanged);
            // 
            // timeoutNumUpDown
            // 
            this.timeoutNumUpDown.Location = new System.Drawing.Point(96, 210);
            this.timeoutNumUpDown.Name = "timeoutNumUpDown";
            this.timeoutNumUpDown.Size = new System.Drawing.Size(34, 20);
            this.timeoutNumUpDown.TabIndex = 12;
            this.timeoutNumUpDown.ValueChanged += new System.EventHandler(this.timeoutNumUpDown_ValueChanged);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(20, 212);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(71, 13);
            this.label1.TabIndex = 13;
            this.label1.Text = "Skip call after";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(132, 212);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(91, 13);
            this.label2.TabIndex = 14;
            this.label2.Text = "TXs w/o progress";
            // 
            // setupButton
            // 
            this.setupButton.Location = new System.Drawing.Point(227, 357);
            this.setupButton.Name = "setupButton";
            this.setupButton.Size = new System.Drawing.Size(57, 27);
            this.setupButton.TabIndex = 35;
            this.setupButton.Text = "Setup";
            this.setupButton.UseVisualStyleBackColor = true;
            this.setupButton.Click += new System.EventHandler(this.setupButton_Click);
            // 
            // addCallLabel
            // 
            this.addCallLabel.AutoSize = true;
            this.addCallLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Underline, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.addCallLabel.ForeColor = System.Drawing.Color.Blue;
            this.addCallLabel.Location = new System.Drawing.Point(121, 11);
            this.addCallLabel.Name = "addCallLabel";
            this.addCallLabel.Size = new System.Drawing.Size(51, 13);
            this.addCallLabel.TabIndex = 36;
            this.addCallLabel.Text = "More info";
            this.addCallLabel.Click += new System.EventHandler(this.addCallLabel_Click);
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label3.Location = new System.Drawing.Point(187, 485);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(41, 13);
            this.label3.TabIndex = 40;
            this.label3.Text = "label3";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label4.Location = new System.Drawing.Point(187, 471);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(41, 13);
            this.label4.TabIndex = 39;
            this.label4.Text = "label4";
            // 
            // label18
            // 
            this.label18.AutoSize = true;
            this.label18.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label18.Location = new System.Drawing.Point(13, 485);
            this.label18.Name = "label18";
            this.label18.Size = new System.Drawing.Size(48, 13);
            this.label18.TabIndex = 38;
            this.label18.Text = "label18";
            // 
            // label19
            // 
            this.label19.AutoSize = true;
            this.label19.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label19.Location = new System.Drawing.Point(13, 469);
            this.label19.Name = "label19";
            this.label19.Size = new System.Drawing.Size(48, 13);
            this.label19.TabIndex = 37;
            this.label19.Text = "label19";
            // 
            // label20
            // 
            this.label20.AutoSize = true;
            this.label20.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label20.Location = new System.Drawing.Point(209, 501);
            this.label20.Name = "label20";
            this.label20.Size = new System.Drawing.Size(48, 13);
            this.label20.TabIndex = 42;
            this.label20.Text = "label20";
            // 
            // label21
            // 
            this.label21.AutoSize = true;
            this.label21.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label21.Location = new System.Drawing.Point(13, 501);
            this.label21.Name = "label21";
            this.label21.Size = new System.Drawing.Size(48, 13);
            this.label21.TabIndex = 41;
            this.label21.Text = "label21";
            // 
            // label22
            // 
            this.label22.AutoSize = true;
            this.label22.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label22.Location = new System.Drawing.Point(200, 517);
            this.label22.Name = "label22";
            this.label22.Size = new System.Drawing.Size(48, 13);
            this.label22.TabIndex = 43;
            this.label22.Text = "label22";
            // 
            // label23
            // 
            this.label23.AutoSize = true;
            this.label23.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label23.Location = new System.Drawing.Point(222, 389);
            this.label23.Name = "label23";
            this.label23.Size = new System.Drawing.Size(48, 13);
            this.label23.TabIndex = 44;
            this.label23.Text = "label23";
            // 
            // UseDirectedHelpLabel
            // 
            this.UseDirectedHelpLabel.AutoSize = true;
            this.UseDirectedHelpLabel.BackColor = System.Drawing.SystemColors.Control;
            this.UseDirectedHelpLabel.Font = new System.Drawing.Font("Segoe Print", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.UseDirectedHelpLabel.ForeColor = System.Drawing.Color.Blue;
            this.UseDirectedHelpLabel.Location = new System.Drawing.Point(265, 232);
            this.UseDirectedHelpLabel.Name = "UseDirectedHelpLabel";
            this.UseDirectedHelpLabel.Size = new System.Drawing.Size(15, 19);
            this.UseDirectedHelpLabel.TabIndex = 45;
            this.UseDirectedHelpLabel.Text = "?";
            this.UseDirectedHelpLabel.Visible = false;
            this.UseDirectedHelpLabel.Click += new System.EventHandler(this.UseDirectedHelpLabel_Click);
            // 
            // AlertDirectedHelpLabel
            // 
            this.AlertDirectedHelpLabel.AutoSize = true;
            this.AlertDirectedHelpLabel.BackColor = System.Drawing.SystemColors.Control;
            this.AlertDirectedHelpLabel.Font = new System.Drawing.Font("Segoe Print", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.AlertDirectedHelpLabel.ForeColor = System.Drawing.Color.Blue;
            this.AlertDirectedHelpLabel.Location = new System.Drawing.Point(265, 280);
            this.AlertDirectedHelpLabel.Name = "AlertDirectedHelpLabel";
            this.AlertDirectedHelpLabel.Size = new System.Drawing.Size(15, 19);
            this.AlertDirectedHelpLabel.TabIndex = 46;
            this.AlertDirectedHelpLabel.Text = "?";
            this.AlertDirectedHelpLabel.Visible = false;
            this.AlertDirectedHelpLabel.Click += new System.EventHandler(this.AlertDirectedHelpLabel_Click);
            // 
            // LogEarlyHelpLabel
            // 
            this.LogEarlyHelpLabel.AutoSize = true;
            this.LogEarlyHelpLabel.BackColor = System.Drawing.SystemColors.Control;
            this.LogEarlyHelpLabel.Font = new System.Drawing.Font("Segoe Print", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.LogEarlyHelpLabel.ForeColor = System.Drawing.Color.Blue;
            this.LogEarlyHelpLabel.Location = new System.Drawing.Point(265, 188);
            this.LogEarlyHelpLabel.Name = "LogEarlyHelpLabel";
            this.LogEarlyHelpLabel.Size = new System.Drawing.Size(15, 19);
            this.LogEarlyHelpLabel.TabIndex = 47;
            this.LogEarlyHelpLabel.Text = "?";
            this.LogEarlyHelpLabel.Visible = false;
            this.LogEarlyHelpLabel.Click += new System.EventHandler(this.LogEarlyHelpLabel_Click);
            // 
            // verLabel3
            // 
            this.verLabel3.Location = new System.Drawing.Point(16, 371);
            this.verLabel3.Name = "verLabel3";
            this.verLabel3.Size = new System.Drawing.Size(82, 13);
            this.verLabel3.TabIndex = 48;
            // 
            // AutoReplyHelpLabel
            // 
            this.AutoReplyHelpLabel.AutoSize = true;
            this.AutoReplyHelpLabel.BackColor = System.Drawing.SystemColors.Control;
            this.AutoReplyHelpLabel.Font = new System.Drawing.Font("Segoe Print", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.AutoReplyHelpLabel.ForeColor = System.Drawing.Color.Blue;
            this.AutoReplyHelpLabel.Location = new System.Drawing.Point(132, 304);
            this.AutoReplyHelpLabel.Name = "AutoReplyHelpLabel";
            this.AutoReplyHelpLabel.Size = new System.Drawing.Size(15, 19);
            this.AutoReplyHelpLabel.TabIndex = 49;
            this.AutoReplyHelpLabel.Text = "?";
            this.AutoReplyHelpLabel.Visible = false;
            this.AutoReplyHelpLabel.Click += new System.EventHandler(this.AutoReplyHelpLabel_Click);
            // 
            // ExcludeHelpLabel
            // 
            this.ExcludeHelpLabel.AutoSize = true;
            this.ExcludeHelpLabel.BackColor = System.Drawing.SystemColors.Control;
            this.ExcludeHelpLabel.Font = new System.Drawing.Font("Segoe Print", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.ExcludeHelpLabel.ForeColor = System.Drawing.Color.Blue;
            this.ExcludeHelpLabel.Location = new System.Drawing.Point(266, 305);
            this.ExcludeHelpLabel.Name = "ExcludeHelpLabel";
            this.ExcludeHelpLabel.Size = new System.Drawing.Size(15, 19);
            this.ExcludeHelpLabel.TabIndex = 50;
            this.ExcludeHelpLabel.Text = "?";
            this.ExcludeHelpLabel.Visible = false;
            this.ExcludeHelpLabel.Click += new System.EventHandler(this.ExcludeHelpLabel_Click);
            // 
            // Controller
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(298, 536);
            this.Controls.Add(this.AutoReplyHelpLabel);
            this.Controls.Add(this.ExcludeHelpLabel);
            this.Controls.Add(this.verLabel3);
            this.Controls.Add(this.LogEarlyHelpLabel);
            this.Controls.Add(this.AlertDirectedHelpLabel);
            this.Controls.Add(this.UseDirectedHelpLabel);
            this.Controls.Add(this.label23);
            this.Controls.Add(this.label22);
            this.Controls.Add(this.label20);
            this.Controls.Add(this.label21);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.label18);
            this.Controls.Add(this.label19);
            this.Controls.Add(this.addCallLabel);
            this.Controls.Add(this.setupButton);
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
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.MaximumSize = new System.Drawing.Size(314, 575);
            this.MinimumSize = new System.Drawing.Size(314, 429);
            this.Name = "Controller";
            this.Text = "WSJT-X Controller";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.Controller_FormClosing);
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.Controller_FormClosed);
            this.Load += new System.EventHandler(this.Form_Load);
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
        private System.Windows.Forms.Button setupButton;
        public System.Windows.Forms.Label addCallLabel;
        public System.Windows.Forms.Label label3;
        public System.Windows.Forms.Label label4;
        public System.Windows.Forms.Label label18;
        public System.Windows.Forms.Label label19;
        public System.Windows.Forms.Label label20;
        public System.Windows.Forms.Label label21;
        public System.Windows.Forms.Label label22;
        public System.Windows.Forms.Label label23;
        public System.Windows.Forms.Label UseDirectedHelpLabel;
        public System.Windows.Forms.Label AlertDirectedHelpLabel;
        public System.Windows.Forms.Label LogEarlyHelpLabel;
        public System.Windows.Forms.Label verLabel3;
        public System.Windows.Forms.CheckBox replyCqCheckBox;
        public System.Windows.Forms.Label AutoReplyHelpLabel;
        public System.Windows.Forms.CheckBox exceptCheckBox;
        public System.Windows.Forms.TextBox exceptTextBox;
        public System.Windows.Forms.Label ExcludeHelpLabel;
    }
}

