using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using LibBcore;

using System.Timers;

namespace bCoreDriverMx.Model
{
    /// <summary>
    /// bCore操作クラス
    /// </summary>
    class BcoreController
    {
        private const double TimerInterval = 50;

        private const int IdxBurstCmdMotor = 1;
        private const int IdxBurstCmdPortOut = 2;
        private const int IdxBurstCmdServoStart = 3;

        private readonly Context _context;

        private BcoreManager _manager;

        private readonly BcoreStatusReceiver _receiver;

        private BcoreInfo _selectedBcoreInfo;

        private bool _isInitialized;

        private readonly byte[] _burstValue =
        {
            Bcore.StopMotorPwm, Bcore.StopMotorPwm, 0,
            Bcore.CenterServoPos, Bcore.CenterServoPos, Bcore.CenterServoPos, Bcore.CenterServoPos,
        };

        private Timer _timerCommunication;

        private int _timerCounter;

        #region property

        /// <summary>
        /// 接続状態
        /// </summary>
        public bool IsConnected => _manager != null;

        /// <summary>
        /// バーストコマンド有効フラグ
        /// </summary>
        public bool IsEnableBurst => _manager?.IsEnableBurst ?? false;

        /// <summary>
        /// サーボ2,3有効フラグ
        /// </summary>
        public bool IsEnableSubServo => (FunctionInfo?.IsEnableServo(1) ?? false) ||
                                        (FunctionInfo?.IsEnableServo(2) ?? false);

        /// <summary>
        /// サーボ機能状態
        /// </summary>
        public BcoreFunctionInfo FunctionInfo { get; private set; }

        #endregion

        #region events

        public event EventHandler BcoreConnected;
        public event EventHandler BcoreInitialized;
        public event EventHandler<BcoreReadBatteryVoltageEventArgs> BcoreReadBatteryVoltage;
        public event EventHandler BcoreDisconnected; 

        #endregion

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="context">コンテキスト</param>
        public BcoreController(Context context)
        {
            _context = context;
            _manager = new BcoreManager(_context);
            _receiver = new BcoreStatusReceiver();
            _receiver.ChangedConnectionStatus += OnBcoreConnectionChanged;
            _receiver.DiscoveredService += OnBcoreServiceDiscoverd;
            _receiver.ReadBattery += OnBcoreBatteryVoltageRead;
            _receiver.ReadFunctions += OnBcoreFunctionRead;

            InitCommTimer();
        }

        /// <summary>
        /// 接続
        /// </summary>
        /// <param name="info">接続するbCore情報</param>
        public void Connect(BcoreInfo info)
        {
            if (_manager?.IsConnected ?? false)
            {
                Disconnect();
            }

            _selectedBcoreInfo = info;

            InitOutputValue();

            _manager.Connect(info.DeviceAddress);
        }

        /// <summary>
        /// 切断
        /// </summary>
        public void Disconnect()
        {
            if (_manager == null && !(_manager?.IsConnected ?? false)) return;

            StopTimer();

            _manager?.Disconnect();
        }

        /// <summary>
        /// レシーバ登録
        /// </summary>
        public void RegisterReceiver()
        {
            _context.RegisterReceiver(_receiver, BcoreStatusReceiver.CreateFilter());
        }

        /// <summary>
        /// レシーバ解除
        /// </summary>
        public void UnregisterReceiver()
        {
            _context.UnregisterReceiver(_receiver);
        }

        #region bcore status event

        /// <summary>
        /// bCore接続状態変更イベント
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e">接続状態</param>
        private void OnBcoreConnectionChanged(object sender, BcoreConnectionChangedEventArgs e)
        {
            if (e.Address != _selectedBcoreInfo?.DeviceAddress) return;

            if (e.State == EBcoreConnectionState.Connected)
            {
                BcoreConnected?.Invoke(this, EventArgs.Empty);
            }
            else if (e.State == EBcoreConnectionState.Disconnected)
            {
                if (_manager != null)
                {
                    BcoreDisconnected?.Invoke(this, EventArgs.Empty);
                }

                StopTimer();
            }
        }

        /// <summary>
        /// bCore BLEサービス発見イベント
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnBcoreServiceDiscoverd(object sender, BcoreDiscoverdServiceEventArgs e)
        {
            _manager?.ReadBatteryVoltage();
        }

        /// <summary>
        /// bCore機能読み込みイベント
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnBcoreFunctionRead(object sender, BcoreReadFunctionsEventArgs e)
        {
            _isInitialized = true;

            FunctionInfo = e.FunctionInfo;

            BcoreInitialized?.Invoke(this, EventArgs.Empty);

            StartTimer();
        }

        /// <summary>
        /// bCoreバッテリ電圧読み込みインベント
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnBcoreBatteryVoltageRead(object sender, BcoreReadBatteryVoltageEventArgs e)
        {
            if (!_isInitialized)
            {
                _manager.ReadFunctions();
            }

            BcoreReadBatteryVoltage?.Invoke(this, e);
        }

        #endregion

        #region communication timer

        /// <summary>
        /// 通信タイマ初期化
        /// </summary>
        private void InitCommTimer()
        {
            _timerCommunication = new Timer(TimerInterval)
            {
                AutoReset = true,
            };

            _timerCommunication.Elapsed += OnTimerTick;
        }

        /// <summary>
        /// タイマ開始
        /// </summary>
        private void StartTimer()
        {
            if (_timerCommunication.Enabled) return;

            _timerCounter = 0;
            _timerCommunication.Start();
        }

        /// <summary>
        /// タイマ終了
        /// </summary>
        private void StopTimer()
        {
            if (!_timerCommunication.Enabled) return;

            _timerCommunication.Stop();
        }

        /// <summary>
        /// タイマイベント
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnTimerTick(object sender, ElapsedEventArgs e)
        {
            if (_manager?.IsEnableBurst ?? false)
            {
                OnTimerUseBurst();
            }
            else if (_manager != null)
            {
                OnTimerUseNoBurst();
            }

            _timerCounter = (_timerCounter + 1) % 10;
        }

        /// <summary>
        /// バーストコマンド有効時タイマイベント処理
        /// </summary>
        private void OnTimerUseBurst()
        {
            if (_timerCounter % 5 > 0)
            {
                _manager?.WriteBurstCommand(_burstValue);
            }
            else
            {
                _manager?.ReadBatteryVoltage();
            }
        }

        /// <summary>
        /// バーストコマンド無効時タイマイベント処理
        /// </summary>
        private void OnTimerUseNoBurst()
        {
            switch (_timerCounter)
            {
                case 0:
                case 1:
                case 2:
                case 5:
                case 6:
                case 7:
                    var idx = _timerCounter % 5;
                    var value = _burstValue[idx + IdxBurstCmdServoStart];
                    _manager.WriteServoPos(idx, value);
                    break;
                case 3:
                case 8:
                    _manager.WriteMotorPwm(1, _burstValue[IdxBurstCmdMotor]);
                    break;
                case 4:
                    _manager.WritePortOut(_burstValue[IdxBurstCmdPortOut]);
                    break;
                case 9:
                    _manager.ReadBatteryVoltage();
                    break;
            }
        }

        #endregion

        #region bcore value method

        /// <summary>
        /// サーボ1中央値調整
        /// </summary>
        public void UpdateServoTrim()
        {
            _burstValue[IdxBurstCmdServoStart] = (byte)(Bcore.CenterServoPos + _selectedBcoreInfo.ServoTrim);
        }

        /// <summary>
        /// サーボ2,3中央値調整
        /// </summary>
        public void UpdateSubServoTrim()
        {
            _burstValue[IdxBurstCmdServoStart + 1] = (byte)(Bcore.CenterServoPos + _selectedBcoreInfo.SubServoTrim);
            _burstValue[IdxBurstCmdServoStart + 1] = (byte)(Bcore.CenterServoPos + _selectedBcoreInfo.SubServoTrim);
        }

        /// <summary>
        /// モータ速度設定
        /// </summary>
        /// <param name="value"></param>
        public void SetMotorSpeed(int value)
        {
            _burstValue[IdxBurstCmdMotor] = (byte) (Bcore.StopMotorPwm +
                                                    value * (_selectedBcoreInfo.IsMotorFlip ? 1 : -1));
        }

        /// <summary>
        /// ステアリング値設定
        /// </summary>
        /// <param name="value"></param>
        public void SetSteerValue(int value)
        {
            var offset = (int) (value * _selectedBcoreInfo.ServoSwing);

            SetServoValue(0, offset, _selectedBcoreInfo.ServoTrim, _selectedBcoreInfo.IsServoFlip);
            SetServoValue(1, offset, _selectedBcoreInfo.SubServoTrim, !_selectedBcoreInfo.IsServoFlip);
            SetServoValue(2, offset, _selectedBcoreInfo.SubServoTrim, _selectedBcoreInfo.IsServoFlip);
        }

        private void SetServoValue(int servoIdx, int offset, int trim, bool isFlip)
        {
            if (isFlip) offset *= -1;
            var value = Bcore.CenterServoPos + offset + trim;
            if (value > Bcore.MaxServoPos) value = Bcore.MaxServoPos;
            else if (value < Bcore.MinServoPos) value = Bcore.MinServoPos;

            _burstValue[servoIdx + IdxBurstCmdServoStart] = (byte) value;
        }

        /// <summary>
        /// ポート出力設定
        /// </summary>
        /// <param name="idx">インデックス</param>
        /// <param name="isOn">出力状態</param>
        public void SetPortOutValue(int idx, bool isOn)
        {
            var value = (0x01) << idx;

            if (isOn)
            {
                _burstValue[IdxBurstCmdPortOut] = (byte) (_burstValue[IdxBurstCmdPortOut] | value);
            }
            else
            {
                _burstValue[IdxBurstCmdPortOut] = (byte)(_burstValue[IdxBurstCmdPortOut] & ~value);
            }
        }

        /// <summary>
        /// 出力状態初期化
        /// </summary>
        private void InitOutputValue()
        {
            _isInitialized = false;

            for (var i = 0; i < 2; i++)
            {
                _burstValue[i] = Bcore.StopMotorPwm;
            }

            _burstValue[IdxBurstCmdPortOut] = 0;

            for (var i = IdxBurstCmdServoStart; i < _burstValue.Length; i++)
            {
                _burstValue[i] = Bcore.CenterServoPos;
            }
        }

        #endregion
    }
}