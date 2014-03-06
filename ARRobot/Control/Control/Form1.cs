using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Diagnostics;
using System.Threading;
using System.Drawing.Imaging;
using AForge;
using AForge.Math;
using AForge.Video;
using AForge.Video.DirectShow;
using AForge.Imaging;
using AForge.Imaging.Filters;
using AForge.Vision.GlyphRecognition;

namespace Control
{
    public partial class Form1 : Form
    {
        // AForge.Vision.GlyphRecognition.
        private GlyphDatabases glyphDatabases = new GlyphDatabases();
        private const string activeDatabaseOption = "ActiveDatabase";
        private GlyphDatabase activeGlyphDatabase = null;
        private string activeGlyphDatabaseName = null;
        private GlyphImageProcessor imageProcessor = new GlyphImageProcessor();
        private ImageList glyphsImageList = new ImageList();
        string glyphNameInEditor = string.Empty;
        private object sync = new object();
        private Stopwatch stopWatch = null;


        //-----------------------------------------------------------------
        CamerasController cam = new CamerasController();
        public delegate void SetTextDelegate(string text);
        private IntRange redRange = new IntRange(0, 255);
        private IntRange greenRange = new IntRange(0, 255);
        private IntRange blueRange = new IntRange(0, 255);
        public event EventHandler OnFilterUpdate;

        // Red range
        public IntRange RedRange
        {
            get { return redRange; }
            set
            {
                redRange = value;
                redSlider.Min = value.Min;
                redSlider.Max = value.Max;
            }
        }
        // Green range
        public IntRange GreenRange
        {
            get { return greenRange; }
            set
            {
                greenRange = value;
                greenSlider.Min = value.Min;
                greenSlider.Max = value.Max;
            }
        }
        // Blue range
        public IntRange BlueRange
        {
            get { return blueRange; }
            set
            {
                blueRange = value;
                blueSlider.Min = value.Min;
                blueSlider.Max = value.Max;
            }
        }

        // Red range was changed
        private void redSlider_ValuesChanged(object sender, EventArgs e)
        {
            redRange.Min = redSlider.Min;
            redRange.Max = redSlider.Max;
            txtRedMin.Text = redRange.Min.ToString();
            txtRedMax.Text = redRange.Max.ToString();
            lblTestColor.BackColor = Color.FromArgb((int)(redRange.Min + redRange.Max) / 2, (int)(greenRange.Min + greenRange.Max) / 2, (int)(blueRange.Min + blueRange.Max) / 2);
            if (OnFilterUpdate != null)
                OnFilterUpdate(this, null);
        }

        // Green range was changed
        private void greenSlider_ValuesChanged(object sender, EventArgs e)
        {
            greenRange.Min = greenSlider.Min;
            greenRange.Max = greenSlider.Max;
            txtGreenMin.Text = greenSlider.Min.ToString();
            txtGreenMax.Text = greenSlider.Max.ToString();
            lblTestColor.BackColor = Color.FromArgb((int)(redRange.Min + redRange.Max) / 2, (int)(greenRange.Min + greenRange.Max) / 2, (int)(blueRange.Min + blueRange.Max) / 2);
            if (OnFilterUpdate != null)
                OnFilterUpdate(this, null);
        }

        // Blue range was changed
        private void blueSlider_ValuesChanged(object sender, EventArgs e)
        {
            blueRange.Min = blueSlider.Min;
            blueRange.Max = blueSlider.Max;
            txtBlueMin.Text = blueSlider.Min.ToString();
            txtBlueMax.Text = blueSlider.Max.ToString();
            lblTestColor.BackColor = Color.FromArgb((int)(redRange.Min + redRange.Max) / 2, (int)(greenRange.Min + greenRange.Max) / 2, (int)(blueRange.Min + blueRange.Max) / 2);
            if (OnFilterUpdate != null)
                OnFilterUpdate(this, null);
        }




        private void tuneObjectFilterForm_OnFilterUpdate(object sender, EventArgs eventArgs)
        {
            colorFilter.Red = RedRange;
            colorFilter.Green = GreenRange;
            colorFilter.Blue = BlueRange;
            this.Text = "ok";


        }






        // list of video devices

        // form for cameras' movement
        //MoveCamerasForm moveCamerasForm;
        // form to show detected objects
        //DetectedObjectsForm detectedObjectsForm;
        // form to tune object detection filter
        //TuneObjectFilterForm tuneObjectFilterForm;





        ColorFiltering colorFilter = new ColorFiltering();
        GrayscaleBT709 grayFilter = new GrayscaleBT709();
        // use two blob counters, so the could run in parallel in two threads
        BlobCounter blobCounter1 = new BlobCounter();
        BlobCounter blobCounter2 = new BlobCounter();

        private AutoResetEvent camera1Acquired = null;
        private AutoResetEvent camera2Acquired = null;
        private Thread trackingThread = null;

        // object coordinates in both cameras
        private float x1, y1, x2, y2;

        public Form1()
        {
            InitializeComponent();
        }

        private void TabControl1_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void Button5_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void videoSourcePlayer1_Click(object sender, EventArgs e)
        {

        }

        private void button8_Click(object sender, EventArgs e)
        {
            VideoCaptureDeviceForm form = new VideoCaptureDeviceForm();


            if (form.ShowDialog(this) == DialogResult.OK)
            {
                // set busy cursor
                //  this.Cursor = Cursors.WaitCursor;

                // reset glyph processor
                imageProcessor.Reset();

                cam.StartCameras(form.VideoDevice, this.videoSourcePlayer);

                // reset stop watch
                stopWatch = null;

                // start timer
                timer.Start();
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            //if (sp.IsOpen)
            //{
            //    sp.Close();
            //    sp.Open();
            //}
            //else



            //    sp.Open();




            List<string> l = cam.FindCamera();
            for (int i = 0; i < l.Count; i++)
            {
                this.camera1Combo.Items.Add(l.ElementAt<string>(i));
                this.camera2Combo.Items.Add(l.ElementAt<string>(i));
            }
            this.camera1Combo.SelectedIndex = 0;
            //this.camera2Combo.SelectedIndex = 1;

            colorFilter.Red = new IntRange(0, 100);
            colorFilter.Green = new IntRange(0, 200);
            colorFilter.Blue = new IntRange(150, 255);

            RedRange = colorFilter.Red;
            GreenRange = colorFilter.Green;
            BlueRange = colorFilter.Blue;





            // configure blob counters
            blobCounter1.MinWidth = 25;
            blobCounter1.MinHeight = 25;
            blobCounter1.FilterBlobs = true;
            blobCounter1.ObjectsOrder = ObjectsOrder.Size;

            blobCounter2.MinWidth = 25;
            blobCounter2.MinHeight = 25;
            blobCounter2.FilterBlobs = true;
            blobCounter2.ObjectsOrder = ObjectsOrder.Size;

            glyphsImageList.ImageSize = new Size(100, 100);
            glyphList.LargeImageList = glyphsImageList;

            Configuration config = Configuration.Instance;


            if (config.Load(glyphDatabases))
            {
                RefreshListOfGlyphDatabases();
                ActivateGlyphDatabase(config.GetConfigurationOption(activeDatabaseOption));

            }
        }



        private void button20_Click(object sender, EventArgs e)
        {
            cam.StartCameras("abed", VP1, VP2, this.camera1Combo.SelectedIndex, this.camera2Combo.SelectedIndex);
            OnFilterUpdate += new EventHandler(tuneObjectFilterForm_OnFilterUpdate);

            camera1Acquired = new AutoResetEvent(false);
            camera2Acquired = new AutoResetEvent(false);
            //start tracking thread
            trackingThread = new Thread(new ThreadStart(TrackingThread));
            trackingThread.Start();


        }

        private void VP1_NewFrame(object sender, ref Bitmap image)
        {
            // if (1==1)
            // {
            Bitmap objectImage = colorFilter.Apply(image);

            // lock image for further processing
            BitmapData objectData = objectImage.LockBits(new Rectangle(0, 0, image.Width, image.Height),
                ImageLockMode.ReadOnly, image.PixelFormat);

            // grayscaling
            UnmanagedImage grayImage = grayFilter.Apply(new UnmanagedImage(objectData));

            // unlock image
            objectImage.UnlockBits(objectData);

            // locate blobs 
            blobCounter1.ProcessImage(grayImage);
            Rectangle[] rects = blobCounter1.GetObjectsRectangles();

            if (rects.Length > 0)
            {
                Rectangle objectRect = rects[0];

                // draw rectangle around derected object
                Graphics g = Graphics.FromImage(image);

                using (Pen pen = new Pen(Color.FromArgb(160, 255, 160), 3))
                {
                    g.DrawRectangle(pen, objectRect);
                }

                g.Dispose();

                // get object's center coordinates relative to image center
                lock (this)
                {
                    x1 = (objectRect.Left + objectRect.Right - objectImage.Width) / 2;
                    y1 = (objectImage.Height - (objectRect.Top + objectRect.Bottom)) / 2;
                    // map to [-1, 1] range
                    x1 /= (objectImage.Width / 2);
                    y1 /= (objectImage.Height / 2);
                    int tempX = (int)x1 * 100;
                    camera1Acquired.Set();
                    SetText(txtRX, tempX.ToString());
                    SetText(txtRY, y1.ToString());
                }
            }


            UpdateObjectPicture(0, objectImage);
            //}
        }




        // received frame from the 2nd camera
        private void VP2_NewFrame(object sender, ref Bitmap image)
        {
            //  if (1==1)
            // {
            Bitmap objectImage = colorFilter.Apply(image);

            // lock image for further processing
            BitmapData objectData = objectImage.LockBits(new Rectangle(0, 0, image.Width, image.Height),
                ImageLockMode.ReadOnly, image.PixelFormat);

            // grayscaling
            UnmanagedImage grayImage = grayFilter.Apply(new UnmanagedImage(objectData));

            // unlock image
            objectImage.UnlockBits(objectData);

            // locate blobs 
            blobCounter2.ProcessImage(grayImage);
            Rectangle[] rects = blobCounter2.GetObjectsRectangles();

            if (rects.Length > 0)
            {
                Rectangle objectRect = rects[0];

                // draw rectangle around derected object
                Graphics g = Graphics.FromImage(image);

                using (Pen pen = new Pen(Color.FromArgb(160, 255, 160), 3))
                {
                    g.DrawRectangle(pen, objectRect);
                }

                g.Dispose();

                // get object's center coordinates relative to image center
                lock (this)
                {
                    x2 = (objectRect.Left + objectRect.Right - objectImage.Width) / 2;
                    y2 = (objectImage.Height - (objectRect.Top + objectRect.Bottom)) / 2;
                    // map to [-1, 1] range
                    x2 /= (objectImage.Width / 2);
                    y2 /= (objectImage.Height / 2);
                    int tempX = (int)x2 * 100;
                    camera2Acquired.Set();
                    SetText(txtLX, tempX.ToString());
                    SetText(txtLY, y2.ToString());


                }
            }


            UpdateObjectPicture(1, objectImage);
            //}
        }
        int tempX = 0;
        private void TrackingThread()
        {
            int targetX = 0;
            int targetY = 0;

            while (true)
            {
                camera1Acquired.WaitOne();
                camera2Acquired.WaitOne();

                lock (this)
                {
                    // stop the thread if it was signaled
                    if ((x1 == -1) && (y1 == -1) && (x2 == -1) && (y2 == -1))
                    {
                        break;
                    }

                    // get middle point
                    targetX = ((((int)(((x1 + x2) / 2) * 100))) % 180);
                    targetY = ((int)(((y1 + y2) / 2) * 100));

                    //if (Math.Abs(targetX - tempX) <= 5 || (tempX == 0))
                    //{

                    //    tempX = targetX;


                    tempX = targetX;
                    SetText(txtArmBase, targetX.ToString());
                    // if(tempX!=0)

                    //}



                }




                int r = Convert.ToInt32(this.txtArmBase.Text) * -1;
                r += 90;



                SetText(txtArmUp, r.ToString());



                //if (moveCamerasForm != null)
                //{
                //    // run motors for the specified amount of degrees
                //    moveCamerasForm.RunMotors(2 * targetX, -2 * targetY);
                //}
            }
        }


        private static int z = 90;
        int count = 0;
        int last = 0;
        bool called = false;
        Thread th;
        int temp = 0;
        int Counter = 0;
        int sum = 0;
        void MoveServo()
        {


            //while(true)
            // {
            //     x += 10;




            // sp.Write(txtArmUp.Text);
            // MessageBox.Show(txtArmUp.Text);


            called = false;

            //while (true)
            //{
            //    count++;

            //    int r = Convert.ToInt32(txtArmUp.Text);


            //    if (last != r && r > 0 && !called)
            //    {
            //        temp += r;
            //        if (count == 5)
            //        {
            //            called = true;
            //            temp = (temp / 5);

            //            //abed
            //            sp.Write(temp.ToString());
            //            last = temp;
            //            temp = 0;

            //        }
            //    }
            //    string str = "";

            //    if (sp.ReadExisting().Equals("0"))
            //    {
            //        //MessageBox.Show("Done");
            //        called = false;
            //        // SetText(txtArmUp, "90");

            //        count = 0;
            //    }

            Thread.Sleep(2000);
            //}

        }
        private void SetText(TextBox txt, string text)
        {
            if (txt.InvokeRequired)
            {
                Invoke((MethodInvoker)(() => txt.Text = text));
                int r = Convert.ToInt32(txtArmUp.Text);
                if (!called && r > 20)
                {
                    th = new Thread(new ThreadStart(MoveServo));
                    th.Start();
                    called = true;

                }



            }
            else
            {
                txt.Text = text;


            }
        }
        private void button19_Click(object sender, EventArgs e)
        {
            redSlider.Min = Convert.ToInt32(txtRedMin.Text);
            redSlider.Max = Convert.ToInt32(txtRedMax.Text);


        }

        private void button18_Click(object sender, EventArgs e)
        {
            greenSlider.Min = Convert.ToInt32(txtGreenMin.Text);
            greenSlider.Max = Convert.ToInt32(txtGreenMax.Text);

        }

        private void button21_Click(object sender, EventArgs e)
        {
            blueSlider.Min = Convert.ToInt32(txtBlueMin.Text);
            blueSlider.Max = Convert.ToInt32(txtBlueMax.Text);
        }

        private void groupBox2_Enter(object sender, EventArgs e)
        {

        }




        // Update object's picture
        public void UpdateObjectPicture(int objectNumber, Bitmap picture)
        {
            System.Drawing.Image oldPicture = null;

            switch (objectNumber)
            {
                case 0:
                    oldPicture = pictureBox1.Image;
                    pictureBox1.Image = picture;
                    break;
                case 1:
                    oldPicture = pictureBox2.Image;
                    pictureBox2.Image = picture;
                    break;
            }

            if (oldPicture != null)
            {
                oldPicture.Dispose();
            }
        }

        private void button17_Click(object sender, EventArgs e)
        {
            this.Close();

        }

        private void Button6_Click(object sender, EventArgs e)
        {
            //NewGlyphCollectionForm newCollectionForm = new NewGlyphCollectionForm(glyphDatabases.GetDatabaseNames());

            ////if (newCollectionForm.ShowDialog() == DialogResult.OK)
            ////{
            string name = nameBox.Text;
            int size = sizeCombo.SelectedIndex + 5;

            GlyphDatabase db = new GlyphDatabase(size);

            try
            {
                glyphDatabases.AddGlyphDatabase(name, db);

                // add new item to list view
                ListViewItem lvi = glyphCollectionsList.Items.Add(name);
                lvi.SubItems.Add(string.Format("{0}x{1}", size, size));
                lvi.Name = name;
            }
            catch
            {
                //ShowErrorBox(string.Format("A glyph database with the name '{0}' already exists.", name));
            }

        }

        private void RefreshListOfGlyphDatabases()
        {
            glyphCollectionsList.Items.Clear();

            List<string> dbNames = glyphDatabases.GetDatabaseNames();

            foreach (string name in dbNames)
            {
                GlyphDatabase db = glyphDatabases[name];
                ListViewItem lvi = glyphCollectionsList.Items.Add(name);
                lvi.Name = name;

                lvi.SubItems.Add(string.Format("{0}x{1}", db.Size, db.Size));
            }
        }

        private void ActivateGlyphDatabase(string name)
        {
            ListViewItem lvi;

            // deactivate previous database
            if (activeGlyphDatabase != null)
            {
                lvi = GetListViewItemByName(glyphCollectionsList, activeGlyphDatabaseName);

                if (lvi != null)
                {
                    Font font = new Font(lvi.Font, FontStyle.Regular);
                    lvi.Font = font;
                }
            }



            // activate new database
            activeGlyphDatabaseName = name;

            if (name != null)
            {
                try
                {
                    activeGlyphDatabase = glyphDatabases[name];

                    lvi = GetListViewItemByName(glyphCollectionsList, name);

                    if (lvi != null)
                    {
                        Font font = new Font(lvi.Font, FontStyle.Bold);
                        lvi.Font = font;
                    }
                }
                catch
                {
                }
            }
            else
            {
                activeGlyphDatabase = null;
            }

            // set the database to image processor ...
            imageProcessor.GlyphDatabase = activeGlyphDatabase;
            // ... and show it to user
            RefreshListOfGlyps();
        }



        // Get item from a list view by its name
        private ListViewItem GetListViewItemByName(ListView lv, string name)
        {
            try
            {
                return lv.Items[name];
            }
            catch
            {
                return null;
            }
        }

        private void button7_Click(object sender, EventArgs e)
        {
            if (glyphCollectionsList.SelectedIndices.Count == 1)
            {
                ActivateGlyphDatabase(glyphCollectionsList.SelectedItems[0].Text);
            }
        }


        // Refresh the list of glyph contained in currently active database
        private void RefreshListOfGlyps()
        {
            // clear list view and its image list
            glyphList.Items.Clear();
            glyphsImageList.Images.Clear();

            if (activeGlyphDatabase != null)
            {
                // update image list first
                foreach (Glyph glyph in activeGlyphDatabase)
                {
                    // create icon for the glyph first
                    glyphsImageList.Images.Add(glyph.Name, CreateGlyphIcon(glyph));

                    // create glyph's list view item
                    ListViewItem lvi = glyphList.Items.Add(glyph.Name);
                    lvi.ImageKey = glyph.Name;
                }
            }
        }

        // Create icon for a glyph
        private Bitmap CreateGlyphIcon(Glyph glyph)
        {
            return CreateGlyphImage(glyph, 32);
        }

        private Bitmap CreateGlyphImage(Glyph glyph, int width)
        {
            Bitmap bitmap = new Bitmap(width, width, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

            int cellSize = width / glyph.Size;
            int glyphSize = glyph.Size;


            for (int i = 0; i < width; i++)
            {
                int yCell = i / cellSize;

                for (int j = 0; j < width; j++)
                {
                    int xCell = j / cellSize;

                    if ((yCell >= glyphSize) || (xCell >= glyphSize))
                    {
                        // set pixel to transparent if it outside of the glyph
                        bitmap.SetPixel(j, i, Color.Transparent);
                    }
                    else
                    {
                        // set pixel to black or white depending on glyph value
                        bitmap.SetPixel(j, i,
                            (glyph.Data[yCell, xCell] == 0) ? Color.Black : Color.White);
                    }
                }
            }


            return bitmap;
        }

        private void button3_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void button22_Click(object sender, EventArgs e)
        {
            if (activeGlyphDatabase != null)
            {
                // create new glyph ...
                Glyph glyph = new Glyph(string.Empty, activeGlyphDatabase.Size);
                MessageBox.Show(activeGlyphDatabase.Size.ToString());
                glyphNameInEditor = string.Empty;
                // ... and pass it the glyph editting form
                EditGlyphForm glyphForm = new EditGlyphForm(glyph, activeGlyphDatabase.GetGlyphNames());
                glyphForm.Text = "New Tag";

                // set glyph data checking handler
                glyphForm.SetGlyphDataCheckingHandeler(new GlyphDataCheckingHandeler(CheckGlyphData));

                if (glyphForm.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        lock (sync)
                        {



                            // add glyph to active database
                            activeGlyphDatabase.Add(glyph);
                        }

                        // create an icon for it
                        glyphsImageList.Images.Add(glyph.Name, CreateGlyphIcon(glyph));
                        //  p.Image = CreateGlyphIcon(glyph);

                        // add it to list view
                        ListViewItem lvi = glyphList.Items.Add(glyph.Name);
                        lvi.ImageKey = glyph.Name;
                    }
                    catch
                    {

                        this.Close();
                        MessageBox.Show(string.Format("A glyph with the name '{0}' already exists in the database.", glyph.Name));
                    }

                }
            }
        }

        //download Images


        public System.Drawing.Image DownloadImage(string _URL)
        {
            System.Drawing.Image _tmpImage = null;

            try
            {
                // Open a connection
                System.Net.HttpWebRequest _HttpWebRequest = (System.Net.HttpWebRequest)System.Net.HttpWebRequest.Create(_URL);

                _HttpWebRequest.AllowWriteStreamBuffering = true;

                // You can also specify additional header values like the user agent or the referer: (Optional)
                _HttpWebRequest.UserAgent = "Mozilla/4.0 (compatible; MSIE 7.0; Windows NT 5.1)";
                _HttpWebRequest.Referer = "http://www.google.com/";

                // set timeout for 20 seconds (Optional)
                _HttpWebRequest.Timeout = 20000;

                // Request response:
                System.Net.WebResponse _WebResponse = _HttpWebRequest.GetResponse();

                // Open data stream:
                System.IO.Stream _WebStream = _WebResponse.GetResponseStream();

                // convert webstream to image
                _tmpImage = System.Drawing.Image.FromStream(_WebStream);

                // Cleanup
                _WebResponse.Close();
                _WebResponse.Close();
            }
            catch (Exception _Exception)
            {
                // Error
                Console.WriteLine("Exception caught in process: {0}", _Exception.ToString());
                return null;
            }

            return _tmpImage;
        }



        // Handler for checking glyph data - need to make sure there is not such glyph in database already
        private bool CheckGlyphData(byte[,] glyphData)
        {
            if (activeGlyphDatabase != null)
            {
                int rotation;
                Glyph recognizedGlyph = activeGlyphDatabase.RecognizeGlyph(glyphData, out rotation);

                if ((recognizedGlyph != null) && (recognizedGlyph.Name != glyphNameInEditor))
                {
                    MessageBox.Show("The database already contains a glyph which looks the same as it is or after rotation.");
                    return false;
                }
            }

            return true;
        }

        private void Button4_Click(object sender, EventArgs e)
        {
            Form2 frm = new Form2();
            frm.Show();
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            sp.Close();
            Configuration config = Configuration.Instance;

            if (WindowState != FormWindowState.Minimized)
            {
                if (WindowState != FormWindowState.Maximized)
                {
                    //config.SetConfigurationOption(mainFormXOption, Location.X.ToString());
                    //config.SetConfigurationOption(mainFormYOption, Location.Y.ToString());
                    //config.SetConfigurationOption(mainFormWidthOption, Width.ToString());
                    //config.SetConfigurationOption(mainFormHeightOption, Height.ToString());
                }
                //config.SetConfigurationOption(mainFormStateOption, WindowState.ToString());
                //config.SetConfigurationOption(mainSplitterOption, splitContainer.SplitterDistance.ToString());
            }

            config.SetConfigurationOption(activeDatabaseOption, activeGlyphDatabaseName);

            //config.SetConfigurationOption(autoDetectFocalLengthOption, autoDetectFocalLength.ToString());
            //config.SetConfigurationOption(focalLengthOption, imageProcessor.CameraFocalLength.ToString());
            //config.SetConfigurationOption(glyphSizeOption, imageProcessor.GlyphSize.ToString());

            try
            {
                config.Save(glyphDatabases);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed saving confguration file.\r\n\r\n" + ex.Message);
            }
        }

        private void button23_Click(object sender, EventArgs e)
        {
            if ((activeGlyphDatabase != null) && (glyphList.SelectedIndices.Count != 0))
            {
                // get selected item
                ListViewItem lvi = glyphList.SelectedItems[0];

                // remove glyph from database, from list view and image list
                lock (sync)
                {
                    activeGlyphDatabase.Remove(lvi.Text);
                }
                glyphList.Items.Remove(lvi);
                glyphsImageList.Images.RemoveByKey(lvi.Text);
            }
        }

        private void timer_Tick(object sender, EventArgs e)
        {
            IVideoSource videoSource = videoSourcePlayer.VideoSource;

            if (videoSource != null)
            {
                // get number of frames since the last timer tick
                int framesReceived = videoSource.FramesReceived;

                if (stopWatch == null)
                {
                    stopWatch = new Stopwatch();
                    stopWatch.Start();
                }
                else
                {
                    stopWatch.Stop();

                    float fps = 1000.0f * framesReceived / stopWatch.ElapsedMilliseconds;
                    //fpsLabel.Text = fps.ToString( "F2" ) + " fps";

                    stopWatch.Reset();
                    stopWatch.Start();
                }
            }
        }



        // On new video frame
        private void videoSourcePlayer_NewFrame(object sender, ref Bitmap image)
        {
            if (activeGlyphDatabase != null)
            {
                if (image.PixelFormat == PixelFormat.Format8bppIndexed)
                {
                    // convert image to RGB if it is grayscale
                    GrayscaleToRGB filter = new GrayscaleToRGB();

                    Bitmap temp = filter.Apply(image);
                    image.Dispose();
                    image = temp;
                }


                lock (sync)
                {
                    List<ExtractedGlyphData> glyphs = imageProcessor.ProcessImage(image);

                    //if (arForm != null)
                    //{
                    //    List<VirtualModel> modelsToDisplay = new List<VirtualModel>();

                    //    foreach (ExtractedGlyphData glyph in glyphs)
                    //    {
                    //        if ((glyph.RecognizedGlyph != null) &&
                    //             (glyph.RecognizedGlyph.UserData != null) &&
                    //             (glyph.RecognizedGlyph.UserData is GlyphVisualizationData) &&
                    //             (glyph.IsTransformationDetected))
                    //        {
                    //            modelsToDisplay.Add(new VirtualModel(
                    //                ((GlyphVisualizationData)glyph.RecognizedGlyph.UserData).ModelName,
                    //                glyph.TransformationMatrix,
                    //                imageProcessor.GlyphSize));

                    //        }
                    //    }

                    // arForm.UpdateScene(image, modelsToDisplay);
                }
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            imageProcessor.VisualizationType = VisualizationType.Image;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            imageProcessor.VisualizationType = VisualizationType.Name;
        }

        private void button24_Click(object sender, EventArgs e)
        {
            videoSourcePlayer.SignalToStop();
        }

        private void button25_Click(object sender, EventArgs e)
        {
            VP2.SignalToStop();
            VP1.SignalToStop();
        }

        private void button26_Click(object sender, EventArgs e)
        {
            //sp.Write(this.txtServo1.Text);
        }

        private void button27_Click(object sender, EventArgs e)
        {
            //sp.Write(this.txtServo.Text);
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            // int r = Convert.ToInt32(this.txtArmBase.Text) * -1;
            //r += 90;
            //sp.Write(r.ToString());
        }

        private void button28_Click(object sender, EventArgs e)
        {
            if (this.timer1.Enabled)
                timer1.Enabled = false;
            else
                timer1.Enabled = true;
        }



        private void button16_Click_1(object sender, EventArgs e)
        {
            
            foreach (Object  bn in this.splitContainer1.Panel1.Controls)
            {
                if (bn.GetType() == typeof(PictureBox))
                    ((PictureBox)(bn)).Visible=false;

           
           
           }
            
            PictureBox b = new PictureBox();
            localhost.ServiceSoapClient c = new localhost.ServiceSoapClient();
          

            List<string> urls;

            urls = c.PrintString(rtfLessonScript.Text.ToString());

            int top, left;
            top = left = 0;
            for (int i = 0; i < urls.Count; i++)
            {
                // MessageBox.Show(urls[i].ToString());

                b = new PictureBox();
                b.Width = 100;
                b.Height = 100;
                b.Top = 10 + top;
                b.Left = 50 + left;
                b.SizeMode = PictureBoxSizeMode.StretchImage;
                this.splitContainer1.Panel1.Controls.Add(b);


                b.Image = DownloadImage(urls[i].ToString());
                left = left + 110;

            }






        }

        private void button14_Click(object sender, EventArgs e)
        {
            ofd.ShowDialog();
            rtfLessonScript.LoadFile(ofd.FileName.ToString());

             
        }

        private void button13_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}