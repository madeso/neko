namespace Neko
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Drawing;
    using System.Drawing.Imaging;
    using System.IO;
    using System.Linq;
    using System.Runtime.InteropServices;
    using System.Windows;
    using System.Windows.Input;
    using System.Windows.Media.Imaging;
    using System.Windows.Threading;

    using Neko.Annotations;

    /// <summary>
    ///     Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        private const double Dist = 70;
        private const double RunSpeed = 0.1;
        private const double ScratchSpeed = 0.3;
        private const double Speed = 120;

        private readonly Animation alertAnimation = new Animation("alert", -1, Properties.Resources.alert);
        private readonly Stopwatch deltatStopwatch = new Stopwatch();

        private readonly Animation escratchAnimation = new Animation("scatch", ScratchSpeed, Properties.Resources.escratch1, Properties.Resources.escratch2);
        private readonly DispatcherTimer followTimer = new DispatcherTimer();
        private readonly Animation idleAnimation = new Animation("idle", -1, Properties.Resources.still);
        private readonly Animation itchAnimation = new Animation("itch", 0.12, Properties.Resources.itch1, Properties.Resources.itch2);
        private readonly Animation nscratchAnimation = new Animation("scatch", ScratchSpeed, Properties.Resources.nscratch1, Properties.Resources.nscratch2);
        private readonly Random rr = new Random();

        private readonly Animation nrunAnimation = new Animation("run", RunSpeed, Properties.Resources.nrun1, Properties.Resources.nrun2);
        private readonly Animation erunAnimation = new Animation("run", RunSpeed, Properties.Resources.erun1, Properties.Resources.erun2);
        private readonly Animation srunAnimation = new Animation("run", RunSpeed, Properties.Resources.srun1, Properties.Resources.srun2);
        private readonly Animation wrunAnimation = new Animation("run", RunSpeed, Properties.Resources.wrun1, Properties.Resources.wrun2);

        private readonly Animation nerunAnimation = new Animation("run", RunSpeed, Properties.Resources.nerun1, Properties.Resources.nerun2);
        private readonly Animation serunAnimation = new Animation("run", RunSpeed, Properties.Resources.serun1, Properties.Resources.serun2);
        private readonly Animation swrunAnimation = new Animation("run", RunSpeed, Properties.Resources.swrun1, Properties.Resources.swrun2);
        private readonly Animation nwrunAnimation = new Animation("run", RunSpeed, Properties.Resources.nwrun1, Properties.Resources.nwrun2);

        private readonly Animation sleepAnimation = new Animation("sleep", 0.25, Properties.Resources.sleep1, Properties.Resources.sleep2);
        private readonly Animation sscratchAnimation = new Animation("scatch", ScratchSpeed, Properties.Resources.sscratch1, Properties.Resources.sscratch2);
        private readonly Animation wscratchAnimation = new Animation("scatch", ScratchSpeed, Properties.Resources.wscratch1, Properties.Resources.wscratch2);
        private readonly Animation yawnAnimation = new Animation("yawn", 1, Properties.Resources.yawn);
        private int animindex;
        private string animname = "";
        private double animtime;
        private BitmapImage currentFrame;
        private State state = State.Idle;
        private double stateTimer = 1;

        public MainWindow()
        {
            this.InitializeComponent();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private enum State
        {
            Idle,
            Alert,
            Itch,
            Sleep,
            Yawn,
            Scratch,
            PrepareToSleep,
            Run
        }

        public BitmapImage CurrentFrame
        {
            get
            {
                return this.currentFrame;
            }
            set
            {
                if (Equals(value, this.currentFrame))
                {
                    return;
                }
                this.currentFrame = value;
                this.OnPropertyChanged("CurrentFrame");
            }
        }

        protected PointF WindowPosition
        {
            get
            {
                return new PointF((float)this.Left, (float)this.Top);
            }
            set
            {
                this.Left = value.X;
                this.Top = value.Y;
            }
        }

        private Animation CurrentAnimation
        {
            set
            {
                if (this.animname != value.Name)
                {
                    this.animname = value.Name;
                    this.animindex = 0;
                    this.animtime = 0;
                }

                if (value.Speed < 0)
                {
                    this.animtime = 0;
                }
                else
                {
                    while (this.animtime > value.Speed)
                    {
                        this.animtime -= value.Speed;
                        ++this.animindex;
                    }
                }

                this.animindex = this.animindex % value.Frames.Count;
                this.CurrentFrame = value.Frames[this.animindex];
            }
        }

        private double DistanceToMouse
        {
            get
            {
                var mouse = GetMousePosition();
                return Vector.Distance(this.WindowPosition, mouse);
            }
        }

        public static PointF GetMousePosition()
        {
            Win32Point w32Mouse = new Win32Point();
            GetCursorPos(ref w32Mouse);
            return new PointF(w32Mouse.X, w32Mouse.Y);
        }

        internal static BitmapImage C(Image bitmap)
        {
            using (MemoryStream memory = new MemoryStream())
            {
                bitmap.Save(memory, ImageFormat.Png);
                memory.Position = 0;
                BitmapImage bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.StreamSource = memory;
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.EndInit();
                return bitmapImage;
            }
        }

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool GetCursorPos(ref Win32Point pt);

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged(string propertyName)
        {
            var handler = this.PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        private void FollowTimerOnTick(object sender, EventArgs eventArgs)
        {
            this.deltatStopwatch.Stop();
            double delta = this.deltatStopwatch.ElapsedMilliseconds / 1000.0;
            this.deltatStopwatch.Reset();
            this.deltatStopwatch.Start();
            this.OnUpdate(delta);
        }

        private double GetAngleToMouse()
        {
            var dir = Vector.Unit(Vector.Diff(GetMousePosition(), this.WindowPosition));
            var angle = Vector.Angle(dir);
            return angle;
        }

        private bool IsBetween(double lower, double angle, double upper)
        {
            if (angle <= lower)
            {
                return false;
            }
            if (angle >= upper)
            {
                return false;
            }
            return true;
        }

        private bool IsWithin90Span(double angle, double b)
        {
            return this.IsBetween(b - 45, angle, b + 45);
        }

        private void MoveToMouse(double delta)
        {
            var mouse = GetMousePosition();
            var move = Vector.Diff(mouse, this.WindowPosition);
            var d = (delta * Speed) / Vector.Length(move);
            move = Vector.Scale(move, d);
            this.WindowPosition = Vector.Add(this.WindowPosition, move);
        }

        private void OnUpdate(double delta)
        {
            this.animtime += delta;

            switch (this.state)
            {
                case State.Idle:
                    this.CurrentAnimation = this.idleAnimation;
                    if (this.DistanceToMouse > Dist*2)
                    {
                        this.SetStateAlert();
                    }
                    else if (this.stateTimer < 0)
                    {
                        var r = this.Rand();
                        if (r < 0.33)
                        {
                            this.SetStateItch();
                        }
                        else if (r < 0.66)
                        {
                            this.SetStateScatch();
                        }
                        else
                        {
                            this.SetStateYawn();
                        }
                    }
                    else
                    {
                        this.stateTimer -= delta;
                    }
                    break;
                case State.Alert:
                    this.CurrentAnimation = this.alertAnimation;
                    if (this.stateTimer < 0)
                    {
                        this.SetStateRun();
                    }
                    else
                    {
                        this.stateTimer -= delta;
                    }
                    break;
                case State.Itch:
                    this.CurrentAnimation = this.itchAnimation;
                    if (this.stateTimer < 0)
                    {
                        this.SetStateIdle();
                    }
                    else
                    {
                        this.stateTimer -= delta;
                    }
                    break;
                case State.Sleep:
                    this.CurrentAnimation = this.sleepAnimation;
                    if (this.stateTimer < 0)
                    {
                        this.SetStateIdle();
                    }
                    else
                    {
                        this.stateTimer -= delta;
                    }
                    break;
                case State.Yawn:
                    this.CurrentAnimation = this.yawnAnimation;
                    if (this.stateTimer < 0)
                    {
                        this.SetStatePrepareToSleep();
                    }
                    else
                    {
                        this.stateTimer -= delta;
                    }
                    break;
                case State.Scratch:
                    this.SetAnimation(this.nscratchAnimation, this.escratchAnimation, this.sscratchAnimation, this.wscratchAnimation);
                    if (this.stateTimer < 0)
                    {
                        this.SetStateIdle();
                    }
                    else
                    {
                        this.stateTimer -= delta;
                    }
                    break;
                case State.PrepareToSleep:
                    this.CurrentAnimation = this.idleAnimation;
                    if (this.stateTimer < 0)
                    {
                        this.SetStateSleep();
                    }
                    else
                    {
                        this.stateTimer -= delta;
                    }
                    break;
                case State.Run:
                    this.SetAnimation(this.nrunAnimation, this.erunAnimation, this.srunAnimation, this.wrunAnimation,
                        this.nerunAnimation, serunAnimation, swrunAnimation, this.nwrunAnimation);
                    if (this.DistanceToMouse > Dist)
                    {
                        this.MoveToMouse(delta);
                    }
                    else
                    {
                        this.SetStateIdle();
                    }
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private double Rand()
        {
            return this.rr.NextDouble();
        }

        private void SetAnimation(Animation north, Animation east, Animation south, Animation west)
        {
            var angle = this.GetAngleToMouse();
            if (this.IsWithin90Span(angle, 90))
            {
                this.CurrentAnimation = east;
            }
            else if (this.IsWithin90Span(angle, 180))
            {
                this.CurrentAnimation = south;
            }
            else if (this.IsWithin90Span(angle, 180 + 90))
            {
                this.CurrentAnimation = west;
            }
            else
            {
                this.CurrentAnimation = north;
            }
        }

        private void SetAnimation(Animation north, Animation east, Animation south, Animation west, Animation nourthEast,
            Animation southEeast, Animation southWest, Animation nourthWest)
        {
            var angle = this.GetAngleToMouse();
            if (this.IsWithin45Span(angle, 45))
            {
                this.CurrentAnimation = nourthEast;
            }
            else if (this.IsWithin45Span(angle, 90))
            {
                this.CurrentAnimation = east;
            }
            else if (this.IsWithin45Span(angle, 135))
            {
                this.CurrentAnimation = southEeast;
            }
            else if (this.IsWithin45Span(angle, 180))
            {
                this.CurrentAnimation = south;
            }
            else if (this.IsWithin45Span(angle, 225))
            {
                this.CurrentAnimation = southWest;
            }
            else if (this.IsWithin45Span(angle, 180 + 90))
            {
                this.CurrentAnimation = west;
            }
            else if (this.IsWithin45Span(angle, 315))
            {
                this.CurrentAnimation = nourthWest;
            }
            else
            {
                this.CurrentAnimation = north;
            }
        }

        private bool IsWithin45Span(double angle, int b)
        {
            return this.IsBetween(b - 22.5, angle, b + 22.5);
        }

        private void SetStateAlert()
        {
            this.state = State.Alert;
            this.stateTimer = 0.2;
        }

        private void SetStateIdle()
        {
            this.state = State.Idle;
            this.stateTimer = 1 + this.Rand() * 3;
        }

        private void SetStateItch()
        {
            this.state = State.Itch;
            this.stateTimer = 1 + this.Rand() * 5;
        }

        private void SetStatePrepareToSleep()
        {
            this.state = State.PrepareToSleep;
            this.stateTimer = 1;
            this.stateTimer = 0.5 + this.Rand() * 1.5;
        }

        private void SetStateRun()
        {
            this.state = State.Run;
        }

        private void SetStateScatch()
        {
            this.state = State.Scratch;
            this.stateTimer = 5;
        }

        private void SetStateSleep()
        {
            this.state = State.Sleep;
            this.stateTimer = 1 + 30 * this.Rand();
        }

        private void SetStateYawn()
        {
            this.state = State.Yawn;
            this.stateTimer = 0.5;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            this.DataContext = this;

            try
            {
                this.followTimer.Interval = new TimeSpan(0, 0, 0, 0, 50);
                this.followTimer.Tick += this.FollowTimerOnTick;
                this.followTimer.Start();
                this.deltatStopwatch.Start();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        private void Window_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
            this.Close();
        }

        private void Window_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            this.SetStateIdle();
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct Win32Point
        {
            public Int32 X;
            public Int32 Y;
        };

        private void Window_MouseEnter(object sender, MouseEventArgs e)
        {
            if (this.state == State.Idle && this.Rand() > 0.8)
            {
                this.SetStateItch();
            }
        }
    }

    internal class Animation
    {
        public Animation(string name, double speed, params Bitmap[] frames)
        {
            this.Name = name;
            this.Speed = speed;
            this.Frames = frames.Select((frame, num) => MainWindow.C(frame)).ToList();
        }

        public List<BitmapImage> Frames
        {
            get;
            private set;
        }

        public string Name
        {
            get;
            set;
        }

        public double Speed
        {
            get;
            private set;
        }
    }
}
