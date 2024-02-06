using UnityEngine;
using VitalRouter;

namespace MyNamespace;


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

