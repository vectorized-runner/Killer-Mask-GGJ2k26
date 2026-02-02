using UnityEngine;

[CreateAssetMenu]
public class GameConfig : ScriptableObject
{
    public GameObject[] Masks;
    public Material[] MaskMaterials;

    public static GameConfig Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = Resources.Load<GameConfig>("GameConfig");
            }

            return _instance;
        }
    }

    private static GameConfig _instance;
}
