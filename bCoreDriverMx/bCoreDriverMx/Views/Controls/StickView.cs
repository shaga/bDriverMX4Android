using System;
using Android.Content;
using Android.Graphics;
using Android.Util;
using Android.Views;

namespace bCoreDriverMx.Views.Controls
{
    public class StickView : View
    {
        private const float StickSizeByHight = 0.4f;
        private const float VerticalStickHightRateByHeight = 0.9f;
        private const float VerticalStickTopMarginRateByHeight = 0.05f;
        private const float VerticalStickLeftMarginRateBytWidth = 0.75f;
        private const float HorizontalStickWidthRateByWidth = 0.4f;
        private const float HorizontalStickTopMarginRateByHeight = 0.3f;
        private const float HorizontalStickLeftMarginRateByWidth = 0.33f;
        private const int BorderWidthDp = 10;

        private bool _isInitalized;

        private float _density;
        private float _borderWidth;
        private Paint _borderPaintWhite;
        private Paint _borderPaintGray;

        private Path _hStickAreaPath;
        private Path _vStickAreaPath;

        private RectF _vArea;
        private RectF _hArea;

        private float _vStickCenterX;
        private float _vStickCenterY;

        private float _hStickCetnerX;
        private float _hStickCenterY;

        private int _vid = -1;

        private int _hid = -1;

        private bool _isUseMotion;

        private float StickRaduis => AreaRadius - 10;

        private float AreaRadius => Height * 0.2f;

        private float AreaCenterVX => _vArea.CenterX();
        private float AreaCenterVY => _vArea.CenterY();

        private float VAreaMovableSize => Math.Abs(_vArea.Height() - AreaRadius * 2) / 2;

        private float AreaCenterHX => _hArea.CenterX();
        private float AreaCenterHY => _hArea.CenterY();

        private float HAreaMovableSize => Math.Abs(_hArea.Width() - AreaRadius * 2) / 2;

        private float VTouchBorderX => (_hArea.Right * 2 + _vArea.Left * 3) / 5;

        private float HTouchBorderX => (_hArea.Right * 3 + _vArea.Left * 2) / 5;

        public bool IsUseMotion
        {
            get => _isUseMotion;
            set
            {
                if (_isUseMotion != value)
                {
                    _isUseMotion = value;
                    Invalidate();
                }
            }
        }

        public event EventHandler<int> UpdateStickValueV;
        public event EventHandler<int> UpdateStickValueH;

        public StickView(Context context, IAttributeSet attrs) :
            base(context, attrs)
        {
            Initialize();
        } 

        public StickView(Context context, IAttributeSet attrs, int defStyle) :
            base(context, attrs, defStyle)
        {
            Initialize();
        }

        private void Initialize()
        {
            _isInitalized = false;

            _density = Context.Resources.DisplayMetrics.Density;

            _borderWidth = BorderWidthDp * _density;

            _borderPaintWhite = new Paint {AntiAlias = true, Color = Color.White, StrokeWidth = _borderWidth};
            _borderPaintWhite.SetStyle(Paint.Style.Stroke);
            _borderPaintGray = new Paint {AntiAlias = true, Color = Color.LightGray, StrokeWidth = _borderWidth};
            _borderPaintGray.SetStyle(Paint.Style.Stroke);
        }

        protected override void OnSizeChanged(int w, int h, int oldw, int oldh)
        {
            base.OnSizeChanged(w, h, oldw, oldh);

            _isInitalized = false;

            UpdatePath();

            _isInitalized = true;
        }

        protected override void OnDraw(Canvas canvas)
        {
            base.OnDraw(canvas);

            if (Width == 0 || Height == 0 || !_isInitalized) return;

            // Draw Right Vertical Stick Area Path
            canvas.DrawPath(_vStickAreaPath, _borderPaintWhite);

            // Draw Right Vertical Stick Circle
            canvas.DrawCircle(_vStickCenterX, _vStickCenterY, StickRaduis, _borderPaintWhite);

            // Dara Left Horizontal Stick Area Path
            canvas.DrawPath(_hStickAreaPath, _borderPaintWhite);

            // Draw Left Horizontal Stick Circle
            canvas.DrawCircle(_hStickCetnerX, _hStickCenterY, StickRaduis, IsUseMotion ? _borderPaintGray : _borderPaintWhite);
        }

        public override bool OnTouchEvent(MotionEvent e)
        {
            var idx = e.ActionIndex;
            var id = e.GetPointerId(idx);
            var x = e.GetX(idx);
            var y = e.GetY(idx);

            switch (e.ActionMasked)
            {
                case MotionEventActions.Down:
                case MotionEventActions.PointerDown:
                    if (SetPressStickV(id, x, y)) return true;
                    if (SetPressStickH(id, x, y)) return true;
                    break;
                case MotionEventActions.Up:
                case MotionEventActions.PointerUp:
                case MotionEventActions.Cancel:
                case MotionEventActions.Outside:
                    SetReleaseStickV(id);
                    SetReleaseStickH(id);
                    break;
                case MotionEventActions.Move:
                    for (var i = 0; i < e.PointerCount; i++)
                    {
                        id = e.GetPointerId(i);
                        x = e.GetX(i);
                        y = e.GetY(i);

                        MovePointV(id, x, y);
                        MovePointH(id, x, y);
                    }
                    break;
            }

            Invalidate();

            return base.OnTouchEvent(e);
        }

        public void SetMotionValue(double value)
        {
            if (!_isInitalized) return;

            var offset = (int)(HAreaMovableSize * value + 0.05);

            _hStickCetnerX = AreaCenterHX + offset;

            Invalidate();
        }

        private void MovePointH(int id, float x, float y)
        {
            if (id != _hid) return;

            UpdateStickH(x);
        }

        private void MovePointV(int id, float x, float y)
        {
            if (id != _vid) return;

            UpdateStickV(y);
        }

        private void SetReleaseStickV(int id)
        {
            if (_vid != id) return;

            _vid = -1;
            _vStickCenterX = AreaCenterVX;
            _vStickCenterY = AreaCenterVY;

            UpdateStickValueV?.Invoke(this, 0);
        }

        private void SetReleaseStickH(int id)
        {
            if (_hid != id) return;

            _hid = -1;
            _hStickCetnerX = AreaCenterHX;
            _hStickCenterY = AreaCenterHY;

            UpdateStickValueH?.Invoke(this, 0);
        }

        private void UpdateStickV(float y)
        {
            var offset = y - AreaCenterVY;

            if (Math.Abs(offset) > VAreaMovableSize)
            {
                offset = VAreaMovableSize * (offset > 0 ? 1 : -1);
            }

            _vStickCenterY = AreaCenterVY + offset;

            var v = (int)(100 * offset / VAreaMovableSize);

            UpdateStickValueV?.Invoke(this, v);
        }

        private void UpdateStickH(float x)
        {
            var offset = x - AreaCenterHX;

            if (Math.Abs(offset) > HAreaMovableSize)
            {
                offset = HAreaMovableSize * (offset > 0 ? 1 : -1);
            }

            _hStickCetnerX = AreaCenterHX + offset;

            var v = (int) (100 * offset / HAreaMovableSize);

            UpdateStickValueH?.Invoke(this, v);
        }

        private bool SetPressStickV(int id, float x, float y)
        {
            if (id == _vid) return true;
            if (_vid >= 0 || x < VTouchBorderX) return false;

            Android.Util.Log.Debug("StickView", $"Touch VerticalStick:{DateTime.Now:HH:mm:ss.fff}");

            _vid = id;

            UpdateStickV(y);
            return true;
        }

        private bool SetPressStickH(int id, float x, float y)
        {
            if (id == _hid) return true;
            if (IsUseMotion || _hid >= 0 || x > HTouchBorderX) return false;

            _hid = id;
            UpdateStickH(x);

            return true;
        }

        private void UpdateVRangePath()
        {
            var marginV = Height * VerticalStickTopMarginRateByHeight;
            var width = Height * StickSizeByHight;
            var height = Height * VerticalStickHightRateByHeight;
            var marginLeft = Width * VerticalStickLeftMarginRateBytWidth - width/2;

            _vArea = new RectF(marginLeft, marginV, marginLeft + width, marginV + height);

            _vStickCenterX = AreaCenterVX;
            _vStickCenterY = AreaCenterVY;

            var t = new RectF(_vArea) {Bottom = _vArea.Top + AreaRadius * 2};

            var b = new RectF(_vArea) {Top = _vArea.Bottom - AreaRadius * 2};

            // stick area path
            _vStickAreaPath = new Path();
            _vStickAreaPath.MoveTo(_vArea.Left, _vArea.Top + AreaRadius);
            _vStickAreaPath.ArcTo(t, 180, 180);
            _vStickAreaPath.LineTo(_vArea.Right, _vArea.Bottom - AreaRadius);
            _vStickAreaPath.ArcTo(b, 0, 180);
            _vStickAreaPath.LineTo(_vArea.Left, _vArea.Top + AreaRadius);
        }

        private void UpdateHRangePath()
        {
            var height = Height * StickSizeByHight;
            var width = Width * HorizontalStickWidthRateByWidth;
            var marginTop = Height * HorizontalStickTopMarginRateByHeight;
            var marginLeft = Width * HorizontalStickLeftMarginRateByWidth - width /2;

            _hArea = new RectF(marginLeft, marginTop, marginLeft + width, marginTop + height);

            _hStickCetnerX = AreaCenterHX;
            _hStickCenterY = AreaCenterHY;

            var l = new RectF(_hArea) {Right = _hArea.Left + AreaRadius * 2};
            var r = new RectF(_hArea) {Left = _hArea.Right - AreaRadius * 2};

            // stick area path
            _hStickAreaPath = new Path();
            _hStickAreaPath.MoveTo(_hArea.Left + AreaRadius, _hArea.Top);
            _hStickAreaPath.LineTo(_hArea.Right-AreaRadius, _hArea.Top);
            _hStickAreaPath.ArcTo(r, 270, 180);
            _hStickAreaPath.LineTo(_hArea.Right - AreaRadius, _hArea.Bottom);
            _hStickAreaPath.ArcTo(l, 90, 180);
        }

        private void UpdatePath()
        {
            UpdateVRangePath();
            UpdateHRangePath();
        }
    }
}