using System;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Tesseract;
using WindowsFormsScreenCap.Services;
using WindowsFormsScreenCap.Services.Entities;

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

        private const string dataPath = @".\data";
        private const string imageAnalysisPath = @".\imageAnalysis";

        // 黒 白 赤　緑　青　黄色　紫　水色
        private (int bl, int wh, int r, int g, int b, int y, int p, int lb) count;

        public MainForm()
        {
            InitializeComponent();

            //ウィンドウを透明に
            TransparencyKey = Color.Red;

            //タイトルバー
            FormBorderStyle = FormBorderStyle.FixedDialog;

            //sqlite 初期処理
            _ = SqliteService.Current;

            //初期化
            Clear();

            //画像保存してから終わる
            FormClosing += (object sender, FormClosingEventArgs e) => SaveImage();
        }

        // 黒 白 赤　緑　青　黄色　紫　水色
        private ((int bl, int wh, int r, int g, int b, int y, int p, int lb), Bitmap bitmap) GetColorCount()
        {
            (int bl, int wh, int r, int g, int b, int y, int p, int lb) count = (0, 0, 0, 0, 0, 0, 0, 0);

            Bitmap bitmap = null;

            if (capImage.Image != null)
            {

                Bitmap capImageImage = (Bitmap)capImage.Image;

                //サイズ変更 不要な赤と青の色を与える性別情報を抜く
                Rectangle rect = new Rectangle(0, 0, 75, capImageImage.Height);

                bitmap = capImageImage.Clone(rect, capImageImage.PixelFormat);


                for (int i = 0; i < bitmap.Width; i++)
                {
                    for (int j = 0; j < bitmap.Height; j++)
                    {
                        Color color = bitmap.GetPixel(i, j);

                        //黒白はどうしよう悩ましい
                        if (color == Color.Black) count.bl++;
                        if (color == Color.White) count.wh++;

                        //red
                        {
                            //rの値がgとbの合計より大きければそれは赤とする
                            if (color.R > color.G + color.B) count.r++;
                        }
                        //green
                        {
                            if (color.G > color.R + color.B) count.g++;
                        }
                        //blue
                        {
                            if (color.B > color.R + color.G) count.b++;
                        }

                        // yellow
                        {
                            //黄色 赤と緑の合計が青の2倍より大きい
                            if (color.R + color.G > color.B * 2) count.y++;
                        }
                        //purple
                        {
                            //紫 赤と青の合計が緑の2倍より大きい
                            if (color.R + color.B > color.G * 2) count.p++;
                        }
                        //light blue
                        {
                            //水色 青と緑の合計が赤の2倍より大きい
                            if (color.B + color.G > color.R * 2) count.lb++;
                        }

                    }
                }

#if DEBUG
                bitmap.Save($"{dataPath}\\d_{count.r}_{count.g}_{count.b}_{count.y}_{count.p}_{count.lb}_{DateTime.Now:yyyyMMddhhmmss}.jpg", System.Drawing.Imaging.ImageFormat.Jpeg);
#endif 
            }

            return (count, bitmap);
        }

        private void SaveImage()
        {
            if (capImage.Image != null)
            {
                //イメージの画像を残しておく
                if (Directory.Exists(dataPath) == false)
                {
                    //ディレクトリがなければ作
                    Directory.CreateDirectory(dataPath);
                }

                using (Bitmap bmp = new Bitmap(Width, Height))
                {
                    using (Graphics graphics = Graphics.FromImage(bmp))
                        graphics.CopyFromScreen(new Point(Location.X, Location.Y), new Point(0, 0), bmp.Size);

                    bmp.Save($"{dataPath}\\{count.r}_{count.g}_{count.b}_{count.y}_{count.p}_{count.lb}_{DateTime.Now:yyyyMMddhhmmss}.jpg", System.Drawing.Imaging.ImageFormat.Jpeg);
                }



                //チェックボックスで選出結果のカウントアップ
                #region Local Function
                void CountUp(TextBox textBox, CheckBox check)
                {
                    string name = textBox.Text;
                    if (name != "???" && name != string.Empty)
                    {
                        if (check.Checked)
                        {
                            SqliteService.Current.CountUp(name);
                        }
                    }
                }
                #endregion Local Function
                

                //DB open
                SqliteService.Current.Open();

                CountUp(textBox3, checkBox7);
                CountUp(textBox4, checkBox8);
                CountUp(textBox5, checkBox9);
                CountUp(textBox6, checkBox10);
                CountUp(textBox7, checkBox11);
                CountUp(textBox8, checkBox12);

                //DB close
                SqliteService.Current.Close();

            }
        }

        private void Clear()
        {
            //元に戻す
            Cursor = Cursors.Arrow;
            stamp = null;

            //前のスレッドを停止
            richTextBox1.Text = "タスク終了中";
            source?.Cancel();
            groupBox1.Visible = false;
            groupBox2.Visible = false;

            richTextBox1.Text = string.Empty;

            textBox3.Text = string.Empty;
            textBox4.Text = string.Empty;
            textBox5.Text = string.Empty;
            textBox6.Text = string.Empty;
            textBox7.Text = string.Empty;
            textBox8.Text = string.Empty;

        }

        /// <summary>
        /// 画面をキャプチャ
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ScreenCap_Click(object sender, EventArgs e)
        {
            Clean_Click(sender, e);

            #region Local Function
            void SetImage(PictureBox picture, Point upperLeftSource)
            {
                var screenBounds = Screen.PrimaryScreen.Bounds;
                using (Bitmap bmp = new Bitmap(screenBounds.Width, screenBounds.Height))
                {
                    //ウィンドウのバー位置考慮の調整
                    Point upperLeftDestination = new Point(-60, -100);

                    using (Graphics graphics = Graphics.FromImage(bmp))
                    {
                        graphics.CopyFromScreen(upperLeftSource, upperLeftDestination,
                            new Size(picture.Width + Math.Abs(upperLeftDestination.X), picture.Height + Math.Abs(upperLeftDestination.Y)));
                    }

                    //サイズ変更
                    Rectangle rect = new Rectangle(0, 0, picture.Width, picture.Height);

                    picture.Image = bmp.Clone(rect, bmp.PixelFormat);
                }
            }
            #endregion Local Function

            SetImage(capImage, new Point(Location.X, Location.Y + 70));
            SetImage(pictureBox1, new Point(Location.X + 300, Location.Y - 45));
            SetImage(pictureBox2, new Point(Location.X + 300, Location.Y + 33));
            SetImage(pictureBox3, new Point(Location.X + 300, Location.Y + 105));
            SetImage(pictureBox4, new Point(Location.X + 300, Location.Y + 182));
            SetImage(pictureBox5, new Point(Location.X + 300, Location.Y + 253));
            SetImage(pictureBox6, new Point(Location.X + 300, Location.Y + 333));

            var GetColorCountResult = GetColorCount();
            count = GetColorCountResult.Item1;
            using (Bitmap iaImage = GetColorCountResult.bitmap)
            {
                #region Local Function
                void SetIAImage(PictureBox picture, int x, int y)
                {
                    using (Bitmap bmp = new Bitmap(picture.Width, picture.Height))
                    {
                        //サイズ変更
                        Rectangle rect = new Rectangle(x, y, picture.Width, picture.Height);

                        picture.Image = iaImage.Clone(rect, bmp.PixelFormat);
                    }
                }
                #endregion Local Function

                SetIAImage(pictureBox15, 10, 0);
                SetIAImage(pictureBox16, 10, 65);
                SetIAImage(pictureBox17, 10, 135);
                SetIAImage(pictureBox18, 10, 200);
                SetIAImage(pictureBox19, 10, 260);
                SetIAImage(pictureBox20, 10, 320);
            }

            //近そうな画像を探す
            SelectImage();


        }

        /// <summary>
        /// カラーパターンが近い画像を探す
        /// </summary>
        private void SelectImage()
        {
            listView1.Items.Clear();

            if (Directory.Exists(dataPath))
            {

                string[] files = Directory.GetFiles(dataPath);

                foreach (string filePath in files)
                {
                    //ファイル名だけにする
                    string file = filePath.Replace(dataPath, string.Empty).Replace("\\", string.Empty);

                    //要素を分解
                    string[] elements = file.Split('_');

                    if (elements[0] != "d" && elements.Length > 3)
                    {
                        int r = int.Parse(elements[0]);
                        int g = int.Parse(elements[1]);
                        int b = int.Parse(elements[2]);
                        int y = int.Parse(elements[3]);
                        int p = int.Parse(elements[4]);
                        int lb = int.Parse(elements[5]);

                        //閾値
                        int threshold = int.Parse(thresholdValue.Text);

                        //±閾値
                        bool isWithin(int value, int target) => value + threshold >= target && target >= value - threshold;

                        if (isWithin(count.r, r))
                            if (isWithin(count.g, g))
                                if (isWithin(count.b, b))
                                    if (isWithin(count.y, y))
                                        if (isWithin(count.p, p))
                                            if (isWithin(count.lb, lb))
                                            {
                                                _ = listView1.Items.Add(file);
                                            }
                    }
                }


            }
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

        private void button15_Click(object sender, EventArgs e) => Button_Click(Properties.Resources._1);
        private void button16_Click(object sender, EventArgs e) => Button_Click(Properties.Resources._2);
        private void button17_Click(object sender, EventArgs e) => Button_Click(Properties.Resources._3);

        /// <summary>
        /// コメントをスタンプ化
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button18_Click(object sender, EventArgs e)
        {
            Bitmap bitmap = new Bitmap(50, 50);

            using (Graphics g = Graphics.FromImage(bitmap))
            {
                using (Font font = new Font("Meiryo", 10))
                {
                    g.DrawString(textBox2.Text, font, Brushes.BlueViolet, 0, 0);

                    Button_Click(bitmap);
                }
            }


        }

        private void button19_Click(object sender, EventArgs e)
        {
            //カーソルを元に戻す
            Cursor = Cursors.Arrow;
            stamp = null;
        }

        private void button20_Click(object sender, EventArgs e) => SelectImage();
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

                richTextBox1.Text = string.Empty;

                //CancellationTokenSourceの作り直し
                //source?.Dispose();
                source = new CancellationTokenSource();
                CancellationToken token = source.Token;

                //ワーカースレッド 翻訳処理
                //await Task.Run( async() =>
                await Task.Run(() =>
                              {
                                  string prevText = string.Empty;
                                  while (true)
                                  {
                                      //キャンセルの確認
                                      token.ThrowIfCancellationRequested();

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
                                      token.ThrowIfCancellationRequested();


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
                              }, token).ContinueWith(
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
            SaveImage();

            Clear();

            capImage.Image = null;
            pictureBox1.Image = null;
            pictureBox2.Image = null;
            pictureBox3.Image = null;
            pictureBox4.Image = null;
            pictureBox5.Image = null;
            pictureBox6.Image = null;

            checkBox1.Checked = false;
            checkBox2.Checked = false;
            checkBox3.Checked = false;
            checkBox4.Checked = false;
            checkBox5.Checked = false;
            checkBox6.Checked = false;
        }

        #region checkBox
        private void checkBox1_CheckedChanged(object sender, EventArgs e) => pictureBox9.Visible = !checkBox1.Checked;
        private void checkBox2_CheckedChanged(object sender, EventArgs e) => pictureBox10.Visible = !checkBox2.Checked;
        private void checkBox3_CheckedChanged(object sender, EventArgs e) => pictureBox11.Visible = !checkBox3.Checked;
        private void checkBox4_CheckedChanged(object sender, EventArgs e) => pictureBox12.Visible = !checkBox4.Checked;
        private void checkBox5_CheckedChanged(object sender, EventArgs e) => pictureBox13.Visible = !checkBox5.Checked;
        private void checkBox6_CheckedChanged(object sender, EventArgs e) => pictureBox14.Visible = !checkBox6.Checked;
        #endregion checkBox

        /// <summary>
        /// 選択された画像を開く
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void listView1_SelectedIndexChanged_1(object sender, EventArgs e) => System.Diagnostics.Process.Start($"{dataPath}\\{ ((ListView)sender).FocusedItem.Text}");

        /// <summary>
        /// 画像のあるフォルダを開く
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OpenWindowsExplorer_Click_1(object sender, EventArgs e) => System.Diagnostics.Process.Start(dataPath);

        #region 画像解析のボタン
        private void SaveImageForImageAnalysis(string name, PictureBox pictureBox)
        {
            //イメージの画像を残しておく
            if (Directory.Exists(imageAnalysisPath) == false)
            {
                //ディレクトリがなければ作
                Directory.CreateDirectory(imageAnalysisPath);
            }

            string fileName = $"{name}-{DateTime.Now:yyyyMMddhhmmss}.jpg";

            pictureBox.Image.Save($"{imageAnalysisPath}\\{fileName}", System.Drawing.Imaging.ImageFormat.Jpeg);

            ColorsEntity entity = GetColorsEntity((Bitmap)pictureBox.Image, fileName);

            SqliteService.Current.Insert(entity);
        }

        private ColorsEntity GetColorsEntity(Bitmap bitmap, string fileName = "")
        {
            ColorsEntity entity = new ColorsEntity()
            {
                FileName = fileName,
            };

            for (int i = 0; i < bitmap.Width; i++)
            {
                for (int j = 0; j < bitmap.Height; j++)
                {
                    Color color = bitmap.GetPixel(i, j);

                    switch (color)
                    {

                        //red
                        case Color c when c.R > c.G + c.B:
                            {
                                //rの値がgとbの合計より大きければそれは赤とする
                                entity.Red++;

                                break;
                            }
                        //green
                        case Color c when c.G > c.R + c.B:
                            {
                                entity.Green++;

                                break;
                            }
                        //blue
                        case Color c when c.B > c.R + c.G:
                            {
                                entity.Blue++;

                                break;
                            }

                        // yellow
                        case Color c when c.R + c.G > c.B * 2:
                            {
                                //黄色 赤と緑の合計が青の2倍より大きい
                                entity.Yellow++;

                                break;
                            }
                        //purple
                        case Color c when c.R + c.B > c.G * 2:
                            {
                                //紫 赤と青の合計が緑の2倍より大きい
                                entity.Purple++;

                                break;
                            }
                        //light blue
                        case Color c when c.B + c.G > c.R * 2:
                            {
                                //水色 青と緑の合計が赤の2倍より大きい
                                entity.LightBlue++;

                                break;
                            }
                        //Black
                        case Color c when 150 > c.B + c.G + c.R:
                            {
                                entity.Black++;

                                break;
                            }
                        //Gray
                        case Color c when 600 > c.B + c.G + c.R:
                            {
                                entity.Gray++;

                                break;
                            }
                        //White
                        default:
                            {
                                entity.White++;

                                break;
                            }
                    }
                }
            }

            return entity;
        }

        private void button22_Click(object sender, EventArgs e) => SaveImageForImageAnalysis(textBox3.Text, pictureBox15);
        private void button21_Click(object sender, EventArgs e) => SaveImageForImageAnalysis(textBox4.Text, pictureBox16);
        private void button23_Click(object sender, EventArgs e) => SaveImageForImageAnalysis(textBox5.Text, pictureBox17);
        private void button24_Click(object sender, EventArgs e) => SaveImageForImageAnalysis(textBox6.Text, pictureBox18);
        private void button25_Click(object sender, EventArgs e) => SaveImageForImageAnalysis(textBox7.Text, pictureBox19);
        private void button26_Click(object sender, EventArgs e) => SaveImageForImageAnalysis(textBox8.Text, pictureBox20);
        #endregion 画像解析のボタン

        /// <summary>
        /// 画像解析の実行
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button27_Click(object sender, EventArgs e)
        {
            richTextBox2.Text = string.Empty;

            //誤差
            decimal thresholdValue = 50;

            #region Local Function
            void Select(TextBox textBox, PictureBox picture)
            {
                ColorsEntity entity = GetColorsEntity((Bitmap)picture.Image);
                var result = SqliteService.Current.Select(entity, thresholdValue);

                if (result.name != "???")
                {
                    textBox.Text = result.name;

                    richTextBox2.Text += $"{textBox.Text} は {result.count} 回選出されていたようだ\r\n";
                }
                else
                {
                    textBox.Text = "???";
                }
            }
            #endregion Local Function

            //DB open
            SqliteService.Current.Open();

            Select(textBox3, pictureBox15);
            Select(textBox4, pictureBox16);
            Select(textBox5, pictureBox17);
            Select(textBox6, pictureBox18);
            Select(textBox7, pictureBox19);
            Select(textBox8, pictureBox20);

            //DB close
            SqliteService.Current.Close();
        }

        private void button28_Click(object sender, EventArgs e)
        {
            richTextBox2.Text = string.Empty;

            SqliteService.Current.Reset();

            textBox3.Text = string.Empty;
            textBox4.Text = string.Empty;
            textBox5.Text = string.Empty;
            textBox6.Text = string.Empty;
            textBox7.Text = string.Empty;
            textBox8.Text = string.Empty;

        }
    }
}
