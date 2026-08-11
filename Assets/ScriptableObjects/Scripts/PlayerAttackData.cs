using System;
using System.Collections.Generic;
using UnityEngine;


[Serializable]
public class PlayerAttackData
{
    [field: SerializeField] public List<AttackInfo> AttackDatas { get; private set; }
    [field: SerializeField] public AttackInfo DodgeAttackInfo { get; private set; }    // 회피공격 전용 데이터(콤보 배열과 별개, 독립 튜닝)
    [field: SerializeField] public List<AttackInfo> AirAttackDatas { get; private set; }    // 공중 콤보 전용 데이터(지상과 별도 3단 체인)

    public int GetAttackInfoCount() { return AttackDatas.Count; }
    public AttackInfo GetAttackInfo(int index)
    {
        return AttackDatas[index];
    }

    public AttackInfo GetAirAttackInfo(int index)
    {
        return AirAttackDatas[index];
    }

}

[Serializable]
public class AttackInfo
{
    [field: SerializeField] public string AttackName { get; private set; }  // 공격명
    [field: SerializeField] public int ComboStateIndex { get; private set; }    // 현재타수
    [field: SerializeField] [field: Range(0f, 1f)] public float ComboTransitionTime { get; private set; }   // 입력유효시간
    [field: SerializeField] [field: Range(0f, 3f)] public float ForceTransitionTime { get; private set; }   // 전진시간
    [field: SerializeField] [field: Range(-10f, 10f)] public float Force { get; private set; }  // 전진거리

    [field: SerializeField] public int Damage { get; private set; }
}