using System;

namespace Util.TimerEvent
{
    /// <summary>
    /// TCP 데이터 수신 타임아웃을 관리하는 인터페이스
    /// </summary>
    public interface IDataTimeoutManager : IDisposable
    {
        void RegisterDataReceived(string dataType);
        void SetTimeoutAction(string dataType, Action timeoutAction);
        void RemoveTimeout(string dataType);
        void StartMonitoring();
        void StopMonitoring();
    }
}
