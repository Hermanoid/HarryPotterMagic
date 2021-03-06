﻿//------------------------------------------------------------------------------
// <copyright file="MainWindow.xaml.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

namespace Microsoft.Samples.Kinect.InfraredBasics
{
    using System;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Threading.Tasks;
    using System.Windows;
    using System.Windows.Media;
    using System.Windows.Media.Imaging;
    using System.Linq;
    using Microsoft.Kinect;
    using System.Windows.Threading;
    using System.Windows.Controls;

    /// <summary>
    /// Interaction logic for the MainWindow
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        /// <summary>
        /// Maximum value (as a float) that can be returned by the InfraredFrame
        /// </summary>
        private const float InfraredSourceValueMaximum = (float)ushort.MaxValue;
        
        /// <summary>
        /// The value by which the infrared source data will be scaled
        /// </summary>
        private const float InfraredSourceScale = 0.75f;

        /// <summary>
        /// Smallest value to display when the infrared data is normalized
        /// </summary>
        private const float InfraredOutputValueMinimum = 0.01f;

        /// <summary>
        /// Largest value to display when the infrared data is normalized
        /// </summary>
        private const float InfraredOutputValueMaximum = 1.0f;

        /// <summary>
        /// Active Kinect sensor
        /// </summary>
        private KinectSensor kinectSensor = null;

        /// <summary>
        /// Reader for infrared frames
        /// </summary>
        private InfraredFrameReader infraredFrameReader = null;

        /// <summary>
        /// Description (width, height, etc) of the infrared frame data
        /// </summary>
        private FrameDescription infraredFrameDescription = null;

        /// <summary>
        /// Bitmap to display
        /// </summary>
        private WriteableBitmap infraredBitmap = null;

        /// <summary>
        /// Current status text to display
        /// </summary>
        private string statusText = null;

        private Magic magic;

        /// <summary>
        /// Initializes a new instance of the MainWindow class.
        /// </summary>
        public MainWindow()
        {
            // get the kinectSensor object
            this.kinectSensor = KinectSensor.GetDefault();

            // open the reader for the depth frames
            this.infraredFrameReader = this.kinectSensor.InfraredFrameSource.OpenReader();

            // wire handler for frame arrival
            this.infraredFrameReader.FrameArrived += this.Reader_InfraredFrameArrived;

            // get FrameDescription from InfraredFrameSource
            this.infraredFrameDescription = this.kinectSensor.InfraredFrameSource.FrameDescription;

            // create the bitmap to display
            this.infraredBitmap = new WriteableBitmap(this.infraredFrameDescription.Width, this.infraredFrameDescription.Height, 96.0, 96.0, PixelFormats.Gray32Float, null);

            // set IsAvailableChanged event notifier
            this.kinectSensor.IsAvailableChanged += this.Sensor_IsAvailableChanged;

            // open the sensor
            this.kinectSensor.Open();

            // set the status text
            this.StatusText = this.kinectSensor.IsAvailable ? Properties.Resources.RunningStatusText
                                                            : Properties.Resources.NoSensorStatusText;

            
            // use the window object as the view model in this simple example
            this.DataContext = this;

            magic = new Magic(infraredFrameDescription);
            magic.gameController.bluetoothController.OnSpellListChanged += (spells) =>
            {
                spellsText.Dispatcher.Invoke(() =>
                {
                    spellsText.Text = string.Join(", ", spells);
                });
            };


                
            // initialize the components (controls) of the window
            this.InitializeComponent();

            DispatcherTimer timer = new DispatcherTimer();
            timer.Interval = new TimeSpan(0, 0, 1);
            timer.Start();

            Task.Run(() => magic.Initialize());
            //this.detection_counter.Text = "";

            Action castSpellUpdate = () =>
            {
                castSpell.Text = string.Join(
                    " or ", magic.gameController.machine.PermittedTriggers
                        .Where(t => t != Trigger.Dud && t != Trigger.Times_Up)
                        .Select(t => t.ToString().Replace("_", " ")));
            };
            magic.gameController.machine.OnTransitionCompleted((transition) =>
            {
                robotStatus.Dispatcher.Invoke(() =>
                {
                    robotStatus.Text = transition.Destination.ToString();
                });
                castSpell.Dispatcher.Invoke(castSpellUpdate);
            });
            timer.Tick += (sender, e) =>
            {
                magic.gameController.dispatcherTimer_Tick(sender, e);
                timeRemaining.Dispatcher.Invoke(() =>
                {
                    var current_time = magic.gameController.current_time;
                    if (magic.gameController.machine.State == State.Waiting)
                    {
                        timeRemaining.Text = "Not Playing";
                    }
                    else if (current_time == 0)
                    {
                        timeRemaining.Text = "Clock is Off";
                    }
                    else
                    {
                        timeRemaining.Text = $"{current_time} second{(current_time == 1 ? "" : "s")}";
                    }
                });
            };
            castSpellUpdate();
            robotStatus.Text = magic.gameController.machine.State.ToString();
            timeRemaining.Text = "Not Playing";
        }

        /// <summary>
        /// INotifyPropertyChangedPropertyChanged event to allow window controls to bind to changeable data
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Gets the bitmap to display
        /// </summary>
        public ImageSource ImageSource
        {
            get
            {
                return this.infraredBitmap;
            }
        }

        /// <summary>
        /// Gets or sets the current status text to display
        /// </summary>
        public string StatusText
        {
            get
            {
                return this.statusText;
            }

            set
            {
                if (this.statusText != value)
                {
                    this.statusText = value;

                    // notify any bound elements that the text has changed
                    if (this.PropertyChanged != null)
                    {
                        this.PropertyChanged(this, new PropertyChangedEventArgs("StatusText"));
                    }
                }
            }
        }

        /// <summary>
        /// Execute shutdown tasks
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void MainWindow_Closing(object sender, CancelEventArgs e)
        {
            if (this.infraredFrameReader != null)
            {
                // InfraredFrameReader is IDisposable
                this.infraredFrameReader.Dispose();
                this.infraredFrameReader = null;
            }

            if (this.kinectSensor != null)
            {
                this.kinectSensor.Close();
                this.kinectSensor = null;
            }
        }

        /// <summary>
        /// Handles the user clicking on the screenshot button
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void ScreenshotButton_Click(object sender, RoutedEventArgs e)
        {
            if (this.infraredBitmap != null)
            {
                // create a png bitmap encoder which knows how to save a .png file
                BitmapEncoder encoder = new PngBitmapEncoder();

                // create frame from the writable bitmap and add to encoder
                encoder.Frames.Add(BitmapFrame.Create(this.infraredBitmap));

                string time = System.DateTime.Now.ToString("hh'-'mm'-'ss", CultureInfo.CurrentUICulture.DateTimeFormat);

                string myPhotos = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);

                string path = Path.Combine(myPhotos, "KinectScreenshot-Infrared-" + time + ".png");

                // write the new file to disk
                try
                {
                    // FileStream is IDisposable
                    using (FileStream fs = new FileStream(path, FileMode.Create))
                    {
                        encoder.Save(fs);
                    }

                    this.StatusText = string.Format(CultureInfo.CurrentCulture, Properties.Resources.SavedScreenshotStatusTextFormat, path);
                }
                catch (IOException)
                {
                    this.StatusText = string.Format(CultureInfo.CurrentCulture, Properties.Resources.FailedScreenshotStatusTextFormat, path);
                }
            }
        }

        /// <summary>
        /// Handles the infrared frame data arriving from the sensor
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void Reader_InfraredFrameArrived(object sender, InfraredFrameArrivedEventArgs e)
        {
            // InfraredFrame is IDisposable
            using (InfraredFrame infraredFrame = e.FrameReference.AcquireFrame())
            {
                if (infraredFrame != null)
                {
                    // the fastest way to process the infrared frame data is to directly access 
                    // the underlying buffer
                    using (Microsoft.Kinect.KinectBuffer infraredBuffer = infraredFrame.LockImageBuffer())
                    {
                        // verify data and write the new infrared frame data to the display bitmap
                        if (((this.infraredFrameDescription.Width * this.infraredFrameDescription.Height) == (infraredBuffer.Size / this.infraredFrameDescription.BytesPerPixel)) &&
                            (this.infraredFrameDescription.Width == this.infraredBitmap.PixelWidth) && (this.infraredFrameDescription.Height == this.infraredBitmap.PixelHeight))
                        {
                            this.ProcessInfraredFrameData(infraredBuffer.UnderlyingBuffer, infraredBuffer.Size);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Directly accesses the underlying image buffer of the InfraredFrame to 
        /// create a displayable bitmap.
        /// This function requires the /unsafe compiler option as we make use of direct
        /// access to the native memory pointed to by the infraredFrameData pointer.
        /// </summary>
        /// <param name="infraredFrameData">Pointer to the InfraredFrame image data</param>
        /// <param name="infraredFrameDataSize">Size of the InfraredFrame image data</param>
        private unsafe void ProcessInfraredFrameData(IntPtr infraredFrameData, uint infraredFrameDataSize)
        {
            // infrared frame data is a 16 bit value
            ushort* frameData = (ushort*)infraredFrameData;

            // lock the target bitmap
            this.infraredBitmap.Lock();

            // get the pointer to the bitmap's back buffer
            float* backBuffer = (float*)this.infraredBitmap.BackBuffer;

            if (checkBox.IsChecked.HasValue && checkBox.IsChecked.Value)
            {
                int detection_count = magic.ProcessFrame(frameData, infraredFrameDataSize, infraredFrameDescription, captureSpellOption.IsChecked.Value, spellNameBox.Text);
                var data = (float*)magic.traceCanvas.Data;
                for (int i = 0; i < (int)(infraredFrameDataSize / this.infraredFrameDescription.BytesPerPixel); ++i)
                {
                    backBuffer[i] = (float)data[i];
                }
                //this.detection_counter.Text = detection_count.ToString();
            }
            else
            {
                // process the infrared data
                for (int i = 0; i < (int)(infraredFrameDataSize / this.infraredFrameDescription.BytesPerPixel); ++i)
                {
                    // since we are displaying the image as a normalized grey scale image, we need to convert from
                    // the ushort data (as provided by the InfraredFrame) to a value from [InfraredOutputValueMinimum, InfraredOutputValueMaximum]
                    backBuffer[i] = Math.Min(InfraredOutputValueMaximum, (((float)frameData[i] / InfraredSourceValueMaximum * InfraredSourceScale) * (1.0f - InfraredOutputValueMinimum)) + InfraredOutputValueMinimum);
                }
            }

            // mark the entire bitmap as needing to be drawn
            this.infraredBitmap.AddDirtyRect(new Int32Rect(0, 0, this.infraredBitmap.PixelWidth, this.infraredBitmap.PixelHeight));

            // unlock the bitmap
            this.infraredBitmap.Unlock();
        }

        /// <summary>
        /// Handles the event which the sensor becomes unavailable (E.g. paused, closed, unplugged).
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void Sensor_IsAvailableChanged(object sender, IsAvailableChangedEventArgs e)
        {
            // set the status text
            this.StatusText = this.kinectSensor.IsAvailable ? Properties.Resources.RunningStatusText
                                                            : Properties.Resources.SensorNotAvailableStatusText;
        }

        private async void OnTextClick(object sender, RoutedEventArgs e)
        {
            await magic.gameController.TriggerSpell(Spell.Lumos);
        }

        private bool is_grabbing = false;
        private async void GrabControlButton_Click(object sender, RoutedEventArgs e)
        {
            if (magic.gameController.machine.State != State.Waiting)
            {
                return;
            }
            if (!is_grabbing)
            {
                is_grabbing = true;
                State target_state = (State)Enum.Parse(typeof(State), ((ComboBoxItem)GrabControlOption.SelectedItem).Tag.ToString());
                await magic.gameController.ManualStartGrab(target_state);
            }
            else
            {
                is_grabbing = false;
                await magic.gameController.ManualFinishGrab();
            }
        }
    }
}
