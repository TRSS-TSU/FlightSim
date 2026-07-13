using UnityEngine;

public sealed class HamburgerMenuController : MonoBehaviour
{
    private static readonly int OpenState = Animator.StringToHash("OpenHamMenu");
    private static readonly int CloseState = Animator.StringToHash("CloseHamMenu");

    [SerializeField] private Animator animator;
    [SerializeField] private DeveloperTimeScalePanel timeScalePanel;

    private bool isOpen;

    private void Awake()
    {
        animator.updateMode = AnimatorUpdateMode.UnscaledTime;
        animator.Play(CloseState, 0, 1f);
        animator.Update(0f);
    }

    public void Toggle()
    {
        isOpen = !isOpen;
        animator.Play(isOpen ? OpenState : CloseState, 0, 0f);
    }

    public void Close()
    {
        isOpen = false;
        animator.Play(CloseState, 0, 0f);
    }

    public void SetSpeed(float value)
    {
        timeScalePanel.SetTimeScale(value);
        Close();
    }

    public void ExitToMenu() => FlightSession.Instance.ReturnToMenu();
}
