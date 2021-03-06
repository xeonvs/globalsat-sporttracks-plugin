/*
Copyright (C) 2010 Zone Five Software

This library is free software; you can redistribute it and/or
modify it under the terms of the GNU Lesser General Public
License as published by the Free Software Foundation; either
version 2 of the License, or (at your option) any later version.

This library is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
Lesser General Public License for more details.

You should have received a copy of the GNU Lesser General Public
License along with this library. If not, see <http://www.gnu.org/licenses/>.
 */
// Author: Aaron Averill


using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Text;
using System.Windows.Forms;

using ZoneFiveSoftware.Common.Visuals;

namespace ZoneFiveSoftware.SportTracks.Device.Globalsat
{
    public partial class DeviceConfigurationDlg : Form
    {
        public DeviceConfigurationDlg(FitnessDevice_Globalsat fitnessDevice) :
            this(fitnessDevice, false)
        {
        }
        public DeviceConfigurationDlg(FitnessDevice_Globalsat fitnessDevice, bool detect)
        {
            InitializeComponent();

            this.fitnessDevice = fitnessDevice;
            Text = CommonResources.Text.Devices.ConfigurationDialog_Title;
            this.chkImportDistance.Text = Properties.Resources.DeviceConfigurationDlg_chkImportDistance;
            this.chkImportOnlyNew.Text = Properties.Resources.DeviceConfigurationDlg_chkImportOnlyNew_Text;
            this.optDetectPausesOff.Text = CommonResources.Text.LabelNone;
            this.optDetectPausesFromSpeed.Text = Properties.Resources.DeviceConfigurationDlg_optDetectPausesFromSpeed;
            this.optDetectPausesFromSpeedCadence.Text = Properties.Resources.DeviceConfigurationDlg_optDetectPausesFromSpeedCadence;
            labelHoursOffset.Text = CommonResources.Text.Devices.ConfigurationDialog_HoursOffsetLabel_Text;
            btnOk.Text = CommonResources.Text.ActionOk;
            btnCancel.Text = CommonResources.Text.ActionCancel;

            if (Plugin.Instance.Application != null)
            {
                ThemeChanged(Plugin.Instance.Application.VisualTheme);
            }

            txtHoursOffset.Validated += new EventHandler(TxtHoursOffset_Validated);
            btnOk.Click += new EventHandler(BtnOk_Click);
            btnCancel.Click += new EventHandler(BtnCancel_Click);
            this.chkImportOnlyNew.Checked = this.fitnessDevice.configInfo.ImportOnlyNew;
            this.chkImportDistance.Checked = this.fitnessDevice.configInfo.ImportSpeedDistanceTrack;
            switch (this.fitnessDevice.configInfo.DetectPauses)
            {
                case 1:
                    //detect pause from speed
                    this.optDetectPausesFromSpeed.Checked = true;
                    break;
                case 2:
                    //detect pause from speed and cadance track
                    this.optDetectPausesFromSpeedCadence.Checked = true;
                    break;
                case 0:
                default:
                    //no pause detect
                    this.optDetectPausesOff.Checked = true;
                    break;
            }
            this.txtHoursOffset.Text = this.fitnessDevice.configInfo.HoursAdjustment.ToString();
            this.buttonDetect.CenterImage = ZoneFiveSoftware.Common.Visuals.CommonResources.Images.Refresh16;
            this.buttonDetect.Text = "";// ZoneFiveSoftware.Common.Visuals.CommonResources.Text.ActionRefresh;
            this.labelDetect.Text = "";
            if (detect)
            {
                this.VisibleChanged += DeviceConfigurationDlg_VisibleChanged;
            }

            this.buttonDelete.CenterImage = ZoneFiveSoftware.Common.Visuals.CommonResources.Images.Delete16;
            this.buttonDelete.Text = "";
            this.dateTimePickerOldest.Visible = false;
            GlobalsatProtocol device = this.fitnessDevice.Device();
            if (device is GlobalsatProtocol2)
            {
                //this.labelRemainingTime.Text = "Click to get remaining time";
                LabelRemainingTime_Click();
                this.labelDelete.Text = Properties.Resources.DeviceConfigurationDlg_DeleteAllActivities;
                DateTime oldest = DateTime.Now - TimeSpan.FromDays(31);
                this.dateTimePickerOldest.Value = oldest;
            }
            else
            {
                this.labelRemainingTime.Visible = false;
                this.buttonDelete.Visible = false;
                this.labelDelete.Visible = false;
                if (fitnessDevice is FitnessDevice_GH561)
                {
                    this.labelHoursOffset.Visible = false;
                    this.txtHoursOffset.Visible = false;
                    this.chkImportOnlyNew.Visible = false;
                    this.chkImportDistance.Visible = false;
                    this.groupBoxPauseDetect.Visible = false;
                }
            }
            //Device Configuration
            this.buttonImportDeviceConfig.LeftImage = CommonResources.Images.Import16;
            this.buttonExportDeviceConfig.LeftImage = CommonResources.Images.Export16;

            this.buttonImportDeviceConfig.Text = Properties.Resources.UI_Settings_ImportConfigButton_Text;
            this.buttonExportDeviceConfig.Text = Properties.Resources.UI_Settings_ExportConfigButton_Text;
            this.labelStatus.Text = "";

            this.groupBoxDeviceConfig.Text = Properties.Resources.UI_Settings_DeviceConfiguration_Title;

            //Screen capture
            if (fitnessDevice is FitnessDevice_GB1000)
            {
                groupBoxScreenCapture.Enabled = false;
            }
            this.buttonCaptureScreen.CenterImage = CommonResources.Images.Refresh16;
            this.buttonSave.CenterImage = CommonResources.Images.Save16;

            this.buttonSave.Enabled = false;
            this.pictureBox1.Left = buttonCaptureScreen.Right + 8;
        }

        #region Public properties

 
        internal DeviceConfigurationInfo ConfigurationInfo
        {
            get
            {
                this.fitnessDevice.configInfo.ImportOnlyNew = this.chkImportOnlyNew.Checked;
                this.fitnessDevice.configInfo.ImportSpeedDistanceTrack = this.chkImportDistance.Checked;
                //The Detect Pauses is read from defaults and set at changes
                //this.fitnessDevice.configInfo.DetectPauses
                return this.fitnessDevice.configInfo;
            }
        }
        #endregion

        #region Public methods
        public void ThemeChanged(ITheme visualTheme)
        {
            theme = visualTheme;
            labelHoursOffset.ForeColor = visualTheme.ControlText;
            txtHoursOffset.ThemeChanged(visualTheme);
            this.chkImportOnlyNew.ForeColor = visualTheme.ControlText;
            this.chkImportDistance.ForeColor = visualTheme.ControlText;
            this.optDetectPausesFromSpeed.ForeColor = visualTheme.ControlText;
            this.optDetectPausesFromSpeedCadence.ForeColor = visualTheme.ControlText;
            this.optDetectPausesOff.ForeColor = visualTheme.ControlText;
            BackColor = visualTheme.Control;
        }
        #endregion

        #region Event handlers
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
#if ST_2_1
            MessageDialog.DrawButtonRowBackground(e.Graphics, this.ClientRectangle, theme);
#else
            MessageDialog.DrawButtonRowBackground(e.Graphics, this, theme);
#endif
        }

        void TxtHoursOffset_Validated(object sender, EventArgs e)
        {
            try
            {
                if (txtHoursOffset.Text.Trim().Length == 0)
                {
                    this.fitnessDevice.configInfo.HoursAdjustment = 0;
                }
                else
                {
                    this.fitnessDevice.configInfo.HoursAdjustment = (int)double.Parse(txtHoursOffset.Text);
                }
            }
            catch { }
            txtHoursOffset.Text = this.fitnessDevice.configInfo.HoursAdjustment.ToString();
        }

        void BtnOk_Click(object sender, EventArgs e)
        {
            this.DialogResult = btnOk.DialogResult;
            Close();
        }

        void BtnCancel_Click(object sender, EventArgs e)
        {
            this.DialogResult = btnCancel.DialogResult;
            Close();
        }

        #endregion

        #region Private methods

        #endregion

        private void DeviceConfigurationDlg_VisibleChanged(object sender, EventArgs e)
        {
            if (this.Visible == true)
            {
                this.ButtonDetect_Click(sender, e);
            }
        }

        private void ButtonDetect_Click(object sender, EventArgs e)
        {
            this.labelDetect.Text = this.fitnessDevice.Detect();
        }

        private void ButtonCaptureScreen_Click(object sender, EventArgs e)
        {
            JobMonitor jobMonitor = new JobMonitor();
            try
            {
                GlobalsatProtocol device2 = this.fitnessDevice.Device();
                if (device2 != null)
                {
                    Bitmap screenshot = device2.GetScreenshot(jobMonitor);

                    if (screenshot != null)
                    {
                        this.Height += screenshot.Height - pictureBox1.Height;
                        this.groupBoxScreenCapture.Height += screenshot.Height - pictureBox1.Height;
                        this.pictureBox1.ClientSize = new Size(screenshot.Width, screenshot.Height);
                        this.pictureBox1.Image = screenshot;
                        this.Refresh();

                        this.buttonSave.Enabled = true;
                    }
                }
            }
            catch (Exception ex)
            {
                jobMonitor.ErrorText += ex.Message;
            }
            if (!string.IsNullOrEmpty(jobMonitor.ErrorText))
            {
                MessageDialog.Show(jobMonitor.ErrorText, Properties.Resources.UI_Settings_ScreenCapture_Error, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }


        private void ButtonSave_Click(object sender, EventArgs e)
        {
            System.Windows.Forms.SaveFileDialog saveFileDialog1 = new System.Windows.Forms.SaveFileDialog()
            {
                Filter = "Bitmap (*.bmp)|*.bmp|JPEG (*.jpg)|*.jpg|PNG (*.png)|*.png",
                FileName = this.fitnessDevice.Device().devId
            };
            if (saveFileDialog1.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    if (pictureBox1.Image != null)
                    {
                        System.Drawing.Imaging.ImageFormat format = System.Drawing.Imaging.ImageFormat.Bmp;
                        if (saveFileDialog1.FilterIndex == 1)
                        {
                            format = System.Drawing.Imaging.ImageFormat.Jpeg;
                        }
                        else if (saveFileDialog1.FilterIndex == 2)
                        {
                            format = System.Drawing.Imaging.ImageFormat.Png;
                        }
                        pictureBox1.Image.Save(saveFileDialog1.FileName, format);
                    }
                }
                catch (Exception ex)
                {
                    MessageDialog.Show(ex.Message, Properties.Resources.UI_Settings_ScreenCaptureSave_Error, MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
            }
        }

        private void ButtonImportDeviceConfig_Click(object sender, EventArgs e)
        {

            labelStatus.Text = "";
            System.Windows.Forms.OpenFileDialog openFileDialog1 = new System.Windows.Forms.OpenFileDialog();;
            openFileDialog1.Filter = "Configuration Files (*.cfg)|*.cfg";
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                GlobalsatDeviceConfiguration importedDeviceConfig = null;

                try
                {
                    importedDeviceConfig = GlobalsatDeviceConfiguration.Load(openFileDialog1.FileName);
                }
                catch (Exception)
                {
                    importedDeviceConfig = null;
                }
                if (importedDeviceConfig == null || importedDeviceConfig.SystemConfigDataRaw == null)
                {
                    MessageDialog.Show(Properties.Resources.UI_Settings_ImportConfig_InvalidConfiguration, Properties.Resources.UI_Settings_ImportConfig_Error, MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                JobMonitor jobMonitor = new JobMonitor();
                try
                {
                    GlobalsatProtocol device2 = this.fitnessDevice.Device();
                    if (device2 != null)
                    {
                        GlobalsatDeviceConfiguration currentDeviceConfig = device2.GetSystemConfiguration2(jobMonitor);

                        if (importedDeviceConfig != null && currentDeviceConfig != null && 
                            importedDeviceConfig.DeviceName == currentDeviceConfig.DeviceName && 
                            importedDeviceConfig.SystemConfigDataRaw.Length == currentDeviceConfig.SystemConfigDataRaw.Length)
                        {
                            device2.SetSystemConfiguration2(importedDeviceConfig, jobMonitor);
                            labelStatus.Text = CommonResources.Text.Devices.ImportJob_Status_ImportComplete;
                        }
                        else
                        {
                            labelStatus.Text = Properties.Resources.Device_OpenDevice_Error + "Received " + importedDeviceConfig.SystemConfigDataRaw.Length + " bytes ";
                            //MessageDialog.Show(Properties.Resources.UI_Settings_ImportConfig_InvalidConfiguration, Properties.Resources.UI_Settings_ImportConfig_Error, MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                    else
                    {
                        labelStatus.Text = Properties.Resources.Device_OpenDevice_Error;
                    }
                }
                catch (Exception ex)
                {
                    jobMonitor.ErrorText += ex.Message;
                }
                if(!string.IsNullOrEmpty(jobMonitor.ErrorText))
                {
                    MessageDialog.Show(jobMonitor.ErrorText, Properties.Resources.UI_Settings_ImportConfig_Error, MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void ButtonExportDeviceConfig_Click(object sender, EventArgs e)
        {
            labelStatus.Text = "";
            try
            {
                GlobalsatProtocol device2 = this.fitnessDevice.Device();
                GlobalsatDeviceConfiguration currentDeviceConfig = null;
                JobMonitor jobMonitor = new JobMonitor();

                if (device2 != null)
                {
                    currentDeviceConfig = device2.GetSystemConfiguration2(jobMonitor);
                }
                if (!string.IsNullOrEmpty(jobMonitor.ErrorText))
                {
                    MessageDialog.Show(jobMonitor.ErrorText, Properties.Resources.UI_Settings_ExportConfig_Error, MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                if (currentDeviceConfig != null)
                {
                    System.Windows.Forms.SaveFileDialog saveFileDialog1 = new System.Windows.Forms.SaveFileDialog()
                    {
                        Filter = "Configuration Files (*.cfg)|*.cfg",
                        FileName = currentDeviceConfig.DeviceName
                    };
                    if (saveFileDialog1.ShowDialog() == DialogResult.OK)
                    {
                        currentDeviceConfig.Save(saveFileDialog1.FileName);

                        labelStatus.Text = CommonResources.Text.MessageExportComplete;
                        //MessageDialog.Show(CommonResources.Text.MessageExportComplete, Properties.Resources.UI_Settings_ExportConfigButton_Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
                else
                {
                    labelStatus.Text = Properties.Resources.Device_OpenDevice_Error;
                }
            }
            catch (Exception ex)
            {
                MessageDialog.Show(ex.Message, Properties.Resources.UI_Settings_ExportConfig_Error, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        #region Private members
        private ITheme theme;
        private FitnessDevice_Globalsat fitnessDevice;
        #endregion

        private void LabelRemainingTime_Click(object sender, EventArgs e)
        {
            LabelRemainingTime_Click();
        }

        private void LabelRemainingTime_Click()
        {
            GlobalsatProtocol device = this.fitnessDevice.Device();
            if (device is GlobalsatProtocol2)
            {
                GlobalsatProtocol2 device2 = device as GlobalsatProtocol2;
                JobMonitor jobMonitor = new JobMonitor();
                TimeSpan time = device2.GetRemainingTime(jobMonitor);

                if (!string.IsNullOrEmpty(jobMonitor.ErrorText))
                {
                    this.labelRemainingTime.Text = "Remaining time: " + jobMonitor.ErrorText;
                    //MessageDialog.Show(jobMonitor.ErrorText, "", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Information);
                }
                else if (time <= TimeSpan.MinValue)
                {
                    MessageDialog.Show(Properties.Resources.Device_Unsupported, "", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Information);
                }
                else
                {
                    this.labelRemainingTime.Text = "Remaining time: " + time;
                }
            }
        }

        private void ButtonDelete_Click(object sender, EventArgs e)
        {
            GlobalsatProtocol device = this.fitnessDevice.Device();
            //device should already be checked, no error
            if (device is GlobalsatProtocol2)
            {
                //Delete date time not fully working, keep structure for now
                DateTime oldest = DateTime.MaxValue; //this.dateTimePickerOldest.Value.ToLocalTime().ToShortDateString()
                //string msg = string.Format("Are you sure you want to delete all activities older than {0}?", this.dateTimePickerOldest.Value.ToLocalTime().ToShortDateString());
                string msg = string.Format("Are you sure you want to delete all device activities?");
                if (MessageDialog.Show(msg, "", System.Windows.Forms.MessageBoxButtons.OKCancel, System.Windows.Forms.MessageBoxIcon.Question) == DialogResult.OK)
                {
                    GlobalsatProtocol2 device2 = device as GlobalsatProtocol2;
                    JobMonitor jobMonitor = new JobMonitor();
                    int n = device2.DeleteTracks(oldest, jobMonitor);
                    if (!string.IsNullOrEmpty(jobMonitor.ErrorText))
                    {
                        msg = jobMonitor.ErrorText;
                    }
                    else if (n >= 0)
                    {
                        msg = string.Format("Deleted {0} activities", n);
                    }
                    else
                    {
                        msg = string.Format("Failed to delete activities");
                    }
                    MessageDialog.Show(msg, "", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Information);
                }
            }
        }

        private void optDetectPausesFromSpeedCadence_CheckedChanged(object sender, EventArgs e)
        {
            if ( ((RadioButton)sender).Checked )
            {
                this.fitnessDevice.configInfo.DetectPauses = 2;
            }
        }

        private void optDetectPausesFromSpeed_CheckedChanged(object sender, EventArgs e)
        {
            if (((RadioButton)sender).Checked)
            {
                this.fitnessDevice.configInfo.DetectPauses = 1;
            }
        }

        private void optDetectPausesOff_CheckedChanged(object sender, EventArgs e)
        {
            if (((RadioButton)sender).Checked)
            {
                this.fitnessDevice.configInfo.DetectPauses = 0;
            }
        }
    }
}