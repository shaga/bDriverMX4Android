using System;
using Android.App;
using Android.Hardware;
using Android.OS;
using Android.Views;
using Android.Widget;
using bCoreDriverMx.Model;

namespace bCoreDriverMx.Views.Fragments
{
    public class SettingFragment : Fragment
    {
        private BcoreInfo _bcoreInfo;

        private EditText _editDisplayName;

        private ToggleButton _toggleMotionSteering;

        private ToggleButton _toggleMotorFlip;

        private ToggleButton _toggleServoFlip;

        private SeekBar _seekBarServoSwing;

        private SeekBar _seekBarServoTrim;

        private SeekBar _seekBarSubServoTrim;

        private TextView _labelSubServoTrim;

        private int _savedServoTrim;

        private int _savedSubServoTrim;

        public event EventHandler UpdatedServoTrim;

        public event EventHandler UpdatedSubServoTrim;

        public BcoreInfo BcoreInfo
        {
            get => _bcoreInfo;
            set
            {
                _bcoreInfo = value;
                UpdateValue();
            }
        }

        public bool IsEnableSubServo { get; set; }

        private bool CanUseMotion
        {
            get
            {
                var manager = Activity.GetSystemService(Android.Content.Context.SensorService) as SensorManager;

                var accelerometer = manager?.GetDefaultSensor(SensorType.Accelerometer);

                return accelerometer != null;
            }
        }

        public static SettingFragment CreateInstance()
        {
            var fragment = new SettingFragment();
            var args = new Bundle();
            fragment.Arguments = args;
            return fragment;
        }

        public override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            // Create your fragment here
        }

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            // Use this to return your custom view for this Fragment
            // return inflater.Inflate(Resource.Layout.YourFragment, container, false);

            var view = inflater.Inflate(Resource.Layout.Setting, container, false);

            _editDisplayName = view.FindViewById<EditText>(Resource.Id.setting_display_name);

            _toggleMotionSteering = view.FindViewById<ToggleButton>(Resource.Id.setting_motion_str);
            _toggleMotionSteering.Enabled = CanUseMotion;
            if (!CanUseMotion) _toggleMotionSteering.Checked = false;

            _toggleMotorFlip = view.FindViewById<ToggleButton>(Resource.Id.setting_motor_flip);

            _toggleServoFlip = view.FindViewById<ToggleButton>(Resource.Id.setting_servo_flip);

            _seekBarServoSwing = view.FindViewById<SeekBar>(Resource.Id.seekbar_servo_swing);

            _seekBarServoTrim = view.FindViewById<SeekBar>(Resource.Id.seekbar_servo_trim);
            _seekBarServoTrim.ProgressChanged += (s, e) =>
            {
                if (!IsVisible) return;
                if (BcoreInfo != null)
                    BcoreInfo.ServoTrimSeekBarValue = e.Progress;
                UpdatedServoTrim?.Invoke(this, EventArgs.Empty);
            };

            _seekBarSubServoTrim = view.FindViewById<SeekBar>(Resource.Id.seekbar_subservo_trim);
            _seekBarSubServoTrim.ProgressChanged += (s, e) =>
            {
                if (BcoreInfo != null)
                    BcoreInfo.SubServoTrimSeekBarValue = e.Progress;
                UpdatedSubServoTrim?.Invoke(this, EventArgs.Empty);
            };

            _labelSubServoTrim = view.FindViewById<TextView>(Resource.Id.label_subservo_trim);

            return view;
        }

        public override void OnHiddenChanged(bool hidden)
        {
            base.OnHiddenChanged(hidden);

            if (!hidden)
            {
                UpdateValue();
            }
        }

        private void UpdateValue()
        {
            Activity.RunOnUiThread(() =>
            {
                _editDisplayName.Text = BcoreInfo.DisplayName;
                _toggleMotionSteering.Checked = BcoreInfo.IsUseMotion && CanUseMotion;
                _toggleMotorFlip.Checked = BcoreInfo.IsMotorFlip;
                _toggleServoFlip.Checked = BcoreInfo.IsServoFlip;
                _seekBarServoSwing.Progress = BcoreInfo.ServoSwingSeekBarValue;
                _savedServoTrim = BcoreInfo.SubServoTrim;
                _seekBarServoTrim.Progress = BcoreInfo.ServoTrimSeekBarValue;
                _savedSubServoTrim = BcoreInfo.SubServoTrim;
                if (IsEnableSubServo)
                {
                    _seekBarSubServoTrim.Progress = BcoreInfo.SubServoTrimSeekBarValue;
                    _labelSubServoTrim.Visibility= ViewStates.Visible;
                    _seekBarSubServoTrim.Visibility = ViewStates.Visible;
                }
                else
                {
                    _labelSubServoTrim.Visibility = ViewStates.Invisible;
                    _seekBarSubServoTrim.Visibility = ViewStates.Invisible;
                }
            });
        }

        public void UpdateData()
        {
            if (BcoreInfo == null) return;

            bool isChanged = false;
            if (_editDisplayName.Text != BcoreInfo.DisplayName)
            {
                isChanged = true;
                BcoreInfo.DisplayName = _editDisplayName.Text;
            }

            if (_toggleMotionSteering.Checked != BcoreInfo.IsUseMotion)
            {
                isChanged = true;
                BcoreInfo.IsUseMotion = _toggleMotionSteering.Checked;
            }

            if (_toggleMotorFlip.Checked != BcoreInfo.IsMotorFlip)
            {
                isChanged = true;
                BcoreInfo.IsMotorFlip = _toggleMotorFlip.Checked;
            }

            if (_toggleServoFlip.Checked != BcoreInfo.IsServoFlip)
            {
                isChanged = true;
                BcoreInfo.IsServoFlip = _toggleServoFlip.Checked;
            }

            if (_seekBarServoSwing.Progress != BcoreInfo.ServoSwingSeekBarValue)
            {
                isChanged = true;
                BcoreInfo.ServoSwingSeekBarValue = _seekBarServoSwing.Progress;
            }

            if (_savedServoTrim != BcoreInfo.ServoTrim)
            {
                isChanged = true;
            }

            if (_savedSubServoTrim != BcoreInfo.SubServoTrim)
            {
                isChanged = true;
            }

            if (!isChanged) return;

            BcoreInfo.Save();
        }
    }
}