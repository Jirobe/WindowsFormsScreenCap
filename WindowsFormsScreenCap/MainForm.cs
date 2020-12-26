using System;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Tesseract;

namespace WindowsFormsScreenCap
{
    /// <summary>
    /// 画面キャプチャにスタンプを押すアプリサンプル
    /// 
    /// スタンプのアイコンは下記からお借りした
    /// https://icooon-mono.com/
    /// https://crocro.com/tools/item/gen_str_stamp_img.html
    /// </summary>
    public partial class MainForm : Form
    {
        private Bitmap stamp = null;
        private (int x, int y) offset;

        public MainForm()
        {
            InitializeComponent();

            //ウィンドウを透明に
            TransparencyKey = Color.Red;

            //タイトルバー
            FormBorderStyle = FormBorderStyle.FixedDialog;

            //初期化
            Clear();
        }

        private void Clear()
        {
            //元に戻す
            Cursor = Cursors.Arrow;
            stamp = null;

            groupBox1.Visible = false;
            groupBox2.Visible = false;

            richTextBox1.Text = string.Empty;

        }

        /// <summary>
        /// 画面をキャプチャ
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ScreenCap_Click(object sender, EventArgs e)
        {
            Clear();


            void SetImage(PictureBox picture, Point upperLeftSource)
            {
                var screenBounds = Screen.PrimaryScreen.Bounds;
                Bitmap bmp = new Bitmap(screenBounds.Width, screenBounds.Height);

                using (Graphics graphics = Graphics.FromImage(bmp))
                    graphics.CopyFromScreen(upperLeftSource, new Point(-60, -100), bmp.Size);

                picture.Image = bmp;
            }

            SetImage(capImage, new Point(Location.X, Location.Y));

            SetImage(pictureBox1, new Point(Location.X + 300, Location.Y - 45));
            SetImage(pictureBox2, new Point(Location.X + 300, Location.Y + 33));
            SetImage(pictureBox3, new Point(Location.X + 300, Location.Y + 105));
            SetImage(pictureBox4, new Point(Location.X + 300, Location.Y + 182));
            SetImage(pictureBox5, new Point(Location.X + 300, Location.Y + 253));
            SetImage(pictureBox6, new Point(Location.X + 300, Location.Y + 333));
        }

        /// <summary>
        /// キャプチャ画面をクリックしてスタンプ貼り付け
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Image_Click(PictureBox picture, object sender, EventArgs e)
        {
            if (stamp != null)
            {
                MouseEventArgs mouseEvent = (MouseEventArgs)e;

                Bitmap bmp = (Bitmap)picture.Image;

                using (Graphics graphics = Graphics.FromImage(bmp))
                    graphics.DrawImage(stamp, mouseEvent.X - offset.x, mouseEvent.Y - offset.y, stamp.Width, stamp.Height);

                picture.Image = bmp;
            }
        }

        private void CapImage_Click(object sender, EventArgs e) => Image_Click(capImage, sender, e);

        private void pictureBox1_Click(object sender, EventArgs e) => Image_Click(pictureBox1, sender, e);

        private void pictureBox2_Click(object sender, EventArgs e) => Image_Click(pictureBox2, sender, e);

        private void pictureBox3_Click(object sender, EventArgs e) => Image_Click(pictureBox3, sender, e);

        private void pictureBox4_Click(object sender, EventArgs e) => Image_Click(pictureBox4, sender, e);

        private void pictureBox5_Click(object sender, EventArgs e) => Image_Click(pictureBox5, sender, e);

        private void pictureBox6_Click(object sender, EventArgs e) => Image_Click(pictureBox6, sender, e);

        /// <summary>
        /// スタンプボタン押下時の処理
        /// スタンプの設定とマウスカーソルの変更
        /// </summary>
        /// <param name="icon"></param>
        /// <param name="offset_x"></param>
        /// <param name="offset_y"></param>
        private void Button_Click(Bitmap icon, int offset_x = 20, int offset_y = 20)
        {
            stamp = icon;
            Cursor = new Cursor(icon.GetHicon());
            offset = (offset_x, offset_y);
        }

        #region icon
        private void Button1_Click(object sender, EventArgs e) => Button_Click(Properties.Resources.circle);

        private void Button2_Click(object sender, EventArgs e) => Button_Click(Properties.Resources.X);

        private void button3_Click(object sender, EventArgs e) => Button_Click(Properties.Resources.dai);

        private void button4_Click(object sender, EventArgs e) => Button_Click(Properties.Resources.オボン);
        private void button5_Click(object sender, EventArgs e) => Button_Click(Properties.Resources.ゴツメ);
        private void button6_Click(object sender, EventArgs e) => Button_Click(Properties.Resources.チョッキ);
        private void button7_Click(object sender, EventArgs e) => Button_Click(Properties.Resources.ラム);
        private void button8_Click(object sender, EventArgs e) => Button_Click(Properties.Resources.拘スカーフ);
        private void button9_Click(object sender, EventArgs e) => Button_Click(Properties.Resources.拘眼鏡);
        private void button10_Click(object sender, EventArgs e) => Button_Click(Properties.Resources.拘鉢巻);
        private void button11_Click(object sender, EventArgs e) => Button_Click(Properties.Resources.残飯);
        private void button12_Click(object sender, EventArgs e) => Button_Click(Properties.Resources.保険);
        private void button13_Click(object sender, EventArgs e) => Button_Click(Properties.Resources.命玉);
        private void button14_Click(object sender, EventArgs e) => Button_Click(Properties.Resources.襷);
        #endregion icon

        private bool isReadLog = false;
        private CancellationTokenSource source;
        private SemaphoreSlim semaphore = new SemaphoreSlim(1, 1);

        private async void LogText_Click(object sender, EventArgs e)
        {
            Clear();

            //前のスレッドを停止
            richTextBox1.Text = "タスク終了中";
            source?.Cancel();

            //前のスレッドが終わるまで待機
            await semaphore.WaitAsync();


            isReadLog = !isReadLog;

            if (isReadLog)
            {
                LogText.Text = "Stop: ReadLog";

                groupBox1.Visible = true;
                groupBox2.Visible = true;

                //CancellationTokenSourceの作り直し
                source?.Dispose();
                source = new CancellationTokenSource();

                richTextBox1.Text = string.Empty;

                //ワーカースレッド 翻訳処理
                //await Task.Run( async() =>
                await Task.Run(() =>
                              {
                                  string prevText = string.Empty;
                                  while (true)
                                  {
                                      //キャンセルの確認
                                      source?.Token.ThrowIfCancellationRequested();

                                      //OCR 1行を2回
                                      Bitmap pictureBox7_image = GetImage(new Point(Location.X + 98, Location.Y + 500), pictureBox7);
                                      Bitmap pictureBox8_image = GetImage(new Point(Location.X + 98, Location.Y + 550), pictureBox8);

                                      string line1 = GetOCRText(pictureBox7_image);
                                      string line2 = GetOCRText(pictureBox8_image);

                                      if (line1 == "ョョョョョョョョョョョョョョ") continue;
                                      if (line2 == "ョョョョョョョョョョョョョョ") continue;

                                      string text = $"{line1}\r\n{line2}\r\n";

                                      if (text.Replace("\r\n", string.Empty).Length < 5) continue;

                                      if (prevText != string.Empty)
                                      {
                                          if (prevText.Length >= line1.Length)
                                          {
                                              if (prevText.Substring(0, line1.Length) == line1)
                                              {
                                                  //更新不要
                                                  continue;
                                              }
                                          }
                                      }


                                      Invoke((MethodInvoker)delegate ()
                                      {
                                          richTextBox1.Text += $"{text}\r\n";
                                      });

                                      prevText = text;

                                      //キャンセルの確認
                                      source?.Token.ThrowIfCancellationRequested();


                                      #region Local Function
                                      Bitmap GetImage(Point upperLeftSource, PictureBox pictureBox)
                                      {
                                          Bitmap bmp = new Bitmap(808, 38);

                                          using (Graphics graphics = Graphics.FromImage(bmp))
                                          {
                                              graphics.CopyFromScreen(upperLeftSource, new Point(0, 0), bmp.Size);
                                          }

                                          //デバッグ用
                                          //Invoke((MethodInvoker)delegate ()
                                          //{
                                          //    pictureBox.Image = bmp;
                                          //});

                                          return bmp;
                                      }

                                      string GetOCRText(Bitmap bmp)
                                      {
                                          using (Bitmap temp = bmp)
                                          {

                                              using (TesseractEngine engine = new TesseractEngine(@".\traineddata", "jpn"))
                                              {
                                                  string whitelist = string.Empty;

                                                  //whitelist += "0123456789";

                                                  whitelist += "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
                                                  whitelist += "abcdefghijklmnopqrstuvwxyz";

                                                  whitelist += "あいうえおかきくけこさしすせそたちつてとなにぬねのはひふへほまみむめもらりるれろやゆよわをんー";
                                                  whitelist += "がきぐげござじずぜぞだぢづでどばびぶべぼ";
                                                  whitelist += "ぱぴぷぺぽ";
                                                  whitelist += "ぁぃぅぇぉっゃゅょ";

                                                  whitelist += "アイウエオカキクケコサシスセソタチツテトナニヌネノハヒフヘホマミムメモラリルレロヤユヨワヲン―";
                                                  whitelist += "ガキグゲゴザジズゼゾダヂヅデドバビブベボ";
                                                  whitelist += "パピプペポ";
                                                  whitelist += "ァィゥェォッャュョ";


                                                  engine.SetVariable("tessedit_char_whitelist", whitelist);

                                                  using (Page page = engine.Process(temp, PageSegMode.SingleLine))
                                                  {
                                                      string t = page?.GetText();

                                                      void Replace(string s) => t = t.Replace(s, string.Empty);

                                                      Replace(" ");
                                                      Replace("\r");
                                                      Replace("\n");
                                                      Replace("/");
                                                      Replace("_");
                                                      return t;
                                                  }
                                              }
                                          }

                                      }
                                      #endregion Local Function

                                      ////1ms 待機
                                      //await Task.Delay(1);


                                  }
                              }, source.Token).ContinueWith(
                                   taskResult =>
                                   {
                                       switch (taskResult.Status)
                                       {
                                           case TaskStatus.Canceled:
                                               {
                                                   //キャンセルされたのでリソース開放
                                                   semaphore.Release();
                                                   break;
                                               }
                                           default:
                                               {
                                                   //想定外：何かが起きた
                                                   Invoke((MethodInvoker)delegate ()
                                                                  {
                                                                      richTextBox1.Text = taskResult?.Exception?.ToString();
                                                                      if (richTextBox1.Text == string.Empty) richTextBox1.Text = "何事???";
                                                                  });

                                                   break;
                                               }
                                       }
                                   });
            }

            else
            {
                LogText.Text = "Start: ReadLog";
            }



        }

        /// <summary>
        /// 画像のクリア
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Clean_Click(object sender, EventArgs e)
        {
            Clear();

            capImage.Image = null;
            pictureBox1.Image = null;
            pictureBox2.Image = null;
            pictureBox3.Image = null;
            pictureBox4.Image = null;
            pictureBox5.Image = null;
            pictureBox6.Image = null;
        }

        #region checkBox
        private void checkBox1_CheckedChanged(object sender, EventArgs e) => pictureBox9.Visible = !checkBox1.Checked;
        private void checkBox2_CheckedChanged(object sender, EventArgs e) => pictureBox10.Visible = !checkBox2.Checked;
        private void checkBox3_CheckedChanged(object sender, EventArgs e) => pictureBox11.Visible = !checkBox3.Checked;
        private void checkBox4_CheckedChanged(object sender, EventArgs e) => pictureBox12.Visible = !checkBox4.Checked;
        private void checkBox5_CheckedChanged(object sender, EventArgs e) => pictureBox13.Visible = !checkBox5.Checked;
        private void checkBox6_CheckedChanged(object sender, EventArgs e) => pictureBox14.Visible = !checkBox6.Checked;
        #endregion checkBox
    }
}
