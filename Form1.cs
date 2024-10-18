using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Windows.Forms;
using System.Windows.Forms.VisualStyles;
using System.Xml;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TaskbarClock;

namespace SerialTerminal
{
    public partial class SerialTerminal : Form
    {
        public SerialPort myPort;
        private DateTime dateTime;
        private string com = "COM4";
        private string title = " ";
        SaveFileDialog SaveFileDialog1;
        MemoryStream userInput = new MemoryStream();
        List<List<string>> gpsgroup = new List<List<string>>();
       

        public SerialTerminal()
        {
            InitializeComponent();
            richTextBox1.BackColor = Color.Black;
            richTextBox1.ForeColor = Color.White;
            button1.Text = "Open";
            button2.Text = "Close";
            GetSerialPorts();
            SaveFileDialog1 = new SaveFileDialog();
            Richbox_show_logo();

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
                title = "Serial Console" + " : " + com;
                this.Text = title;
                myPort.Open();
                //test_gps();
                gpsgroup_init();
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
            richTextBox1.Text = "\n\n\n"+"HyunSung" + "\n\n" + "Serial Console";
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
            dateTime = DateTime.Now;
            System.Drawing.Color mycolor = Color.White;

            for (int i = 0; i < 256; i++)
            {
                string data = Readline(existing_data, i);
                if (data == null) break;
                if (data == "\n" || data == " ") continue;

                data = Remove_special_char(data);
                Coloring(data);

                string time = dateTime.Hour + ":" + dateTime.Minute + ":" + dateTime.Second;
                richTextBox1.AppendText(time + " " + data);

                if (data.Contains("PRN:"))
                { 
                    string prn = "";
                    string cn0 = "";
                    prn = Get_gps_token_value(data, "PRN:");
                    cn0 = Get_gps_token_value(data, "C/N0:");
                    gpsgroup_Addcn0(prn, cn0);
                }
            }
        }

        private void test_gps()
        {
            string test = "[00:03:34.203,521] <dbg> location: method_gnss_print_pvt: PRN:  31, C/N0: 25.3, in fix: 0, unhealthy: 0\r\n";
            for(int i =0;i<20;i++)
            {
                string prn = "";
                string cn0 = "";
                prn = Get_gps_token_value(test, "PRN:");
                cn0 = Get_gps_token_value(test, "C/N0:");
                gpsgroup_Addcn0(prn, cn0);
            }
            show_gpsgroup();
        }

        private void gpsgroup_Addcn0(string prn, string cn0)
        {
           
            for (int i=0; i < gpsgroup.Count; i++)
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
            List<string> new_gps = new List<string>();
            new_gps.Add(prn);
            new_gps.Add(cn0);
            gpsgroup.Insert(0, new_gps);
            return;

            // show_gpsgroup();
            return;
        }

        private void show_gpsgroup()
        {
            string sum_string ="";

            for (int i = 0; i< gpsgroup.Count; i++)
            {
                sum_string += "PRN : ";
                for (int j = 0;  gpsgroup[i].Count > j ; j++)
                {
                    if (j == 1) sum_string += "C/N0 : ";
                    sum_string += gpsgroup[i][j] + ", ";
                }
                richTextBox2.AppendText(sum_string+"\n");
                sum_string = "";
            }
        }

        private void gpsgroup_init()
        {

            List<string> gps = new List<string>();
            gps.Add("");
            gpsgroup.Add(gps);

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
            char[] temp = info_string.ToCharArray();
            string_length = info_string.Length;

            info_string = "";
            for (int j = 0; j < string_length; j++)
            {
                if (temp[j] != ' ')
                {
                    info_string += temp[j];
                }
            }
            // info_string is "PRN:10,C/N0:26.7,infix:0,unhealthy:0"

            // 3. extract token PRN value to string
            char[] info_chars = info_string.ToCharArray();
            string_length = info_string.Length;

            string value = "";
            int i=0;
            for (i=token.Length;info_chars[i] != ',';i++) // skip "PRN:" length
            {
                value += info_chars[i];
                if (i >= string_length) break;
            }

            return value;
            
        }

        private void Coloring(string data)
        {
            Color mycolor = Color.White;
            string time = dateTime.Hour + ":" + dateTime.Minute + ":" + dateTime.Second;
            if (data.Contains("err")) mycolor = Color.Red;
            else mycolor = Color.White;

            richTextBox1.SelectionColor = mycolor;
        }

        private string Remove_special_char(string src)
        {
            string dst="";
            byte ch = 0x1b;

            string remove_string = (char)ch + "[0m";
            dst = src.Replace(remove_string, "");

            remove_string = (char)ch + "[1;31m";
            dst = dst.Replace(remove_string, "");

            return dst;
        }

        private string Readline(string data,int index)
        {
            string result="";
            char[] chars = data.ToCharArray();
            int ret_index = 0;
            for(int i=0;i< data.Length; i++)
            {
                result += chars[i];
                if (chars[i] == '\n')
                {
                    if (ret_index == index || result == null) return result;
                    ret_index++;
                    result = "";
                }
            }
            return null;
        }

        private void richTextBox1_TextChanged(object sender, EventArgs e)
        {
            richTextBox1.SelectionStart = richTextBox1.TextLength;
            richTextBox1.ScrollToCaret();
            this.Text = title + "("+richTextBox1.TextLength+" byte)";
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }
        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            int select_index = comboBox1.SelectedIndex;
            com = comboBox1.Items[select_index].ToString();
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

        private void button5_Click(object sender, EventArgs e)
        {
            show_gpsgroup();
            gpsgroup.Clear();
            gpsgroup_init();
        }

    }
}
