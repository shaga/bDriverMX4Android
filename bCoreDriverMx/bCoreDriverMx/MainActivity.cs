using System;
using System.Linq;
using Android;
using Android.App;
using Android.Bluetooth;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Provider;
using Android.Support.V4.App;
using Android.Support.V4.Content;
using Android.Support.V7.App;
using Android.Views;
using Android.Widget;
using bCoreDriverMx.Model;
using bCoreDriverMx.Views.Fragments;
using Java.Lang;
using LibBcore;
using Uri = Android.Net.Uri;
using Fragment = Android.App.Fragment;

namespace bCoreDriverMx
{
    [Activity(Label = "@string/ApplicationName", MainLauncher = true, Icon = "@drawable/app_icon", Theme = "@style/ThemeSplash", ScreenOrientation = ScreenOrientation.Landscape)]
    public class MainActivity : AppCompatActivity
    {
        #region const

        private const string FragmentTagScanner = "bCoreDriverMx.MainActivity.Tag.Scanner";
        private const string FragmentTagMessage = "bCoreDriverMx.MainActivity.Tag.Message";
        private const string FragmentTagController = "bCoreDriverMx.MainActivity.Tag.Controller";
        private const string FragmentTagSetting = "bCoreDriverMx.MainActivity.Tag.Setting";

        private const int RequestCodePermissionAccessLocation = 100;

        private enum EState
        {
            None,
            Scan,
            Connecting,
            Control,
            Setting,
        }

        #endregion

        #region field

        /// <summary>
        /// showing fragment
        /// </summary>
        private Fragment _currentFragment;

        /// <summary>
        /// fragment for setting
        /// </summary>
        private SettingFragment _settingFragment;

        /// <summary>
        /// fragment to control bCore
        /// </summary>
        private ControllerFragment _controllerFragment;

        /// <summary>
        /// fragment to scan bCore
        /// </summary>
        private ScannerFragment _scannerFragment;

        /// <summary>
        /// fragment to show initializing messages
        /// </summary>
        private InitializeMessageFramgent _initializeMessageFramgent;

        /// <summary>
        /// selected bCore infomation
        /// </summary>
        private BcoreInfo _selectBcoreInfo;

        /// <summary>
        /// fragment status
        /// </summary>
        private EState _state;

        /// <summary>
        /// controller for bCore
        /// </summary>
        private BcoreController _controller;

        #endregion

        #region method

        #region overrid activity method

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);

            SetTheme(Resource.Style.MyTheme);

            // Set our view from the "main" layout resource
            SetContentView(Resource.Layout.Main);

            SetFullScreen();

            InitToolbar();

            InitFragment();

            InitController();
        }

        protected override void OnResume()
        {
            base.OnResume();

            SetState(EState.Scan);

            RequestLocationPermission();

            _controller?.RegisterReceiver();

            if (Build.VERSION.SdkInt < BuildVersionCodes.M)
            {
                RequestBluetoothEnable();
            }
        }

        protected override void OnPause()
        {
            base.OnPause();

            //SetState(EState.Scan);
            _controller?.UnregisterReceiver();
            _controller?.Disconnect();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
        }

        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, Permission[] grantResults)
        {
            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);

            if (requestCode != RequestCodePermissionAccessLocation) return;
            if ((grantResults?.Length ?? 0) > 0 && grantResults[0] == Permission.Granted)
            {
                RequestBluetoothEnable();
                return;
            }

            Toast.MakeText(this, Resource.String.ToastMsgDeniedLocaitonPermission, ToastLength.Short).Show();
        }

        public override bool OnSupportNavigateUp()
        {
            if (_state != EState.Scan)
            {
                OnBackPressed();
            }
            return base.OnSupportNavigateUp();
        }

        public override void OnBackPressed()
        {
            if (_state == EState.Scan)
            {
                base.OnBackPressed();
                return;
            }

            switch (_state)
            {
                case EState.Connecting:
                case EState.Control:
                    SetState(EState.Scan);
                    _controller.Disconnect();
                    break;
                case EState.Setting:
                    SetState(EState.Control);
                    break;
            }
        }

        public override bool OnCreateOptionsMenu(IMenu menu)
        {
            if (_state == EState.Control)
            {
                MenuInflater.Inflate(Resource.Menu.menu_controller, menu);
                return true;
            }
            return base.OnCreateOptionsMenu(menu);
        }

        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            if (item.ItemId == Resource.Id.menu_setting)
            {
                SetState(EState.Setting);
            }

            return base.OnOptionsItemSelected(item);
        }

        #endregion

        #region initalize

        /// <summary>
        /// Request to set bluetooth to enable
        /// </summary>
        private void RequestBluetoothEnable()
        {
            var manager = GetSystemService(Context.BluetoothService) as BluetoothManager;

            if (manager?.Adapter?.IsEnabled ?? false) return;

            Toast.MakeText(this, Resource.String.ToastMsgDisableBluetooth, ToastLength.Long).Show();

            var intent = new Intent();
            intent.SetAction(Settings.ActionBluetoothSettings);
            StartActivity(intent);
        }

        /// <summary>
        /// Request to location permission
        /// </summary>
        private void RequestLocationPermission()
        {
            if (Build.VERSION.SdkInt < BuildVersionCodes.M) return;

            var permission = PermissionChecker.CheckSelfPermission(this, Manifest.Permission.AccessCoarseLocation);

            if (permission == PermissionChecker.PermissionGranted) return;

            if (ActivityCompat.ShouldShowRequestPermissionRationale(this, Manifest.Permission.AccessCoarseLocation))
            {
                Toast.MakeText(this, Resource.String.ToastMsgRequireLocationPermission, ToastLength.Long).Show();

                var intent = new Intent();
                intent.SetAction(Settings.ActionApplicationDetailsSettings);
                intent.SetData(Uri.FromParts("package", ApplicationContext.PackageName, null));
                StartActivity(intent);
            }
            else
            {
                ActivityCompat.RequestPermissions(this, new[] { Manifest.Permission.AccessCoarseLocation }, RequestCodePermissionAccessLocation);
            }
        }

        /// <summary>
        /// Set full screen mode(hide navigation)
        /// </summary>
        private void SetFullScreen()
        {
            var view = Window.DecorView;

            var current = (int)view.SystemUiVisibility;

            var newoption = current | (int)SystemUiFlags.ImmersiveSticky | (int)SystemUiFlags.Fullscreen | (int)SystemUiFlags.HideNavigation;

            view.SystemUiVisibility = (StatusBarVisibility)newoption;
        }

        /// <summary>
        /// initalize toolbar
        /// </summary>
        private void InitToolbar()
        {
            var toolbar = FindViewById<Android.Support.V7.Widget.Toolbar>(Resource.Id.toolbar);

            SetSupportActionBar(toolbar);
            SupportActionBar.Title = GetString(Resource.String.ScanTitle);
        }

        /// <summary>
        /// initialize fragments
        /// </summary>
        private void InitFragment()
        {
            var transaction = FragmentManager.BeginTransaction();

            _settingFragment = FragmentManager.FindFragmentByTag<SettingFragment>(FragmentTagSetting);

            if (_settingFragment == null)
            {
                _settingFragment = SettingFragment.CreateInstance();
                transaction.Add(Resource.Id.frame, _settingFragment, FragmentTagSetting);
            }
            _settingFragment.UpdatedServoTrim += OnUpdatedServoTrim;
            _settingFragment.UpdatedSubServoTrim += OnUpdatedSubServoTrim;

            _controllerFragment = FragmentManager.FindFragmentByTag<ControllerFragment>(FragmentTagController);

            if (_controllerFragment == null)
            {
                _controllerFragment = ControllerFragment.CreateFragment();
                transaction.Add(Resource.Id.frame, _controllerFragment, FragmentTagController);
            }
            _controllerFragment.UpdateSpeedValue += OnUpdateSpeedValue;
            _controllerFragment.UpdateSteeringValue += OnUpdateSteeringValue;
            _controllerFragment.UpdatePortOutValue += OnUpdatePortOutValue;

            _scannerFragment = FragmentManager.FindFragmentByTag<ScannerFragment>(FragmentTagScanner);

            if (_scannerFragment == null)
            {
                _scannerFragment = ScannerFragment.CreateInstance();
                transaction.Add(Resource.Id.frame, _scannerFragment, FragmentTagScanner);
            }
            _scannerFragment.SelectBcore += OnSelectedBcore;

            _initializeMessageFramgent =
                FragmentManager.FindFragmentByTag<InitializeMessageFramgent>(FragmentTagMessage);

            if (_initializeMessageFramgent == null)
            {
                _initializeMessageFramgent = InitializeMessageFramgent.CreateInstance();
                transaction.Add(Resource.Id.frame, _initializeMessageFramgent, FragmentTagMessage);
            }

            transaction.Hide(_settingFragment);
            transaction.Hide(_controllerFragment);
            transaction.Hide(_initializeMessageFramgent);
            transaction.Hide(_scannerFragment);
            transaction.Commit();

            _state = EState.None;

            _currentFragment = null;
        }

        #endregion

        #region BcoreController

        private void InitController()
        {
            _controller = new BcoreController(this);

            _controller.BcoreConnected += OnConnectedBcore;
            _controller.BcoreDisconnected += OnDisconnectedBcore;
            _controller.BcoreInitialized += OnInitializedBcore;
            _controller.BcoreReadBatteryVoltage += OnReadBcoreBatteryVoltage;
        }

        private void OnConnectedBcore(object sender, EventArgs eventArgs)
        {
            _initializeMessageFramgent.SetStateInitializing();
        }

        private void OnDisconnectedBcore(object sender, EventArgs e)
        {
            if (_state == EState.Scan) return;

            SetState(EState.Scan);

            Toast.MakeText(this, Resource.String.ToastMsgDisconnected, ToastLength.Short).Show();
        }

        private void OnInitializedBcore(object sender, EventArgs eventArgs)
        {
            _controllerFragment.SetFunctionInfo(_controller.FunctionInfo);
            _controllerFragment.IsEnablueBurst = _controller.IsEnableBurst;

            _settingFragment.IsEnableSubServo = _controller.IsEnableSubServo;

            SetState(EState.Control);
        }

        private void OnReadBcoreBatteryVoltage(object sender, BcoreReadBatteryVoltageEventArgs e)
        {
            _controllerFragment.SetBatteryVoltage(e.Value);
        }

        #endregion

        #region event for fragment

        #region setting fragment

        private void OnUpdatedServoTrim(object sender, EventArgs e)
        {
            if (!(_controller?.IsConnected ?? false)) return;

            _controller.UpdateServoTrim();
        }

        private void OnUpdatedSubServoTrim(object sender, EventArgs e)
        {
            if (!(_controller?.IsConnected ?? false)) return;

            _controller.UpdateSubServoTrim();
        }

        #endregion

        #region control fragment

        private void OnUpdateSpeedValue(object sender, int value)
        {
            if (!(_controller?.IsConnected ?? false)) return;

            _controller.SetMotorSpeed(value);
        }

        private void OnUpdateSteeringValue(object sender, int value)
        {
            if (!(_controller?.IsConnected ?? false)) return;

            _controller.SetSteerValue(value);
        }

        private void OnUpdatePortOutValue(object sender, PortOutEventArgs e)
        {
            if (!(_controller?.IsConnected ?? false)) return;

            _controller.SetPortOutValue(e.Idx, e.IsOn);
        }

        #endregion

        #region scanner fragment

        /// <summary>
        /// selected bCore to connect
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="bcore"></param>
        private void OnSelectedBcore(object sender, BcoreInfo bcore)
        {
            if (bcore == null) return;

            _selectBcoreInfo = bcore;
            _settingFragment.BcoreInfo = bcore;
            _controllerFragment.BcoreInfo = bcore;

            _controller.Connect(bcore);

            SetState(EState.Connecting);
        }

        #endregion

        #endregion

        #region set to show fragment

        private void SetState(EState state)
        {
            if (state == _state) return;

            if (_state == EState.Setting)
            {
                _settingFragment.UpdateData();
            }

            Fragment next;

            switch (state)
            {
                case EState.Connecting:
                    next = _initializeMessageFramgent;
                    _initializeMessageFramgent.SetStateConnecting();
                    RunOnUiThread(() => SupportActionBar.Title = _selectBcoreInfo?.DisplayName);
                    break;
                case EState.Control:
                    next = _controllerFragment;
                    RunOnUiThread(() => SupportActionBar.Title = _selectBcoreInfo?.DisplayName);
                    break;
                case EState.Setting:
                    RunOnUiThread(() => SupportActionBar.Title = GetString(Resource.String.EditSetting));
                    next = _settingFragment;
                    _controller.SetMotorSpeed(0);
                    _controller.SetSteerValue(0);
                    break;
                default:
                    next = _scannerFragment;
                    RunOnUiThread(() => SupportActionBar.Title = GetString(Resource.String.ScanTitle));
                    break;
            }

            ChangeFragment(next, state != EState.Scan && state != EState.Connecting);

            _state = state;

            SupportActionBar.InvalidateOptionsMenu();
        }

        private void ChangeFragment(Fragment next, bool isShowBack = true)
        {
            var transaction = FragmentManager.BeginTransaction();
            if (_currentFragment != null) transaction.Hide(_currentFragment);
            transaction.Show(next);
            transaction.Commit();
            _currentFragment = next;

            SupportActionBar.SetDisplayShowHomeEnabled(isShowBack);
            SupportActionBar.SetDisplayHomeAsUpEnabled(isShowBack);
        }

        #endregion

        #endregion
    }
}



