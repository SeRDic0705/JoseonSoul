using UnityEngine;

public class PlayerInput : MonoBehaviour
{
    public InputActions inputActions { get; private set; }
    public InputActions.PlayerActions playerActions { get; private set; }

    private void Awake()
    {
        inputActions = new InputActions();

        playerActions = inputActions.Player;
    }

    private void OnEnable()
    {
        inputActions.Enable();
    }

    private void OnDisable()
    {
        inputActions.Disable();
    }
}
