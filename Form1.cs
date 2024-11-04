using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Net.NetworkInformation;
using System.Reflection.Emit;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Windows.Forms;
using System.Windows.Forms.VisualStyles;
using System.Xml;
using static System.Net.Mime.MediaTypeNames;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TaskbarClock;

namespace SerialTerminal
{
    public partial class SerialTerminal : Form
    {
        public SerialPort myPort;
        private DateTime dateTime;
        private string com = "COM4";
        private string title = "Serial Console";
        SaveFileDialog SaveFileDialog1;
        MemoryStream userInput = new MemoryStream();
        List<List<string>> gpsgroup = new List<List<string>>();
        bool allowAutoScroll = true;
        string scroll_mode="auto scroll";
        Color Current_Color;

        public SerialTerminal()
        {
            InitializeComponent();
            richTextBox1.BackColor = Color.Black;
            richTextBox1.ForeColor = Color.White;
            richTextBox2.BackColor = Color.Black;
            richTextBox2.ForeColor = Color.White;
            button1.Text = "Open";
            button2.Text = "Close";
            GetSerialPorts();
            SaveFileDialog1 = new SaveFileDialog();
            Richbox_show_logo();
            gpsgroup_init();
        }

        public void GetSerialPorts()
        {
            List<string> portNames = new List<string>();
            string[] port = System.IO.Ports.SerialPort.GetPortNames();
            foreach(string portName in port)
            {
                comboBox1.Items.Add(portName);
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            myPort = new SerialPort();
            myPort.BaudRate = 115200;
            myPort.PortName = com;
            myPort.Parity = Parity.None;
            myPort.DataBits = 8;
            myPort.StopBits = StopBits.One;
            myPort.DataReceived += MyPort_DataReceived;

            button1.BackColor = Color.LightGray;

            Richbox_clear_logo();

            try
            {
                show_title_info();
                myPort.Open();
                //test_gps();
            }
            catch (Exception exc)
            {
                MessageBox.Show(exc.Message, "Errore");
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            try
            {
                myPort.Close();
            }
            catch (Exception exc4)
            {
                MessageBox.Show(exc4.Message, "Error");
            }
        }

        private void Richbox_show_logo()
        {
            richTextBox1.Font = new Font("Consolas", 30);
            richTextBox1.Text = "\n\n\n" + "Serial Console";
            richTextBox1.SelectAll();
            richTextBox1.SelectionAlignment = HorizontalAlignment.Center;
            richTextBox1.Select(0, 0);
        }
        private void Richbox_clear_logo()
        {
            richTextBox1.Font = new Font("Consolas", 10);
            richTextBox1.Text = "";
            richTextBox1.SelectAll();
            richTextBox1.SelectionAlignment = HorizontalAlignment.Left;
        }

        void MyPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            var inData = myPort.ReadExisting(); // Use local variable

            Invoke(new Action<string>(displayData_event), new object[] { inData });
        }


        private void displayData_event(string existing_data)
        {

            System.Drawing.Color mycolor = Color.White;
            dateTime = DateTime.Now;
            string time = dateTime.ToString("HH:mm:ss");
            string input_data = "";
            input_data = existing_data;

            for (int linenumber = 0; linenumber < 256; linenumber++)
            {
                string data = Readline(input_data, linenumber);

                if (data == null) break;
                if (data.Equals("\n") || data.Equals(" ")) continue;

                Ansi_Coloring(time,data);
                //  Google, Reset 문자열을 Summary창에 보여준다
                ETCSummary(time, data);
                //  GPS CN0를 읽어 Summary창에 보여준다
                PRNSummary(time, data);
                // input_data에 여러 라인이 있을 경우,
                // 읽은 라인은 제거하고 next 라인을 읽는다

            }
        }

        private void PRNSummary(string time, string log)
        {

            if (log.Contains("PRN:"))
            {
                // PRN이 나타나면 CN0를 추출하기 시작하고
                string prn = "";
                string cn0 = "";
                prn = Get_gps_token_value(log, "PRN:");
                cn0 = Get_gps_token_value(log, "C/N0:");
                gpsgroup_Addcn0(prn, cn0);
            }
            else if (log.Contains("location_core_cancel") || log.Contains("Location method timeout")
                || log.Contains("Method specific timeout expired") || log.Contains("Location acquired successfully"))
            {
                // gps종료조건을 만나면 추출된 CN0를 보여준다 
                show_gpsgroup();
                gpsgroup.Clear();
                gpsgroup_init();
            }

        }
        private void ETCSummary( string time, string log)
        {
            if (log.Contains("location_core_timer_start"))
            {
                richTextBox2.AppendText(time + "\n");
            }

            if (log.Contains("Google"))
            {
                // 좌표를 잡으면 보여준다. 이 때 기록된 cn0값의 강도를 점검한다
                int index = log.IndexOf("Google");
                int length = log.Length;
                string position = log.Substring(index, length - index);
                richTextBox2.AppendText(position);
            }

            if (log.Contains("RESTREAS"))
            {
                // 리셋이 발생하면 빨간색으로 표시해준다
                richTextBox2.SelectionColor = Color.Red;                
                richTextBox2.AppendText(log);
            }
            /*
            if (log.Contains("SMS") || log.Contains("sms"))
            {
                // SMS 수신하면 빨간색으로 표시해준다
                richTextBox2.SelectionColor = Color.Yellow;
                richTextBox2.AppendText(log);
            }
            */

        }

        private void test_gps()
        {

            string test;
            test = "[00:03:34.203,521] <dbg> location: method_gnss_print_pvt: PRN:  31, C/N0: 25.3, in fix: 0, unhealthy: 0\r\n";
            //test = "[00:00:00.389,038] <err> main: RESTREAS : 0";
            //test = "[00:04:43.760,437] <dbg> location: location_core_event_cb_fn:   Google maps URL: https://maps.google.com/?q=37.414010,126.976831";
            //test = "Google maps URL: https://maps.google.com/?q=37.414010,126.976831\r\n";

            for (int i =0;i<20;i++)
            {
                string prn = "";
                string cn0 = "";
                prn = Get_gps_token_value(test, "PRN:");
                cn0 = Get_gps_token_value(test, "C/N0:");
                if(prn != null) gpsgroup_Addcn0(prn, cn0);
            }
            if (test.Contains("Google"))
            {

                int index = test.IndexOf("Google");
                int length = test.Length;
                string position = test.Substring(index, length-index);
                richTextBox2.AppendText(position);
            }
            else if (test.Contains("RESTREAS"))
            {
                richTextBox2.SelectionColor = Color.Red;
                richTextBox2.AppendText(test);
                //richTextBox2.SelectionColor = Color.Black;
            }
            show_gpsgroup();
        }

        private void gpsgroup_Addcn0(string prn, string cn0)
        {
            // gpsgroup의 구조는 아래와 같다
            //prn_value,cn0,cn0,cn0....
            int i = 0;
            for ( i=0; i < gpsgroup.Count; i++)
            {
                if (gpsgroup[i][0].Equals(prn))
                {
                    // gpsgroup에 prn이 있으면 cn0만 추가
                    gpsgroup[i].Add(cn0);
                    //show_gpsgroup();
                    return;
                } 
            }

            // gpsgroup에 prn이 없으면 새로 추가
            List<string> new_prn = new List<string>();
            new_prn.Add(prn);
            new_prn.Add(cn0);
            gpsgroup.Add(new_prn);

            //richTextBox2.AppendText("add prn" + prn +"\n");
            return;

        }

        private void gpsgroup_init()
        {
            List<string> new_prn = new List<string>();
            new_prn.Add("");
            new_prn.Add("");
            gpsgroup.Add(new_prn);
        }

        private void show_gpsgroup()
        {
            int j = 0;
            float average_cn0 = 0;
            int cn0_count = 0;
            // gpsgroup : 3, 36.6, 24.7, 23.9, 24.6
            // PRN : 3, C/N0 : 26.6, 24.7, 23.9, 24.6,  >> average : 0
            richTextBox2.SelectionColor = Color.Black;
            for (int i = 0; i< gpsgroup.Count; i++)
            {
                string sum_string=""; 
                for ( j = 0;  j < gpsgroup[i].Count ; j++)
                {
                    if(j==0)
                    {
                        sum_string += "PRN : ";
                        sum_string += gpsgroup[i][j] + ", ";
                    } else
                    {
                        if (j == 1) sum_string += "C/N0 : ";
                        if (!gpsgroup[i][j].Equals(""))
                        {
                            average_cn0 += float.Parse(gpsgroup[i][j]);
                            sum_string += gpsgroup[i][j] + ", ";
                            cn0_count++;
                        }
                    }
                }
                if (cn0_count > 0) average_cn0 = average_cn0 / cn0_count;
                richTextBox2.AppendText(sum_string + " >> " +average_cn0.ToString()+"\n");
                sum_string = "";
                average_cn0 = 0;
                cn0_count = 0;

            }
            dateTime = DateTime.Now;
            //string time = dateTime.Hour + ":" + dateTime.Minute + ":" + dateTime.Second;
            string time = dateTime.ToString("HH:mm:ss");
            time += "----------------------------------------------------------\n";
            richTextBox2.AppendText(time);
        }

        private void show_title_info()
        {
            this.Text = title + "(" + richTextBox1.TextLength + " byte)" + " : " + scroll_mode;
        }

        private string Get_gps_token_value(string data, string token)
        {

            int start = 0;
            string info_string="";
            int string_length=0;

            // data string is "[00:00:31.990,600] <dbg> location: method_gnss_print_pvt: PRN:  10, C/N0: 26.7, in fix: 0, unhealthy: 0";
            // 1. extract token "PRN ~"
            start =  data.IndexOf(token);
            if (start == -1) return null;
            info_string = data.Substring(start);
            //info_string is "PRN:  10, C/N0: 26.7, in fix: 0, unhealthy: 0"

            // 2. delete all space
            info_string = info_string.Replace(" ", "");
            // info_string is "PRN:10,C/N0:26.7,infix:0,unhealthy:0"

            // 3. extract token PRN value to string
            char[] info_chars = info_string.ToCharArray();
            string_length = info_string.Length;

            string value = "";
            int i=0;
            for (i=token.Length;info_chars[i] != ',';i++) // skip "PRN:" length
            {
                value += info_chars[i];
            }

            return value;
            
        }


        private void Ansi_Coloring(string time, string data)
        {
            char esc_ch =(char) 0x1b;

            richTextBox1.AppendText(time);
            if (data.Contains(esc_ch)) {
                string[] normal_string = data.Split(esc_ch);
                int array_length = normal_string.Length;

                set_coloring(normal_string[0]);

                if (array_length == 2)
                {
                    set_coloring(normal_string[1]);

                } else if (array_length == 3)
                {
                    set_coloring(normal_string[1]);
                    set_coloring(normal_string[2]);
                }
            }
            else
            {
                // control 문자가 없으면
                richTextBox1.SelectionColor = Current_Color;
                if(data.Contains("\n")) richTextBox1.AppendText(data);
                else richTextBox1.AppendText(data + "\r\n");
            }

        }

        private void set_coloring(string original)
        {
            string control_red_start = "[1;31m";
            string control_white_start = "[0m";
            string src= original;

            if (src.Contains(control_red_start))
            {
                Current_Color = Color.Red;
                richTextBox1.SelectionColor = Current_Color;
                src = src.Replace(control_red_start, "");
                richTextBox1.AppendText(src);

            }
            else if (src.Contains(control_white_start))
            {
                Current_Color = Color.White;
                richTextBox1.SelectionColor = Current_Color;
                src = src.Replace(control_white_start, "");
                richTextBox1.AppendText(src);
            }
        }

        private string Readline(string data,int index)
        {
            string result="";
            char[] chars = data.ToCharArray();
            int ret_index = 0;
            for(int i=0;i< data.Length; i++)
            {
                result += chars[i];
                if (chars[i].Equals('\n'))
                {
                    if (ret_index == index || result == null) return result;
                    ret_index++;
                    result = "";
                }
            }
            return null;
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            int select_index = comboBox1.SelectedIndex;
            com = comboBox1.Items[select_index].ToString();

            myPort = new SerialPort();
            myPort.BaudRate = 115200;
            myPort.PortName = com;
            myPort.Parity = Parity.None;
            myPort.DataBits = 8;
            myPort.StopBits = StopBits.One;
            myPort.DataReceived += MyPort_DataReceived;

            button1.BackColor = Color.LightGray;

            Richbox_clear_logo();

            try
            {
                            
                show_title_info();
                myPort.Open();
                //test_gps();
                gpsgroup_init();
                this.richTextBox2.Focus();
            }
            catch (Exception exc)
            {
                MessageBox.Show(exc.Message, "Errore");
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {

            richTextBox1.SaveFile(userInput, RichTextBoxStreamType.PlainText);
            // Set the properties on SaveFileDialog1 so the user is
            // prompted to create the file if it doesn't exist 
            // or overwrite the file if it does exist.
            SaveFileDialog1.CreatePrompt = true;
            SaveFileDialog1.OverwritePrompt = true;

            // Set the file name to myText.txt, set the type filter
            // to text files, and set the initial directory to the 
            // MyDocuments folder.

            dateTime = DateTime.Now;
            string time = dateTime.Year + "" + dateTime.Month + ""+ dateTime.Day +"_"+ dateTime.Hour + "" + dateTime.Minute+"" + dateTime.Second;

            SaveFileDialog1.FileName = "log"+"_"+time;
            // DefaultExt is only used when "All files" is selected from 
            // the filter box and no extension is specified by the user.
            SaveFileDialog1.DefaultExt = "txt";
            SaveFileDialog1.Filter =
                "Text files (*.txt)|*.txt|All files (*.*)|*.*";
            SaveFileDialog1.InitialDirectory =
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

            // Call ShowDialog and check for a return value of DialogResult.OK,
            // which indicates that the file was saved. 
            DialogResult result = SaveFileDialog1.ShowDialog();
            Stream fileStream;

            if (result == DialogResult.OK)
            {
                // Open the file, copy the contents of memoryStream to fileStream,
                // and close fileStream. Set the memoryStream.Position value to 0 
                // to copy the entire stream. 
                fileStream = SaveFileDialog1.OpenFile();
                userInput.Position = 0;
                userInput.WriteTo(fileStream);
                fileStream.Close();
            }
        }

        private void button4_Click(object sender, EventArgs e)
        {
            richTextBox1.Clear();
            richTextBox1.SelectAll();
            richTextBox1.Update();
        }

        private void RichTextBox1_VScroll(object sender, EventArgs e)
        {

        }

        private void RichTextBox1_Click(object sender, EventArgs e)
        {
            // auto scroll할 때는 해당 textbox에 포커스를 해주고
            // stop scroll할 때는 해당 textbox에 포커스를 빼준다
            // 포커스에 따라서 auto/stop기능 활성화가 정상적으로 동작한다
            if (allowAutoScroll) { allowAutoScroll = false; scroll_mode = "stop scroll"; this.richTextBox2.Focus(); }
            else { allowAutoScroll = true; scroll_mode = "auto scroll"; this.richTextBox1.Focus(); }

            show_title_info();

        }

        private void richTextBox1_TextChanged(object sender, EventArgs e)
        {
            
            if (allowAutoScroll)
            {
                richTextBox1.SelectionStart = richTextBox1.TextLength;
                richTextBox1.ScrollToCaret();
            }
            show_title_info();
            
        }

        private void richTextBox2_TextChanged(object sender, EventArgs e)
        {
            richTextBox2.SelectionStart = richTextBox2.TextLength;
            richTextBox2.ScrollToCaret();
        }
        private static Color GetDarkerColor(Color clr)
        {
            Color c = new Color();
            int r, g, b;

            r = clr.R - 18;
            g = clr.G - 18;
            b = clr.B - 18;

            if (r < 0) r = 0;
            if (g < 0) g = 0;
            if (b < 0) b = 0;

            c = Color.FromArgb(r, g, b);
            return c;
        }

        private void button6_Click(object sender, EventArgs e)
        {
            string cmd = comboBox2.Text;
            if(comboBox2.FindString(cmd) == -1) comboBox2.Items.Add(cmd);
            cmd += "\r\n";
            myPort.Write(cmd);
            //myPort.WriteLine(cmd);
            //richTextBox2.AppendText(cmd);
        }

        private void comboBox2_SelectedIndexChanged(object sender, EventArgs e)
        {
            string cmd = comboBox2.Text;
        }
    }


}
