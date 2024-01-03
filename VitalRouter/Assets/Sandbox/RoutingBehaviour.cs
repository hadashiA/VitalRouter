using UnityEngine;
using VitalRouter;


[Routes]
public partial class RoutingBehaviour : MonoBehaviour
{
    void Start()
    {
    }

    public void On(CharacterMoveCommand cmd)
    {
        Debug.Log($"{GetType()} {cmd.GetType()}");
    }
}

