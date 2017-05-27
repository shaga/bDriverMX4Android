using System.IO;
using LibBcore;
using Newtonsoft.Json;
using Environment = System.Environment;

namespace bCoreDriverMx.Model
{
    /// <summary>
    /// bCore情報
    /// </summary>
    [JsonObject(MemberSerialization.OptIn)]
    public class BcoreInfo : Java.Lang.Object
    {
        /// <summary>
        /// 振れ幅係数デフォルト
        /// </summary>
        private const double ServoSwingDefault = 1.0;

        /// <summary>
        /// 中央位置調整デフォルト
        /// </summary>
        private const int ServoTrimDefault = 0;

        /// <summary>
        /// 保存ファイル拡張子
        /// </summary>
        private const string FileExt = ".json";

        /// <summary>
        /// データ保存先パス
        /// </summary>
        private static string DataPath => System.Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

        /// <summary>
        /// 表示名
        /// </summary>
        [JsonProperty]
        public string DisplayName { get; set; }

        /// <summary>
        /// bCore固有名
        /// </summary>
        public string DeviceName { get; set; }

        /// <summary>
        /// bCore MACアドレス
        /// </summary>
        public string DeviceAddress { get; set; }

        /// <summary>
        /// モーションコントロールフラグ
        /// </summary>
        [JsonProperty]
        public bool IsUseMotion { get; set; }

        /// <summary>
        /// モーター反転フラグ
        /// </summary>
        [JsonProperty]
        public bool IsMotorFlip { get; set; }

        /// <summary>
        /// サーボ反転フラグ
        /// </summary>
        [JsonProperty]
        public bool IsServoFlip { get; set; }

        /// <summary>
        /// サーボ振れ幅係数
        /// </summary>
        [JsonProperty]
        public double ServoSwing { get; set; } = ServoSwingDefault;

        /// <summary>
        /// サーボ振れ幅係数SeekBar向け
        /// </summary>
        public int ServoSwingSeekBarValue
        {
            get => (int) (ServoSwing * 100 ) - 50;
            set => ServoSwing = (value + 50) / 100.0;
        }

        /// <summary>
        /// サーボ1中央調整値
        /// </summary>
        [JsonProperty]
        public int ServoTrim { get; set; } = ServoTrimDefault;

        /// <summary>
        /// サーボ1中央調整値SeekBar向け
        /// </summary>
        public int ServoTrimSeekBarValue
        {
            get => ServoTrim + 30;
            set => ServoTrim = value - 30;
        }

        /// <summary>
        /// サーボ2,3中央調整値
        /// </summary>
        [JsonProperty]
        public int SubServoTrim { get; set; } = ServoTrimDefault;

        /// <summary>
        /// サーブ2,3中央調整値SeekBar向け
        /// </summary>
        public int SubServoTrimSeekBarValue
        {
            get => SubServoTrim + 30;
            set => SubServoTrim = value - 30;
        }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        [JsonConstructor]
        private BcoreInfo()
        {
        }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="bcore">bCoreデバイス情報</param>
        private BcoreInfo(BcoreDeviceInfo bcore) : this()
        {
            DisplayName = bcore.Name;
            DeviceName = bcore.Name;
            DeviceAddress = bcore.Address;
            IsUseMotion = true;
        }

        /// <summary>
        /// 保存
        /// </summary>
        public void Save()
        {
            var path = GetFilePath(DeviceName);

            var jsonSrc = JsonConvert.SerializeObject(this, Formatting.Indented);

            File.WriteAllText(path, jsonSrc);
        }

        /// <summary>
        /// 読み込み
        /// </summary>
        /// <param name="bcore">bCoreデバイス情報</param>
        /// <returns></returns>
        public static BcoreInfo Load(BcoreDeviceInfo bcore)
        {
            var info = Load(bcore.Name, bcore.Address) ?? new BcoreInfo(bcore);

            return info;
        }

        /// <summary>
        /// 読み込み
        /// </summary>
        /// <param name="name">bCore固有名</param>
        /// <param name="address">bCore MACアドレス</param>
        /// <returns></returns>
        public static BcoreInfo Load(string name, string address)
        {
            var path = GetFilePath(name);

            if (!File.Exists(path)) return null;

            var jsonSrc = File.ReadAllText(path);

            var info = JsonConvert.DeserializeObject<BcoreInfo>(jsonSrc);

            info.DeviceName = name;
            info.DeviceAddress = address;

            return info;
        }

        /// <summary>
        /// 保存JSONファイルパス取得
        /// </summary>
        /// <param name="name">bCore 固有名</param>
        /// <returns></returns>
        private static string GetFilePath(string name)
        {
            return Path.Combine(DataPath, name + FileExt);
        }
    }
}