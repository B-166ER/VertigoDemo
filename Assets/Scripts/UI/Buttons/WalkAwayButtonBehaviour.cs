using UnityEngine;
using UnityEngine.EventSystems;

// end session — save, advance zone, quit app (or stop play mode in editor)
public class WalkAwayButtonBehaviour : MonoBehaviour, IPointerClickHandler
{
	public void OnPointerClick(PointerEventData eventData)
	{
		if (GameManager.Instance == null)
			return;

		GameManager.Instance.StartNewGameLoop();
		GameManager.Instance.SaveGameState();

#if UNITY_EDITOR
		UnityEditor.EditorApplication.isPlaying = false;
#else
		Application.Quit();
#endif
	}
}
