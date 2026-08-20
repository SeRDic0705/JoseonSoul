public interface IHitWindowEventReceiver
{
    void OnOpenHitWindow();   // 애니메이션 이벤트: 무기가 실제로 닿는 판정 구간 시작
    void OnCloseHitWindow();  // 애니메이션 이벤트: 판정 구간 종료
}
