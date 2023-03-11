using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Threading;
using System.Diagnostics;
using System.Net;
using System.IO;
using System.Web;
using System.Net.Mime;
using Leo.Algorithm;

namespace XServer_test
{
    public partial class Form1 : Form
    {
        /// <summary>
        /// 监听的主程序
        /// </summary>
        Thread mainThread;
        /// <summary>
        /// 监听器
        /// </summary>
        HttpListener listener;
        /// <summary>
        /// 请求响应线程列表
        /// </summary>
        List<Thread> request;


        public Form1()
        {
            InitializeComponent();
            //允许跨线程访问
            Control.CheckForIllegalCrossThreadCalls = false;

            //将本地端口添加到监听器
            listener = new HttpListener();
            listener.Prefixes.Add("http://+:10925/");

            //初始化线程列表
            request = new List<Thread>();
        }

        /// <summary>
        /// 选择文件夹按钮
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button1_Click(object sender, EventArgs e)
        {
            if (folderBrowserDialog1.ShowDialog() == DialogResult.Cancel) return;
            textBox1.Text = folderBrowserDialog1.SelectedPath;
        }

        /// <summary>
        /// 启动服务器按钮
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button2_Click(object sender, EventArgs e)
        {
            //文件夹选择框变为只读
            textBox1.ReadOnly = true;

            //允许断开连接按钮使用
            button3.Enabled = true;



            //不允许点击选择文件夹按钮
            button1.Enabled = false;

            //不允许点击连接按钮
            button2.Enabled = false;

            //开启监听主进程
            mainThread = new Thread(() => UpLoad(textBox1.Text));
            mainThread.Start();





        }


        /// <summary>
        /// 终止连接按钮
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button3_Click(object sender, EventArgs e)
        {
            Disconnect();
            listener.Stop();
            richTextBox1.Text += DateTime.Now.ToString() + "[console]:" + "连接已断开" + "\r\n";


            textBox1.ReadOnly = false;
            button1.Enabled = true;
            button2.Enabled = true;
            button3.Enabled = false;
        }


        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            Disconnect();
            listener.Stop();
            Application.Exit();
        }

        public void SendMessage(string message)
        {
            richTextBox1.Text += message;
        }

        void UpLoad(string args)
        {



            listener.Start();
            //向控制台输出
            //richTextBox1.Text += DateTime.Now.ToString() + "[console]:" + Dns.GetHostEntry(Dns.GetHostName()).AddressList[0].ToString() + "\r\n";
            //richTextBox1.Text += DateTime.Now.ToString() + "[console]:" + Dns.GetHostEntry(Dns.GetHostName()).AddressList[1].ToString() + "\r\n";
            richTextBox1.Text += DateTime.Now.ToString() + "[console]:" + "成功打开服务器" + listener.Prefixes.First<string>() + "\r\n";




            //循环监听
            while (true)
            {
                //接收到请求，开启新线程响应
                HttpListenerContext context = listener.GetContext();
                request.Add(new Thread(() => { Run(context); }));
                request[request.Count - 1].Start();

            }
            void Run(HttpListenerContext context)
            {

                //向控制台输出
                Program.form.SendMessage(DateTime.Now.ToString() + "[request]:" + "新的客户端请求" + context.Request.UserHostName + "\r\n");
                Program.form.SendMessage(DateTime.Now.ToString() + "[request]:" + "本地地址:" + context.Request.UserHostAddress + context.Request.RawUrl + "\r\n");



                //请求的文件在本地的路径
                string path;

                //响应的mime格式
                string mime = null;


                //如果标头有range，存入输入的值
                long rangeStart = 0;
                long rangeEnd = 0;

                int maxage = -1;











                //解码传输形式的url
                string raw = context.Request.RawUrl.Replace("+", "%2B");
                raw = HttpUtility.UrlDecode(raw);
                raw = raw.Replace('/', '\\');

                //请求的文件在本地的路径
                path = @args + @raw;



                string extension = Path.GetExtension(path);
                switch (extension)
                {
                    case ".jpg": { mime = MediaTypeNames.Image.Jpeg; break; }

                    case ".png": { mime = MediaTypeNames.Image.Jpeg; break; }
                    case ".PNG": { mime = MediaTypeNames.Image.Jpeg; break; }

                    case ".mp4": { mime = "video/mp4"; break; }
                    case ".ts": { mime = "video/mp4"; break; }

                    case ".txt": { mime = MediaTypeNames.Text.RichText; break; }

                    case ".zip": { break; }

                    case ".html": { mime = "text/html"; break; }

                    case ".ico": { mime = ""; break; }

                    case "": { mime = "text/html"; break; }

                    default: { mime = "null"; break; }
                }

                context.Response.ContentType = mime;





                //将请求标头读取至headers
                foreach (string key in context.Request.Headers.AllKeys)
                {
                    //此请求标头将会存在rangeStart,rangeEnd中,在读取媒体文件时执行(下面的代码)
                    if (key == "Range")
                    {




                        //读取数据
                        /*
                         
                        ①请求从0至500的byte数据：Range: bytes=0-500
                        ②请求第500个byte以后的全部数据：Range: bytes=501-
                        ③请求最后500个byte的数据：Range:bytes=-500
                        ④请求多个分段时，各分段以,分割：Range: bytes=0-100,101-200
                         
                         */



                        Leo.Algorithm.StringReader reader = new Leo.Algorithm.StringReader(context.Request.Headers.GetValues("Range")[0]);

                        reader.FindNext("bytes=");
                        //情况③
                        if (!reader.isEnd && reader.PeekFor(1) == "-")
                        {


                            rangeStart = 0;

                            //升位以跳过"-"
                            reader.Read();

                            rangeEnd = long.Parse(reader.ReadTo(' '));
                        }
                        else
                        {
                            rangeStart = long.Parse(reader.ReadTo('-'));

                            //情况②
                            if (reader.isEnd || reader.PeekFor(1) == " ")
                            {
                                //读到末尾规定为-1
                                rangeEnd = -1;


                            }
                            //情况①
                            else
                            {
                                rangeEnd = long.Parse(reader.ReadTo('-'));
                            }
                        }


                        context.Response.StatusCode = (int)HttpStatusCode.PartialContent;

                    }
                    else if (key == "Cache-Control")
                    {






                    }
                    else if (key == "max-age")
                    {
                        maxage = int.Parse(context.Request.Headers.GetValues("max-age")[0]);
                    }
                }



                //向响应流中写入标头
                context.Response.Headers.Add("Date", DateTime.Now.ToString());
                context.Response.Headers.Add("Age", 36000.ToString());
                context.Response.Headers.Add("Accept-Ranges", "bytes");



                //读取文件

                //请求的文件类型是文件夹
                if (extension == "")
                {
                    bool isPictures = true;

                    //检测是否是漫画模式
                    if (new DirectoryInfo(path).GetDirectories().Length != 0) isPictures = false;
                    foreach (FileInfo file in new DirectoryInfo(path).GetFiles())
                    {
                        if (isPictures == false) break;
                        if (file.Extension != ".png" && file.Extension != ".jpg" && file.Extension != ".PNG")
                        {
                            isPictures = false;
                            break;
                        }
                    }

                    byte[] read;
                    int length;

                    if (isPictures)
                    {
                        //漫画模式
                        read = Encoding.UTF8.GetBytes(CreatePicturesPage(@path));
                        length = read.Length;
                    }
                    else
                    {
                        //文件夹阅览模式
                        read = Encoding.UTF8.GetBytes(CreateDirectoryPage(@path));
                        length = read.Length;
                    }

                    //写入响应流
                    Stream ws = context.Response.OutputStream;

                    try
                    {

                        ws.Write(read, 0, length);
                        ws.Flush();
                        ws.Close();
                    }
                    catch { }


                }
                //请求的文件类型是txt
                else if (extension == ".txt")
                {
                    StreamReader file = new StreamReader(@path);
                    byte[] read = Encoding.Default.GetBytes(file.ReadToEnd());
                    int length = read.Length;


                    //写入响应流
                    Stream ws = context.Response.OutputStream;

                    try
                    {

                        ws.Write(read, 0, length);
                        ws.Flush();
                        ws.Close();
                    }
                    catch { }
                }
                //请求的文件类型是媒体
                else if (extension == ".mp4")
                {
                    FileStream file;
                    try
                    {
                        file = new FileStream(@path, FileMode.Open, FileAccess.Read);
                    }
                    catch (Exception)
                    {
                        context.Response.Close();
                        return;
                    }


                    //rangeEnd=-1规定为文件末尾
                    if (rangeEnd == -1 || rangeEnd == 0)
                    {
                        rangeEnd = file.Length - 1;
                    }

                    //执行Range请求标头


                    //添加至响应标头
                    context.Response.Headers.Add("Content-Range", "bytes " + rangeStart.ToString() + "-" + rangeEnd.ToString() + "/" + file.Length);

                    //响应的长度
                    long length = rangeEnd - rangeStart + 1;

                    //短文件
                    if (length < int.MaxValue / 2)
                    {



                        //要读取的变量
                        byte[] read = new byte[length];

                        //将读取的位置放到rangeStart处
                        file.Position = rangeStart;


                        length = file.Read(read, 0, (int)length);


                        //写入响应流
                        Stream ws = context.Response.OutputStream;


                        try
                        {

                            ws.Write(read, 0, (int)length);
                        }
                        catch { }
                        try
                        {

                            ws.Flush();
                        }
                        catch { }
                        try
                        {

                            ws.Close();
                        }
                        catch { }

                    }
                    //长文件,需要分段传输
                    else
                    {
                        //将读取的位置放到rangeStart处
                        file.Position = rangeStart;
                        //要写入的响应流
                        Stream ws = context.Response.OutputStream;

                        while (true)
                        {
                            //如果剩余的文件还大于int的最大值
                            if (rangeEnd - file.Position + 1 > ushort.MaxValue)
                            {

                                byte[] read = new byte[ushort.MaxValue];
                                file.Read(read, 0, read.Length);


                                try
                                {
                                    ws.Write(read, 0, read.Length);
                                    ws.Flush();
                                }
                                catch
                                {
                                    break;
                                }

                            }
                            //如果剩余的文件已经小于int的最大值了
                            else
                            {
                                byte[] read = new byte[rangeEnd - file.Position + 1];
                                file.Read(read, 0, read.Length);



                                try
                                {
                                    ws.Write(read, 0, read.Length);
                                    ws.Flush();
                                }
                                catch { break; }
                                break;
                            }

                        }


                    }




                }
                else if(mime == MediaTypeNames.Image.Jpeg)
                {
                    FileStream file;
                    try
                    {
                        file = new FileStream(@path, FileMode.Open, FileAccess.Read);
                    }
                    catch (Exception)
                    {
                        context.Response.Close();
                        return;
                    }
                    //要读取的变量
                    byte[] read = new byte[file.Length];
                    file.Read(read, 0, read.Length);

                    //写入响应流
                    Stream ws = context.Response.OutputStream;


                    try
                    {

                        ws.Write(read, 0, read.Length);
                    }
                    catch { }
                    try
                    {

                        ws.Flush();
                    }
                    catch { }
                    try
                    {

                        ws.Close();
                    }
                    catch { }


                }
                else if (mime == "null")
                {
                    context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                }



                //richTextBox1.Text += DateTime.Now.ToString() + "[response]:" + "上传:" + path+"\r\n";
                //richTextBox1.Text += DateTime.Now.ToString()+"[response]:" + context.Response.StatusCode+"\r\n";
                context.Response.Close();
            }




        }

        /// <summary>
        /// 断开连接
        /// </summary>
        void Disconnect()
        {
            foreach (Thread thread in request.ToArray())
            {
                thread.Abort();
            }
            if (mainThread != null) mainThread.Abort();
        }

        static public string CreateDirectoryPage(string path)
        {
            DirectoryInfo root = new DirectoryInfo(path);
            DirectoryInfo[] directories = root.GetDirectories();
            FileInfo[] files = root.GetFiles();

            //从外部资源读取page
            StreamReader sr = new StreamReader(Environment.CurrentDirectory + "\\DirectoriesPage.html");
            string page = sr.ReadToEnd();

            //读取文件夹中的子文件夹和文件，写成h5形式
            StringBuilder content = new StringBuilder();
            for (int i = 0; i < directories.Length; i++)
            {
                string div = "<div class=\"content directory\" style=\"\">" + directories[i].Name + "</div>";
                content.AppendLine(div);
            }
            for (int i = 0; i < files.Length; i++)
            {
                string div = "<div class=\"content file\" style=\"\">" + files[i].Name + "</div>";
                content.AppendLine(div);
            }

            page = page.Replace("#AddContent", content.ToString());

            return page;

        }

        static public string CreatePicturesPage(string path)
        {
            FileInfo[] files = new DirectoryInfo(path).GetFiles();


            //从外部资源读取page
            StreamReader sr = new StreamReader(Environment.CurrentDirectory + "\\PicturesPage.html");
            string page = sr.ReadToEnd();
            StringBuilder content = new StringBuilder();
            for (int i = 0; i < files.Length; i++)
            {
                string img = "<img src=\"" + files[i].Name + "\" width=\"1464px\"><br><span>" + (i + 1).ToString() + "/" + files.Length.ToString() + "</span></div><div style=\"text-align:center;color:#999;padding-bottom:10px;font-size:13px;\">";
                content.AppendLine(img);
            }

            page = page.Replace("#AddContent", content.ToString());
            return page;

        }
    }
}
