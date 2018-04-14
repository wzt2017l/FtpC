using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Net;
using System.IO;

namespace FtpC
{
    public partial class Form1 : Form
    {
        string changeDir;
        bool isAnonymous;
        FtpHelper helper;
        public Form1()
        {
            InitializeComponent();
            textBox1.Text = "ftp://127.0.0.1";
            textBox2.Text = "21";
            changeDir = string.Empty;
            helper = new FtpHelper();
        }
        //选中复选框
        private void checkBox1_Click(object sender, EventArgs e)
        {
            if (checkBox1.Checked)
            {
                panel1.Enabled = false;
                isAnonymous = true;
            }
            else
            {
                panel1.Enabled = true;
                isAnonymous = false;
            }
        }
        //连接
        private void button1_Click(object sender, EventArgs e)
        {
            Cursor cursor = this.Cursor;
            this.Cursor = Cursors.WaitCursor;
            string ftp = textBox1.Text + ":" + textBox2.Text;
            ShowFileList(ftp);
            this.Cursor = cursor;
        }
        //更改目录
        private void button2_Click(object sender, EventArgs e)
        {
            Cursor curr = this.Cursor;
            this.Cursor = Cursors.WaitCursor;
            string subDir = listBox1.SelectedItem.ToString().Trim();
            changeDir += "/" + subDir;
            string path = textBox1.Text + ":" + textBox2.Text;
            path += changeDir;
            ShowFileList(path);
            this.Cursor = curr;
        }
        //下载
        private void button3_Click(object sender, EventArgs e)
        {
            bool isSucessed = false;
            Cursor curr = this.Cursor;
            this.Cursor = Cursors.WaitCursor;
            string ftp = textBox1.Text + ":" + textBox2.Text;
            string fileName = listBox1.SelectedItem.ToString().Trim();
            string fullName = changeDir + "/" + fileName;
            ftp += fullName;
            if (isAnonymous)
            {
                helper.ConnectionToFtp(ftp);
            }
            else
            {
                helper.ConnectionToFtp(ftp,textBox3.Text,textBox4.Text);
            }
            SaveFileDialog sfd = new SaveFileDialog();
            sfd.FileName = fileName;
            if (sfd.ShowDialog()==DialogResult.OK)
            {
                isSucessed = helper.DownLoad(sfd.FileName);
            }
            if (isSucessed)
            {
                MessageBox.Show(string.Format("文件{0}\n下载成功!",sfd.FileName),"成功",MessageBoxButtons.OK,MessageBoxIcon.Information);
            }
            else
            {
                MessageBox.Show("文件下载失败!", "失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            this.Cursor = curr;
        }
        //上传
        private void button4_Click(object sender, EventArgs e)
        {
            bool isSucessed = false;
            Cursor curr = this.Cursor;
            this.Cursor = Cursors.WaitCursor;
            string ftp = textBox1.Text + ":" + textBox2.Text + "/" + changeDir + "/";
            string sourceFile = "";
            OpenFileDialog ofd = new OpenFileDialog();
            if (ofd.ShowDialog()==DialogResult.OK)
            {
                sourceFile = ofd.FileName;
                string fullFtpPath = ftp + sourceFile.Substring(sourceFile.LastIndexOf('\\')+1);
                if (isAnonymous)
                {
                    helper.ConnectionToFtp(fullFtpPath);
                }
                else
                {
                    helper.ConnectionToFtp(fullFtpPath,textBox3.Text,textBox4.Text);
                }
                isSucessed = helper.FileUp(sourceFile);
            }
            if (isSucessed)
            {
                MessageBox.Show(string.Format("文件{0}\n上传成功!", ofd.FileName), "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                MessageBox.Show("文件下载失败!", "失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            ShowFileList(ftp);
            this.Cursor = curr;
        }

        void ShowFileList(string path)//显示列表
        {
            bool isSucessed = false;
            if (isAnonymous)
            {
                isSucessed = helper.ConnectionToFtp(path);
            }
            else
            {
                isSucessed = helper.ConnectionToFtp(path,textBox3.Text,textBox4.Text);
            }
            listBox1.DataSource = helper.getFilesList();
            if (!isSucessed)
            {
                MessageBox.Show("无法连接服务器","错误",MessageBoxButtons.OK,MessageBoxIcon.Error);
            }
        }

       
    }

    public class FtpHelper
    {
        FtpWebRequest request;

        public FtpHelper()
        {
            request = null;
        }

        public bool ConnectionToFtp(string uri,string user,string pwd)//创建连接
        {
            try
            {
                request = (FtpWebRequest)WebRequest.Create(uri);
                request.Credentials = new NetworkCredential(user,pwd);
                return true;
            }
            catch
            {
                return false;
            }
        }
        public bool ConnectionToFtp(string uri)//创建连接
        {
            try
            {
                request= (FtpWebRequest)WebRequest.Create(uri);
                request.AuthenticationLevel = System.Net.Security.AuthenticationLevel.None;
                return true;
            }
            catch
            {
                return false;
            }
        }

        public string[] getFilesList()//获取文件列表
        {
            Encoding encoding = System.Text.Encoding.GetEncoding("gb2312");
            request.Method = WebRequestMethods.Ftp.ListDirectoryDetails;
            FtpWebResponse response = (FtpWebResponse)request.GetResponse();
            Stream stream = response.GetResponseStream();//获取相应返回的数据
            StreamReader sr = new StreamReader(stream,encoding);
            string content = sr.ReadToEnd();
            string[] files = content.Split('\n');
            for (int i=0;i<files.Length;i++)
            {
                int start = files[i].LastIndexOf(' ');
                files[i] = files[i].Substring(start+1);
            }
            sr.Close();
            stream.Close();
            response.Close();
            return files;
        }

        public bool DownLoad(string path)//下载文件
        {
           try { 
                request.Method = WebRequestMethods.Ftp.DownloadFile;
                FtpWebResponse response = (FtpWebResponse)request.GetResponse();
                Stream down = response.GetResponseStream();
                Stream outdown = File.OpenWrite(path);
                byte[] buffer = new byte[1024];
                int size = 0;
                while ((size=down.Read(buffer,0,1024))>0)
                {
                    outdown.Write(buffer,0,size);
                }
                down.Close();
                outdown.Close();
                response.Close();
                return true;
           }
            catch
            {
                return false;
            }
           
        }

        public bool FileUp(string path)//上传文件
        {
            try
            {
                request.Method = WebRequestMethods.Ftp.UploadFile;
                StreamReader inStream = new StreamReader(path);
                byte[] contents = Encoding.UTF8.GetBytes(inStream.ReadToEnd());
                inStream.Close();
                Stream upstream = request.GetRequestStream();
                upstream.Write(contents,0,contents.Length);//上传数据
                upstream.Close();
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
