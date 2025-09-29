using System;
using System.Collections.Generic;
using System.Timers;

namespace TimerEvent
{
    /// <summary>
    /// TCP 데이터 수신 타임아웃 관리 클래스 (자세한 설명은 하단 참조)
    /// </summary>
    public class DataTimeoutManager : IDataTimeoutManager
    {
        private readonly Dictionary<string, Timer> _timers;
        private readonly Dictionary<string, Action> _timeoutActions;
        private readonly int _timeoutMilliseconds;
        private readonly object _lock = new object();
        private bool _disposed = false;

        public DataTimeoutManager(int timeoutSeconds = 3)
        {
            _timeoutMilliseconds = timeoutSeconds * 1000;
            _timers = new Dictionary<string, Timer>();
            _timeoutActions = new Dictionary<string, Action>();
        }

        /// <summary>
        /// 데이터 수신을 등록하고 타이머를 리셋
        /// </summary>
        /// <param name="dataType">데이터 타입 식별자</param>
        public void RegisterDataReceived(string dataType)
        {
            lock (_lock)
            {
                if (_disposed) return;

                try
                {
                    if (_timers.ContainsKey(dataType))
                    {
                        _timers[dataType].Stop();
                        _timers[dataType].Start();
                    }
                    else
                    {
                        CreateTimer(dataType);
                    }
                }
                catch (Exception ex)
                {
                    // Debug Log 등록
                }
            }
        }

        /// <summary>
        /// 타임아웃 발생 시 실행할 액션 설정
        /// </summary>
        /// <param name="dataType">데이터 타입 식별자</param>
        /// <param name="timeoutAction">타임아웃 시 실행할 액션</param>
        public void SetTimeoutAction(string dataType, Action timeoutAction)
        {
            lock (_lock)
            {
                if (_disposed) return;

                _timeoutActions[dataType] = timeoutAction;

                // 타이머가 이미 존재하면 새로 생성
                if (_timers.ContainsKey(dataType))
                {
                    _timers[dataType].Dispose();
                }
                CreateTimer(dataType, false);
            }
        }

        /// <summary>
        /// 특정 데이터 타입의 타임아웃 모니터링 제거
        /// </summary>
        /// <param name="dataType">데이터 타입 식별자</param>
        public void RemoveTimeout(string dataType)
        {
            lock (_lock)
            {
                if (_timers.ContainsKey(dataType))
                {
                    _timers[dataType].Dispose();
                    _timers.Remove(dataType);
                }

                if (_timeoutActions.ContainsKey(dataType))
                {
                    _timeoutActions.Remove(dataType);
                }
            }
        }

        /// <summary>
        /// 모든 타이머 모니터링 시작
        /// </summary>
        public void StartMonitoring()
        {
            lock (_lock)
            {
                if (_disposed) return;

                foreach (var timer in _timers.Values)
                {
                    timer.Start();
                }
            }
        }

        /// <summary>
        /// 모든 타이머 모니터링 중지
        /// </summary>
        public void StopMonitoring()
        {
            lock (_lock)
            {
                foreach (var timer in _timers.Values)
                {
                    timer.Stop();
                }
            }
        }

        private void CreateTimer(string dataType, bool startImmediately = true)
        {
            var timer = new Timer(_timeoutMilliseconds)
            {
                AutoReset = false
            };

            timer.Elapsed += (sender, e) => OnTimeout(dataType);
            _timers[dataType] = timer;

            if (startImmediately && _timeoutActions.ContainsKey(dataType))
            {
                timer.Start();
            }
        }

        private void OnTimeout(string dataType)
        {
            try
            {
                lock (_lock)
                {
                    if (_disposed) return;

                    if (_timeoutActions.ContainsKey(dataType))
                    {
                        // 타임아웃 발생 Debug Log 등록 : 안해도 됨
                        _timeoutActions[dataType]?.Invoke();
                    }
                }
            }
            catch (Exception ex)
            {
                // Debug Log 등록
            }
        }

        public void Dispose()
        {
            lock (_lock)
            {
                if (_disposed) return;

                _disposed = true;

                foreach (var timer in _timers.Values)
                {
                    timer?.Dispose();
                }

                _timers.Clear();
                _timeoutActions.Clear();
            }
        }
    }
}

//핵심 컴포넌트

//IDataTimeoutManager 인터페이스: 타임아웃 관리 기능을 정의
//DataTimeoutManager 클래스: 실제 타임아웃 관리 로직 구현
//TimeoutManagedViewModel 추상 클래스: 타임아웃 기능이 포함된 ViewModel 기본 클래스

//주요 기능
//1. 재사용 가능한 설계

//다른 ViewModel에서도 TimeoutManagedViewModel을 상속받아 사용 가능
//타임아웃 시간을 생성자에서 설정 가능 (기본 3초)

//2.타입별 타임아웃 관리

//각 데이터 타입별로 독립적인 타이머 관리
//데이터 수신 시 해당 타입의 타이머만 리셋

//3. 유연한 초기화 액션

//데이터 타입별로 다른 초기화 로직 설정 가능
//전체 모델 초기화 또는 부분 초기화 지원

//===============================================================//
// 화면 업데이트 부분에 RegisterDataReceived("데이터타입명"); 추가
// 다음과 같은 형식으로 사용 가능

//public class AnotherViewModel : TimeoutManagedViewModel
//{
//    public AnotherViewModel() : base(5) // 5초 타임아웃
//    {
//        _timeoutManager.StartMonitoring();
//    }

//    protected override void SetupTimeoutActions()
//    {
//        _timeoutManager.SetTimeoutAction("DataType1", () => InitializeData1());
//        _timeoutManager.SetTimeoutAction("DataType2", () => InitializeData2());
//    }

//    public override void InitializeToDefault()
//    {
//        // 전체 모델 초기화 로직
//    }
//}
//===============================================================//