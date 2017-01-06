using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Runtime.InteropServices;
//using Windows7.Multitouch;
//using Windows7.Multitouch.WPF;
using EyeTribe.ClientSdk;
using EyeTribe.ClientSdk.Data;
using MessageBox = System.Windows.MessageBox;
using System.Windows.Forms;
using System.Net;
using System.Net.Sockets;

using System.IO;
using System.Threading;
using System.Text.RegularExpressions;
using System.Windows.Media.Effects;

namespace EyeTribeCSharp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : IGazeListener
    {
        #region Variables

        private const float DPI_DEFAULT = 96f; // default system DIP setting
        private const double SPEED_BOOST = 20.0;
        private const double ACTIVE_SCROLL_AREA = 0.25; // 25% top and bottom
        private const int MAX_IMAGE_WIDTH = 800;
        private ImageButton latestSelection;
        private readonly double dpiScale;
        private Matrix transfrm;
        //private readonly Timer scrollTimer;
        //private double scrollLevel;
        //private bool canScroll;
        enum Direction { Up = -1, Down = 1 }

        private Point lastPointUpdate = new Point(0, 0);
        private int curr_x = 0, curr_y = 0; // Temporary variable to record the current coordinates.

        //line tracking
        System.Windows.Shapes.Polyline baseLine;
        private bool newLine = true;
        private bool trackOn = false;
        private DateTime prevTime = System.DateTime.Now;
        private static int listmax = 100;
        private int[] xlist = new int[listmax], ylist = new int[listmax];


        private Point lastPointDrawn = new Point(0, 0);

        //spot
        private int gazeTime = 0;
        private double spotWidth = 8;
        private double spotHeight = 8;
        private bool firstSpotLine = false; //line been done yet?
        private Point markPoint;
        private Point lastMarkPoint;

        //gaze smooth
        private Point currentPoint;
        private Point lastPoint = new Point(0, 0);
        private DateTime lastTimeSmooth = System.DateTime.Now;
        private DateTime currentTime;
        private double velocity;

        //highlight areas of interest
        private bool highlightOn = false;
        private double[] range = { 0, 0.1, 0, 0.1 }; //x, x, y, y
        private int highlightColumn;
        private double w;
        private double h;
        // private int pastRow = 0;
        //  private int pastCol = 0;

        //switch cases
        private int caseNum = 1;
        //send/receive
        private bool SenderOn = false;
        private bool ReceiverOn = false;
        private static int ReceiverPort = 11000, SenderPort = 11000;//ReceiverPort is the port used by Receiver, SenderPort is the port used by Sender
        private bool communication_started_Receiver = false;//indicates whether the Receiver is ready to receive message(coordinates). Used for thread control
        private bool communication_started_Sender = false;//Indicate whether the program is sending its coordinates to others. Used for thread control
        private System.Threading.Thread communicateThread_Receiver; //Thread for receiver
        private System.Threading.Thread communicateThread_Sender;   //Thread for sender
        private static string SenderIP = "", ReceiverIP = ""; //The IP's for sender and receiver.
        private static string defaultSenderIP = "169.254.50.139"; //The default IP for sending messages.
                                                                  //SenderIP = "169.254.50.139"; //seahorse laptop.//SenderIP = "169.254.41.115"; //Jellyfish laptop
        private static int x_received, y_received;

        private static string IPpat = @"(\d+)(\.)(\d+)(\.)(\d+)(\.)(\d+)\s+"; // regular expression used for matching ip address
        private Regex r = new Regex(IPpat, RegexOptions.IgnoreCase);//regular expression variable
        private static string NumPat = @"(\d+)\s+";
        private Regex regex_num = new Regex(NumPat, RegexOptions.IgnoreCase);
        private System.Windows.Threading.DispatcherTimer dispatcherTimer;

        //recording to file (not yet used)
        // Constants for file names and recording frequency
        private string fileDirectory = Directory.GetCurrentDirectory();//output file directory
        private string fileName = @"GazeData";//output file name. Will automatically add .txt at the end
        private const int recordEveryXTime = 3;//The default is 30Hz so if recordEveryXTime = 3 then it records every 0.1 seconds
        private int recordEveryXTimeCounter = 0; // Helper variable... The record function records everytime this counter is divisible by 3
        private bool isRecording = false, shouldRecord = false;//isRecording: whether the user pressed the record button. shouldRecord: whether the user entered their laptop number. If they haven't we shouldn't start recording
        private string laptop_num = "-1";

        #endregion

        #region Get/Set

        private bool IsTouchEnabled { get; set; }

        #endregion

        #region Enums

        public enum DeviceCap
        {
            /// <summary>
            /// Logical pixels inch in X
            /// </summary>
            LOGPIXELSX = 88,
            /// <summary>
            /// Logical pixels inch in Y
            /// </summary>
            LOGPIXELSY = 90
        }

        #endregion
        public MainWindow()
        {
            var connectedOk = true;
            GazeManager.Instance.Activate(GazeManager.ApiVersion.VERSION_1_0, GazeManager.ClientMode.Push);
            GazeManager.Instance.AddGazeListener(this);

            if (!GazeManager.Instance.IsActivated)
            {
                Dispatcher.BeginInvoke(new Action(() => MessageBox.Show("EyeTribe Server has not been started")));
                connectedOk = false;
            }
            /**/
            else if (!GazeManager.Instance.IsCalibrated)
            {
                Dispatcher.BeginInvoke(new Action(() => MessageBox.Show("User is not calibrated")));
                connectedOk = false;
            }
            if (!connectedOk)
            {
                Close();
                return;
            }

            InitializeComponent();

            // Register for mouse clicks
            PreviewMouseDown += TapDown;
            PreviewMouseUp += TapUp;

            // Register for key events
            KeyDown += WindowKeyDown;
            KeyUp += WindowKeyUp;

            // Get the current DIP scale
            dpiScale = CalcDpiScale();

            // Hide all from start
            GridTop.Visibility = Visibility.Collapsed;

            Loaded += (sender, args) =>
            {
                if (Screen.PrimaryScreen.Bounds.Width > MAX_IMAGE_WIDTH)
                    WebImage.Width = MAX_IMAGE_WIDTH * dpiScale;
                else
                    WebImage.Width = Screen.PrimaryScreen.Bounds.Width * dpiScale;

                // Transformation matrix that accomodate for the DPI settings
                var presentationSource = PresentationSource.FromVisual(this);
                transfrm = presentationSource.CompositionTarget.TransformFromDevice;

                WebImage.Source = new BitmapImage(new Uri("Graphics/CaseA/graphic1.png", UriKind.RelativeOrAbsolute));
            };

            //file header?

            //  DispatcherTimer setup
            dispatcherTimer = new System.Windows.Threading.DispatcherTimer();
            dispatcherTimer.Tick += new EventHandler(update);
            dispatcherTimer.Interval = new TimeSpan(0, 0, 0, 0, 25);
            dispatcherTimer.Start();
        }
        #region Public methods

        public void OnGazeUpdate(GazeData gazeData)
        {
            var x = (int)Math.Round(gazeData.SmoothedCoordinates.X, 0);
            var y = (int)Math.Round(gazeData.SmoothedCoordinates.Y, 0);
            if (x == 0 & y == 0) return;
            curr_x = x;
            curr_y = y;
            // Invoke thread
            //do this on update function, timer: Dispatcher.BeginInvoke(new Action(() => UpdateUI(x, y)));

            if ((isRecording))
            {
                if (shouldRecord == false) { }
                else if ((recordEveryXTimeCounter % recordEveryXTime == 0))
                {
                    string[] lines = { "Client Num:, " + laptop_num + ", X: ," + x + ", Y: ," + y + ",Time: ," + gazeData.TimeStampString };
                    try { System.IO.File.AppendAllLines(fileDirectory + fileName + ".csv", lines); }
                    catch (Exception e)
                    {
                        Console.WriteLine("An error occurred: '{0}'", e);
                    }

                    if (ReceiverOn && x_received > 0 && y_received > 0)
                    {

                        string[] lines_Received = { "Client Num: ," + laptop_num + ", X_Received: ," + x_received + ", Y_Received: ," + y_received + ",Time: ," + gazeData.TimeStampString };
                        try { System.IO.File.AppendAllLines(fileDirectory + fileName + ".csv", lines_Received); }
                        catch (Exception e)
                        {
                            Console.WriteLine("An error occurred: '{0}'", e);
                        }
                    }
                }
                recordEveryXTimeCounter++;
            }
        }


        #endregion
        #region Private methods
        private void TapDown(object sender, MouseButtonEventArgs e)
        {
            DoTapDown();
        }

        private void TapUp(object sender, MouseButtonEventArgs e)
        {
            DoTapUp();
        }

        private void WindowKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.KeyboardDevice.IsKeyDown(Key.A))
            {
                caseNum = 1;
                if (highlightOn)
                    WebImage.Source = new BitmapImage(new Uri("Graphics/CaseA/BWgraphic1.png", UriKind.RelativeOrAbsolute));
                else
                    WebImage.Source = new BitmapImage(new Uri("Graphics/CaseA/graphic1.png", UriKind.RelativeOrAbsolute));
            }
            else if (e.KeyboardDevice.IsKeyDown(Key.B))
            {
                caseNum = 2;
                if (highlightOn)
                    WebImage.Source = new BitmapImage(new Uri("Graphics/CaseB/BWgraphic2.png", UriKind.RelativeOrAbsolute));
                else
                    WebImage.Source = new BitmapImage(new Uri("Graphics/CaseB/graphic2.png", UriKind.RelativeOrAbsolute));
            }
            else if (e.KeyboardDevice.IsKeyDown(Key.C))
            {
                caseNum = 3;
                if (highlightOn)
                    WebImage.Source = new BitmapImage(new Uri("Graphics/CaseC/BWgraphic3.png", UriKind.RelativeOrAbsolute));
                else
                    WebImage.Source = new BitmapImage(new Uri("Graphics/CaseC/graphic3.png", UriKind.RelativeOrAbsolute));
            }
            else
            {
                DoTapDown();
            }
        }

        private void WindowKeyUp(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.VolumeDown || e.Key == Key.VolumeUp || e.Key == Key.Escape)
                Close();
            //else
            //DoTapUp();
        }

        private void DoTapDown()
        {
            //GridTop.Visibility = Visibility.Visible;
        }

        private void DoTapUp()
        {
            //if (GridTop.Visibility == Visibility.Collapsed) return;

            //// Hide panlel and exe button click if needed
            //GridTop.Visibility = Visibility.Collapsed;
            //var selectedButton = latestSelection;
            //if (selectedButton != null)
            //{
            //    ExecuteSelectedButton(selectedButton.Name);
            //}
            if (GridTop.Visibility == Visibility.Collapsed)
            {
                GridTop.Visibility = Visibility.Visible;
            }
            else if (GridTop.Visibility == Visibility.Visible)
            {
                DoButtonCheck(System.Windows.Forms.Cursor.Position.X, System.Windows.Forms.Cursor.Position.Y);
                GridTop.Visibility = Visibility.Collapsed;
                var selectedButton = latestSelection;
                if (selectedButton != null)
                {
                    ExecuteSelectedButton(selectedButton.Name);
                }
            }
        }
        void update(object sender, EventArgs e) //sender/receiver
        {
            //If user pressed Receiver or Cursor button but communication haven't started yet or has terminated, start a thread on tryCommunicateReceiver()
            if (ReceiverOn && communication_started_Receiver == false)
            {
                communication_started_Receiver = true;
                communicateThread_Receiver = new System.Threading.Thread(new ThreadStart(() => tryCommunicateReceiver(curr_x, curr_y)));
                communicateThread_Receiver.Start();
            }

            //If user pressed Sender button but communication haven't started yet or has terminated, start a thread on tryCommunicateSender()
            if (SenderOn && communication_started_Sender == false)
            {
                communication_started_Sender = true;
                communicateThread_Sender = new System.Threading.Thread(new ThreadStart(() => tryCommunicateSender(curr_x, curr_y)));
                communicateThread_Sender.Start();
            }
            // Invoke thread
            //Console.WriteLine(System.Windows.Forms.Control.MousePosition.ToString());
            Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, new Action(() => UpdateUI(curr_x, curr_y)));
        }
        private void UpdateUI(int x, int y)
        {
            // Unhide the GazePointer if you want to see your gaze point
            //GazePointer.Visibility = Visibility.Visible;

            var relativePt = new Point(x, y);
            relativePt = transfrm.Transform(relativePt);
            if (!ReceiverOn)
            {
                if ((GazePointer.Visibility == Visibility.Visible) &&
                    (Math.Sqrt((relativePt.X - lastPointUpdate.X) * (relativePt.X - lastPointUpdate.X) + (relativePt.Y - lastPointUpdate.Y) * (relativePt.Y - lastPointUpdate.Y)) > 3))
                {
                    Canvas.SetLeft(GazePointer, relativePt.X - GazePointer.Width / 2);
                    Canvas.SetTop(GazePointer, relativePt.Y - GazePointer.Height / 2);
                    Console.Write(relativePt.X);
                }
                lastPointUpdate = relativePt;

                currentTime = System.DateTime.Now;
            }
            else if (ReceiverOn && x_received > 0 && y_received > 0)
            {
                receiveGaze(x_received, y_received);
            }
            if (trackOn) //turn on the track and draw!
            {
                //check if significant distance
                if ((Math.Abs(relativePt.X - lastPoint.X)) > 10 && (Math.Abs(relativePt.Y - lastPoint.Y)) > 40)
                {
                    if (ReceiverOn && x_received > 0 && y_received > 0)
                    {
                        Point receivedPt = new Point(x_received, y_received);
                        spot(receivedPt);
                    }
                    else if (!ReceiverOn)
                    {
                        spot(relativePt);
                    }
                    // if (gazeSmooth(relativePt)) {drawTrack(relativePt);};
                };
            };
            if (highlightOn)
            {
                if (ReceiverOn && x_received > 0 && y_received > 0)
                {
                    Point receivedPt = new Point(x_received, y_received);
                    checkRegion(receivedPt);
                }
                else if (!ReceiverOn)
                {
                    checkRegion(relativePt);
                }
            };
            if (GridTop.Visibility == Visibility.Collapsed) { }
            else
            {
                //DoButtonCheck(x, y);
            }
        }
        private void checkRegion(Point pt)
        {
            h = WebImage.Height;
            w = 0.77 * WebImage.Width; //width not including answer key section
            //double leftLimit = 675 - WebImage.Width / 2; //fix
            double leftLimit = (Screen.PrimaryScreen.Bounds.Width / 2) - (WebImage.Width / 2);
            double rightLimit = leftLimit + WebImage.Width - 140;
            //rightLimit = leftLimit + (WebImage.Width * 0.77);
            double section = WebImage.Width / 4;// (rightLimit - leftLimit) / 4;
            double wsection = w / 4; //a fourth of the width of the diagram part
            AnswerEllipse.Visibility = Visibility.Hidden;
            double pad = 40;
            //Check column: far left, left, right, far right
            if (pt.X >= rightLimit || pt.X <= leftLimit) //no region
            {
                highlightColumn = 0;
                if (caseNum == 1)
                    WebImage.Source = new BitmapImage(new Uri("Graphics/CaseA/BWgraphic1.png", UriKind.RelativeOrAbsolute));
                else if (caseNum == 2)
                    WebImage.Source = new BitmapImage(new Uri("Graphics/CaseB/BWgraphic2.png", UriKind.RelativeOrAbsolute));
                else if (caseNum == 3)
                    WebImage.Source = new BitmapImage(new Uri("Graphics/CaseC/BWgraphic3.png", UriKind.RelativeOrAbsolute));
                #region answerkey
                if (pt.X >= rightLimit && pt.X <= (leftLimit + WebImage.Width + 140)) //hovering over answer key
                {
                    Canvas.SetLeft(AnswerEllipse, rightLimit + 40);
                    AnswerEllipse.Visibility = Visibility.Visible;
                    double top = 192; // 400 - (WebImage.Height/2) + 50;
                    double interval = 76; //(WebImage.Height - 50)/7;
                    if (pt.Y < top || pt.Y > top + 7 * interval)
                        AnswerEllipse.Visibility = Visibility.Hidden;
                    else if (pt.Y >= top && pt.Y <= top + interval)
                        Canvas.SetTop(AnswerEllipse, top);
                    else if (pt.Y <= top + 2 * interval)
                        Canvas.SetTop(AnswerEllipse, top + interval);
                    else if (pt.Y <= top + 3 * interval)
                        Canvas.SetTop(AnswerEllipse, top + 2 * interval);
                    else if (pt.Y <= top + 4 * interval)
                        Canvas.SetTop(AnswerEllipse, top + 3 * interval);
                    else if (pt.Y <= top + 5 * interval)
                        Canvas.SetTop(AnswerEllipse, top + 4 * interval);
                    else if (pt.Y <= top + 6 * interval)
                        Canvas.SetTop(AnswerEllipse, top + 5 * interval);
                    else if (pt.Y <= top + 7 * interval)
                        Canvas.SetTop(AnswerEllipse, top + 6 * interval);
                }
                #endregion
            }
            else if (pt.X <= leftLimit + wsection) //far left
            {
                highlightColumn = 1;
            }
            else if (pt.X <= leftLimit + 2 * wsection) //left
            {
                highlightColumn = 2;
            }
            else if (pt.X <= leftLimit + 3 * wsection) //right
            {
                highlightColumn = 3;
            }
            else if (pt.X <= rightLimit) //far right
            {
                highlightColumn = 4;
            };
            #region columns14
            if (highlightColumn == 1 || highlightColumn == 4)
            {
                /*
                double bottomLimit = 400; // WebImage.Height / 2;
                double topLimit = 760; // WebImage.Height - WebImage.Height / 5;
                */
                double topLimit = 0.363 * WebImage.Width; // * w
                double bottomLimit = 0.787 * WebImage.Width;

                if (pt.Y >= topLimit && pt.Y <= bottomLimit)
                {
                    if (highlightColumn == 4)
                    {
                        if (caseNum == 1)
                            WebImage.Source = new BitmapImage(new Uri("Graphics/CaseA/R3graphic1.png", UriKind.RelativeOrAbsolute));
                        else if (caseNum == 2)
                            WebImage.Source = new BitmapImage(new Uri("Graphics/CaseB/R3graphic2.png", UriKind.RelativeOrAbsolute));
                        else if (caseNum == 3)
                            WebImage.Source = new BitmapImage(new Uri("Graphics/CaseC/R3graphic3.png", UriKind.RelativeOrAbsolute));
                    }
                    else
                    {
                        if (caseNum == 1)
                            WebImage.Source = new BitmapImage(new Uri("Graphics/CaseA/L3graphic1.png", UriKind.RelativeOrAbsolute));
                        else if (caseNum == 2)
                            WebImage.Source = new BitmapImage(new Uri("Graphics/CaseB/L3graphic2.png", UriKind.RelativeOrAbsolute));
                        else if (caseNum == 3)
                            WebImage.Source = new BitmapImage(new Uri("Graphics/CaseC/L3graphic3.png", UriKind.RelativeOrAbsolute));
                    }
                }
            }
            #endregion
            #region column2
            else if (highlightColumn == 2)
            {
                if (pt.Y <= pad + section)
                {
                    if (caseNum == 1)
                        WebImage.Source = new BitmapImage(new Uri("Graphics/CaseA/L1graphic1.png", UriKind.RelativeOrAbsolute));
                    else if (caseNum == 2)
                        WebImage.Source = new BitmapImage(new Uri("Graphics/CaseB/L1graphic2.png", UriKind.RelativeOrAbsolute));
                    else if (caseNum == 3)
                        WebImage.Source = new BitmapImage(new Uri("Graphics/CaseC/L1graphic3.png", UriKind.RelativeOrAbsolute));
                }
                else if (pt.Y <= pad + 2 * section)
                {
                    if (caseNum == 1)
                        WebImage.Source = new BitmapImage(new Uri("Graphics/CaseA/L2graphic1.png", UriKind.RelativeOrAbsolute));
                    else if (caseNum == 2)
                        WebImage.Source = new BitmapImage(new Uri("Graphics/CaseB/L2graphic2.png", UriKind.RelativeOrAbsolute));
                    else if (caseNum == 3)
                        WebImage.Source = new BitmapImage(new Uri("Graphics/CaseC/L2graphic3.png", UriKind.RelativeOrAbsolute));
                }
                else if (pt.Y >= pad + 3 * section)
                {
                    if (caseNum == 1)
                        WebImage.Source = new BitmapImage(new Uri("Graphics/CaseA/L4graphic1.png", UriKind.RelativeOrAbsolute));
                    else if (caseNum == 2)
                        WebImage.Source = new BitmapImage(new Uri("Graphics/CaseB/L4graphic2.png", UriKind.RelativeOrAbsolute));
                    else if (caseNum == 3)
                        WebImage.Source = new BitmapImage(new Uri("Graphics/CaseC/L4graphic3.png", UriKind.RelativeOrAbsolute));
                }
                else
                {
                    if (caseNum == 1)
                        WebImage.Source = new BitmapImage(new Uri("Graphics/CaseA/BWgraphic1.png", UriKind.RelativeOrAbsolute));
                    else if (caseNum == 2)
                        WebImage.Source = new BitmapImage(new Uri("Graphics/CaseB/BWgraphic2.png", UriKind.RelativeOrAbsolute));
                    else if (caseNum == 3)
                        WebImage.Source = new BitmapImage(new Uri("Graphics/CaseC/BWgraphic3.png", UriKind.RelativeOrAbsolute));
                }
                #endregion
            }
            #region column3
            else if (highlightColumn == 3)
            {
                if (pt.Y <= pad + section)
                {
                    if (caseNum == 1)
                        WebImage.Source = new BitmapImage(new Uri("Graphics/CaseA/R1graphic1.png", UriKind.RelativeOrAbsolute));
                    else if (caseNum == 2)
                        WebImage.Source = new BitmapImage(new Uri("Graphics/CaseB/R1graphic2.png", UriKind.RelativeOrAbsolute));
                    else if (caseNum == 3)
                        WebImage.Source = new BitmapImage(new Uri("Graphics/CaseC/R1graphic3.png", UriKind.RelativeOrAbsolute));
                }
                else if (pt.Y <= pad + 2 * section)
                {
                    if (caseNum == 1)
                        WebImage.Source = new BitmapImage(new Uri("Graphics/CaseA/R2graphic1.png", UriKind.RelativeOrAbsolute));
                    else if (caseNum == 2)
                        WebImage.Source = new BitmapImage(new Uri("Graphics/CaseB/R2graphic2.png", UriKind.RelativeOrAbsolute));
                    else if (caseNum == 3)
                        WebImage.Source = new BitmapImage(new Uri("Graphics/CaseC/R2graphic3.png", UriKind.RelativeOrAbsolute));
                }
                else if (pt.Y >= pad + 3 * section)
                {
                    if (caseNum == 1)
                        WebImage.Source = new BitmapImage(new Uri("Graphics/CaseA/R4graphic1.png", UriKind.RelativeOrAbsolute));
                    else if (caseNum == 2)
                        WebImage.Source = new BitmapImage(new Uri("Graphics/CaseB/R4graphic2.png", UriKind.RelativeOrAbsolute));
                    else if (caseNum == 3)
                        WebImage.Source = new BitmapImage(new Uri("Graphics/CaseC/R4graphic3.png", UriKind.RelativeOrAbsolute));
                }
                else
                {
                    if (caseNum == 1)
                        WebImage.Source = new BitmapImage(new Uri("Graphics/CaseA/BWgraphic1.png", UriKind.RelativeOrAbsolute));
                    else if (caseNum == 2)
                        WebImage.Source = new BitmapImage(new Uri("Graphics/CaseB/BWgraphic2.png", UriKind.RelativeOrAbsolute));
                    else if (caseNum == 3)
                        WebImage.Source = new BitmapImage(new Uri("Graphics/CaseC/BWgraphic3.png", UriKind.RelativeOrAbsolute));
                }
                #endregion
            };
        }
        //private void fishCheckRegion(Point pt)
        //{
        //    w = fish_overlay.ActualWidth / 4;
        //    h = fish_overlay.ActualHeight / 4;

        //    //check if NOT in previous region
        //    if (!(pt.X >= pastCol * w && pt.X <= (pastCol + 1) * w && pt.Y >= pastRow * h && pt.Y <= (1 + pastRow) * h))
        //    {
        //        //check range for rows (h)
        //        for (int i = 0; i < 4; i++)
        //        {
        //            if ((pt.Y >= i * h) && (pt.Y <= (i + 1) * h))
        //            {
        //                //check range for columns (w)
        //                for (int j = 0; j < 4; j++)
        //                {
        //                    if ((pt.X >= j * w) && (pt.X <= (j + 1) * w))
        //                    {
        //                        pastRow = i;
        //                        pastCol = j;
        //                        if (pastCol == 0)
        //                        {
        //                            SolidColorBrush strokeBrush = new SolidColorBrush(Color.FromRgb(255, 0, 0));
        //                            FishCircle.Stroke = strokeBrush;
        //                        }
        //                        else
        //                        {
        //                            SolidColorBrush strokeBrush2 = new SolidColorBrush(Color.FromRgb(0, 0, 255));
        //                            FishCircle.Stroke = strokeBrush2;
        //                        }
        //                        Canvas.SetLeft(FishCircle, pastCol * w);
        //                        Canvas.SetTop(FishCircle, pastRow * h);
        //                        FishCircle.Visibility = Visibility.Visible;
        //                        return;
        //                    }
        //                }
        //                return;
        //            }
        //        }
        //    }
        //}
        private void spot(Point pt)
        {
            //highlight focus spots in track
            if (pt.X >= range[0] && pt.X <= range[1] && pt.Y >= range[2] && pt.Y <= range[3]) //in RANGE
            {
                gazeTime += 1;
                if (gazeTime > 6)
                {
                    if (spotWidth <= 70 && spotHeight <= 70)
                    {
                        spotWidth += 0.4;
                        spotHeight += 0.4;
                    }
                    GazeSpot.Width = spotWidth;
                    GazeSpot.Height = spotHeight;

                    lastMarkPoint = markPoint;
                    Canvas.SetLeft(GazeSpot, lastMarkPoint.X - spotWidth / 2);
                    Canvas.SetTop(GazeSpot, lastMarkPoint.Y - spotHeight / 2);
                    GazeSpot.Visibility = Visibility.Visible;

                    if (firstSpotLine == true)
                    {
                        SpotLine.Visibility = Visibility.Visible;
                        SpotLine.X2 = lastMarkPoint.X;
                        SpotLine.Y2 = lastMarkPoint.Y;
                    }
                    else
                    {
                        SpotLine.X1 = lastMarkPoint.X;
                        SpotLine.Y1 = lastMarkPoint.Y;
                    }
                    firstSpotLine = true;
                }
            }
            else
            {
                gazeTime = 0;
                if (firstSpotLine == true)
                {
                    PrevGazeSpot.Width = GazeSpot.Width;
                    PrevGazeSpot.Height = GazeSpot.Height;
                    Canvas.SetLeft(PrevGazeSpot, lastMarkPoint.X - GazeSpot.Width / 2);
                    Canvas.SetTop(PrevGazeSpot, lastMarkPoint.Y - GazeSpot.Height / 2);
                    PrevGazeSpot.Visibility = Visibility.Visible;

                    SpotLine.X1 = lastMarkPoint.X;
                    SpotLine.Y1 = lastMarkPoint.Y;
                }
                //GazeSpot.Visibility = Visibility.Hidden;
                spotWidth = 8;
                spotHeight = 8;
                markPoint = pt;

                //update range
                range[0] = markPoint.X - 60;
                range[1] = markPoint.X + 60;
                range[2] = markPoint.Y - 60;
                range[3] = markPoint.Y + 60;
            }
        }

        private void drawTrack(Point pt)
        {
            //var x = (int)Math.Round(pt.X, 0);
            //var y = (int)Math.Round(pt.Y, 0);
            //xlist[lasti] = x;
            //ylist[lasti] = y;

            //begin drawing
            if (newLine) //after you just turn track ON
            {
                baseLine = new System.Windows.Shapes.Polyline
                {

                    Stroke = this.FindResource("semitransbrush") as Brush,
                    StrokeThickness = 15
                };
                track_overlay.Children.Add(baseLine);
                newLine = false;
            }

            if (baseLine.Points.Count() > 7)
            {
                baseLine.Points.RemoveAt(0);
                baseLine.Points.RemoveAt(1);
            };
            //add to line

            baseLine.Points.Add(pt);
        }
        private bool gazeSmooth(Point pt)
        {
            // ensure tracking line is smooth
            // blinking - return false if gaze doesn't stay in one place for long enough?
            // if (set time) has passed, update?
            // if velocity is not too fast, then return true

            // check distance
            currentPoint = pt;
            // check velocity
            currentTime = System.DateTime.Now;
            double timeElapsed = (currentTime - lastTimeSmooth).TotalMilliseconds;
            velocity = (Math.Sqrt((currentPoint.X - lastPoint.X) * (currentPoint.X - lastPoint.X) + (currentPoint.Y - lastPoint.Y) * (currentPoint.Y - lastPoint.Y)) / timeElapsed);

            if (velocity < 1.0) //|| timeElapsed > 500)
            {
                lastTimeSmooth = currentTime;
                lastPoint = currentPoint;
                return true;
            }
            else
            {
                return false;
            };
        }
        private void DoButtonCheck(int x, int y)
        {
            var pt = new Point(x, y);
            pt = transfrm.Transform(pt);
            var foundCandidate = false;
            foreach (var child in GridButtons.Children.Cast<ImageButton>())
            {
                var isChecked = MouseHitTest(child, pt);
                // var isChecked = HitTest(child, pt);
                child.IsChecked = isChecked;
                if (!isChecked) continue;
                foundCandidate = true;
                latestSelection = child;
            }
            if (!foundCandidate)
            {
                latestSelection = null;
            }
        }

        private bool HitTest(ImageButton control, Point gazePt)
        {
            var gridPt = transfrm.Transform(control.PointToScreen(new Point(0, 0)));
            return gazePt.X > gridPt.X && gazePt.X < gridPt.X + control.ActualWidth &&
            gazePt.Y > gridPt.Y && gazePt.Y < gridPt.Y + control.ActualHeight;
        }
        // Check if the mouse coordinate is over one specific button.
        private bool MouseHitTest(ImageButton control, Point mousePt)
        {
            try
            {
                var gridPt = transfrm.Transform(control.PointToScreen(new Point(0, 0)));
                return mousePt.X > gridPt.X && mousePt.X < gridPt.X + control.ActualWidth &&
                mousePt.Y > gridPt.Y && mousePt.Y < gridPt.Y + control.ActualHeight;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                return false;
            }
        }

        private void ExecuteSelectedButton(string selectedButtonName)
        {
            if (selectedButtonName == null) return;

            WebImageScroll.ScrollToVerticalOffset(0); // reset scroll
            switch (selectedButtonName)
            {
                case "Receive":  //receive from other computer
                    ReceiverOn = !ReceiverOn;
                    if (ReceiverOn)
                    {
                        /*
                        GazePointer.Visibility = Visibility.Hidden;
                        ReceiveGazePointer.Visibility = Visibility.Visible;
                        */
                        IPHostEntry ipHostInfo = Dns.GetHostByName(Dns.GetHostName());
                        IPAddress ipAddress = ipHostInfo.AddressList[0];
                        Receive_Text.Text = "Receiver On\nIP:" + ipAddress.ToString();
                        Receive_Status_Text.Text = "Receiving Data\nIP:" + ipAddress.ToString();
                        Receive_Status_Text.Visibility = Visibility.Visible;
                        //Receiver_Pop.IsOpen = true;
                        //Receiver_Pop_TextBox.Text = "Please enter your IP address";
                        //Receiver_Pop_TextBox.SelectAll();
                    }
                    else
                    {
                        /*
                        GazePointer.Visibility = Visibility.Visible;
                        ReceiveGazePointer.Visibility = Visibility.Hidden;
                        */
                        Receive_Text.Text = "Receive Off";
                        Receive_Status_Text.Visibility = Visibility.Hidden;
                        //if wrap below???
                        ReceiverIP = "";
                        try
                        {
                            communicateThread_Receiver.Abort();

                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(e.ToString());
                        }
                        communication_started_Receiver = false;
                    }
                    break;
                case "Share": //send to other computer
                    SenderOn = !SenderOn;
                    if (SenderOn)
                    {
                        Share_Text.Text = "Share On";
                        if (defaultSenderIP != "")
                        {
                            SenderIP = defaultSenderIP;
                        }
                        else
                        {
                            Sender_Pop.IsOpen = true;
                            Sender_Pop_TextBox.Text = "Please enter other's IP address";
                            Sender_Pop_TextBox.SelectAll();
                        }
                        Share_Status_Text.Text = "Sharing Data\nIP:" + SenderIP.ToString();
                        Share_Status_Text.Visibility = Visibility.Visible;
                        communication_started_Sender = false;
                    }
                    else
                    {
                        Share_Text.Text = "Share Off";
                        Sender_Pop.IsOpen = false;
                        SenderIP = "";
                        Share_Status_Text.Visibility = Visibility.Hidden;
                        try
                        {
                            communicateThread_Sender.Abort();
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(e.ToString());
                        }
                        communication_started_Sender = false;
                    }
                    break;
                case "Trace": //show path
                    trackOn = !trackOn;
                    GazeSpot.Visibility = Visibility.Hidden;
                    PrevGazeSpot.Visibility = Visibility.Hidden;
                    SpotLine.Visibility = Visibility.Hidden;
                    if (baseLine != null)
                    {
                        baseLine.Points.Clear();
                        track_overlay.Children.Clear();
                        newLine = true;
                    }
                    if (trackOn)
                    {
                        Track_Text.Text = "Track On";
                        if (caseNum == 1)
                        {
                            WebImage.Source = new BitmapImage(new Uri("Graphics/CaseA/graphic1.png", UriKind.RelativeOrAbsolute));
                        }
                        else if (caseNum == 2)
                        {
                            WebImage.Source = new BitmapImage(new Uri("Graphics/CaseB/graphic2.png", UriKind.RelativeOrAbsolute));
                        }
                        else if (caseNum == 3)
                        {
                            WebImage.Source = new BitmapImage(new Uri("Graphics/CaseC/graphic3.png", UriKind.RelativeOrAbsolute));
                        }
                        firstSpotLine = false;
                        //also turn off highlight
                        if (highlightOn)
                        {
                            highlightOn = !highlightOn;
                            Highlight_Text.Text = "Highlight Off";
                            //FishCircle.Visibility = Visibility.Hidden;
                        }
                    }
                    else
                        Track_Text.Text = "Track Off";
                    break;
                case "Highlight":
                    highlightOn = !highlightOn;
                    gazeTime = 0;
                    //FishCircle.Visibility = Visibility.Hidden;
                    if (highlightOn)
                    {
                        Highlight_Text.Text = "Highlight ON";
                        if (caseNum == 1)
                        {
                            WebImage.Source = new BitmapImage(new Uri("Graphics/CaseA/BWgraphic1.png", UriKind.RelativeOrAbsolute));
                        }
                        else if (caseNum == 2)
                        {
                            WebImage.Source = new BitmapImage(new Uri("Graphics/CaseB/BWgraphic2.png", UriKind.RelativeOrAbsolute));
                        }
                        else if (caseNum == 3)
                        {
                            WebImage.Source = new BitmapImage(new Uri("Graphics/CaseC/BWgraphic3.png", UriKind.RelativeOrAbsolute));
                        }
                        //also turn off track
                        if (trackOn)
                        {
                            trackOn = !trackOn;
                            GazeSpot.Visibility = Visibility.Hidden;
                            PrevGazeSpot.Visibility = Visibility.Hidden;
                            SpotLine.Visibility = Visibility.Hidden;
                            firstSpotLine = false;
                            if (baseLine != null)
                            {
                                baseLine.Points.Clear();
                                track_overlay.Children.Clear();
                                newLine = true;
                            }
                            Track_Text.Text = "Track Off";
                        }
                    }
                    else
                    {
                        if (caseNum == 1)
                        {
                            WebImage.Source = new BitmapImage(new Uri("Graphics/CaseA/graphic1.png", UriKind.RelativeOrAbsolute));
                        }
                        else if (caseNum == 2)
                        {
                            WebImage.Source = new BitmapImage(new Uri("Graphics/CaseB/graphic2.png", UriKind.RelativeOrAbsolute));
                        }
                        else if (caseNum == 3)
                        {
                            WebImage.Source = new BitmapImage(new Uri("Graphics/CaseC/graphic3.png", UriKind.RelativeOrAbsolute));
                        }
                        Highlight_Text.Text = "Highlight Off";
                    }
                    break;
                case "Exit":
                    try
                    {
                        communicateThread_Receiver.Abort();
                        communicateThread_Sender.Abort();
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.ToString());
                    }
                    Close();
                    break;
                case "startrecording":
                    isRecording = !isRecording;
                    if (isRecording)
                    {
                        Data_Record_Status_Text.Text = "Data Record On";
                        Data_Record_Pop.IsOpen = true;
                        Data_Record_Pop_TextBox.Text = "Please enter the number on this laptop";
                        BitmapImage bimage = new BitmapImage();
                        bimage.BeginInit();
                        bimage.UriSource = new Uri("Graphics/stop-red-icon.png", UriKind.Relative);
                        bimage.EndInit();
                        startrecording.Icon = bimage;
                        //the following one is slow comparatively
                        //startrecording.Icon = new ImageSourceConverter().ConvertFromString("pack://application:,,,Graphics/stop-red-icon.png") as ImageSource;
                    }
                    else
                    {
                        shouldRecord = false;
                        Data_Record_Status_Text.Text = "Data Record Off";
                        Data_Record_Pop.IsOpen = false;
                        BitmapImage bimage = new BitmapImage();
                        bimage.BeginInit();
                        bimage.UriSource = new Uri("Graphics/start-icon.png", UriKind.Relative);
                        bimage.EndInit();
                        startrecording.Icon = bimage;
                        //startrecording.Icon = new ImageSourceConverter().ConvertFromString("pack://application:,,,Graphics/start-icon.png") as ImageSource;
                    }
                    break;
            }
        }

        private void CleanUp()
        {
            GazeManager.Instance.Deactivate();
            //scrollTimer.Stop();
            // De-register events
            if (IsTouchEnabled)
            {
                //
            }
            PreviewMouseDown -= TapDown;
            PreviewMouseUp -= TapUp;
            KeyDown -= WindowKeyDown;
            KeyUp -= WindowKeyUp;
        }
        private void receiveGaze(int x, int y)
        {
            var relativePt = new Point(x, y);
            relativePt = transfrm.Transform(relativePt);
            if ((ReceiveGazePointer.Visibility == Visibility.Visible) &&
                (Math.Sqrt((relativePt.X - lastPointUpdate.X) * (relativePt.X - lastPointUpdate.X) + (relativePt.Y - lastPointUpdate.Y) * (relativePt.Y - lastPointUpdate.Y)) > 3))
            {
                Canvas.SetLeft(ReceiveGazePointer, relativePt.X - ReceiveGazePointer.Width / 2);
                Canvas.SetTop(ReceiveGazePointer, relativePt.Y - ReceiveGazePointer.Height / 2);
                Console.Write(relativePt.X);
            }
            else
            {
                TestBox.Text = "Receiver's side Not Visible";
            }
            lastPointUpdate = relativePt;
            //send to textbox the coordinates in real time and the recording status
        }
        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            CleanUp();

            //clean up recording?
            isRecording = false;
            shouldRecord = false;

            trackOn = false;
            SenderOn = false;
            ReceiverOn = false;
            try
            {
                communicateThread_Receiver.Abort();
                communicateThread_Sender.Abort();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }

            base.OnClosing(e);
        }

        private static double CalcDpiScale()
        {
            return DPI_DEFAULT / GetSystemDpi().X;
        }

        #endregion
        #region Native methods

        public static Point GetSystemDpi()
        {
            Point result = new Point();
            IntPtr hDc = GetDC(IntPtr.Zero);
            result.X = GetDeviceCaps(hDc, (int)DeviceCap.LOGPIXELSX);
            result.Y = GetDeviceCaps(hDc, (int)DeviceCap.LOGPIXELSY);
            ReleaseDC(IntPtr.Zero, hDc);
            return result;
        }

        [DllImport("gdi32.dll")]
        private static extern int GetDeviceCaps(IntPtr hdc, int nIndex);

        [DllImport("user32.dll")]
        private static extern IntPtr GetDC(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern int ReleaseDC(IntPtr hWnd, IntPtr hDc);

        #endregion

        #region Sender/Receiver Methods

        public void tryCommunicateReceiver(int x, int y)
        {
            IPHostEntry ipHostInfo = Dns.Resolve(Dns.GetHostName());
            ReceiverIP = ipHostInfo.AddressList[0].ToString();
            //x_received = 0;
            //y_received = 0;
            //AsynchronousClient.StartClient(x, y);
            while (ReceiverIP == "")
            {
                System.Threading.Thread.Sleep(1000);
            }
            AsynchronousSocketListener.StartListening();


        }
        public void tryCommunicateSender(int x, int y)
        {
            while (SenderIP == "")
            {
                System.Threading.Thread.Sleep(1000);
            }
            SynchronousClient.StartClient(x, y);
            communication_started_Sender = false;

            //AsynchronousSocketListener.StartListening();

            //receiveGaze(x_received, y_received);
        }
        public class StateObject
        {
            // Client  socket.
            public Socket workSocket = null;
            // Size of receive buffer.
            public const int BufferSize = 1024;
            // Receive buffer.
            public byte[] buffer = new byte[BufferSize];
            // Received data string.
            public StringBuilder sb = new StringBuilder();
        }
        //THis is the Receiver function (Asyncronous)
        // Citation: https://msdn.microsoft.com/en-us/library/fx6588te%28v=vs.110%29.aspx
        public class AsynchronousSocketListener
        {
            // Thread signal.
            public static ManualResetEvent allDone = new ManualResetEvent(false);

            public AsynchronousSocketListener()
            {
            }

            public static void StartListening()
            {
                if (ReceiverIP != "")
                {

                    // Data buffer for incoming data.
                    byte[] bytes = new Byte[1024];

                    // Establish the local endpoint for the socket.
                    // The DNS name of the computer
                    IPHostEntry ipHostInfo = Dns.Resolve(Dns.GetHostName());
                    IPAddress ipAddress = IPAddress.Parse(ReceiverIP);
                    IPEndPoint localEndPoint = new IPEndPoint(ipAddress, ReceiverPort);

                    // Create a TCP/IP socket.
                    Socket listener = new Socket(AddressFamily.InterNetwork,
                        SocketType.Stream, ProtocolType.Tcp);

                    // Bind the socket to the local endpoint and listen for incoming connections.
                    try
                    {
                        listener.Bind(localEndPoint);
                        listener.Listen(100);
                        //ommunication_received==false
                        while (true)
                        {
                            // Set the event to nonsignaled state.
                            allDone.Reset();

                            // Start an asynchronous socket to listen for connections.
                            //Console.WriteLine("Waiting for a connection...");
                            listener.BeginAccept(
                                new AsyncCallback(AcceptCallback),
                                listener);

                            allDone.WaitOne();

                            // Wait until a connection is made before continuing.


                        }

                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.ToString());
                    }

                    //Console.WriteLine("\nPress ENTER to continue...");
                    //Console.Read();

                }
            }

            public static void AcceptCallback(IAsyncResult ar)
            {
                // Signal the main thread to continue.
                allDone.Set();

                // Get the socket that handles the client request.
                Socket listener = (Socket)ar.AsyncState;
                Socket handler = listener.EndAccept(ar);

                // Create the state object.
                StateObject state = new StateObject();
                state.workSocket = handler;
                handler.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
                    new AsyncCallback(ReadCallback), state);
            }

            public static void ReadCallback(IAsyncResult ar)
            {
                String content = String.Empty;

                // Retrieve the state object and the handler socket
                // from the asynchronous state object.
                StateObject state = (StateObject)ar.AsyncState;
                Socket handler = state.workSocket;

                // Read data from the client socket. 
                int bytesRead = handler.EndReceive(ar);

                if (bytesRead > 0)
                {
                    // There  might be more data, so store the data received so far.
                    state.sb.Append(Encoding.ASCII.GetString(
                        state.buffer, 0, bytesRead));

                    // Check for end-of-file tag. If it is not there, read 
                    // more data.
                    content = state.sb.ToString();
                    if (content.IndexOf("<EOF>") > -1)
                    {
                        // All the data has been read from the 
                        // client. Display it on the console.
                        int x_start_ind = content.IndexOf("x: "), x_end_ind = content.IndexOf("xend ");
                        int y_start_ind = content.IndexOf("y: "), y_end_ind = content.IndexOf("yend ");
                        int cursor_x_start_ind = content.IndexOf("cursorx: "), cursor_x_end_ind = content.IndexOf("cursorxend ");
                        int cursor_y_start_ind = content.IndexOf("cursory: "), cursor_y_end_ind = content.IndexOf("cursoryend ");
                        if (x_start_ind > -1 && x_end_ind > -1 && y_start_ind > -1 && y_end_ind > -1)
                        {
                            try
                            {
                                //convert the received string into x and y                                
                                x_received = Convert.ToInt32(content.Substring(x_start_ind + 3, x_end_ind - (x_start_ind + 3)));
                                y_received = Convert.ToInt32(content.Substring(y_start_ind + 3, y_end_ind - (y_start_ind + 3)));
                            }
                            catch (FormatException)
                            {
                                Console.WriteLine("Input string is not a sequence of digits.");
                            }
                            catch (OverflowException)
                            {
                                Console.WriteLine("The number cannot fit in an Int32.");
                            }
                        }
                        // Show the data on the console.
                        //Console.WriteLine("x : {0}  y: {1}", x_received, y_received);

                        // Echo the data back to the client.
                        Send(handler, content);
                    }
                    else
                    {
                        // Not all data received. Get more.
                        handler.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
                        new AsyncCallback(ReadCallback), state);
                    }
                }
            }

            private static void Send(Socket handler, String data)
            {
                // Convert the string data to byte data using ASCII encoding.
                byte[] byteData = Encoding.ASCII.GetBytes(data);

                // Begin sending the data to the remote device.
                handler.BeginSend(byteData, 0, byteData.Length, 0,
                    new AsyncCallback(SendCallback), handler);
            }

            private static void SendCallback(IAsyncResult ar)
            {
                try
                {
                    // Retrieve the socket from the state object.
                    Socket handler = (Socket)ar.AsyncState;

                    // Complete sending the data to the remote device.
                    int bytesSent = handler.EndSend(ar);
                    //Console.WriteLine("Sent {0} bytes to client.", bytesSent);x

                    handler.Shutdown(SocketShutdown.Both);
                    handler.Close();

                }
                catch (Exception e)
                {
                    Console.WriteLine(e.ToString());
                }
            }
        }
        //This is the sending function (Syncronous)
        public class SynchronousClient
        {

            public static void StartClient(int x, int y)
            {
                // Data buffer for incoming data.
                byte[] bytes = new byte[1024];

                // Connect to a remote device.
                try
                {
                    // Establish the remote endpoint for the socket.
                    // This example uses port 11000 on the local computer.
                    IPHostEntry ipHostInfo = Dns.GetHostByName(Dns.GetHostName());
                    IPAddress ipAddress = IPAddress.Parse(SenderIP);
                    IPEndPoint remoteEP = new IPEndPoint(ipAddress, SenderPort);

                    // Create a TCP/IP  socket.
                    Socket sender = new Socket(AddressFamily.InterNetwork,
                        SocketType.Stream, ProtocolType.Tcp);

                    // Connect the socket to the remote endpoint. Catch any errors.
                    try
                    {
                        sender.Connect(remoteEP);

                        Console.WriteLine("Socket connected to {0}",
                            sender.RemoteEndPoint.ToString());
                        string message_being_sent = "x: " + x + "xend y: " + y + "yend cursorx: " +
                            System.Windows.Forms.Cursor.Position.X + "cursorxend cursory: " +
                            System.Windows.Forms.Cursor.Position.Y + "cursoryend <EOF>";
                        // Encode the data string into a byte array.
                        byte[] msg = Encoding.ASCII.GetBytes(message_being_sent);

                        // Send the data through the socket.
                        int bytesSent = sender.Send(msg);

                        // Receive the response from the remote device.
                        int bytesRec = sender.Receive(bytes);
                        Console.WriteLine("Echoed test = {0}",
                            Encoding.ASCII.GetString(bytes, 0, bytesRec));

                        // Release the socket.
                        sender.Shutdown(SocketShutdown.Both);
                        sender.Close();

                    }
                    catch (ArgumentNullException ane)
                    {
                        Console.WriteLine("ArgumentNullException : {0}", ane.ToString());
                    }
                    catch (SocketException se)
                    {
                        Console.WriteLine("SocketException : {0}", se.ToString());
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("Unexpected exception : {0}", e.ToString());
                    }

                }
                catch (Exception e)
                {
                    Console.WriteLine(e.ToString());
                }
            }

            public static string data = null;
            public static int x_received, y_received;
            public static int cursor_x_received, cursor_y_received;
            public static void StartListening()
            {
                // Data buffer for incoming data.
                byte[] bytes = new Byte[1024];

                // Establish the local endpoint for the socket.
                // Dns.GetHostName returns the name of the 
                // host running the application.
                IPHostEntry ipHostInfo = Dns.GetHostByName(Dns.GetHostName());
                IPAddress ipAddress = IPAddress.Parse(SenderIP);
                IPEndPoint localEndPoint = new IPEndPoint(ipAddress, SenderPort);

                // Create a TCP/IP socket.
                Socket listener = new Socket(AddressFamily.InterNetwork,
                    SocketType.Stream, ProtocolType.Tcp);

                // Bind the socket to the local endpoint and 
                // listen for incoming connections.
                try
                {
                    listener.Bind(localEndPoint);
                    listener.Listen(10);

                    // Start listening for connections.
                    while (true)
                    {
                        Console.WriteLine("Waiting for a connection...");
                        // Program is suspended while waiting for an incoming connection.
                        Socket handler = listener.Accept();
                        data = null;

                        // An incoming connection needs to be processed.
                        while (true)
                        {
                            bytes = new byte[1024];
                            int bytesRec = handler.Receive(bytes);
                            data += Encoding.ASCII.GetString(bytes, 0, bytesRec);
                            if (data.IndexOf("<EOF>") > -1)
                            {
                                break;
                            }
                        }
                        int x_start_ind = data.IndexOf("x: "), x_end_ind = data.IndexOf("xend ");
                        int y_start_ind = data.IndexOf("y: "), y_end_ind = data.IndexOf("yend ");
                        //int cursor_x_start_ind = data.IndexOf("cursorx: "), cursor_x_end_ind = data.IndexOf("cursorxend ");
                        //int cursor_y_start_ind = data.IndexOf("cursory: "), cursor_y_end_ind = data.IndexOf("cursoryend ");
                        if (x_start_ind > -1 && x_end_ind > -1 && y_start_ind > -1 && y_end_ind > -1)
                        {
                            try
                            {
                                x_received = Convert.ToInt32(data.Substring(x_start_ind + 2, x_end_ind - 1));
                                y_received = Convert.ToInt32(data.Substring(y_start_ind + 2, y_end_ind - 1));
                            }
                            catch (FormatException)
                            {
                                Console.WriteLine("Input string is not a sequence of digits.");
                            }
                            catch (OverflowException)
                            {
                                Console.WriteLine("The number cannot fit in an Int32.");
                            }
                        }
                        //if (cursor_x_start_ind > -1 && cursor_x_end_ind > -1 && cursor_y_start_ind > -1 && cursor_y_end_ind > -1)
                        //{
                        //    try
                        //    {
                        //        cursor_x_received = Convert.ToInt32(data.Substring(cursor_x_start_ind + 8, cursor_x_end_ind - 1));
                        //        cursor_y_received = Convert.ToInt32(data.Substring(cursor_y_start_ind + 8, cursor_y_end_ind - 1));
                        //    }
                        //    catch (FormatException)
                        //    {
                        //        Console.WriteLine("Input string is not a sequence of digits.");
                        //    }
                        //    catch (OverflowException)
                        //    {
                        //        Console.WriteLine("The number cannot fit in an Int32.");
                        //    }
                        //}
                        // Show the data on the console.
                        Console.WriteLine("x : {0}  y: {1}", x_received, y_received);

                        // Echo the data back to the client.
                        byte[] msg = Encoding.ASCII.GetBytes(data);

                        handler.Send(msg);
                        handler.Shutdown(SocketShutdown.Both);
                        handler.Close();
                    }

                }
                catch (Exception e)
                {
                    Console.WriteLine(e.ToString());
                }
                Console.WriteLine("\nPress ENTER to continue...");
                Console.Read();
            }
        }
        #endregion
    }

}