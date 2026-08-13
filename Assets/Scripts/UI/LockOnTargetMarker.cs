using UnityEngine;

// 락온 중인 타겟 위치에 화면상 표식(세키로식 작은 점)을 띄운다. 반드시 항상 활성 상태인 오브젝트
// (Canvas 등)에 부착할 것 — 마커 자신(markerRect)을 SetActive(false)하면 이 스크립트의 Update 계열
// 콜백도 함께 멈춰서 다시 켤 수 없게 되므로, 스크립트와 마커 오브젝트를 분리했다(CodexBot 교차검증).
public class LockOnTargetMarker : MonoBehaviour
{
    [SerializeField] private Player player;
    [SerializeField] private RectTransform markerRect;
    [Tooltip("계산된 화면 좌표에 픽셀 단위로 더할 오프셋. 락온 지점(LockPoint)이 원하는 시각적 위치(예: HP바 바로 위)와 안 맞을 때 미세조정용.")]
    [SerializeField] private Vector2 screenPixelOffset = Vector2.zero;

    private void Awake()
    {
        SetVisible(false);
    }

    private void LateUpdate()
    {
        if (!TryGetScreenPosition(out Vector2 screenPos))
        {
            SetVisible(false);
            return;
        }

        RectTransform parent = markerRect.parent as RectTransform;
        if (parent == null || !RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, screenPos, null, out Vector2 localPos))
        {
            SetVisible(false);
            return;
        }

        markerRect.anchoredPosition = localPos;
        SetVisible(true);
    }

    private bool TryGetScreenPosition(out Vector2 screenPos)
    {
        screenPos = default;

        if (player == null || player.LockOn == null || player.CameraBridge == null) return false;

        Transform target = player.LockOn.CurrentTarget;
        Transform lockPoint = player.LockOn.LockPoint;
        Camera renderCamera = player.CameraBridge.RenderCamera;
        if (target == null || lockPoint == null || renderCamera == null) return false;

        Vector3 viewportPos = renderCamera.WorldToViewportPoint(lockPoint.position);
        if (viewportPos.z <= 0f) return false;    // 카메라 뒤쪽
        if (viewportPos.x < 0f || viewportPos.x > 1f || viewportPos.y < 0f || viewportPos.y > 1f) return false;    // 화면 밖

        screenPos = new Vector2(viewportPos.x * renderCamera.pixelWidth, viewportPos.y * renderCamera.pixelHeight) + screenPixelOffset;
        return true;
    }

    private void SetVisible(bool visible)
    {
        if (markerRect != null && markerRect.gameObject.activeSelf != visible)
            markerRect.gameObject.SetActive(visible);
    }
}
