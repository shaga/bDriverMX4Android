using System;

namespace bCoreDriverMx.Model
{
    /// <summary>
    /// ポート出力変更イベント変数
    /// </summary>
    public class PortOutEventArgs : EventArgs
    {
        /// <summary>
        /// インデックス
        /// </summary>
        public int Idx { get; }

        /// <summary>
        /// 出力状態
        /// </summary>
        public bool IsOn { get; }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="idx">インデックス</param>
        /// <param name="isOn">出力状態</param>
        public PortOutEventArgs(int idx, bool isOn)
        {
            Idx = idx;
            IsOn = isOn;
        }
    }
}