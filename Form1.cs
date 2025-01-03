using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Net.NetworkInformation;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
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
        VScrollBar vScrollBar1;
        private bool port_flag=false;
        private bool timestamp_flag = false;

        public SerialTerminal()
        {
            InitializeComponent();

            button2.Text = "Close";
            GetSerialPorts();
            SaveFileDialog1 = new SaveFileDialog();
            Richbox_show_logo();
            gpsgroup_init();
            comport_speed_init();
            register_cmd_table();
            Set_Default_Color();
            AddMyScrollEventHandlers();
        }

        public void GetSerialPorts()
        {
            List<string> portNames = new List<string>();
            string[] port = System.IO.Ports.SerialPort.GetPortNames();
            foreach(string portName in port)
            {
                comBox1.Items.Add(portName);
            }
        }


        private void button2_Click(object sender, EventArgs e)
        {
            try
            {
  
                if (port_flag.Equals(true))
                {
                    myPort.Close();
                    button2.Text = "Open";
                    port_flag = false;
                } else
                {
                    myPort.Open();
                    button2.Text = "Close";
                    port_flag = true;

                }
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
            //var inData = myPort.ReadExisting(); // Use local variable
            //Invoke(new Action<string>(displayData_event), new object[] { inData });
            int RecvSize = myPort.BytesToRead;
            byte[] buff = new byte[RecvSize];

            if (RecvSize != 0)
            {
                myPort.Read(buff, 0, RecvSize);
                if (RecvSize > 0)
                    Invoke(new Action<byte[]>(displayData_event), new object[] { buff });
            }
        }

        private void displayData_event(byte[] byte_array)
        {
            System.Drawing.Color mycolor = Color.White;
            dateTime = DateTime.Now;

            // byte를 char로 변환시켜 string으로 build한다
            int len = byte_array.Length;
            String input_string = string.Empty;
            for (int i = 0; i < len; i++)
            {
                // \r = 0xd(Carriage Return), \n = 0xa(linefeed 줄바꿈)
                // carrige return(0xd)이 여러번 오는 경우가 있다
                // 그래서 0xd를 모두 빼고 0xa(linefeed)로 파싱한다
                if (!byte_array[i].Equals(0xd))
                    input_string += (char)byte_array[i];
            }

            string[] lines = input_string.Split('\n');

            if (lines.Length > 0)
            for(int i = 0;i<lines.Length;i++)
            {
                lines[i] += '\n';
                //HexDump(lines[i]);
                if (lines[i].Equals(null) || lines[i].Equals("\n")) continue;// lines[i] = "\r\n";
                Ansi_Coloring(lines[i]);
                //  Google, Reset 문자열을 Summary창에 보여준다
                ETCSummary(lines[i]);

                //  GPS CN0를 읽어 Summary창에 보여준다
                PRNSummary(lines[i]);
                // input_data에 여러 라인이 있을 경우,
                // 읽은 라인은 제거하고 next 라인을 읽는다

            }
        }


        private void Ansi_Coloring(string data)
        {
            char esc_ch = (char)0x1b;
            string time = dateTime.ToString("HH:mm:ss");
            time = time + " ";

            if (data.Contains(esc_ch))
            {
                string[] normal_string = data.Split(esc_ch);
                int array_length = normal_string.Length;

                //if(timestamp_flag==true) richTextBox1.AppendText(time);

                if (array_length == 1)
                {
                    display_color_string(normal_string[0]);
                }
                else if (array_length == 2)
                {
                     display_color_string(normal_string[1]);

                }
                else if (array_length == 3)
                {
                    display_color_string(normal_string[1]);
                    display_color_string(normal_string[2]);
                }

            }
            else
            {
                
                // control 문자가 없으면
                richTextBox1.SelectionColor = Current_Color;
                if (timestamp_flag == true ) richTextBox1.AppendText(time + data);
                else richTextBox1.AppendText(data);

            }
        }

        private string display_color_string(string original)
        {
            string control_red_start = "[1;31m";
            string control_white_start = "[0m";
            string src = original;
            string time = dateTime.ToString("HH:mm:ss");
            time = time + " ";

            if (src.Contains(control_red_start))
            {
                Current_Color = Color.Red;
                richTextBox1.SelectionColor = Current_Color;
                src = src.Replace(control_red_start, "");
            }
            else if (src.Contains(control_white_start))
            {
                Current_Color = Color.White;
                richTextBox1.SelectionColor = Current_Color;
                src = src.Replace(control_white_start, "");
            }
            else
            {
                Current_Color = Color.White;
                richTextBox1.SelectionColor = Current_Color;
            }

            //richTextBox1.AppendText(src+"\r\n");
            richTextBox1.AppendText(src);
            return src;
            //richTextBox1.AppendText(time+" ");


        }

        void HexDump(String str)
        {
            string hex_str="";
            char[] buff = new char[1000];
            if (str != null)
            {
                str.ToCharArray().CopyTo(buff, 0);
                for (int i = 0; i < buff.Length; i++)
                {
                    int value = Convert.ToInt32(buff[i]);
                    hex_str += String.Format("{0:X}", value);
                    hex_str += " ";
                }
                richTextBox1.AppendText(hex_str+'\n');
            }
        }


        private void PRNSummary(string log)
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
                gpsgroup_init();
            }

        }
        private void ETCSummary( string log)
        {
            string time = dateTime.ToString("HH:mm:ss");
            time = time + " ";



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
                richTextBox2.AppendText(time);
                // 리셋이 발생하면 빨간색으로 표시해준다
                richTextBox2.SelectionColor = Color.Red;                
                richTextBox2.AppendText(log);
                richTextBox2.SelectionColor = Color.White;
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
            gpsgroup.Clear();

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
            time += " --- End GPS Summary -------------------------------------------\n";
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

        private void Set_Default_Color()
        {
            richTextBox1.BackColor = Color.Black;
            richTextBox1.ForeColor = Color.White;
            richTextBox2.BackColor = Color.Black;
            richTextBox2.ForeColor = Color.White;

            Current_Color = Color.White;
            richTextBox1.SelectionColor = Current_Color;
            richTextBox2.SelectionColor = Current_Color;
        }

     
        private string Readline(string data,int index)
        {
            /*
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
            */
            string[] lines = data.Split('\n');
            if (index > lines.Length) return null;
            else return lines[index];
        }


        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            int select_index = comBox1.SelectedIndex;

            com = comBox1.Items[select_index].ToString();
            comport_init();
            Richbox_clear_logo();

            try
            {
                            
                show_title_info();
                myPort.Open();
                button2.Text = "Close";
                port_flag = true;
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
            richTextBox2.Clear();
        }

        private void RichTextBox1_Click(object sender, EventArgs e)
        {
            // auto scroll할 때는 해당 textbox에 포커스를 해주고
            // stop scroll할 때는 해당 textbox에 포커스를 빼준다
            // 포커스에 따라서 auto/stop기능 활성화가 정상적으로 동작한다

            if (allowAutoScroll) 
            { 
                allowAutoScroll = false; 
                scroll_mode = "stop scroll";
                this.richTextBox2.Focus();
                richTextBox1.BackColor = Color.DarkBlue;
            }
            else { 
                allowAutoScroll = true; 
                scroll_mode = "auto scroll"; 
                this.richTextBox1.Focus();
                richTextBox1.BackColor = Color.Black;
            }

            show_title_info();

        }

        private void richTextBox1_TextChanged(object sender, EventArgs e)
        {
            
            if (allowAutoScroll)
            {


                /*
                
                int text_length = richTextBox1.TextLength; 
                if (text_length >= 300000)
                {
                    int delete_line_length = richTextBox1.Find("\n");
                    richTextBox1.Text.ToArray("\n");
                    text_array[0] = "";
                }
                TextReader read = new System.IO.StringReader(richTextBox1.Text);
                String oldest_line = read.ReadLine();
                richTextBox1.SelectedText.Replace(oldest_line, "");
                richTextBox1.
                */

                richTextBox1.SelectionStart = richTextBox1.TextLength;
                richTextBox1.ScrollToCaret();
            }
            show_title_info();
            
        }

        private void AddMyScrollEventHandlers()
        {
            // Create and initialize a VScrollBar.
            vScrollBar1 = new VScrollBar();

            // Add event handlers for the OnScroll and OnValueChanged events.
            vScrollBar1.Scroll += new ScrollEventHandler(this.vScrollBar1_Scroll);
            //vScrollBar1.ValueChanged += new ScrollEventHandler(this.vScrollBar1_ValueChanged);

        }

        private void vScrollBar1_Scroll(object sender, ScrollEventArgs e)
        {
            //vScrollBar1.Value = vScrollBar1.Value + 40;
            String temp_txt = "vScrollBar Value:(OnScroll  Event) " + e.NewValue.ToString();
            temp_txt += "\n" + " height : " + vScrollBar1.Height.ToString();
            richTextBox2.AppendText(temp_txt);
        }

        // Create the ValueChanged event handler.
        private void vScrollBar1_ValueChanged(Object sender, EventArgs e)
        {
            // Display the new value in the label.
            richTextBox2.Text = "vScrollBar Value:(OnValueChanged Event) " + vScrollBar1.Value.ToString();
        }

        /*
        private void richTextBox1_MouseDown(object sender, EventArgs e)
        {
            //if (Focused)  Focus();
            // 마우스 버튼이 눌려지고
            if (Focused && e.Button == MouseButtons.Left)
            {
                //if(스크롤이 가장 하단에 있으면)){
                    allowAutoScroll = true;
                    richTextBox2.ScrollToCaret();
                //}

            }

            base.OnMouseDown(e);
        }
        */
        private void richTextBox2_TextChanged(object sender, EventArgs e)
        {
            richTextBox2.SelectionStart = richTextBox2.TextLength;
            richTextBox2.ScrollToCaret();
        }

        private void button6_Click(object sender, EventArgs e)
        {
            string cmd = cmdBox.Text;
            if(cmdBox.FindString(cmd) == -1) cmdBox.Items.Insert(0,cmd);
            cmd += "\r\n";
            myPort.Write(cmd);

        }

        private void comboBox2_SelectedIndexChanged(object sender, EventArgs e)
        {
            string cmd = cmdBox.Text;
            if(cmd.Contains("\n")) myPort.Write(cmd);
        }

        private void comboBox3_SelectedIndexChanged(object sender, EventArgs e)
        {
            if(!speedboBox.SelectedText.Equals(""))
                myPort.BaudRate = int.Parse(speedboBox.SelectedText);
        }

        private void comport_speed_init()
        {
            speedboBox.Items.Add("9600");
            speedboBox.Items.Add("19200");
            speedboBox.Items.Add("38400");
            speedboBox.Items.Add("115200");
            speedboBox.SelectedIndex = 3;
            if (!speedboBox.SelectedText.Equals(""))
                myPort.BaudRate = int.Parse(speedboBox.SelectedText);
        }

        private void comport_init()
        {
            myPort = new SerialPort();
            myPort.BaudRate = 115200;
            myPort.PortName = com;
            myPort.Parity = Parity.None;
            myPort.DataBits = 8;
            myPort.StopBits = StopBits.One;
            myPort.DataReceived += MyPort_DataReceived;
        }

        private void register_cmd_table()
        {
            string[] cmd_table =
            {
                "at+gps=1",
                "at+calib=1",
                "at+batt=1",
                "at+modem=1",
                "at+baro=1",
                "at+iccid=1",
                "at+init=1",
            };
            for(int i=0;i<cmd_table.Length;i++)
            {
                cmdBox.Items.Add(cmd_table[i]);
            }
            cmdBox.SelectedIndex = 0;
        }

        private void HSend_Click(object sender, EventArgs e)
        {
            if(timestamp_flag == true)
            {
                StampButton.Text = "StampOn";
                timestamp_flag = false;
            }
            else
            {
                StampButton.Text = "StampOff";
                timestamp_flag = true;
            }
        }

    }


}
