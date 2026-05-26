using OPHIO.Core;
using UnityEngine;

public class AutoSelect : MonoBehaviour
{
    public SceneNavigator sceneNavigator;
    public string characterName;

    private void Start()
    {
        sceneNavigator.SelectCharacter(characterName);
    }
}
