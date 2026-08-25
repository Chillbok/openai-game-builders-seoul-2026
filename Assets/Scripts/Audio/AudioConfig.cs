using UnityEngine;

/// <summary>
/// 게임 전역 오디오 설정 — 단일 진실 원천.
/// 에셋들(사운드).md:121 채택 현황 22파일을 한 곳에서 관리한다.
/// </summary>
[CreateAssetMenu(fileName = "DefaultAudioConfig", menuName = "Scriptable Objects/AudioConfig")]
public sealed class AudioConfig : ScriptableObject
{
    [Header("BGM — 단일 AudioSource 크로스페이드")]
    [Tooltip("시작 메뉴 BGM — 루프 재생 (TownTheme.mp3)")]
    [SerializeField]
    private AudioClip menuBgm;

    [Tooltip("게임플레이 BGM — 루프 재생 (battleThemeA.mp3)")]
    [SerializeField]
    private AudioClip battleBgm;

    [Header("SFX - 플레이어 공격")]
    [Tooltip("3연격 타별 휘두르기음 — Sword Attack 1~3.wav")]
    [SerializeField]
    private AudioClip[] attackSwingClips = new AudioClip[3];

    [Tooltip("강화 공격(완벽 회피 2배/패링 3배) 휘두르기음 — Sword Impact Hit 1~3.wav")]
    [SerializeField]
    private AudioClip[] empoweredAttackClips = new AudioClip[3];

    [Header("SFX - 처형")]
    [Tooltip("처형 돌진음 — Stone Land.wav (회피음 재사용 가능)")]
    [SerializeField]
    private AudioClip executionDashClip;

    [Tooltip("처형 접촉 둔탁음")]
    [SerializeField]
    private AudioClip executionImpactClip;

    [Tooltip("처형 준비 동작음 — Sword Parry 1.wav (BeginPresentation 0초 시점)")]
    [SerializeField]
    private AudioClip executionParryClip;

    [Tooltip("처형 검격음 — machete_stab_flesh_attack_04.wav (TryCompleteExecution + MoveThroughTarget 0.26초 시점)")]
    [SerializeField]
    private AudioClip executionStabClip;

    [Header("SFX - 회피/피격/사망/회복")]
    [Tooltip("회피 사운드 — jump_square.wav")]
    [SerializeField]
    private AudioClip dodgeClip;

    [Tooltip("완벽한 회피 성공음 — coin_2.wav (회피음과 함께 출력)")]
    [SerializeField]
    private AudioClip perfectDodgeClip;

    [Tooltip("플레이어 피격음 — Spell Impact 1.wav (사망과 구분, 무적 0.1초 동안 미재생)")]
    [SerializeField]
    private AudioClip hurtClip;

    [Header("SFX - 영혼 충전")]
    [Tooltip("영혼 충전 단계 상승음 — power_up.wav")]
    [SerializeField]
    private AudioClip soulChargeStageUpClip;

    [Tooltip("영혼 충전 단계 하락음 — power_up.wav 피치 다운 또는 동일")]
    [SerializeField]
    private AudioClip soulChargeStageDownClip;

    [Tooltip("영혼 충전 4단계 광역 폭발음 — Fireball 3.wav")]
    [SerializeField]
    private AudioClip soulExplosionClip;

    [Tooltip("영혼 충전 폭발음 쿨다운(초) — 동시 다발 폭발 시 겹침 방지")]
    [SerializeField, Min(0f)]
    private float soulExplosionCooldown = 0.15f;

    [Tooltip("일반 처치 누적 팝업 틱 — 1/4마다 재생, 최대 단계 미재생")]
    [SerializeField]
    private AudioClip progressTickClip;

    [Tooltip("저체력 경고음 — 권장")]
    [SerializeField]
    private AudioClip lowHpClip;

    [Header("SFX - 적")]
    [Tooltip("적 공격음 — chop 4.wav")]
    [SerializeField]
    private AudioClip enemyAttackClip;

    [Tooltip("적 피격음 — hurt.wav")]
    [SerializeField]
    private AudioClip enemyHurtClip;

    [Tooltip("적 기절 진입음 — fall_quick.wav (10초 기절 시작 시)")]
    [SerializeField]
    private AudioClip enemyStunClip;

    [Tooltip("적 사망음 — 일반/폭발/처형 reason별 분리 가능")]
    [SerializeField]
    private AudioClip enemyDieClip;

    [Tooltip("적 사망음 쿨다운(초) — 동시 다발 사망 시 겹침 방지")]
    [SerializeField, Min(0f)]
    private float enemyDieCooldown = 0.1f;

    [Header("SFX - 시스템/맵")]
    [Tooltip("문 개방음 — Door Open 1.wav")]
    [SerializeField]
    private AudioClip doorOpenClip;

    [Tooltip("맵 전이 효과음 — 페이드 아웃 구간")]
    [SerializeField]
    private AudioClip mapTransitClip;

    [Header("UI")]
    [Tooltip("버튼 누름음 — menu_blip.wav (기본 확인음)")]
    [SerializeField]
    private AudioClip buttonClickClip;

    [Header("볼륨 버스 — 인스펙터 노출, PlayerPrefs 저장")]
    [Tooltip("마스터 볼륨 (0~1)")]
    [SerializeField, Range(0f, 1f)]
    private float masterVolume = 1f;

    [Tooltip("BGM 볼륨 (0~1)")]
    [SerializeField, Range(0f, 1f)]
    private float bgmVolume = 0.8f;

    [Tooltip("SFX 볼륨 (0~1)")]
    [SerializeField, Range(0f, 1f)]
    private float sfxVolume = 1f;

    [Tooltip("UI 볼륨 (0~1)")]
    [SerializeField, Range(0f, 1f)]
    private float uiVolume = 1f;

    public AudioClip MenuBgm => menuBgm;
    public AudioClip BattleBgm => battleBgm;
    public AudioClip[] AttackSwingClips => attackSwingClips;
    public AudioClip[] EmpoweredAttackClips => empoweredAttackClips;
    public AudioClip ExecutionDashClip => executionDashClip;
    public AudioClip ExecutionImpactClip => executionImpactClip;
    public AudioClip ExecutionParryClip => executionParryClip;
    public AudioClip ExecutionStabClip => executionStabClip;
    public AudioClip DodgeClip => dodgeClip;
    public AudioClip PerfectDodgeClip => perfectDodgeClip;
    public AudioClip HurtClip => hurtClip;
    public AudioClip SoulChargeStageUpClip => soulChargeStageUpClip;
    public AudioClip SoulChargeStageDownClip => soulChargeStageDownClip;
    public AudioClip SoulExplosionClip => soulExplosionClip;
    public float SoulExplosionCooldown => soulExplosionCooldown;
    public AudioClip ProgressTickClip => progressTickClip;
    public AudioClip LowHpClip => lowHpClip;
    public AudioClip EnemyAttackClip => enemyAttackClip;
    public AudioClip EnemyHurtClip => enemyHurtClip;
    public AudioClip EnemyStunClip => enemyStunClip;
    public AudioClip EnemyDieClip => enemyDieClip;
    public float EnemyDieCooldown => enemyDieCooldown;
    public AudioClip DoorOpenClip => doorOpenClip;
    public AudioClip MapTransitClip => mapTransitClip;
    public AudioClip ButtonClickClip => buttonClickClip;
    public float MasterVolume => masterVolume;
    public float BgmVolume => bgmVolume;
    public float SfxVolume => sfxVolume;
    public float UiVolume => uiVolume;

    /// <summary>
    /// 타수(1~3)에 해당하는 휘두르기 클립을 반환한다. 강화 여부에 따라 배열을 교체한다.
    /// </summary>
    public AudioClip GetSwingClip(int attackCount, bool isEmpowered)
    {
        AudioClip[] source = isEmpowered ? empoweredAttackClips : attackSwingClips;
        if (source == null || source.Length == 0) return null;
        int index = Mathf.Clamp(attackCount - 1, 0, source.Length - 1);
        return source[index];
    }
}
