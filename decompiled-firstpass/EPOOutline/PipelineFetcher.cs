using UnityEngine;
using UnityEngine.Rendering;

namespace EPOOutline;

public static class PipelineFetcher
{
	public static RenderPipelineAsset CurrentAsset
	{
		get
		{
			RenderPipelineAsset renderPipelineAsset = QualitySettings.renderPipeline;
			if (renderPipelineAsset == null)
			{
				renderPipelineAsset = GraphicsSettings.renderPipelineAsset;
			}
			return renderPipelineAsset;
		}
	}
}
