using UnityEngine;

/// <summary>
/// Shared URP shader lookup with fallback chain.
/// Used by Ghost, ProtonBeam, and health bar material setup.
/// </summary>
public static class ShaderUtils
{
    /// <summary>
    /// Find a URP shader with fallback chain: URP/Lit → Simple Lit → Unlit → Sprites/Default.
    /// Logs warnings/errors as it falls through. Returns null only if every fallback is stripped.
    /// </summary>
    public static Shader FindURPShader()
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader != null) return shader;

        Debug.LogWarning("[ShaderUtils] URP/Lit not found, trying fallbacks");
        shader = Shader.Find("Universal Render Pipeline/Simple Lit");
        if (shader != null) return shader;

        shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader != null) return shader;

        shader = Shader.Find("Sprites/Default");
        if (shader != null)
        {
            Debug.LogError("[ShaderUtils] No URP shader found, using Sprites/Default");
            return shader;
        }

        Debug.LogError("[ShaderUtils] ALL shader fallbacks failed — no shaders available!");
        return null;
    }
}
