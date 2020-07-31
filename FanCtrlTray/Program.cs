using FanCtrlCommon;
using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.ServiceModel;
using System.Windows.Forms;

namespace FanCtrlTray
{
    class Program
    {
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        extern static bool DestroyIcon(IntPtr handle);

        const int UpdateTime = 1000;
        const int CycleTime = 50;
        const int CyclesToWaste = (UpdateTime / CycleTime) - 1;

        static bool run = true;

        static void Main(string[] args)
        {
            ChannelFactory<IFanCtrlInterface> pipeFactory = new ChannelFactory<IFanCtrlInterface>(new NetNamedPipeBinding(NetNamedPipeSecurityMode.None), new EndpointAddress("net.pipe://localhost/FanCtrlInterface"));
            IFanCtrlInterface interf = null;

            ContextMenuStrip strip = new ContextMenuStrip();

            strip.Items.Add("Exit");
            strip.ItemClicked += Strip_ItemClicked;

            NotifyIcon icon = new NotifyIcon();
            icon.Text = "FanCtrl";
            icon.Visible = true;
            icon.ContextMenuStrip = strip;

            Pen[] pens = new Pen[] { new Pen(Color.Green), new Pen(Color.White), new Pen(Color.Red) };
            SolidBrush brush = new SolidBrush(Color.White);
            Font font = new Font("Tahoma", 8);
            Bitmap bitmap = new Bitmap(16, 16);
            Graphics graph = Graphics.FromImage(bitmap);
            SizeF txtSize;
            string txt;
            byte fanlvl;
            FanCtrlData d;

            uint counter = 0;

            while (run)
            {
                if (counter == 0)
                {
                    try
                    {
                        if (interf == null)
                            interf = pipeFactory.CreateChannel();

                        d = interf.GetData();

                        if (d.SystemTemperature < 100)
                            txt = d.SystemTemperature.ToString();
                        else
                            txt = "!";

                        fanlvl = Math.Min((byte)d.FanLevel, (byte)2);
                    }
                    catch (Exception)
                    {
                        txt = "?";
                        fanlvl = 2;
                        interf = null;
                    }

                    graph.Clear(Color.Transparent);
                    graph.DrawRectangle(pens[fanlvl], 0, 0, 15, 15);
                    txtSize = graph.MeasureString(txt, font);
                    graph.DrawString(txt, font, brush, 8 - txtSize.Width / 2, 8 - txtSize.Height / 2);

                    icon.Icon = Icon.FromHandle(bitmap.GetHicon());
                    DestroyIcon(icon.Icon.Handle);

                    counter = CyclesToWaste;
                }
                else
                {
                    counter--;
                }

                Application.DoEvents();
                System.Threading.Thread.Sleep(CycleTime);
            }

            icon.Visible = false;

            try
            {
                pipeFactory.Close();
            }
            catch(Exception)
            {

            }
        }

        private static void Strip_ItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {
            run = false;
        }
    }
}
