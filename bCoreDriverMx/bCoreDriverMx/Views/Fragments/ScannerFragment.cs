using System;
using System.Collections.Generic;
using System.Linq;
using Android.App;
using Android.OS;
using Android.Views;
using Android.Widget;
using bCoreDriverMx.Model;
using LibBcore;

namespace bCoreDriverMx.Views.Fragments
{
    public class ScannerFragment : Fragment
    {
        private const long ScanTimeoutLength = 10000;

        private Button _buttonScan;

        private BcoreScanner _scanner;

        private IList<BcoreInfo> _listBcore;

        private ListView _listViewBcore;

        private BcoreAdapter _adapter;

        private ProgressBar _progress;

        private Handler _handlerTimeout;

        public event EventHandler<BcoreInfo> SelectBcore;

        public static ScannerFragment CreateInstance()
        {
            var fragment = new ScannerFragment();
            var args = new Bundle();
            fragment.Arguments = args;
            return fragment;
        }

        public override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            _listBcore = new List<BcoreInfo>();

            _adapter = new BcoreAdapter(Activity, _listBcore);

            _handlerTimeout = null;

            _scanner = new BcoreScanner(Activity);
            _scanner.FoundBcore += OnFoundBcore;
        }

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            var view = inflater.Inflate(Resource.Layout.Scanner, container, false);

            _buttonScan = view.FindViewById<Button>(Resource.Id.scan_button);
            _buttonScan.Click += OnClickScan;
            _buttonScan.Enabled = true;

            _listViewBcore = view.FindViewById<ListView>(Resource.Id.bcore_list);
            _listViewBcore.ItemClick += OnSelectBcore;
            _listViewBcore.Adapter = _adapter;

            _progress = view.FindViewById<ProgressBar>(Resource.Id.progressBar);

            return view;
        }

        public override void OnHiddenChanged(bool hidden)
        {
            base.OnHiddenChanged(hidden);

            if (!hidden) return;

            _handlerTimeout?.RemoveCallbacks(OnScanTimeout);
            _handlerTimeout = null;

            _scanner?.StopScan();

            Activity?.RunOnUiThread(() =>
            {
                SetScanState();
                _listBcore.Clear();
                _adapter.NotifyDataSetChanged();
            });
        }

        public override void OnDestroyView()
        {
            base.OnDestroyView();

            _buttonScan.Click -= OnClickScan;
            _listViewBcore.ItemClick -= OnSelectBcore;
        }

        public override void OnDestroy()
        {
            base.OnDestroy();

            _scanner.FoundBcore -= OnFoundBcore;
        }

        private void OnSelectBcore(object sender, AdapterView.ItemClickEventArgs itemClickEventArgs)
        {
            var bcore = _adapter[itemClickEventArgs.Position];

            if (bcore == null) return;

            SelectBcore?.Invoke(this, bcore);
        }

        private void OnFoundBcore(object sender, BcoreDeviceInfo bcoreDeviceInfo)
        {
            if(_listBcore.Any(b => b.DeviceAddress == bcoreDeviceInfo.Address && b.DeviceName == bcoreDeviceInfo.Name)) return;

            var bcore = BcoreInfo.Load(bcoreDeviceInfo);

            _listBcore.Add(bcore);

            Activity.RunOnUiThread(() => _adapter.NotifyDataSetChanged());
        }

        private void OnClickScan(object sender, EventArgs eventArgs)
        {
            if (_scanner.IsScanning)
            {
                _handlerTimeout?.RemoveCallbacks(OnScanTimeout);
                _handlerTimeout = null;
                _scanner.StopScan();
            }
            else
            {
                _listBcore.Clear();
                _scanner.StartScan();
                _handlerTimeout = new Handler();
                _handlerTimeout.PostDelayed(OnScanTimeout, ScanTimeoutLength);
            }

            SetScanState();
        }

        private void SetScanState()
        {
            Activity.RunOnUiThread(() =>
            {
                _buttonScan?.SetText(_scanner.IsScanning ? Resource.String.ScanStop : Resource.String.ScanStart);
                if (_progress != null) _progress.Visibility = _scanner.IsScanning ? ViewStates.Visible : ViewStates.Invisible;
                _adapter.NotifyDataSetChanged();
            });
        }

        private void OnScanTimeout()
        {
            _handlerTimeout?.RemoveCallbacks(OnScanTimeout);
            _handlerTimeout = null;
            _scanner.StopScan();
            SetScanState();
        }
    }
}