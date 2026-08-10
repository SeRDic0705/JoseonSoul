public interface IComboWindowEventReceiver
{
    void OnOpenComboWindow();   // 애니메이션 이벤트: 다음 콤보 입력을 받기 시작하는 시점
    void OnCloseComboWindow();  // 애니메이션 이벤트: 다음 콤보 입력 확정/마감 시점
}
