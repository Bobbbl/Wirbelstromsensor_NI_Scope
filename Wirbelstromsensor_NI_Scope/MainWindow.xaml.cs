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
using System.Collections;
using Microsoft.Research.DynamicDataDisplay;
using Microsoft.Research.DynamicDataDisplay.PointMarkers;
using Microsoft.Research.DynamicDataDisplay.DataSources;
using NationalInstruments.ModularInstruments;
using NationalInstruments.ModularInstruments.NIScope;
using NationalInstruments.ModularInstruments.SystemServices.DeviceServices;
using NationalInstruments;
using System.Windows.Threading;
using System.Threading;

namespace Wirbelstromsensor_NI_Scope
{

    public struct PointXY
    {
        public double _curveDate;
        public double _curveValue;

        public PointXY(double curveDate, double curveValue)
        {
            this._curveDate = curveDate;
            this._curveValue = curveValue;
        }
    }

    public class DatenSerie : System.Collections.ObjectModel.ObservableCollection<PointXY>
    {
        public DatenSerie()
        {
            this.Add(new PointXY(1, 1));
            this.Add(new PointXY(2, 2));
            this.Add(new PointXY(3, 3));
        }
    }
    /// <summary>
    /// Interaktionslogik für MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private Point _Point_2_Position;
        private double _minimum;
        private Point _Point_1_Position;
        private NIScope _scopeSession;
        private bool _stop;
        private double _sampleratemin = 200000;
        private AnalogWaveformCollection<double> _spectrumwaveform = null;
        private double _maximum;
        private bool started;
        private System.Windows.Threading.DispatcherTimer timer;
        private ObservableDataSource<Point> XY;

        public Point Point_2_Position
        {
            get
            {
                return this._Point_2_Position;
            }

            set
            {
                this._Point_2_Position = value;
                double p = Point_2_Position.X - Point_1_Position.X;
                this._minimum = p > 0 ? Point_1_Position.X : Point_2_Position.X;
                this._maximum = p < 0 ? Point_1_Position.X : Point_2_Position.X;


            }
        }
        public Point Point_1_Position
        {
            get
            {
                return this._Point_1_Position;
            }
            set
            {
                this._Point_1_Position = value;
                double p = Point_2_Position.X - Point_1_Position.X;
                this._minimum = p > 0 ? Point_1_Position.X : Point_2_Position.X;
                this._maximum = p < 0 ? Point_1_Position.X : Point_2_Position.X;
            }
        }

        public string resourceName
        {
            get
            {
                return ComboBoxResource.Text;
            }
            private set
            {
                ComboBoxResource.SelectedItem = value;
            }
        }

        public int recordlengthmin
        {
            get
            {
                return (int)ComboBoxRecordLength.SelectedItem;
            }
            set
            {

            }
        }

        public string ChannelName
        {
            get
            {
                return TextBlockChannel.Text;
            }
            set
            {
                TextBlockChannel.Text = value;
            }
        }

        public ScopeArrayMeasurementType ProcessStepMeasurement
        {
            get
            {
                return (ScopeArrayMeasurementType)ComboBoxProcessing.SelectedItem;
            }
        }

        public AnalogWaveformCollection<double> waveformfromscope { get; private set; }
        public ScopeMeasurementFilterType FilterType {
            get
            {
                return (ScopeMeasurementFilterType)this.ComboBoxFilter.SelectedItem;
            }
            
        }

        public double CenterFrequency {
            get
            {
                return Convert.ToDouble(TextblockCenter.Text);
            }
        }
        public double CutoffFrequency
        {
            get
            {
                return Convert.ToDouble(TextblockCutoff.Text);
            }
        }

        public double BandPassWidth {
            get
            {
                return Convert.ToDouble(TextblockWidth.Text);
            }
        }

        public MainWindow()
        {
            InitializeComponent();

            initializeComboBoxRecordLength();
            initializeComboBoxResource();
            initializeComboBoxProcessing();
            intializeTimer(100);
            initializeEvents();
            initializePlot();
            initializeComboBoxFilter();

        }

        private void initializeComboBoxFilter()
        {
            foreach (ScopeMeasurementFilterType enumValue in Enum.GetValues(typeof(ScopeMeasurementFilterType)))
            {
                ComboBoxFilter.Items.Add(enumValue);
            }
            ComboBoxFilter.SelectedItem = ComboBoxFilter.Items[0];
        }

        private void initializePlot()
        {
            XY = new ObservableDataSource<Point>();
            XY.SetXYMapping(p => p);
            plotter.AddLineGraph(XY, Colors.Black);
            plotter.FitToView();
            Point_1_Position = Point_1.Position;
            Point_2_Position = Point_2.Position;
        }

        private void intializeTimer(int v)
        {
            timer = new System.Windows.Threading.DispatcherTimer();
            timer.Tick += Timer_Tick;
            timer.Interval = new TimeSpan(0, 0, 0, 0, v);
            

        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            UpdatePoints();
        }

        protected void DoEvents()
        {
            if (Application.Current != null)
                Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new ThreadStart(delegate { }));
        }

        private void UpdatePoints()
        {
            if (_spectrumwaveform == null)
            {
                return;
            }

            List<double> measurementlist = FinderToolbox.ConvertNICollectionToPointCollection(_spectrumwaveform);

            List<Point> tmp = CalculateXY_List(measurementlist, _sampleratemin, recordlengthmin);

            XY.Collection.Clear();

            XY.AppendMany(tmp);

            //Get Minimum
            double min = _minimum;
            double max = _maximum;
            
            int index_min = FinderToolbox.FindIndexOfValue(FinderToolbox.PointToList(tmp, "X"), min);
            int index_max = FinderToolbox.FindIndexOfValue(FinderToolbox.PointToList(tmp, "X"), max);

            double peak = FinderToolbox.FindHighestValueInCollection(tmp, index_min, index_max);

            this.TextBlockAmplitude.Text = Convert.ToString(peak);

            if (XY == null)
            {
                return;
            }



        }

        private ObservableDataSource<Point> CalculateXY(List<double> measurementlist, double sampleratemin, int recordlengthmin)
        {
            ObservableDataSource<Point> r = new ObservableDataSource<Point>();

            double x, y;

            for (int i = 1; i < measurementlist.Count; i++)
            {
                x = Convert.ToDouble(i) / (recordlengthmin / sampleratemin);
                y = (measurementlist[i] * 2 * 1000000) / measurementlist.Count;

                r.Collection.Add(new Point(x, y));
            }

            return r;
        }

        private List<Point> CalculateXY_List(List<double> measurementlist, double sampleratemin, int recordlengthmin)
        {
            List<Point> r = new List<Point>();

            double x, y;

            for (int i = 1; i < measurementlist.Count; i++)
            {
                x = Convert.ToDouble(i) / (recordlengthmin / sampleratemin);
                //y = (measurementlist[i] * 2 * 1000000) / measurementlist.Count;
                y = (measurementlist[i]) * 10 / sampleratemin;
                y = y * 1000000;

                r.Add(new Point(x, y));
            }

            return r;
        }

        private void initializeComboBoxProcessing()
        {
            foreach (ScopeArrayMeasurementType enumValue in Enum.GetValues(typeof(ScopeArrayMeasurementType)))
            {
                ComboBoxProcessing.Items.Add(enumValue);
            }
            ComboBoxProcessing.SelectedItem = ComboBoxProcessing.Items[0];
        }

        private void initializeComboBoxResource()
        {
            using (ModularInstrumentsSystem scopeDevices = new ModularInstrumentsSystem("NI-Scope"))
            {
                foreach (DeviceInfo device in scopeDevices.DeviceCollection)
                {
                    ComboBoxResource.Items.Add(device.Name);
                }
            }

            if (ComboBoxResource.Items.Count > 0)
            {
                ComboBoxResource.SelectedIndex = 0;
            }
        }

        private void initializeEvents()
        {
            Point_1.PositionChanged += Point_1_PositionChanged;
            Point_2.PositionChanged += Point_2_PositionChanged;
        }

        private void Point_2_PositionChanged(object sender, Microsoft.Research.DynamicDataDisplay.Charts.PositionChangedEventArgs e)
        {
            this.Point_2_Position = e.Position;
        }

        private void Point_1_PositionChanged(object sender, Microsoft.Research.DynamicDataDisplay.Charts.PositionChangedEventArgs e)
        {
            this.Point_1_Position = e.Position;
        }

        private void initializeComboBoxRecordLength()
        {
            List<int> list = new List<int>();
            list.Add(2);
            list.Add(4);
            list.Add(8);
            list.Add(16);
            list.Add(32);
            list.Add(64);
            list.Add(128);
            list.Add(256);
            list.Add(512);
            list.Add(1024);
            list.Add(2048);
            list.Add(4096);
            list.Add(8192);
            list.Add(16384);
            list.Add(32768);
            list.Add(65536);
            this.ComboBoxRecordLength.ItemsSource = list;
            ComboBoxRecordLength.SelectedIndex = 9;
        }


        private void Start_Click(object sender, RoutedEventArgs e)
        {
            StartAcquisition();
        }

        private void StartAcquisition()
        {
            ChangeControlState(false);

            double referenceposition = 50.0;
            int numberofrecords = 1;
            bool enforcerealtime = true;

            timer.Start();

            try
            {
                InitializeSession();
                _scopeSession.Measurement.AutoSetup();

                


                while (!_stop)
                {
                    //Filter
                    _scopeSession.Channels[ChannelName].Measurement.Filter.Type = FilterType;
                    _scopeSession.Channels[ChannelName].Measurement.Filter.CutoffFrequency = CutoffFrequency;
                    _scopeSession.Channels[ChannelName].Measurement.Filter.CenterFrequency = CenterFrequency;
                    _scopeSession.Channels[ChannelName].Measurement.Filter.Width = BandPassWidth;

                    _scopeSession.Timing.ConfigureTiming(_sampleratemin, recordlengthmin,
                        referenceposition, numberofrecords, enforcerealtime);
                    _scopeSession.Channels[ChannelName].Measurement.AddWaveformProcessing(
                        ScopeArrayMeasurementType.FftAmplitudeSpectrumVoltsRms);

                    waveformfromscope = _scopeSession.Channels[ChannelName].Measurement.Read(
                        PrecisionTimeSpan.FromSeconds(1), 1000, waveformfromscope);
                    _spectrumwaveform = _scopeSession.Channels[ChannelName].Measurement.FetchArrayMeasurement(
                        new PrecisionTimeSpan(), ProcessStepMeasurement, _spectrumwaveform);

                    _scopeSession.Channels[ChannelName].Measurement.ClearWaveformProcessing();

                    if (started == false)
                    {
                        timer.IsEnabled = true;
                        //timer.Start();
                        started = true;
                    }

                    this.DoEvents();
                }

            }
            catch (Exception ex)
            {

                ShowError(ex);
            }
            finally
            {
                CloseSession();
                timer.Stop();
                ChangeControlState(true);
            }
        }

        private void CloseSession()
        {
            if (_scopeSession != null)
            {
                try
                {
                    _scopeSession.Close();
                    _scopeSession = null;
                }
                catch (Exception ex)
                {

                    ShowError(ex);
                    Application.Current.Shutdown();
                }
            }
        }



        private void InitializeSession()
        {
            _scopeSession = new NIScope(resourceName, false, false);
            _scopeSession.DriverOperation.Warning += new EventHandler<ScopeWarningEventArgs>(DriverOperationWarning);
        }

        private void DriverOperationWarning(object sender, ScopeWarningEventArgs e)
        {
            MessageBox.Show(e.Text, "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        private void ShowError(Exception ex)
        {
            MessageBox.Show(ex.Message, ex.GetType().ToString(),
                MessageBoxButton.OK, MessageBoxImage.Error);
        }

        private void ChangeControlState(bool isEnabled)
        {
            ComboBoxResource.IsEnabled = isEnabled;
            TextBlockChannel.IsEnabled = isEnabled;
            Start.IsEnabled = IsEnabled;
            Stop.IsEnabled = IsEnabled;

            if (!isEnabled)
            {
                ClearWaveforms();
            }
        }

        private void ClearWaveforms()
        {
            //Implementiere Löschen von Graph und Diagramm
        }

        private void plotter_Loaded(object sender, RoutedEventArgs e)
        {

        }

        private void Stop_Click(object sender, RoutedEventArgs e)
        {
            this._stop = true;
            timer.Stop();
            started = false;
            this.Stop.IsEnabled = false;

        }
    }
}
