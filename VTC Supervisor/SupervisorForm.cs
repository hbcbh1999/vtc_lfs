using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace VTC_Supervisor
{
    public partial class SupervisorForm : Form
    {
        public SupervisorForm()
        {
            InitializeComponent();
        }

        private void startButton_Click(object sender, EventArgs e)
        {
            SupervisorProcess();
            checkProcessTimer.Start();
        }

        private void stopButton_Click(object sender, EventArgs e)
        {
            checkProcessTimer.Stop();
        }

        private void checkProcessTimer_Tick(object sender, EventArgs e)
        {
            SupervisorProcess();
        }

        private void SupervisorProcess()
        {
            //Count running instances
            var runningInstanceCount = Process.GetProcessesByName("VTC").Length;

            //Count desired instances
            var desiredInstanceCount = 0;
            if (ipcamera1Checkbox.Checked) desiredInstanceCount++;
            if (ipcamera2Checkbox.Checked) desiredInstanceCount++;
            if (ipcamera3Checkbox.Checked) desiredInstanceCount++;

            //If number of running instances is correct, do nothing
            if (runningInstanceCount >= desiredInstanceCount)
            {
                return;
            }

            //Terminate all instances
            foreach (var p in Process.GetProcessesByName("VTC"))
            {
                p.CloseMainWindow();
                p.Close();
            }

            //Re-launch instances
            if (ipcamera1Checkbox.Checked)
            {
                Process.Start("VTC.exe","IP1");
            }

            if (ipcamera2Checkbox.Checked)
            {
                Process.Start("VTC.exe","IP2");
            }

            if (ipcamera3Checkbox.Checked)
            {
                Process.Start("VTC.exe","IP3");
            }
        }
    }
}
