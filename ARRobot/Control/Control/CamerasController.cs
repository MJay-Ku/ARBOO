using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

using AForge;
using AForge.Imaging;
using AForge.Imaging.Filters;
using AForge.Video;
using AForge.Controls;
using AForge.Video.DirectShow;

namespace Control
{
    class CamerasController
    {
        private FilterInfoCollection videoDevices;
        private List<string> myList = new List<string>();

        public void StartCameras(string DName, VideoSourcePlayer videoSourcePlayer1, VideoSourcePlayer videoSourcePlayer2,int l,int r)
        {
            // create first video source
            VideoCaptureDevice videoSource1 = new VideoCaptureDevice(videoDevices[l].MonikerString);
            videoSource1.DesiredFrameRate = 10;

            videoSourcePlayer1.VideoSource = videoSource1;
            videoSourcePlayer1.Start();

            // create second video source
        
                System.Threading.Thread.Sleep(500);

                VideoCaptureDevice videoSource2 = new VideoCaptureDevice(videoDevices[r].MonikerString);
                videoSource2.DesiredFrameRate = 10;

                videoSourcePlayer2.VideoSource = videoSource2;
                videoSourcePlayer2.Start();
            

           // camera1Acquired = new AutoResetEvent(false);
            //camera2Acquired = new AutoResetEvent(false);
            // start tracking thread
           // trackingThread = new Thread(new ThreadStart(TrackingThread));
          //  trackingThread.Start();


        }

        public void StartCameras(IVideoSource source,VideoSourcePlayer VP)
        {
        
      
                      

            // stop current video source
            VP.SignalToStop( );
            VP.WaitForStop( );

            // start new video source
            VP.VideoSource = new AsyncVideoSource( source );
           VP.Start( );

            

        }
        
        


        public List<string> FindCamera()
        {
            try
            {
                // enumerate video devices
                videoDevices = new FilterInfoCollection(FilterCategory.VideoInputDevice);

                if (videoDevices.Count == 0)
                {
                    throw new Exception();
                }

                for (int i = 1, n = videoDevices.Count; i <= n; i++)
                {
                    string cameraName = i + " : " + videoDevices[i - 1].Name;

                    myList.Add(cameraName);
                }



            }

            catch (Exception e)
            {
                Console.WriteLine(e.ToString());

            }
            return myList;


        }
    }
}


