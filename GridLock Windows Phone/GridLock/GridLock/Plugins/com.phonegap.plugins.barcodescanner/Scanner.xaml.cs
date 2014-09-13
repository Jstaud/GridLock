using Microsoft.Devices;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;
using ZXing;
using ZXing.Common;

namespace barcodescanner
{
    public partial class Scanner : PhoneApplicationPage
    {

        private const string VisibilityGroupName = "VisibilityStates";
        private const string OpenVisibilityStateName = "Open";
        private const string ClosedVisibilityStateName = "Closed";
        private const string StateKey_Value = "ScannerPageBase_State_Value";

        private Storyboard _closedStoryboard;

        /// <summary>
        /// Occurs when a barcode scan task is completed.
        /// </summary>
        public event EventHandler<ScannerResult> Completed;

        private ScannerResult result = new ScannerResult(TaskResult.Cancel);
        private DispatcherTimer timer;
        private readonly WriteableBitmap dummyBitmap = new WriteableBitmap(1, 1);
        private PhotoCameraLuminanceSource _luminance;
        private IBarcodeReader _reader;
        private PhotoCamera _photoCamera;
        private SoundEffect scanEffect;

        public Scanner()
        {
            InitializeComponent();

            // Hook up to storyboard(s)
            FrameworkElement templateRoot = VisualTreeHelper.GetChild(this, 0) as FrameworkElement;
            if (null != templateRoot)
            {
                foreach (VisualStateGroup group in VisualStateManager.GetVisualStateGroups(templateRoot))
                {
                    if (VisibilityGroupName == group.Name)
                    {
                        foreach (VisualState state in group.States)
                        {
                            if ((ClosedVisibilityStateName == state.Name) && (null != state.Storyboard))
                            {
                                _closedStoryboard = state.Storyboard;
                                _closedStoryboard.Completed += OnClosedStoryboardCompleted;
                            }
                        }
                    }
                }
            }

            // Play the Open state
            VisualStateManager.GoToState(this, OpenVisibilityStateName, true);
        }

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            // Restore Value if returning to application (to avoid inconsistent state)
            if (State.ContainsKey(StateKey_Value))
            {

                // Back out from scan page for consistency with behavior of core pickers in this scenario
                if (NavigationService.CanGoBack)
                {
                    NavigationService.GoBack();

                    return;
                }
            }

            if (_photoCamera == null){
                _photoCamera = new PhotoCamera();
                _photoCamera.Initialized += OnPhotoCameraInitialized;
                _previewVideo.SetSource(_photoCamera);

                CameraButtons.ShutterKeyHalfPressed += OnButtonHalfPress;
            }

            if (timer == null){
                timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
                timer.Tick += (o, arg) => ScanPreviewBuffer();
            }

            if (scanEffect == null){

                var resourceStream = Application.GetResourceStream(new System.Uri("Plugins/com.phonegap.plugins.barcodescanner/beep.wav", UriKind.Relative));
                if (resourceStream != null)
                {
                    scanEffect = SoundEffect.FromStream(resourceStream.Stream);          
                }
            }

            timer.Start();            
        }

        protected override void OnNavigatedFrom(System.Windows.Navigation.NavigationEventArgs e)
        {

            base.OnNavigatedFrom(e);

            if (timer != null)
            {
                timer.Stop();
                timer = null;
            }

            stopCamera();

            // Save Value if navigating away from application
            if ("app://external/" == e.Uri.ToString())
            {
                State[StateKey_Value] = true;
            }
            else if (this.Completed != null)
            {
                this.Completed(this, result);
            }
        }

        /// <summary>
        /// Called when the Back key is pressed.
        /// </summary>
        /// <param name="e">Event arguments.</param>
        protected override void OnBackKeyPress(CancelEventArgs e)
        {
            if (null == e)
            {
                throw new ArgumentNullException("e");
            }

            // Cancel back action so we can play the Close state animation (then go back)
            e.Cancel = true;
            CloseScannerPage();
        }

        private void CloseScannerPage()
        {
            // Play the Close state (if available)
            if (null != _closedStoryboard)
            {
                VisualStateManager.GoToState(this, ClosedVisibilityStateName, true);
            }
            else
            {
                OnClosedStoryboardCompleted(null, null);
            }
        }

        private void OnClosedStoryboardCompleted(object sender, EventArgs e)
        {
            // Close the scan page
            NavigationService.GoBack();
        }

        private void OnPhotoCameraInitialized(object sender, CameraOperationCompletedEventArgs e)
        {
            var width = Convert.ToInt32(_photoCamera.PreviewResolution.Width);
            var height = Convert.ToInt32(_photoCamera.PreviewResolution.Height);

            Dispatcher.BeginInvoke(() =>
            {
                _previewTransform.Rotation = _photoCamera.Orientation;
                // create a luminance source which gets its values directly from the camera
                // the instance is returned directly to the reader
                _luminance = new PhotoCameraLuminanceSource(width, height);
                _reader = new BarcodeReader(null, bmp => _luminance, null);
            });
        }

        private void ScanPreviewBuffer()
        {
            if (_luminance == null)
                return;

            _photoCamera.GetPreviewBufferY(_luminance.PreviewBufferY);
            // use a dummy writeable bitmap because the luminance values are written directly to the luminance buffer
            var result = _reader.Decode(dummyBitmap);
            Dispatcher.BeginInvoke(() => DisplayResult(result));
        }

        private void DisplayResult(Result resultReader)
        {
            if (resultReader == null)
            {
                return;
            }
            
            result = new ScannerResult(Microsoft.Phone.Tasks.TaskResult.OK);
            result.ScanCode = resultReader.Text;
            result.ScanFormat = resultReader.BarcodeFormat.ToString();

            FrameworkDispatcher.Update();
            scanEffect.Play();

            if (this.NavigationService.CanGoBack)
            {
                this.NavigationService.GoBack();
            }
        }

        private bool stopCamera()
        {

            if (_photoCamera == null)
            {
                return false;
            }
            
            _luminance = null;
            _reader = null;

            _photoCamera.Initialized -= OnPhotoCameraInitialized;
            _photoCamera.Dispose();

            CameraButtons.ShutterKeyHalfPressed -= OnButtonHalfPress; 

            _photoCamera = null;

            return true;
        }

        private void OnButtonHalfPress(object sender, EventArgs e)
        {
            this.makeFocus();
        }

        private void _focusButton_Click(object sender, RoutedEventArgs e)
        {
            this.makeFocus();
        }

        private void makeFocus()
        {
            if (_photoCamera != null && _photoCamera.IsFocusSupported)
            {
                try
                {
                    _photoCamera.Focus();
                }
                catch
                {

                }
            }
        }
    }
}