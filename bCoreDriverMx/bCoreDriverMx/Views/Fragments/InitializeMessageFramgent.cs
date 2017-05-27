using Android.App;
using Android.OS;
using Android.Views;
using Android.Widget;

namespace bCoreDriverMx.Views.Fragments
{
    public class InitializeMessageFramgent : Fragment
    {
        private TextView _textMessage;

        public static InitializeMessageFramgent CreateInstance()
        {
            var fragment = new InitializeMessageFramgent();
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
            var view = inflater.Inflate(Resource.Layout.InitializeMessage, container, false);

            _textMessage = view.FindViewById<TextView>(Resource.Id.init_message);
            _textMessage.SetText(Resource.String.MsgConnecting);

            return view;
        }

        public void SetStateConnecting()
        {
            SetMessage(Resource.String.MsgConnecting);
        }

        public void SetStateInitializing()
        {
            SetMessage(Resource.String.MsgInitializing);
        }

        private void SetMessage(int messageId)
        {
            Activity.RunOnUiThread(() => _textMessage.SetText(messageId));
        }
    }
}