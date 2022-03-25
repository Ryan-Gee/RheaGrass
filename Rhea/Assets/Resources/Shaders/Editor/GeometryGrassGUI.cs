using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEditor;

public class GeometryGrassGUI : ShaderGUI
{
	public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
	{
		//base.OnGUI(materialEditor, properties);

		MaterialProperty _BaseMap = ShaderGUI.FindProperty("_BaseMap", properties);
		materialEditor.ShaderProperty(_BaseMap, _BaseMap.displayName);

		Material targetMat = materialEditor.target as Material;

		MaterialProperty _BaseColor = ShaderGUI.FindProperty("_BaseColor", properties);
		MaterialProperty _FadeColor = ShaderGUI.FindProperty("_FadeColor", properties);
		EditorGUILayout.BeginHorizontal();
		materialEditor.ShaderProperty(_BaseColor, "Blade Coloring");
		EditorGUIUtility.labelWidth = 1;
		materialEditor.ShaderProperty(_FadeColor, "");
		EditorGUIUtility.labelWidth = 0;

		EditorGUILayout.EndHorizontal();

		MaterialProperty _LumaVariation = ShaderGUI.FindProperty("_LumaVariation", properties);
		materialEditor.ShaderProperty(_LumaVariation, "Luma Jitter");

		MaterialProperty _HeightLuma = ShaderGUI.FindProperty("_HeightLuma", properties);
		materialEditor.ShaderProperty(_HeightLuma, "Height Brightness");

		MaterialProperty _HueVariation = ShaderGUI.FindProperty("_HueVariation", properties);
		materialEditor.ShaderProperty(_HueVariation, "Hue Jitter");
		EditorGUILayout.Space();

		DoAtlasGUI(materialEditor, properties);

		DoBladeSizeGUI(targetMat, properties);

		DoBladeWarpingGUI(materialEditor, properties);

		DoTessellationGUI(materialEditor, properties);

		DoTrimmingGUI(materialEditor, properties);

		DoWindGUI(materialEditor, properties);

		DoDisplacementGUI(materialEditor, properties);
	}

	protected void DoAtlasGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
	{
		GUILayout.Label("Atlasing", EditorStyles.boldLabel);
		EditorGUI.indentLevel++;

		MaterialProperty _AtlasLength = ShaderGUI.FindProperty("_AtlasLength", properties);
		int aLength = EditorGUILayout.IntSlider("Atlas Length", (int)Mathf.Floor(_AtlasLength.floatValue), 1, 6);
		if (EditorGUI.EndChangeCheck())
		{
			_AtlasLength.floatValue = aLength;
		}

		MaterialProperty _AtlasBias = ShaderGUI.FindProperty("_AtlasBias", properties);
		materialEditor.ShaderProperty(_AtlasBias, "Bias");

		MaterialProperty _AtlasHeightBias = ShaderGUI.FindProperty("_AtlasHeightBias", properties);
		materialEditor.ShaderProperty(_AtlasHeightBias, "Height Bias");

		EditorGUI.indentLevel--;
		EditorGUILayout.Space();
	}

	protected void DoBladeSizeGUI(Material targetMat, MaterialProperty[] properties)
	{
		GUILayout.Label("Blade Size", EditorStyles.boldLabel);
		EditorGUI.indentLevel++;

		//Blade width setting
		MaterialProperty _BladeWidthMin = ShaderGUI.FindProperty("_BladeWidthMin", properties);
		MaterialProperty _BladeWidthMax = ShaderGUI.FindProperty("_BladeWidthMax", properties);
		float widthMin = _BladeWidthMin.floatValue;
		float widthMax = _BladeWidthMax.floatValue;
		EditorGUILayout.MinMaxSlider("Width", ref widthMin, ref widthMax, 0.0f, 0.3f);
		if (EditorGUI.EndChangeCheck())
		{
			_BladeWidthMin.floatValue = widthMin;
			_BladeWidthMax.floatValue = widthMax;
		}

		//Blade height setting
		MaterialProperty _BladeHeightMin = ShaderGUI.FindProperty("_BladeHeightMin", properties);
		MaterialProperty _BladeHeightMax = ShaderGUI.FindProperty("_BladeHeightMax", properties);
		float heightMin = _BladeHeightMin.floatValue;
		float heightMax = _BladeHeightMax.floatValue;
		EditorGUILayout.MinMaxSlider("Height", ref heightMin, ref heightMax, 0.1f, 1);
		if (EditorGUI.EndChangeCheck())
		{
			_BladeHeightMin.floatValue = heightMin;
			_BladeHeightMax.floatValue = heightMax;
		}

		//Tapered blade setting
		bool tapered = Array.IndexOf(targetMat.shaderKeywords, "TAPERED_BLADES") != -1;
		EditorGUI.BeginChangeCheck();
		tapered = EditorGUILayout.Toggle("Tapered Blades", tapered);
		if (EditorGUI.EndChangeCheck())
		{
			// enable or disable the keyword based on checkbox
			if (tapered)
				targetMat.EnableKeyword("TAPERED_BLADES");
			else
				targetMat.DisableKeyword("TAPERED_BLADES");
		}

		EditorGUI.indentLevel--;
		EditorGUILayout.Space();
	}

	protected void DoBladeWarpingGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
	{
		GUILayout.Label("Blade Warping", EditorStyles.boldLabel);
		EditorGUI.indentLevel++;

		MaterialProperty _BladeBendDistance = ShaderGUI.FindProperty("_BladeBendDistance", properties);
		materialEditor.ShaderProperty(_BladeBendDistance, "Bending");

		MaterialProperty _BladeBendCurve = ShaderGUI.FindProperty("_BladeBendCurve", properties);
		materialEditor.ShaderProperty(_BladeBendCurve, "Curvature");

		MaterialProperty _BendDelta = ShaderGUI.FindProperty("_BendDelta", properties);
		materialEditor.ShaderProperty(_BendDelta, "Variation");

		MaterialProperty _BladeJitter = ShaderGUI.FindProperty("_BladeJitter", properties);
		materialEditor.ShaderProperty(_BladeJitter, "Position Jitter");

		EditorGUI.indentLevel--;
		EditorGUILayout.Space();
	}

	protected void DoTessellationGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
	{
		GUILayout.Label("Tessellation", EditorStyles.boldLabel);
		EditorGUI.indentLevel++;

		MaterialProperty _TessellationGrassDist = ShaderGUI.FindProperty("_TessellationGrassDist", properties);
		materialEditor.ShaderProperty(_TessellationGrassDist, "Density");

		MaterialProperty _TessellationViewDistance = ShaderGUI.FindProperty("_TessellationViewDistance", properties);
		materialEditor.ShaderProperty(_TessellationViewDistance, "View Distance");

		EditorGUI.indentLevel--;
		EditorGUILayout.Space();
	}

	protected void DoTrimmingGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
	{
		GUILayout.Label("Trimming", EditorStyles.boldLabel);
		EditorGUI.indentLevel++;

		MaterialProperty _GrassMap = ShaderGUI.FindProperty("_GrassMap", properties);
		materialEditor.ShaderProperty(_GrassMap, _GrassMap.displayName);

		MaterialProperty _GrassThreshold = ShaderGUI.FindProperty("_GrassThreshold", properties);
		materialEditor.ShaderProperty(_GrassThreshold, "Threshold");

		MaterialProperty _GrassFalloff = ShaderGUI.FindProperty("_GrassFalloff", properties);
		materialEditor.ShaderProperty(_GrassFalloff, "Falloff");

		EditorGUI.indentLevel--;
		EditorGUILayout.Space();
	}

	protected void DoWindGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
	{
		GUILayout.Label("Wind", EditorStyles.boldLabel);
		EditorGUI.indentLevel++;

		MaterialProperty _WindMap = ShaderGUI.FindProperty("_WindMap", properties);
		materialEditor.ShaderProperty(_WindMap, "Wind Vector Map");

		MaterialProperty _WindVelocity = ShaderGUI.FindProperty("_WindVelocity", properties);
		materialEditor.ShaderProperty(_WindVelocity, "Velocity Vector");

		MaterialProperty _WindFrequency = ShaderGUI.FindProperty("_WindFrequency", properties);
		materialEditor.ShaderProperty(_WindFrequency, "Speed");

		EditorGUI.indentLevel--;
		EditorGUILayout.Space();
	}

	protected void DoDisplacementGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
	{
		GUILayout.Label("Object Displacement", EditorStyles.boldLabel);
		EditorGUI.indentLevel++;

		MaterialProperty _DisplacementRadius = ShaderGUI.FindProperty("_DisplacementRadius", properties);
		materialEditor.ShaderProperty(_DisplacementRadius, "Radius");

		MaterialProperty _DisplacementStrength = ShaderGUI.FindProperty("_DisplacementStrength", properties);
		materialEditor.ShaderProperty(_DisplacementStrength, "Strength");

		MaterialProperty _DisplacementFalloff = ShaderGUI.FindProperty("_DisplacementFalloff", properties);
		materialEditor.ShaderProperty(_DisplacementFalloff, "Falloff");

		EditorGUI.indentLevel--;
		EditorGUILayout.Space();
	}
}
