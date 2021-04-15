    #if OPENGL
    #define SV_POSITION POSITION
    #define VS_SHADERMODEL vs_3_0
    #define PS_SHADERMODEL ps_3_0
#else
    #define VS_SHADERMODEL vs_4_0     //_level_9_1
    #define PS_SHADERMODEL ps_4_0     //_level_9_1
#endif

// virtual soure left right and top bottom.
float4 sourceUvRect = float4(0.0f, 0.0f, 1.0f, 1.0f);

sampler2D AlphaStencilTextureSampler = sampler_state
{
    //magfilter = LINEAR; //minfilter = LINEAR; //mipfilter = LINEAR; //AddressU = mirror; //AddressV = mirror; 
    AddressU = clamp;
    AddressV = clamp;
    Texture = <SpriteTexture>;
};

matrix World;
matrix View;
matrix Projection;

float alpha = 1;
float2 TextureOffset;
sampler s0;
//_______________________________________________________________
// techniques 
// Quad Draw  Position Color Texture
//_______________________________________________________________
struct VsInputQuad
{
    float4 Position : POSITION0;
    float4 Color : COLOR0;
    float2 TexureCoordinateA : TEXCOORD0;
};
struct VsOutputQuad
{
    float4 Position : SV_Position;
    float4 Color : COLOR0;
    float2 TexureCoordinateA : TEXCOORD0;
};
// ____________________________
VsOutputQuad VertexShaderQuadDraw(VsInputQuad input)
{
    VsOutputQuad output;
    float4x4 wvp = mul(World, mul(View, Projection));
    output.Position = mul(input.Position, wvp); // Transform by WorldViewProjection
    output.Color = input.Color;
    output.TexureCoordinateA = input.TexureCoordinateA;
    return output;
}
float4 PixelShaderQuadDraw(VsOutputQuad input) : COLOR
{
	float2 texelpos = float2(input.TexureCoordinateA.x + TextureOffset.x, input.TexureCoordinateA.y + TextureOffset.y);
    float4 col = tex2D(s0, texelpos) * input.Color;
    float2 stenTexelpos = float2((input.TexureCoordinateA.x - sourceUvRect.x) / (sourceUvRect.z - sourceUvRect.x), (input.TexureCoordinateA.y - sourceUvRect.y) / (sourceUvRect.w - sourceUvRect.y));
    float4 stenTexelcol = tex2D(AlphaStencilTextureSampler, stenTexelpos);
	float brightness = dot(float3(stenTexelcol.r, stenTexelcol.g, stenTexelcol.g), float3(0.3, 0.59, 0.11));
    return col * stenTexelcol * brightness * alpha;
}

technique QuadDraw
{
    pass
    {
        VertexShader = compile VS_SHADERMODEL VertexShaderQuadDraw();
        PixelShader = compile PS_SHADERMODEL PixelShaderQuadDraw();
    }
}