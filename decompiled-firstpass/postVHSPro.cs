using UnityEngine;

[ExecuteInEditMode]
[RequireComponent(typeof(Camera))]
[AddComponentMenu("Image Effects/VHS Pro")]
public class postVHSPro : MonoBehaviour
{
	private float time_;

	public Shader shader1;

	public Shader shader2;

	public Shader shader3;

	public Shader shader4;

	public Shader shader_clear;

	public Shader shader_tape;

	public Material mat1;

	public Material mat2;

	public Material mat3;

	public Material mat4;

	public Material mat_clear;

	public Material mat_tape;

	private RenderTexture texPass12;

	private RenderTexture texPass23;

	private RenderTexture texLast;

	private RenderTexture texFeedback;

	private RenderTexture texFeedback2;

	private RenderTexture texClear;

	private RenderTexture texTape;

	public bool g_showCRT = true;

	public bool g_showNoise = true;

	public bool g_showJitter = true;

	public bool g_showSignal = true;

	public bool g_showFeedback = true;

	public bool g_showExtra;

	public bool g_showBypass;

	public bool bleedOn = true;

	public int crtMode;

	public int crtLinesMode;

	public float screenLinesNum = 240f;

	public float bleedAmount = 1f;

	public bool bleedDebugOn;

	public AnimationCurve bleedCurveY = AnimationCurve.Linear(0f, 1f, 1f, 0f);

	public AnimationCurve bleedCurveI = AnimationCurve.Linear(0f, 0.5f, 1f, 0f);

	public AnimationCurve bleedCurveQ = AnimationCurve.Linear(0f, 0.5f, 1f, 0f);

	public int bleedLength = 21;

	public bool bleedCurveEditModeOn;

	public bool bleedCurveIQSyncOn = true;

	private int max_curve_length = 50;

	private Texture2D texCurves;

	private Vector4 curvesOffest = new Vector4(0f, 0f, 0f, 0f);

	private float[,] curvesData = new float[50, 3];

	public bool fisheyeOn = true;

	public float fisheyeBend = 2f;

	public int fisheyeType;

	public float fisheyeSize = 1.2f;

	public float cutoffX = 2f;

	public float cutoffY = 3f;

	public float cutoffFadeX = 25f;

	public float cutoffFadeY = 25f;

	public bool vignetteOn;

	public float vignetteAmount = 1f;

	public float vignetteSpeed = 1f;

	public int noiseLinesMode = 1;

	public float noiseLinesNum = 240f;

	public float noiseQuantizeX;

	public bool filmgrainOn;

	public float filmGrainAmount = 0.016f;

	public bool signalNoiseOn = true;

	public float signalNoiseAmount = 0.15f;

	public float signalNoisePower = 0.83f;

	public bool tapeNoiseOn = true;

	public float tapeNoiseTH = 0.63f;

	public float tapeNoiseAmount = 1f;

	public float tapeNoiseSpeed = 1f;

	public bool lineNoiseOn = true;

	public float lineNoiseAmount = 1f;

	public float lineNoiseSpeed = 5f;

	public bool scanLinesOn;

	public float scanLineWidth = 10f;

	public bool linesFloatOn;

	public float linesFloatSpeed = 1f;

	public bool stretchOn = true;

	public bool twitchHOn;

	public float twitchHFreq = 1f;

	public bool twitchVOn;

	public float twitchVFreq = 1f;

	public bool jitterHOn = true;

	public float jitterHAmount = 0.5f;

	public bool jitterVOn;

	public float jitterVAmount = 1f;

	public float jitterVSpeed = 1f;

	public bool signalTweakOn;

	public float signalAdjustY;

	public float signalAdjustI;

	public float signalAdjustQ;

	public float signalShiftY = 1f;

	public float signalShiftI = 1f;

	public float signalShiftQ = 1f;

	public float gammaCorection = 1f;

	public bool feedbackOn;

	public int feedbackMode;

	public float feedbackThresh = 0.1f;

	public float feedbackAmount = 2f;

	public float feedbackFade = 0.82f;

	public Color feedbackColor = new Color(1f, 0.5f, 0f);

	public bool feedbackDebugOn;

	public bool independentTimeOn;

	public Texture bypassTex;

	public Sprite spriteTex;

	private void CreateMaterials()
	{
		mat1 = new Material(shader1);
		mat2 = new Material(shader2);
		mat3 = new Material(shader3);
		mat4 = new Material(shader4);
		mat_clear = new Material(shader_clear);
		mat_tape = new Material(shader_tape);
		if (crtMode == 3)
		{
			buildCurves();
		}
	}

	private void Awake()
	{
		if (crtMode == 3)
		{
			buildCurves();
		}
	}

	private void Update()
	{
	}

	private void CreateTextures(RenderTexture src)
	{
		Object.DestroyImmediate(texClear);
		texClear = new RenderTexture(src.width, src.height, src.depth);
		texClear.filterMode = FilterMode.Point;
		texClear.Create();
		Object.DestroyImmediate(texPass12);
		texPass12 = new RenderTexture(src.width, src.height, src.depth);
		texPass12.filterMode = FilterMode.Point;
		texPass12.Create();
		Object.DestroyImmediate(texPass23);
		texPass23 = new RenderTexture(src.width, src.height, src.depth);
		texPass23.filterMode = FilterMode.Point;
		texPass23.Create();
		Object.DestroyImmediate(texFeedback);
		texFeedback = new RenderTexture(src.width, src.height, 0);
		texFeedback.hideFlags = HideFlags.HideAndDontSave;
		texFeedback.filterMode = FilterMode.Point;
		texFeedback.Create();
		Object.DestroyImmediate(texFeedback2);
		texFeedback2 = new RenderTexture(src.width, src.height, 0);
		texFeedback2.hideFlags = HideFlags.HideAndDontSave;
		texFeedback2.filterMode = FilterMode.Point;
		texFeedback2.Create();
		Object.DestroyImmediate(texLast);
		texLast = new RenderTexture(src.width, src.height, 0);
		texLast.hideFlags = HideFlags.HideAndDontSave;
		texLast.filterMode = FilterMode.Point;
		texLast.Create();
		Graphics.Blit(texClear, texFeedback, mat_clear);
		Graphics.Blit(texClear, texFeedback2, mat_clear);
		Graphics.Blit(texClear, texLast, mat_clear);
	}

	private void OnRenderImage(RenderTexture src, RenderTexture dest)
	{
		if (mat1 == null)
		{
			CreateMaterials();
		}
		if (texPass12 == null || src.width != texPass12.width || src.height != texPass12.height)
		{
			CreateTextures(src);
		}
		float num = screenLinesNum;
		if (num <= 0f)
		{
			num = src.height;
		}
		if ((tapeNoiseOn || filmgrainOn || lineNoiseOn) && (texTape == null || (float)texTape.height != Mathf.Min(noiseLinesNum, num)))
		{
			int num2 = (int)Mathf.Min(noiseLinesNum, num);
			int width = (int)((float)num2 * (float)src.width / (float)src.height);
			Object.DestroyImmediate(texTape);
			texTape = new RenderTexture(width, num2, 0);
			texTape.hideFlags = HideFlags.HideAndDontSave;
			texTape.filterMode = FilterMode.Point;
			texTape.Create();
			Graphics.Blit(texClear, texTape, mat_tape);
		}
		if (independentTimeOn)
		{
			time_ = Time.unscaledTime;
		}
		else
		{
			time_ = Time.time;
		}
		mat1.SetFloat("time_", time_);
		mat1.SetFloat("screenLinesNum", num);
		mat1.SetFloat("noiseLinesNum", noiseLinesNum);
		mat1.SetFloat("noiseQuantizeX", noiseQuantizeX);
		FeatureToggle(mat1, filmgrainOn, "VHS_FILMGRAIN_ON");
		FeatureToggle(mat1, tapeNoiseOn, "VHS_TAPENOISE_ON");
		FeatureToggle(mat1, lineNoiseOn, "VHS_LINENOISE_ON");
		FeatureToggle(mat1, jitterHOn, "VHS_JITTER_H_ON");
		mat1.SetFloat("jitterHAmount", jitterHAmount);
		FeatureToggle(mat1, jitterVOn, "VHS_JITTER_V_ON");
		mat1.SetFloat("jitterVAmount", jitterVAmount);
		mat1.SetFloat("jitterVSpeed", jitterVSpeed);
		FeatureToggle(mat1, linesFloatOn, "VHS_LINESFLOAT_ON");
		mat1.SetFloat("linesFloatSpeed", linesFloatSpeed);
		FeatureToggle(mat1, twitchHOn, "VHS_TWITCH_H_ON");
		mat1.SetFloat("twitchHFreq", twitchHFreq);
		FeatureToggle(mat1, twitchVOn, "VHS_TWITCH_V_ON");
		mat1.SetFloat("twitchVFreq", twitchVFreq);
		FeatureToggle(mat1, scanLinesOn, "VHS_SCANLINES_ON");
		mat1.SetFloat("scanLineWidth", scanLineWidth);
		FeatureToggle(mat1, signalNoiseOn, "VHS_YIQNOISE_ON");
		mat1.SetFloat("signalNoisePower", signalNoisePower);
		mat1.SetFloat("signalNoiseAmount", signalNoiseAmount);
		FeatureToggle(mat1, stretchOn, "VHS_STRETCH_ON");
		FeatureToggle(mat1, fisheyeOn, "VHS_FISHEYE_ON");
		mat1.SetFloat("cutoffX", cutoffX);
		mat1.SetFloat("cutoffY", cutoffY);
		mat1.SetFloat("cutoffFadeX", cutoffFadeX);
		mat1.SetFloat("cutoffFadeY", cutoffFadeY);
		mat2.SetFloat("time_", time_);
		mat2.SetFloat("screenLinesNum", num);
		FeatureToggle(mat2, bleedOn, "VHS_BLEED_ON");
		mat2.DisableKeyword("VHS_OLD_THREE_PHASE");
		mat2.DisableKeyword("VHS_THREE_PHASE");
		mat2.DisableKeyword("VHS_TWO_PHASE");
		if (crtMode == 0)
		{
			mat2.EnableKeyword("VHS_OLD_THREE_PHASE");
		}
		else if (crtMode == 1)
		{
			mat2.EnableKeyword("VHS_THREE_PHASE");
		}
		else if (crtMode == 2)
		{
			mat2.EnableKeyword("VHS_TWO_PHASE");
		}
		else if (crtMode == 3 && bleedCurveEditModeOn)
		{
			buildCurves();
		}
		mat2.SetTexture("_CurvesTex", texCurves);
		mat2.SetVector("curvesOffest", curvesOffest);
		mat2.SetInt("bleedLength", bleedLength);
		FeatureToggle(mat2, crtMode == 3, "VHS_CUSTOM_BLEED_ON");
		FeatureToggle(mat2, bleedDebugOn, "VHS_DEBUG_BLEEDING_ON");
		mat2.SetFloat("bleedAmount", bleedAmount);
		FeatureToggle(mat2, fisheyeOn, "VHS_FISHEYE_ON");
		FeatureToggle(mat2, fisheyeType == 1, "VHS_FISHEYE_HYPERSPACE");
		mat2.SetFloat("fisheyeBend", fisheyeBend);
		mat2.SetFloat("fisheyeSize", fisheyeSize);
		FeatureToggle(mat2, vignetteOn, "VHS_VIGNETTE_ON");
		mat2.SetFloat("vignetteAmount", vignetteAmount);
		mat2.SetFloat("vignetteSpeed", vignetteSpeed);
		FeatureToggle(mat2, signalTweakOn, "VHS_SIGNAL_TWEAK_ON");
		mat2.SetFloat("signalAdjustY", signalAdjustY);
		mat2.SetFloat("signalAdjustI", signalAdjustI);
		mat2.SetFloat("signalAdjustQ", signalAdjustQ);
		mat2.SetFloat("signalShiftY", signalShiftY);
		mat2.SetFloat("signalShiftI", signalShiftI);
		mat2.SetFloat("signalShiftQ", signalShiftQ);
		mat2.SetFloat("gammaCorection", gammaCorection);
		if (tapeNoiseOn || filmgrainOn || lineNoiseOn)
		{
			mat_tape.SetFloat("time_", time_);
			FeatureToggle(mat_tape, filmgrainOn, "VHS_FILMGRAIN_ON");
			mat_tape.SetFloat("filmGrainAmount", filmGrainAmount);
			FeatureToggle(mat_tape, tapeNoiseOn, "VHS_TAPENOISE_ON");
			mat_tape.SetFloat("tapeNoiseTH", tapeNoiseTH);
			mat_tape.SetFloat("tapeNoiseAmount", tapeNoiseAmount);
			mat_tape.SetFloat("tapeNoiseSpeed", tapeNoiseSpeed);
			FeatureToggle(mat_tape, lineNoiseOn, "VHS_LINENOISE_ON");
			mat_tape.SetFloat("lineNoiseAmount", lineNoiseAmount);
			mat_tape.SetFloat("lineNoiseSpeed", lineNoiseSpeed);
			Graphics.Blit(texTape, texTape, mat_tape);
			mat1.SetTexture("_TapeTex", texTape);
			mat1.SetFloat("tapeNoiseAmount", tapeNoiseAmount);
		}
		if (spriteTex != null)
		{
			bypassTex = spriteTex.texture;
		}
		if (bypassTex != null)
		{
			Graphics.Blit(bypassTex, texPass12, mat1);
		}
		else
		{
			Graphics.Blit(src, texPass12, mat1);
		}
		if (!feedbackOn)
		{
			Graphics.Blit(texPass12, dest, mat2);
			return;
		}
		Graphics.Blit(texPass12, texPass23, mat2);
		mat3.SetTexture("_LastTex", texLast);
		mat3.SetTexture("_FeedbackTex", texFeedback);
		mat3.SetFloat("feedbackThresh", feedbackThresh);
		mat3.SetFloat("feedbackAmount", feedbackAmount);
		mat3.SetFloat("feedbackFade", feedbackFade);
		mat3.SetColor("feedbackColor", feedbackColor);
		Graphics.Blit(texPass23, texFeedback2, mat3);
		Graphics.Blit(texFeedback2, texFeedback);
		mat4.SetFloat("feedbackAmp", 1f);
		mat4.SetTexture("_FeedbackTex", texFeedback);
		Graphics.Blit(texPass23, texLast, mat4);
		if (!feedbackDebugOn)
		{
			Graphics.Blit(texLast, dest);
		}
		else
		{
			Graphics.Blit(texFeedback, dest);
		}
	}

	private void FeatureToggle(Material mat, bool propVal, string featureName)
	{
		if (propVal)
		{
			mat.EnableKeyword(featureName);
		}
		else
		{
			mat.DisableKeyword(featureName);
		}
	}

	private void buildCurves()
	{
		if (texCurves == null)
		{
			texCurves = new Texture2D(max_curve_length, 1, TextureFormat.RGBA32, mipChain: false);
		}
		curvesOffest[0] = 0f;
		curvesOffest[1] = 0f;
		curvesOffest[2] = 0f;
		float num = 0f;
		for (int i = 0; i < bleedLength; i++)
		{
			num = (float)i / (float)bleedLength;
			curvesData[i, 0] = bleedCurveY.Evaluate(num);
			curvesData[i, 1] = bleedCurveI.Evaluate(num);
			curvesData[i, 2] = bleedCurveQ.Evaluate(num);
			if (bleedCurveIQSyncOn)
			{
				curvesData[i, 2] = curvesData[i, 1];
			}
			if (curvesOffest[0] > curvesData[i, 0])
			{
				curvesOffest[0] = curvesData[i, 0];
			}
			if (curvesOffest[1] > curvesData[i, 1])
			{
				curvesOffest[1] = curvesData[i, 1];
			}
			if (curvesOffest[2] > curvesData[i, 2])
			{
				curvesOffest[2] = curvesData[i, 2];
			}
		}
		curvesOffest[0] = Mathf.Abs(curvesOffest[0]);
		curvesOffest[1] = Mathf.Abs(curvesOffest[1]);
		curvesOffest[2] = Mathf.Abs(curvesOffest[2]);
		for (int j = 0; j < bleedLength; j++)
		{
			curvesData[j, 0] += curvesOffest[0];
			curvesData[j, 1] += curvesOffest[1];
			curvesData[j, 2] += curvesOffest[2];
			texCurves.SetPixel(-2 + bleedLength - j, 0, new Color(curvesData[j, 0], curvesData[j, 1], curvesData[j, 2]));
		}
		texCurves.Apply();
	}
}
