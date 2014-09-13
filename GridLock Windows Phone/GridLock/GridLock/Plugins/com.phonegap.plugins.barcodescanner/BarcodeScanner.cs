using System.Runtime.Serialization;
using WPCordovaClassLib;
using WPCordovaClassLib.Cordova.Commands;
using WPCordovaClassLib.Cordova.JSON;
using Microsoft.Phone.Shell;
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Tasks;
using System.Windows;
using System.Diagnostics;
using System.Windows.Navigation;


namespace Cordova.Extension.Commands
{
    public class BarcodeScanner : WPCordovaClassLib.Cordova.Commands.BaseCommand
    {

        #region Internal fields

        private PhoneApplicationFrame _frame;
        private object _frameContentWhenOpened;
        private barcodescanner.Scanner _scannerPage;

        #endregion

        /// <summary>
        /// Public method to scan
        /// </summary>
        public void scan(string options) 
        {
            Deployment.Current.Dispatcher.BeginInvoke(() =>
            {

                if (null == _frame)
                {
                    // Hook up to necessary events and navigate
                    _frame = Application.Current.RootVisual as PhoneApplicationFrame;
                    if (null != _frame)
                    {
                        _frameContentWhenOpened = _frame.Content;

                        _frame.Navigated += OnFrameNavigated;
                        _frame.NavigationStopped += OnFrameNavigationStoppedOrFailed;
                        _frame.NavigationFailed += OnFrameNavigationStoppedOrFailed;

                        _frame.Navigate(new System.Uri("/Plugins/com.phonegap.plugins.barcodescanner/Scanner.xaml?dummy=" + Guid.NewGuid().ToString(), UriKind.Relative));
                    }
                }
            });
        }

        /// <summary>
        /// Public method to encode: not implemented
        /// </summary>
        public void encode(string options)
        {
            DispatchCommandResult(new WPCordovaClassLib.Cordova.PluginResult(WPCordovaClassLib.Cordova.PluginResult.Status.ERROR, "Not implemented"));
        }

        private void OnFrameNavigationStoppedOrFailed(object sender, EventArgs e)
        {
            closeScanner();
        }


        private void OnFrameNavigated(object sender, NavigationEventArgs e)
        {

            if (e.Content == _frameContentWhenOpened)
            {
                // Navigation to original page; close the scanner page
                closeScanner();
            }
            else if (null == _scannerPage)
            {
                _scannerPage = e.Content as barcodescanner.Scanner;
                if (null != _scannerPage)
                {
                    _scannerPage.Completed += new EventHandler<barcodescanner.ScannerResult>(scanner_Completed);
                }
            }
        }

        /// <summary>
        /// Deattach events
        /// </summary>
        private void closeScanner()
        {

            // Unhook from events
            if (null != _frame)
            {
                _frame.Navigated -= OnFrameNavigated;
                _frame.NavigationStopped -= OnFrameNavigationStoppedOrFailed;
                _frame.NavigationFailed -= OnFrameNavigationStoppedOrFailed;

                _frame = null;
                _frameContentWhenOpened = null;
            }

            if (null != _scannerPage){
                _scannerPage = null;
            }
        }

        /// <summary>
        /// Callback with the scan result
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void scanner_Completed(object sender, barcodescanner.ScannerResult e)
        {
            string result;

            if (e.TaskResult == TaskResult.OK)
            {
                result = String.Format("\"cancelled\":{0}, \"text\":\"{1}\", \"format\":\"{2}\"", false.ToString().ToLower(), e.ScanCode, e.ScanFormat);
            }
            else
            {
                result = String.Format("\"cancelled\":{0}, \"text\":\"\", \"format\":\"\"", true.ToString().ToLower());
            }

            DispatchCommandResult(new WPCordovaClassLib.Cordova.PluginResult(WPCordovaClassLib.Cordova.PluginResult.Status.OK, "{" + result + "}"));
        }
        
    }
}
