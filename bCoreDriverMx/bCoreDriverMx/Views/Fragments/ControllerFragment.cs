using System;
using System.Collections.Generic;
using System.Linq;
using Android.App;
using Android.Content;
using Android.Hardware;
using Android.OS;
using Android.Views;
using Android.Widget;
using bCoreDriverMx.Model;
using bCoreDriverMx.Views.Controls;
using LibBcore;

namespace bCoreDriverMx.Views.Fragments
{
    public class ControllerFragment : Fragment, ISensorEventListener
    {
        private static readonly int[] PortOutId =
        {
            Resource.Id.toggle_po1,
            Resource.Id.toggle_po2,
            Resource.Id.toggle_po3,
            Resource.Id.toggle_po4,
        };

        private TextView _textBattery;

        private ToggleButton[] _togglePortOuts;

        private StickView _stickView;

        private bool _isEnablueBurst;

        private SensorManager SensorManager => Activity?.GetSystemService(Context.SensorService) as SensorManager;

        private Sensor Accelerometer => SensorManager?.GetDefaultSensor(SensorType.Accelerometer);

        private Sensor MagneticField => SensorManager?.GetDefaultSensor(SensorType.MagneticField);

        private bool HasAccelerometer => Accelerometer != null;

        private bool HasMagneticField => MagneticField != null;

        private List<float> _accelValue;

        private float[] _accelRaw;

        private float[] _magneticRaw;

        private float[] _rotationMatrix = new float[9];

        private float[] _orientation = new float[3];

        public BcoreInfo BcoreInfo { get; set; }

        public bool IsEnablueBurst
        {
            get => _isEnablueBurst;
            set
            {
                _isEnablueBurst = value;
                SetBatteryTextColor();
            }
        }

        public event EventHandler<int> UpdateSpeedValue;

        public event EventHandler<int> UpdateSteeringValue;

        public event EventHandler<PortOutEventArgs> UpdatePortOutValue; 

        public static ControllerFragment CreateFragment()
        {
            var fragment = new ControllerFragment();
            var args = new Bundle();
            fragment.Arguments = args;
            return fragment;
        }

        public override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
        }

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            var view = inflater.Inflate(Resource.Layout.Controller, container, false);

            _textBattery = view.FindViewById<TextView>(Resource.Id.text_battery);
            SetBatteryTextColor();

            _togglePortOuts = new ToggleButton[PortOutId.Length];

            for (var i = 0; i < PortOutId.Length; i++)
            {
                _togglePortOuts[i] = view.FindViewById<ToggleButton>(PortOutId[i]);
                _togglePortOuts[i].CheckedChange += OnPortOutCheckChanged;
            }

            _stickView = view.FindViewById<StickView>(Resource.Id.stick_view);
            _stickView.UpdateStickValueV += OnUpdateStickValueV;
            _stickView.UpdateStickValueH += OnUpdateStickValueH;

            return view;
        }

        private void OnPortOutCheckChanged(object sender, CompoundButton.CheckedChangeEventArgs checkedChangeEventArgs)
        {
            var toggle = sender as ToggleButton;

            if (toggle == null) return;

            var idx = 0;

            switch (toggle.Id)
            {
                case Resource.Id.toggle_po1:
                    idx = 0;
                    break;
                case Resource.Id.toggle_po2:
                    idx = 1;
                    break;
                case Resource.Id.toggle_po3:
                    idx = 2;
                    break;
                case Resource.Id.toggle_po4:
                    idx = 3;
                    break;
                default:
                    return;
            }

            UpdatePortOutValue?.Invoke(this, new PortOutEventArgs(idx, checkedChangeEventArgs.IsChecked));
        }

        public override void OnHiddenChanged(bool hidden)
        {
            base.OnHiddenChanged(hidden);

            if (hidden)
            {
                FinSonsor();
            }
            else
            {
                InitSensor();
                _stickView.IsUseMotion = BcoreInfo?.IsUseMotion ?? false;
            }
        }

        public void SetBatteryVoltage(int voltage)
        {
            Activity.RunOnUiThread(() => _textBattery.Text = $"POW:{voltage/1000.0f:0.00}[V]");
        }

        public void SetFunctionInfo(BcoreFunctionInfo functionInfo)
        {
            Activity.RunOnUiThread(() =>
            {
                for (var i = 0; i < _togglePortOuts.Length; i++)
                {
                    _togglePortOuts[i].Visibility = functionInfo.IsEnablePortOut(i)
                        ? ViewStates.Visible
                        : ViewStates.Invisible;

                    if (!functionInfo.IsEnablePortOut(i)) continue;

                    _togglePortOuts[i].Checked = false;
                }
            });
        }

        private void SetBatteryTextColor()
        {
            if (_textBattery == null) return;

            Activity.RunOnUiThread(() =>
            {
                _textBattery.SetTextColor(IsEnablueBurst
                    ? Android.Graphics.Color.Yellow
                    : Android.Graphics.Color.White);
            });
        }

        private void OnUpdateStickValueV(object sender, int value)
        {
            UpdateSpeedValue?.Invoke(this, value);
        }

        private void OnUpdateStickValueH(object sender, int value)
        {
            UpdateSteeringValue?.Invoke(this, value);
        }

        private void InitSensor()
        {
            if (!(BcoreInfo?.IsUseMotion ?? false)) return;

            if (HasAccelerometer)
            {
                if (_accelValue == null) _accelValue = new List<float>();
                else _accelValue.Clear();
            }

            if (HasAccelerometer)
            {
                SensorManager.RegisterListener(this, Accelerometer, SensorDelay.Game);
            }

            if (HasMagneticField)
            {
                SensorManager.RegisterListener(this, MagneticField, SensorDelay.Game);
            }

        }

        private void FinSonsor()
        {
            if (!(BcoreInfo?.IsUseMotion ?? false)) return;

            if (HasAccelerometer)
            {
                SensorManager.UnregisterListener(this, Accelerometer);
            }

            if (HasMagneticField)
            {
                SensorManager.UnregisterListener(this, MagneticField);
            }
        }

        public void OnAccuracyChanged(Sensor sensor, SensorStatus accuracy)
        {
        }

        public void OnSensorChanged(SensorEvent e)
        {
            if (!BcoreInfo.IsUseMotion || (!HasAccelerometer && !HasMagneticField)) return;

            if (e.Sensor.Type == SensorType.Accelerometer && !HasMagneticField)
            {
                UpdateAccelSensor(e.Values[1]);
            }
            else if (e.Sensor.Type == SensorType.Accelerometer)
            {
                _accelRaw = new float[3];
                e.Values.CopyTo(_accelRaw, 0);
            }
            else if (e.Sensor.Type == SensorType.MagneticField)
            {
                _magneticRaw = new float[3];
                e.Values.CopyTo(_magneticRaw, 0);
            }

            if (_accelRaw != null && _magneticRaw != null)
            {
                UpdateOrientation();
            }
        }

        private void UpdateOrientation()
        {
            SensorManager.GetRotationMatrix(_rotationMatrix, null, _accelRaw, _magneticRaw);
            SensorManager.GetOrientation(_rotationMatrix, _orientation);

            var v = 0 - _orientation[1] / (Math.PI /2);

            v = Math.Min(v, 1);
            v = Math.Max(v, -1);

            _stickView.SetMotionValue(v);

            var steer = (int) (v * 100);

            if (steer > 100) steer = 100;
            else if (steer < -100) steer = -100;

            UpdateSteeringValue?.Invoke(this, steer);
        }

        public void UpdateAccelSensor(float value)
        {
            _accelValue.Add(value);

            if (_accelValue.Count > 10) _accelValue.RemoveAt(0);

            var v = _accelValue.Average() / 9.8;

            v = Math.Min(v, 1);
            v = Math.Max(v, -1);

            _stickView.SetMotionValue(v);

            var steer = (int) (v * 100);

            if (steer > 100) steer = 100;
            else if (steer < -100) steer = -100;

            UpdateSteeringValue?.Invoke(this, steer);
        }
    }
}