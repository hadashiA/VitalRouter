namespace UniTaskPubSub
{
    public sealed class AsyncPublishContext<T>
    {
        public T Message;
        public int CurrenetFilterIndex;

        public AsyncPublishContext(T msg)
        {
            Message = msg;
        }
    }
}