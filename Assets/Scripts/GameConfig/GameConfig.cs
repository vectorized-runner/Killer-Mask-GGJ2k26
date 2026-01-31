using UnityEngine;

[CreateAssetMenu]
public class GameConfig : ScriptableObject
{
    public GameObject[] Masks;

    public static GameConfig Instance { get; private set; }
    
    private void OnEnable()
    {
        Instance = this;
    }
}
