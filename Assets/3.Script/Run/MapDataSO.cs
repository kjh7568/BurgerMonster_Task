using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "BurgerMonster/MapData")]
public class MapDataSO : ScriptableObject
{
    [Serializable]
    public class NodeEntry
    {
        public NodeType type;
        [Tooltip("디버그/저장용 식별자. 비워두면 인덱스로 자동 표기.")]
        public string id;
    }

    [Tooltip("선형 1줄. 첫 칸부터 순서대로 진입.")]
    public List<NodeEntry> nodes = new List<NodeEntry>();
}
