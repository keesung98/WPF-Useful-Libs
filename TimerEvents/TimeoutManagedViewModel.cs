using System;

namespace TimerEvent
{
    /// <summary>
    /// 모델 초기화를 위한 기본 인터페이스
    /// </summary>
    public interface IModelInitializer
    {
        void InitializeToDefault();
    }
    /// <summary>
    /// 타임아웃 관리가 포함된 ViewModel 기본 클래스
    /// </summary>
    public abstract class TimeoutManagedViewModel :IModelInitializer, IDisposable
    {
        protected readonly IDataTimeoutManager _timeoutManager;
        private bool _disposed = false;

        protected TimeoutManagedViewModel(int timeoutSeconds = 3)
        {
            _timeoutManager = new DataTimeoutManager(timeoutSeconds);
            SetupTimeoutActions();
        }

        /// <summary>
        /// 각 ViewModel에서 타임아웃 액션을 설정하는 추상 메서드
        /// </summary>
        protected abstract void SetupTimeoutActions();

        /// <summary>
        /// 모델을 기본값으로 초기화하는 추상 메서드
        /// </summary>
        public abstract void InitializeToDefault();

        /// <summary>
        /// 데이터 수신을 등록
        /// </summary>
        /// <param name="dataType">데이터 타입</param>
        protected void RegisterDataReceived(string dataType)
        {
            _timeoutManager?.RegisterDataReceived(dataType);
        }

        public new virtual void Dispose()
        {
            if (!_disposed)
            {
                _timeoutManager?.Dispose();
                _disposed = true;
            }
        }
    }
}
