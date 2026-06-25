using UnityEngine;
using UnityEngine.Sprites;
using UnityEngine.UI;

// feeds atlas outer UVs into UI pixel-blend wheel shader (_MainOuterUV)
[DisallowMultipleComponent]
[RequireComponent(typeof(Graphic))]
public class UIImagePixelBlendOuterUV : MonoBehaviour
{
	static readonly int MainOuterUVId = Shader.PropertyToID("_MainOuterUV");

	Graphic _graphic;
	Sprite _lastSprite;
	Texture _lastMainTex;

	void Awake()
	{
		_graphic = GetComponent<Graphic>();
	}

	void OnEnable()
	{
		Apply();
	}

	// re-apply when sprite or texture changes
	void LateUpdate()
	{
		var image = _graphic as Image;
		Sprite sp = image != null ? image.sprite : null;
		Texture mt = _graphic.mainTexture;
		if (sp != _lastSprite || mt != _lastMainTex)
			Apply();
	}

	void Apply()
	{
		var image = _graphic as Image;
		_lastSprite = image != null ? image.sprite : null;
		_lastMainTex = _graphic.mainTexture;

		Material m = _graphic.materialForRendering;
		if (m == null || !m.HasProperty(MainOuterUVId))
			return;

		if (_lastSprite == null || _graphic.mainTexture == null)
		{
			m.SetVector(MainOuterUVId, new Vector4(0f, 0f, 1f, 1f));
			return;
		}

		if (_lastSprite.texture == null)
		{
			m.SetVector(MainOuterUVId, new Vector4(0f, 0f, 1f, 1f));
			return;
		}

		Vector4 outer = DataUtility.GetOuterUV(_lastSprite);
		m.SetVector(MainOuterUVId, outer);
	}
}
