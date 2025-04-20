using UnityEngine;

public class PlayerInput : MonoBehaviour
{
    public InputActions InputActions { get; private set; }
    public InputActions.PlayerActions PlayerActions { get; private set; }

    private void Awake()
    {
        InputActions = new InputActions();

        PlayerActions = InputActions.Player;
    }

    private void OnEnable()
    {
        InputActions.Enable();
    }

    private void OnDisable()
    {
        InputActions.Disable();
    }
}
