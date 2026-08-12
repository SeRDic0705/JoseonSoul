using UnityEngine;

// 카메라가 벽 등에 막혀 오빗 거리를 줄이다 더 못 줄어들 만큼 가까워지면 캐릭터를 디더링(다크소울/세키로 스타일)으로 지워나간다.
// 커스텀 셰이더 없이 URP가 이미 내장한 LOD 크로스페이드 디더링(Lit.shader의 LOD_FADE_CROSSFADE + unity_LODFade)을 재사용.
// 전제: URP Asset의 "LOD Cross Fade"가 켜져 있어야 함(PC_RPAsset/Mobile_RPAsset에 이미 켜져 있음, m_LODCrossFadeDitheringType=BlueNoise).
public class PlayerCameraProximityFade : MonoBehaviour
{
    private static readonly int LodFadeId = Shader.PropertyToID("unity_LODFade");

    [Tooltip("이 거리보다 카메라가 가까워지기 시작하면 페이드가 시작된다. CameraFollowTarget(헤드 높이) 기준.")]
    [SerializeField] private float fadeStartDistance = 1f;
    [Tooltip("이 거리에서 완전히 클립(디더링)된다. Deoccluder의 MinimumDistanceFromTarget(0.1)보다 살짝 위로 잡아야 실제로 도달 가능하다.")]
    [SerializeField] private float fadeEndDistance = 0.3f;
    [Tooltip("알파 변화를 부드럽게 하는 시간(초). 벽 모서리 근처에서 깜빡이는 걸 막아준다.")]
    [SerializeField] private float fadeSmoothTime = 0.15f;

    private const float AlphaChangeEpsilon = 0.001f;

    private Player player;    // CameraFollowTarget.FollowPoint(헤드 높이)를 거리 측정 기준으로 쓰기 위함 — 루트(발밑) 기준이면
                               // 오빗 카메라가 아무리 눌려도 높이차 때문에 거리가 절대 fadeEndDistance까지 안 내려감(실측 확인됨)
    private Renderer[] renderers;
    private Material[] materialInstances;    // .materials로 생성된 런타임 전용 복제본 — OnDestroy에서 정리 대상
    private MaterialPropertyBlock propertyBlock;
    private float currentAlpha = 1f;
    private float appliedAlpha = -1f;    // 값이 실제로 바뀔 때만 SetPropertyBlock 하기 위한 마지막 적용값
    private float alphaVelocity;

    private void Awake()
    {
        player = GetComponent<Player>();
        renderers = GetComponentsInChildren<Renderer>();
        propertyBlock = new MaterialPropertyBlock();

        var instances = new System.Collections.Generic.List<Material>();
        foreach (Renderer r in renderers)
        {
            // .materials(복수형)는 렌더러별 인스턴스를 만들어서 공유 에셋(다른 오브젝트)에 영향 없음 — 정리 위해 캐시.
            foreach (Material mat in r.materials)
            {
                mat.EnableKeyword("LOD_FADE_CROSSFADE");
                instances.Add(mat);
            }
        }
        materialInstances = instances.ToArray();
    }

    private void OnDisable()
    {
        // 비활성화 시 완전 표시 상태로 복원 — 다음 활성화까지 클립된 채로 멈춰있지 않도록.
        ApplyLodFade(1f);
    }

    private void OnDestroy()
    {
        if (materialInstances == null) return;
        foreach (Material mat in materialInstances)
            if (mat != null) Destroy(mat);
    }

    private void Update()
    {
        if (Camera.main == null) return;

        Transform reference = player != null && player.CameraFollowTarget != null
            ? player.CameraFollowTarget.FollowPoint
            : transform;
        float distance = Vector3.Distance(Camera.main.transform.position, reference.position);
        float targetAlpha = Mathf.InverseLerp(fadeEndDistance, fadeStartDistance, distance);
        currentAlpha = Mathf.SmoothDamp(currentAlpha, targetAlpha, ref alphaVelocity, fadeSmoothTime);

        if (Mathf.Abs(currentAlpha - appliedAlpha) > AlphaChangeEpsilon)
            ApplyLodFade(currentAlpha);
    }

    private void ApplyLodFade(float alpha)
    {
        appliedAlpha = alpha;
        for (int i = 0; i < renderers.Length; i++)
        {
            // 기존에 셋팅된 다른 프로퍼티(있다면)를 지우지 않도록 GetPropertyBlock으로 시작.
            renderers[i].GetPropertyBlock(propertyBlock);
            propertyBlock.SetVector(LodFadeId, new Vector4(alpha, 0f, 0f, 0f));
            renderers[i].SetPropertyBlock(propertyBlock);
        }
    }
}
