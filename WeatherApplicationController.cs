using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Drawing;
using Microsoft.VisualBasic.ApplicationServices;

namespace UVIndicator2
{
    public class ApplicationController : WindowsFormsApplicationBase
    {
        private WeatherForm mainForm;
        public ApplicationController(WeatherForm form)
        {
            //We keep a reference to main form 
            //To run and also use it when we need to bring to front
            mainForm = form;
            this.IsSingleInstance = true;
            this.StartupNextInstance += this_StartupNextInstance;
        }

        void this_StartupNextInstance(object sender, StartupNextInstanceEventArgs e)
        {
            //Here we bring application to front
            e.BringToForeground = true;
            mainForm.ShowInTaskbar = true;
            mainForm.WindowState = FormWindowState.Minimized;
            mainForm.Show();
            mainForm.WindowState = FormWindowState.Normal;
        }

        protected override void OnCreateMainForm()
        {
            this.MainForm = mainForm;
        }
    }
}
