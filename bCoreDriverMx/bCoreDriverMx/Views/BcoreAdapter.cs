using System.Collections.Generic;
using System.Linq;
using Android.Content;
using Android.Views;
using Android.Widget;
using bCoreDriverMx.Model;

namespace bCoreDriverMx.Views
{
    class BcoreAdapter : BaseAdapter<BcoreInfo>
    {
        private readonly Context _context;

        private readonly IList<BcoreInfo> _listBcore;

        public override int Count => _listBcore?.Count ?? 0;

        public override BcoreInfo this[int position]
        {
            get
            {
                if (_listBcore == null || position < 0 || _listBcore.Count <= position) return null;
                return _listBcore.ElementAt(position);
            }
        }

        public BcoreAdapter(Context context, IList<BcoreInfo> listBcore)
        {
            _context = context;
            _listBcore = listBcore;
        }

        public override long GetItemId(int position)
        {
            return position;
        }

        public override View GetView(int position, View convertView, ViewGroup parent)
        {
            var view = convertView ?? LayoutInflater.FromContext(_context)
                           .Inflate(Resource.Layout.FoundBcoreListItem, parent, false);

            var item = this[position];

            var displayName = view.FindViewById<TextView>(Resource.Id.display_name);

            displayName.Text = item.DisplayName;

            var deviceName = view.FindViewById<TextView>(Resource.Id.bcore_name);

            deviceName.Text = item.DeviceName;

            return view;
        }
    }
}