using UnityEngine;

public class FMSBoot : MonoBehaviour
{
    [SerializeField]
    Animator anim;

    [SerializeField]
    string closedStateName = "Close_FMS";
    static readonly int IsOpen = Animator.StringToHash("IsOpen");

    void Awake()
    {
        if (!anim)
            anim = GetComponent<Animator>();
        if (!anim)
            return;

        anim.enabled = true;

        // Drive closed via parameter first (works even if state names differ)
        anim.SetBool(IsOpen, false);

        // Rebind resets to controller defaults, then Update evaluates immediately
        anim.Rebind();
        anim.Update(0f);

        // Optional: only play the named close pose if it exists
        int closeHash = Animator.StringToHash(closedStateName);
        if (anim.runtimeAnimatorController && anim.HasState(0, closeHash))
        {
            anim.Play(closeHash, 0, 1f);
            anim.Update(0f);
        }
    }
}
