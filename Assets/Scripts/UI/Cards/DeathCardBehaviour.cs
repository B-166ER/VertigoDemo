using UnityEngine;

// bomb / death overlay — spawn anim, hooks revive button
public class DeathCardBehaviour : MonoBehaviour
{
    const string DeathCardSpawnState = "Death Card Spawn";
    public CoinReviveButtonBehaviour ReviveButtonBeh;
	Animator animator;

    void Awake()
    {
        animator = GetComponent<Animator>();
    }

    [ContextMenu("Death Card Spawn")]
    public void PlayDeathCardSpawn()
	{
		gameObject.SetActive(true);
		if (animator == null)
        {
            Debug.LogError("No Animator", this);
            return;
        }

        gameObject.SetActive(true);
        animator.Play(DeathCardSpawnState, 0, 0f);
    }

	// animation event — card fully visible, continue reward flow
	public void onAnimationEnded()
	{
		EventRefrenceManager.Instance?.RaiseRewardCardIsVisible(InventoryManager.RewardedItem);
	}

	public void HideDeathCard()
	{
		gameObject.SetActive(false);
	}

}
