namespace TaskFocusUI.Library.Data.State
{
    public delegate void DataStateChangedHandler(string propertyName, IDataState dataState);

    public interface IDataState
    {
        event DataStateChangedHandler DataStateChanged;
        void InvokeDataStateChanged(string propertyName);
    }
}